using ShoplazzaAddonApp.Models.Auth;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Interface for Shoplazza authentication and authorization services
/// </summary>
public interface IShoplazzaAuthService
{
    /// <summary>
    /// Validates an HMAC signature from Shoplazza
    /// </summary>
    /// <param name="validationModel">The validation parameters</param>
    /// <returns>True if the HMAC is valid</returns>
    Task<bool> ValidateHmacAsync(HmacValidationModel validationModel);

    /// <summary>
    /// Generates the OAuth authorization URL for a shop
    /// </summary>
    /// <param name="shop">The shop domain</param>
    /// <param name="scopes">Required permissions</param>
    /// <param name="state">Optional state parameter</param>
    /// <returns>The authorization URL</returns>
    string GenerateOAuthUrl(string shop, string[] scopes, string? state = null);

    /// <summary>
    /// Exchanges an authorization code for an access token
    /// </summary>
    /// <param name="shop">The shop domain</param>
    /// <param name="code">Authorization code from callback</param>
    /// <returns>Authentication response with access token</returns>
    Task<ShoplazzaAuthResponse> ExchangeCodeForTokenAsync(string shop, string code);

    /// <summary>
    /// Validates an incoming authentication request
    /// </summary>
    /// <param name="authRequest">The authentication request</param>
    /// <returns>True if the request is valid</returns>
    Task<bool> ValidateAuthRequestAsync(ShoplazzaAuthRequest authRequest);

    /// <summary>
    /// Gets the current access token for a shop
    /// </summary>
    /// <param name="shop">The shop domain</param>
    /// <returns>The access token if available</returns>
    Task<string?> GetAccessTokenAsync(string shop);

    /// <summary>
    /// Stores an access token for a shop
    /// </summary>
    /// <param name="shop">The shop domain</param>
    /// <param name="authResponse">The authentication response</param>
    Task StoreAccessTokenAsync(string shop, ShoplazzaAuthResponse authResponse);

    /// <summary>
    /// Removes stored authentication data for a shop (e.g., on uninstall)
    /// </summary>
    /// <param name="shop">The shop domain</param>
    Task RemoveAuthDataAsync(string shop);

    /// <summary>
    /// Gets a partner-level access token for Function API calls using client credentials
    /// </summary>
    /// <returns>The partner access token</returns>
    Task<string> GetPartnerTokenAsync();
}