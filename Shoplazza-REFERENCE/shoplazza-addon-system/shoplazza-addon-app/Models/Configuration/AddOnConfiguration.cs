using System.ComponentModel.DataAnnotations;

namespace ShoplazzaAddonApp.Models.Configuration;

/// <summary>
/// Configuration for an add-on (optional product)
/// </summary>
public class AddOnConfiguration
{
    /// <summary>
    /// Unique identifier for the add-on configuration
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The parent product configuration ID
    /// </summary>
    public int ProductConfigurationId { get; set; }

    /// <summary>
    /// Navigation property to parent product configuration
    /// </summary>
    public ProductConfiguration? ProductConfiguration { get; set; }

    /// <summary>
    /// The add-on product ID in Shoplazza (if using existing product)
    /// </summary>
    public string? AddOnProductId { get; set; }

    /// <summary>
    /// The add-on variant ID in Shoplazza (if using existing variant)
    /// </summary>
    public string? AddOnVariantId { get; set; }

    /// <summary>
    /// Display title for the add-on
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the add-on
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Price of the add-on in cents (e.g., 100 = $1.00)
    /// </summary>
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Price must be non-negative")]
    public int PriceCents { get; set; }

    /// <summary>
    /// Currency code for the price (e.g., USD, EUR)
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Text displayed on the toggle button
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>
    /// SKU for the add-on product
    /// </summary>
    [MaxLength(100)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Whether the add-on requires shipping
    /// </summary>
    public bool RequiresShipping { get; set; } = true;

    /// <summary>
    /// Weight of the add-on in grams (if it requires shipping)
    /// </summary>
    public int WeightGrams { get; set; } = 0;

    /// <summary>
    /// Whether the add-on is taxable
    /// </summary>
    public bool IsTaxable { get; set; } = true;

    /// <summary>
    /// Image URL for the add-on (optional)
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Position/order for displaying multiple add-ons
    /// </summary>
    public int Position { get; set; } = 1;

    /// <summary>
    /// Whether this add-on is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this add-on configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this add-on configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional configuration options for the add-on
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();

    /// <summary>
    /// Formatted display price for UI
    /// </summary>
    public string FormattedPrice => $"{Currency} {PriceCents / 100.0:F2}";

    /// <summary>
    /// Price in decimal format for calculations
    /// </summary>
    public decimal Price => PriceCents / 100.0m;
}