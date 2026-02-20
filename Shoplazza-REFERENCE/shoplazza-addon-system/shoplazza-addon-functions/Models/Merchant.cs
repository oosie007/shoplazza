using System.ComponentModel.DataAnnotations;

namespace ShoplazzaAddonFunctions.Models;

/// <summary>
/// Represents a merchant (simplified for functions app)
/// </summary>
public class Merchant
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Shop domain
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Shop { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted access token
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether the merchant is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to Orders
    /// </summary>
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>
    /// Navigation property to SyncState
    /// </summary>
    public virtual SyncState? SyncState { get; set; }
} 