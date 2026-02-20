namespace ShoplazzaAddonApp.Models.Auth;

/// <summary>
/// Model for HMAC validation parameters
/// </summary>
public class HmacValidationModel
{
    /// <summary>
    /// The raw query string or request body to validate
    /// </summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>
    /// The HMAC signature to verify against
    /// </summary>
    public string ProvidedHmac { get; set; } = string.Empty;

    /// <summary>
    /// The secret key for HMAC calculation
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp tolerance in seconds (default: 5 minutes)
    /// </summary>
    public int TimestampTolerance { get; set; } = 300;

    /// <summary>
    /// The timestamp from the request
    /// </summary>
    public DateTime? RequestTimestamp { get; set; }
}