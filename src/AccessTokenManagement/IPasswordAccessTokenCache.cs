using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityModel.AspNetCore.AccessTokenManagement
{
  /// <summary>
  /// Abstraction for caching password tokens
  /// </summary>
  public interface IPasswordAccessTokenCache
  {
    /// <summary>
    /// Caches a client access token
    /// </summary>
    /// <param name="clientName"></param>
    /// <param name="accessToken"></param>
    /// <param name="expiresIn"></param>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    Task SetAsync(string clientName, string accessToken, int expiresIn, string refreshToken);

    /// <summary>
    /// Retrieves a client access token from the cache
    /// </summary>
    /// <param name="clientName"></param>
    /// <returns></returns>
    Task<PasswordAccessToken> GetAsync(string clientName);

    /// <summary>
    /// Deletes a client access token from the cache
    /// </summary>
    /// <param name="clientName"></param>
    /// <returns></returns>
    Task DeleteAsync(string clientName);
  }
}
