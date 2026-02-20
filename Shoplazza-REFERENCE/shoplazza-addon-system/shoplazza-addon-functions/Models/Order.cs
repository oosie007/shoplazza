using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoplazzaAddonFunctions.Models;

/// <summary>
/// Represents an order from Shoplazza
/// </summary>
public class Order
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Shoplazza order ID
    /// </summary>
    [Required]
    public long ShoplazzaOrderId { get; set; }

    /// <summary>
    /// Merchant ID (foreign key)
    /// </summary>
    [Required]
    public int MerchantId { get; set; }

    /// <summary>
    /// Order number
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer email
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Total price
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Add-on revenue
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AddOnRevenue { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Order status
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Financial status
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string FinancialStatus { get; set; } = string.Empty;

    /// <summary>
    /// Source of the order (webhook, sync, etc.)
    /// </summary>
    [MaxLength(20)]
    public string? Source { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last synced date
    /// </summary>
    public DateTime LastSyncedAt { get; set; }

    /// <summary>
    /// Navigation property to Merchant
    /// </summary>
    public virtual Merchant Merchant { get; set; } = null!;

    /// <summary>
    /// Navigation property to OrderLineItems
    /// </summary>
    public virtual ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();
} 