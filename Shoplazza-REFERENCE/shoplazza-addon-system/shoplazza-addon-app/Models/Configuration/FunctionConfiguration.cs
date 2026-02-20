using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoplazzaAddonApp.Models.Configuration;

/// <summary>
/// Configuration for a cart-transform function registered with Shoplazza
/// </summary>
[Table("FunctionConfigurations")]
public class FunctionConfiguration
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
    public virtual ShoplazzaAddonApp.Data.Entities.Merchant Merchant { get; set; } = null!;

    /// <summary>
    /// The function ID returned by Shoplazza's Function API
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FunctionId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the function as registered with Shoplazza
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// The type of function (e.g., "cart-transform")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FunctionType { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the function
    /// </summary>
    public FunctionStatus Status { get; set; } = FunctionStatus.Pending;

    /// <summary>
    /// When this function configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the function was last activated
    /// </summary>
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// Error message if function registration/activation failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional configuration data as JSON
    /// </summary>
    [MaxLength(2000)]
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Status of a cart-transform function
/// </summary>
public enum FunctionStatus
{
    /// <summary>
    /// Function is being created/uploaded
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Function is active and working
    /// </summary>
    Active = 1,

    /// <summary>
    /// Function creation/activation failed
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Function has been deleted
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// Function status is unknown
    /// </summary>
    Unknown = 4
}
