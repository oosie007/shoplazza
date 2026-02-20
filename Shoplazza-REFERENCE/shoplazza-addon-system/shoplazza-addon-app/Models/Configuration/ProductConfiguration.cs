using System.ComponentModel.DataAnnotations;

namespace ShoplazzaAddonApp.Models.Configuration;

/// <summary>
/// Configuration for a product that can have add-ons
/// </summary>
public class ProductConfiguration
{
    /// <summary>
    /// Unique identifier for the configuration
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The shop domain this configuration belongs to
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Shop { get; set; } = string.Empty;

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
    /// The add-on configuration for this product
    /// </summary>
    public AddOnConfiguration? AddOn { get; set; }

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata for the product
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}