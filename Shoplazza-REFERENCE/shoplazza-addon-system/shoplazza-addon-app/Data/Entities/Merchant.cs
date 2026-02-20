using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoplazzaAddonApp.Data.Entities;

/// <summary>
/// Represents a merchant (Shoplazza store) in the database
/// </summary>
[Table("Merchants")]
public class Merchant
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Shop domain (e.g., example-store.myshoplazza.com)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Shop { get; set; } = string.Empty;

    /// <summary>
    /// Store name from Shoplazza
    /// </summary>
    [MaxLength(500)]
    public string? StoreName { get; set; }

    /// <summary>
    /// Store email from Shoplazza
    /// </summary>
    [MaxLength(255)]
    public string? StoreEmail { get; set; }

    /// <summary>
    /// Encrypted access token for Shoplazza API
    /// </summary>
    [MaxLength(1000)]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Granted scopes for the access token
    /// </summary>
    [MaxLength(500)]
    public string? Scopes { get; set; }

    /// <summary>
    /// When the access token was created
    /// </summary>
    public DateTime? TokenCreatedAt { get; set; }

    /// <summary>
    /// When the access token expires (if applicable)
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// Whether the merchant is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the merchant first installed the app
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the merchant record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the merchant last accessed the app
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// When the app was uninstalled (if applicable)
    /// </summary>
    public DateTime? UninstalledAt { get; set; }

    /// <summary>
    /// Script tag ID for the widget (if installed). API returns string IDs.
    /// </summary>
    public string? ScriptTagId { get; set; }

    /// <summary>
    /// Additional metadata about the merchant
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property to product add-ons
    /// </summary>
    public virtual ICollection<ProductAddOn> ProductAddOns { get; set; } = new List<ProductAddOn>();

    /// <summary>
    /// Navigation property to configuration
    /// </summary>
    public virtual Configuration? Configuration { get; set; }
}