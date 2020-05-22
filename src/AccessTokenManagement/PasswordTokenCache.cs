// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace IdentityModel.AspNetCore.AccessTokenManagement
{
  /// <summary>
  /// Client access token cache using IDistributedCache
  /// </summary>
  public class PasswordTokenCache : IPasswordAccessTokenCache
  {
    private readonly IDistributedCache _cache;
    private readonly ILogger<PasswordTokenCache> _logger;
    private readonly AccessTokenManagementOptions _options;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public PasswordTokenCache(IDistributedCache cache, IOptions<AccessTokenManagementOptions> options, ILogger<PasswordTokenCache> logger)
    {
      _cache = cache;
      _logger = logger;
      _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<PasswordAccessToken> GetAsync(string clientName)
    {
      if (clientName is null) throw new ArgumentNullException(nameof(clientName));

      var entry = await _cache.GetStringAsync(_options.Client.CacheKeyPrefix + clientName);

      if (entry != null)
      {
        try
        {
          _logger.LogDebug("Cache hit for access token for client: {clientName}", clientName);

          return JsonSerializer.Deserialize<PasswordAccessToken>(entry);
        }
        catch (Exception ex)
        {
          _logger.LogCritical(ex, "Error parsing cached access token for client {clientName}", clientName);
          return null;
        }
      }

      _logger.LogDebug("Cache miss for access token for client: {clientName}", clientName);
      return null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(string clientName, string accessToken, int expiresIn, string refreshToken)
    {
      if (clientName is null) throw new ArgumentNullException(nameof(clientName));

      var expiration = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
      var expirationEpoch = expiration.ToUnixTimeSeconds();

      PasswordAccessToken passwordAccessToken = new PasswordAccessToken
      {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        Expiration = DateTimeOffset.FromUnixTimeSeconds(expirationEpoch)
      };

      //var entryOptions = new DistributedCacheEntryOptions
      //{
      //  AbsoluteExpiration = cacheExpiration
      //};

      _logger.LogDebug("Caching access token for client: {clientName}", clientName);
      await _cache.SetStringAsync(_options.Password.CacheKeyPrefix + clientName, JsonSerializer.Serialize(passwordAccessToken));
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string clientName)
    {
      if (clientName is null) throw new ArgumentNullException(nameof(clientName));

      return _cache.RemoveAsync(_options.Password.CacheKeyPrefix + clientName);
    }
  }
}