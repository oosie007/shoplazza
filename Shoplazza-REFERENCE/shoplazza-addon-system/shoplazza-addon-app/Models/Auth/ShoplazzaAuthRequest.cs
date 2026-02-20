using System.ComponentModel.DataAnnotations;

namespace ShoplazzaAddonApp.Models.Auth;

/// <summary>
/// Represents an incoming authentication request from Shoplazza
/// </summary>
public class ShoplazzaAuthRequest
{
    /// <summary>
    /// The shop's domain (e.g., example-store.myshoplazza.com)
    /// </summary>
    [Required]
    public string Shop { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the request
    /// </summary>
    [Required]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// HMAC signature for request verification
    /// </summary>
    [Required]
    public string Hmac { get; set; } = string.Empty;

    /// <summary>
    /// Installation source (e.g., app_store, direct)
    /// </summary>
    public string? InstallSource { get; set; }

    /// <summary>
    /// Additional parameters from Shoplazza
    /// </summary>
    public Dictionary<string, string> AdditionalParams { get; set; } = new();
}