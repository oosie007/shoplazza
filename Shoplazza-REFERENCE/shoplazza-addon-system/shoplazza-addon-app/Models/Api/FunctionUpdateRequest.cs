using System.Text.Json.Serialization;

namespace ShoplazzaAddonApp.Models.Api;

/// <summary>
/// Request model for updating an existing function with Shoplazza
/// </summary>
public class FunctionUpdateRequest
{
    /// <summary>
    /// Namespace of the function
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Updated compiled .wasm file (optional)
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; set; }

    /// <summary>
    /// Updated function source code (optional)
    /// </summary>
    [JsonPropertyName("source_code")]
    public string? SourceCode { get; set; }
}
