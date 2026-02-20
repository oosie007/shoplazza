namespace ShoplazzaAddonApp.Models.Auth;

/// <summary>
/// Represents the response from Shoplazza OAuth token exchange
/// </summary>
public class ShoplazzaAuthResponse
{
    /// <summary>
    /// Access token for API calls
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Comma-separated list of granted scopes
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// The shop domain this token is valid for
    /// </summary>
    public string Shop { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires (if applicable)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}