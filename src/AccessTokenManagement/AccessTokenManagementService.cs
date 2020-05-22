﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace IdentityModel.AspNetCore.AccessTokenManagement
{
  /// <summary>
  /// Implements basic token management logic
  /// </summary>
  public class AccessTokenManagementService : IAccessTokenManagementService
  {
    static readonly ConcurrentDictionary<string, Lazy<Task<string>>> UserRefreshDictionary =
        new ConcurrentDictionary<string, Lazy<Task<string>>>();

    static readonly ConcurrentDictionary<string, Lazy<Task<string>>> ClientTokenRequestDictionary =
        new ConcurrentDictionary<string, Lazy<Task<string>>>();

    static readonly ConcurrentDictionary<string, Lazy<Task<string>>> PasswordTokenRequestDictionary =
        new ConcurrentDictionary<string, Lazy<Task<string>>>();

    private readonly IUserTokenStore _userTokenStore;
    private readonly ISystemClock _clock;
    private readonly AccessTokenManagementOptions _options;
    private readonly ITokenEndpointService _tokenEndpointService;
    private readonly IClientAccessTokenCache _clientAccessTokenCache;
    private readonly IPasswordAccessTokenCache _passwordAccessTokenCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AccessTokenManagementService> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="userTokenStore"></param>
    /// <param name="clock"></param>
    /// <param name="options"></param>
    /// <param name="tokenEndpointService"></param>
    /// <param name="clientAccessTokenCache"></param>
    /// <param name="passwordAccessTokenCache"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    public AccessTokenManagementService(
        IUserTokenStore userTokenStore,
        ISystemClock clock,
        IOptions<AccessTokenManagementOptions> options,
        ITokenEndpointService tokenEndpointService,
        IClientAccessTokenCache clientAccessTokenCache,
        IPasswordAccessTokenCache passwordAccessTokenCache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AccessTokenManagementService> logger)
    {
      _userTokenStore = userTokenStore;
      _clock = clock;
      _options = options.Value;
      _tokenEndpointService = tokenEndpointService;
      _clientAccessTokenCache = clientAccessTokenCache;
      _passwordAccessTokenCache = passwordAccessTokenCache;
      _httpContextAccessor = httpContextAccessor;
      _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GetClientAccessTokenAsync(string clientName = AccessTokenManagementDefaults.DefaultTokenClientName, bool forceRenewal = false)
    {
      if (forceRenewal == false)
      {
        var item = await _clientAccessTokenCache.GetAsync(clientName);
        if (item != null)
        {
          return item.AccessToken;
        }
      }

      try
      {
        return await ClientTokenRequestDictionary.GetOrAdd(clientName, _ =>
        {
          return new Lazy<Task<string>>(async () =>
                  {
              var response = await _tokenEndpointService.RequestClientAccessToken(clientName);

              if (response.IsError)
              {
                _logger.LogError("Error requesting access token for client {clientName}. Error = {error}", clientName, response.Error);
                return null;
              }

              await _clientAccessTokenCache.SetAsync(clientName, response.AccessToken, response.ExpiresIn);
              return response.AccessToken;
            });
        }).Value;
      }
      finally
      {
        ClientTokenRequestDictionary.TryRemove(clientName, out _);
      }
    }

    /// <inheritdoc/>
    public Task DeleteClientAccessTokenAsync(string clientName = AccessTokenManagementDefaults.DefaultTokenClientName)
    {
      return _clientAccessTokenCache.DeleteAsync(clientName);
    }

    /// <inheritdoc/>
    public async Task<string> GetUserAccessTokenAsync(bool forceRenewal = false)
    {
      var user = _httpContextAccessor.HttpContext.User;

      if (!user.Identity.IsAuthenticated)
      {
        return null;
      }

      var userName = user.FindFirst(JwtClaimTypes.Name)?.Value ?? user.FindFirst(JwtClaimTypes.Subject)?.Value ?? "unknown";
      var userToken = await _userTokenStore.GetTokenAsync(_httpContextAccessor.HttpContext.User);

      if (userToken == null)
      {
        _logger.LogDebug("No token data found in user token store.");
        return null;
      }

      var dtRefresh = userToken.Expiration.Subtract(_options.User.RefreshBeforeExpiration);
      if (dtRefresh < _clock.UtcNow || forceRenewal == true)
      {
        _logger.LogDebug("Token for user {user} needs refreshing.", userName);

        try
        {
          return await UserRefreshDictionary.GetOrAdd(userToken.RefreshToken, _ =>
          {
            return new Lazy<Task<string>>(async () =>
                      {
                var refreshed = await RefreshUserAccessTokenAsync();
                return refreshed.AccessToken;
              });
          }).Value;
        }
        finally
        {
          UserRefreshDictionary.TryRemove(userToken.RefreshToken, out _);
        }
      }

      return userToken.AccessToken;
    }

    /// <inheritdoc/>
    public async Task RevokeRefreshTokenAsync()
    {
      var userToken = await _userTokenStore.GetTokenAsync(_httpContextAccessor.HttpContext.User);

      if (!string.IsNullOrEmpty(userToken?.RefreshToken))
      {
        var response = await _tokenEndpointService.RevokeRefreshTokenAsync(userToken.RefreshToken);

        if (response.IsError)
        {
          _logger.LogError("Error revoking refresh token. Error = {error}", response.Error);
        }
      }
    }

    internal async Task<TokenResponse> RefreshUserAccessTokenAsync()
    {
      var userToken = await _userTokenStore.GetTokenAsync(_httpContextAccessor.HttpContext.User);
      var response = await _tokenEndpointService.RefreshUserAccessTokenAsync(userToken.RefreshToken);

      if (!response.IsError)
      {
        await _userTokenStore.StoreTokenAsync(_httpContextAccessor.HttpContext.User, response.AccessToken, response.ExpiresIn, response.RefreshToken);
      }
      else
      {
        _logger.LogError("Error refreshing access token. Error = {error}", response.Error);
      }

      return response;
    }

    /// <inheritdoc/>
    public async Task<string> GetPasswordAccessTokenAsync(string login, string password, string clientName = AccessTokenManagementDefaults.DefaultTokenClientName)
    {
      try
      {
        return await PasswordTokenRequestDictionary.GetOrAdd(clientName, _ =>
        {
          return new Lazy<Task<string>>(async () =>
          {

            var response = await _tokenEndpointService.RequestPasswordTokenAsync(login, password, clientName);

            if (response.IsError)
            {
              _logger.LogError("Error requesting password access token for client {clientName}. Error = {error}", clientName, response.Error);
              return null;
            }

            await _passwordAccessTokenCache.SetAsync(clientName, response.AccessToken, response.ExpiresIn, response.RefreshToken);
            return response.AccessToken;
          });
        }).Value;
      }
      finally
      {
        PasswordTokenRequestDictionary.TryRemove(clientName, out _);
      }
    }

    /// <inheritdoc/>
    public async Task<string> GetPasswordAccessTokenAsync(string clientName = AccessTokenManagementDefaults.DefaultTokenClientName, bool forceRenewal = false)
    {
      try
      {
        return await PasswordTokenRequestDictionary.GetOrAdd(clientName, _ =>
        {
          return new Lazy<Task<string>>(async () =>
          {
            PasswordAccessToken passwordAccessToken = await _passwordAccessTokenCache.GetAsync(clientName); ;
            if (passwordAccessToken == null)
            {
              _logger.LogDebug("No token data found in password token cache");
              var response = await _tokenEndpointService.RequestPasswordTokenAsync("", "", clientName);
              if (response.IsError)
              {
                _logger.LogError("Error requesting password access token for client {clientName}. Error = {error}", clientName, response.Error);
                return null;
              }
              await _passwordAccessTokenCache.SetAsync(clientName, response.AccessToken, response.ExpiresIn, response.RefreshToken);
              return response.AccessToken;
            }

            var dtRefresh = passwordAccessToken.Expiration.Subtract(_options.User.RefreshBeforeExpiration);
            if (dtRefresh < _clock.UtcNow || forceRenewal == true)
            {
              _logger.LogDebug("Token for password needs refreshing.");
              var refreshed = await _tokenEndpointService.RefreshUserAccessTokenAsync(passwordAccessToken.RefreshToken);
              await _passwordAccessTokenCache.SetAsync(clientName, refreshed.AccessToken, refreshed.ExpiresIn, refreshed.RefreshToken);
              return refreshed.AccessToken;
            }

            return passwordAccessToken.AccessToken;
          });
        }).Value;
      }
      finally
      {
        PasswordTokenRequestDictionary.TryRemove(clientName, out _);
      }
    }
  }
}