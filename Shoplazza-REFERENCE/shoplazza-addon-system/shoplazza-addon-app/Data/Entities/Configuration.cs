using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoplazzaAddonApp.Data.Entities;

/// <summary>
/// Global configuration settings for a merchant's store
/// </summary>
[Table("Configurations")]
public class Configuration
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Merchant
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// Navigation property to Merchant
    /// </summary>
    public virtual Merchant Merchant { get; set; } = null!;

    /// <summary>
    /// Whether the add-on system is enabled for this store
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Default currency for add-ons in this store
    /// </summary>
    [Required]
    [MaxLength(3)]

    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>
    /// Default tax setting for add-ons
    /// </summary>
    public bool DefaultTaxable { get; set; } = true;

    /// <summary>
    /// Default shipping requirement for add-ons
    /// </summary>
    public bool DefaultRequiresShipping { get; set; } = true;

    /// <summary>
    /// Widget styling configuration as JSON
    /// </summary>

    public string? WidgetSettings { get; set; }

    /// <summary>
    /// Analytics settings as JSON
    /// </summary>

    public string? AnalyticsSettings { get; set; }

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional custom settings as JSON
    /// </summary>

    public string? CustomSettings { get; set; }
}