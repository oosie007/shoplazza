using System.ComponentModel.DataAnnotations;

namespace ShoplazzaAddonApp.Models.Configuration;

/// <summary>
/// Global settings for a merchant's Shoplazza store
/// </summary>
public class MerchantSettings
{
    /// <summary>
    /// Unique identifier for the merchant settings
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this settings belongs to
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Shop { get; set; } = string.Empty;

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
    /// Theme customization settings
    /// </summary>
    public WidgetSettings Widget { get; set; } = new();

    /// <summary>
    /// Analytics and tracking settings
    /// </summary>
    public AnalyticsSettings Analytics { get; set; } = new();

    /// <summary>
    /// When this merchant was first configured
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When these settings were last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the merchant accessed the app
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Additional merchant-specific settings
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Widget appearance and behavior settings
/// </summary>
public class WidgetSettings
{
    /// <summary>
    /// Primary color for the widget (hex code)
    /// </summary>
    [MaxLength(7)]
    public string PrimaryColor { get; set; } = "#007bff";

    /// <summary>
    /// Text color for the widget (hex code)
    /// </summary>
    [MaxLength(7)]
    public string TextColor { get; set; } = "#333333";

    /// <summary>
    /// Border color for the widget (hex code)
    /// </summary>
    [MaxLength(7)]
    public string BorderColor { get; set; } = "#e1e8ed";

    /// <summary>
    /// Background color for selected state (hex code)
    /// </summary>
    [MaxLength(7)]
    public string SelectedBackgroundColor { get; set; } = "#e7f3ff";

    /// <summary>
    /// Theme variant (default, minimal, rounded, etc.)
    /// </summary>
    [MaxLength(50)]
    public string Theme { get; set; } = "default";

    /// <summary>
    /// Position of the widget on product pages
    /// </summary>
    [MaxLength(50)]
    public string Position { get; set; } = "below-price";

    /// <summary>
    /// Animation type for price updates
    /// </summary>
    [MaxLength(50)]
    public string Animation { get; set; } = "highlight";

    /// <summary>
    /// Whether to show the add-on description
    /// </summary>
    public bool ShowDescription { get; set; } = true;

    /// <summary>
    /// Whether to show the add-on image
    /// </summary>
    public bool ShowImage { get; set; } = false;

    /// <summary>
    /// Custom CSS for advanced styling
    /// </summary>
    public string? CustomCss { get; set; }
}

/// <summary>
/// Analytics and tracking settings
/// </summary>
public class AnalyticsSettings
{
    /// <summary>
    /// Whether to track add-on conversions
    /// </summary>
    public bool EnableConversionTracking { get; set; } = true;

    /// <summary>
    /// Whether to track add-on revenue
    /// </summary>
    public bool EnableRevenueTracking { get; set; } = true;

    /// <summary>
    /// Google Analytics tracking ID (if applicable)
    /// </summary>
    [MaxLength(50)]
    public string? GoogleAnalyticsId { get; set; }

    /// <summary>
    /// Whether to send events to Google Analytics
    /// </summary>
    public bool SendToGoogleAnalytics { get; set; } = false;

    /// <summary>
    /// Custom event tracking configuration
    /// </summary>
    public Dictionary<string, object> CustomEvents { get; set; } = new();

    /// <summary>
    /// Data retention period in days
    /// </summary>
    public int DataRetentionDays { get; set; } = 365;
}