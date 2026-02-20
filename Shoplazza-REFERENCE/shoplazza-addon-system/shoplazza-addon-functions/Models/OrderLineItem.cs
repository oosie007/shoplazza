using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoplazzaAddonFunctions.Models;

/// <summary>
/// Represents a line item within an order
/// </summary>
public class OrderLineItem
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Order ID (foreign key)
    /// </summary>
    [Required]
    public int OrderId { get; set; }

    /// <summary>
    /// Product ID from Shoplazza
    /// </summary>
    [Required]
    public long ProductId { get; set; }

    /// <summary>
    /// Product title
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Variant title
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string VariantTitle { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total price for this line item
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Whether this line item has an add-on
    /// </summary>
    public bool HasAddOn { get; set; }

    /// <summary>
    /// Add-on price
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AddOnPrice { get; set; }

    /// <summary>
    /// Add-on title
    /// </summary>
    [MaxLength(255)]
    public string? AddOnTitle { get; set; }

    /// <summary>
    /// Add-on SKU
    /// </summary>
    [MaxLength(100)]
    public string? AddOnSku { get; set; }

    /// <summary>
    /// Line item properties (JSON)
    /// </summary>
    [Column(TypeName = "text")]
    public string? Properties { get; set; }

    /// <summary>
    /// Navigation property to Order
    /// </summary>
    public virtual Order Order { get; set; } = null!;
} 