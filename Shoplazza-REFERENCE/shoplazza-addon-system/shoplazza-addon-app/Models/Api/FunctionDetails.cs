using System.Text.Json.Serialization;

namespace ShoplazzaAddonApp.Models.Api;

/// <summary>
/// Detailed information about a function from Shoplazza's Function API
/// </summary>
public class FunctionDetails
{
    /// <summary>
    /// Unique identifier for the function
    /// </summary>
    [JsonPropertyName("function_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the function
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the function
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the function
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// When the function was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the function was last updated
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
  
    [JsonPropertyName("input_schema")]
    public string InputSchmea { get; set; } = string.Empty;
}

/// <summary>
/// Function execution settings
/// </summary>
public class FunctionExecutionSettings
{
    /// <summary>
    /// Function timeout in seconds
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    /// <summary>
    /// Memory limit for function execution
    /// </summary>
    [JsonPropertyName("memory_limit")]
    public string? MemoryLimit { get; set; }

    /// <summary>
    /// Whether the function is enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
}

/// <summary>
/// Function error information
/// </summary>
public class FunctionError
{
    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    [JsonPropertyName("occurred_at")]
    public DateTime? OccurredAt { get; set; }
}
