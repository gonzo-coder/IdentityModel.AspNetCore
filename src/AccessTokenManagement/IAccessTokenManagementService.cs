﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Threading.Tasks;

namespace IdentityModel.AspNetCore.AccessTokenManagement
{
  /// <summary>
  /// Abstraction for managing user and client accesss tokens
  /// </summary>
  public interface IAccessTokenManagementService
  {
    /// <summary>
    /// Returns the user access token. If the current token is expired, it will try to refresh it.
    /// </summary>
    /// <returns>An access token or null if refreshing did not work.</returns>
    Task<string> GetUserAccessTokenAsync(bool forceRenewal = false);

    /// <summary>
    /// Revokes the current refresh token
    /// </summary>
    /// <returns></returns>
    Task RevokeRefreshTokenAsync();

    /// <summary>
    /// Returns either a cached or a new access token for a given client configuration or the default client
    /// </summary>
    /// <param name="clientName">Name of the client configuration, or default is omitted.</param>
    /// <param name="forceRenewal">Ignores the cached token, and gets a new one.</param>
    /// <returns>The access token or null if the no token can be requested.</returns>
    Task<string> GetClientAccessTokenAsync(string clientName = AccessTokenManagementDefaults.DefaultTokenClientName, bool forceRenewal = false);

    /// <summary>
    /// Deletes a client access token from the cache
    /// </summary>
    /// <param name="clientName">Name of the client configuration, or default is omitted.</param>
    /// <returns>The access token or null if the no token can be requested.</returns>
    Task DeleteClientAccessTokenAsync(string clientName = AccessTokenManagementDefaults.DefaultTokenClientName);

    /// <summary>
    /// Returns initial access token for a given password grant configuration or the default client and stores it in the cache
    /// </summary>
    /// <param name="login">Login used in password grant</param>
    /// <param name="password">Password used in password grant</param>
    /// <param name="clientName">Name of the client configuration, or default is omitted.</param>
    /// <returns>The access token or null if the no token can be requested.</returns>
    Task<string> GetPasswordAccessTokenAsync(string login, string password, string clientName = AccessTokenManagementDefaults.DefaultTokenClientName);

    /// <summary>
    /// Returns either a cached or a new access token for a given password grant configuration or the default client
    /// </summary>
    /// <param name="clientName">Name of the client configuration, or default is omitted.</param>
    /// <param name="forceRenewal">Ignores the cached token, and gets a new one.</param>
    /// <returns>The access token or null if the no token can be requested.</returns>
    Task<string> GetPasswordAccessTokenAsync(string clientName = AccessTokenManagementDefaults.DefaultTokenClientName, bool forceRenewal = false);
  }
}