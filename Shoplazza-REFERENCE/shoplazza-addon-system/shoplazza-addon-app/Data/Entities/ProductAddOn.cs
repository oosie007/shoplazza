using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoplazzaAddonApp.Data.Entities;

/// <summary>
/// Represents a product with its add-on configuration in the database
/// </summary>
[Table("ProductAddOns")]
public class ProductAddOn
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
    /// Shoplazza product ID
    /// </summary>
    [Required]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Product title from Shoplazza
    /// </summary>
    [MaxLength(500)]
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Product handle/slug from Shoplazza
    /// </summary>
    [MaxLength(255)]
    public string ProductHandle { get; set; } = string.Empty;

    /// <summary>
    /// Whether this product has add-ons enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Add-on product ID in Shoplazza (if using existing product)
    /// </summary>
    public string? AddOnProductId { get; set; }

    /// <summary>
    /// Add-on variant ID in Shoplazza (if using existing variant)
    /// </summary>
    public string? AddOnVariantId { get; set; }

    /// <summary>
    /// Display title for the add-on
    /// </summary>
    [Required]
    [MaxLength(255)]

    public string AddOnTitle { get; set; } = string.Empty;

    /// <summary>
    /// Description of the add-on
    /// </summary>
    [MaxLength(1000)]

    public string AddOnDescription { get; set; } = string.Empty;

    /// <summary>
    /// Price of the add-on in cents
    /// </summary>
    [Required]
    public int AddOnPriceCents { get; set; }

    /// <summary>
    /// Currency code for the add-on price
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

    public string AddOnSku { get; set; } = string.Empty;

    /// <summary>
    /// Whether the add-on requires shipping
    /// </summary>
    public bool RequiresShipping { get; set; } = true;

    /// <summary>
    /// Weight of the add-on in grams
    /// </summary>
    public int WeightGrams { get; set; } = 0;

    /// <summary>
    /// Whether the add-on is taxable
    /// </summary>
    public bool IsTaxable { get; set; } = true;

    /// <summary>
    /// Image URL for the add-on
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
    /// When this product add-on was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this product add-on was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional configuration stored as JSON
    /// </summary>

    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// Formatted display price for UI
    /// </summary>
    [NotMapped]
    public string FormattedPrice => $"{Currency} {AddOnPriceCents / 100.0:F2}";

    /// <summary>
    /// Price in decimal format for calculations
    /// </summary>
    [NotMapped]
    public decimal Price => AddOnPriceCents / 100.0m;
}