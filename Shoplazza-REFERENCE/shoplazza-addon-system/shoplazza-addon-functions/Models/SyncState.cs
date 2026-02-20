using System.ComponentModel.DataAnnotations;

namespace ShoplazzaAddonFunctions.Models;

/// <summary>
/// Represents the sync state for a merchant
/// </summary>
public class SyncState
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Merchant ID (foreign key)
    /// </summary>
    [Required]
    public int MerchantId { get; set; }

    /// <summary>
    /// Last synced order ID
    /// </summary>
    public long? LastSyncedOrderId { get; set; }

    /// <summary>
    /// Sync status
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string SyncStatus { get; set; } = string.Empty;

    /// <summary>
    /// Last sync date
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    [MaxLength(1000)]
    public string? LastError { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to Merchant
    /// </summary>
    public virtual Merchant Merchant { get; set; } = null!;
} 