using System.ComponentModel.DataAnnotations;

namespace ShoplazzaAddonApp.Models.Api;

/// <summary>
/// Request model for registering a new function with Shoplazza
/// </summary>
public class FunctionRegistrationRequest
{
    /// <summary>
    /// Name of the function (must be unique within the shop)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of function (e.g., "cart-transform", "webhook")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the function does
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Base64 encoded WASM file content
    /// </summary>
    [Required]
    public string WasmBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Source code of the function (Rust/JavaScript/AssemblyScript)
    /// </summary>
    [Required]
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>
    /// List of triggers that will invoke this function
    /// </summary>
    [Required]
    public List<string> Triggers { get; set; } = new();

    /// <summary>
    /// Function execution settings
    /// </summary>
    public FunctionSettings Settings { get; set; } = new();

    /// <summary>
    /// Additional metadata for the function
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Settings for function execution
/// </summary>
public class FunctionSettings
{
    /// <summary>
    /// Function timeout in milliseconds
    /// </summary>
    public int Timeout { get; set; } = 5000;

    /// <summary>
    /// Memory limit for function execution
    /// </summary>
    [MaxLength(50)]
    public string MemoryLimit { get; set; } = "128MB";

    /// <summary>
    /// Whether the function should be enabled immediately after creation
    /// </summary>
    public bool AutoEnable { get; set; } = false;

    /// <summary>
    /// Retry configuration for failed executions
    /// </summary>
    public RetrySettings Retry { get; set; } = new();
}

/// <summary>
/// Retry configuration for function execution
/// </summary>
public class RetrySettings
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds
    /// </summary>
    public int DelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}
