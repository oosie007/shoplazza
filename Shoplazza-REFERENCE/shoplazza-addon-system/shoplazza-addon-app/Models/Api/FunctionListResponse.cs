using System.Text.Json.Serialization;

namespace ShoplazzaAddonApp.Models.Api;

/// <summary>
/// Response from Shoplazza Function Details API (partner-level)
/// </summary>
public class FunctionListResponse
{
    /// <summary>
    /// API response code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Response message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Contains function details
    /// </summary>
    [JsonPropertyName("data")]
    public FunctionListData? Data { get; set; }
}

/// <summary>
/// Function list data container
/// </summary>
public class FunctionListData
{
    /// <summary>
    /// Total number of functions
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// List of registered functions
    /// </summary>
    [JsonPropertyName("functions")]
    public List<FunctionDetails> Functions { get; set; } = new();
}
