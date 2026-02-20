using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoplazzaAddonApp.Models.Configuration;

/// <summary>
/// Global configuration for functions that are created once and reused across all merchants
/// </summary>
[Table("GlobalFunctionConfigurations")]
public class GlobalFunctionConfiguration
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The function ID returned by Shoplazza's Partner API
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
    /// The namespace of the function (e.g., "cart_transform")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FunctionNamespace { get; set; } = string.Empty;

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

    /// <summary>
    /// Version of the function (for tracking updates)
    /// </summary>
    [MaxLength(50)]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Whether this function is the current active version
    /// </summary>
    public bool IsActive { get; set; } = true;
}
