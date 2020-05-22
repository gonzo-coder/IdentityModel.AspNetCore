using System.Threading.Tasks;
using IdentityModel.Client;

namespace IdentityModel.AspNetCore.AccessTokenManagement
{
  /// <summary>
  /// Retrieves request details for client credentials, refresh and revocation requests
  /// </summary>
  public interface ITokenClientConfigurationService
  {
    /// <summary>
    /// Returns the request details for a client credentials token request
    /// </summary>
    /// <param name="clientName"></param>
    /// <returns></returns>
    Task<ClientCredentialsTokenRequest> GetClientCredentialsRequestAsync(string clientName);

    /// <summary>
    /// Returns the request details for a refresh token request
    /// </summary>
    /// <returns></returns>
    Task<RefreshTokenRequest> GetRefreshTokenRequestAsync();

    /// <summary>
    /// Returns the request details for a token revocation request
    /// </summary>
    /// <returns></returns>
    Task<TokenRevocationRequest> GetTokenRevocationRequestAsync();

    /// <summary>
    /// Returns the request details for a password grant token request
    /// </summary>
    /// <param name="clientName"></param>
    /// <returns></returns>
    Task<PasswordTokenRequest> GetPasswordGrantRequestAsync(string clientName);

    /// <summary>
    /// Returns the request details for a refresh token request
    /// </summary>
    /// <returns></returns>
    Task<RefreshTokenRequest> GetRefreshPasswordTokenRequestAsync(string clientName);
  }
}