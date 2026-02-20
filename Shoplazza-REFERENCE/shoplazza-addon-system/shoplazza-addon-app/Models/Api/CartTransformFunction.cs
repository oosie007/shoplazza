using System.Text.Json.Serialization;

namespace ShoplazzaAddonApp.Models.Api;

/// <summary>
/// Cart transform function bound to a shop
/// </summary>
public class CartTransformFunction
{
    /// <summary>
    /// Unique identifier for the cart price adjustment function
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The function ID linked to the cart price adjustment
    /// </summary>
    [JsonPropertyName("function_id")]
    public string FunctionId { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether the failure of the function should block the cart and checkout process
    /// </summary>
    [JsonPropertyName("block_on_failure")]
    public bool BlockOnFailure { get; set; } = false;
}

/// <summary>
/// Response from Shoplazza Cart Transform Function List API
/// </summary>
public class CartTransformFunctionListResponse
{
    /// <summary>
    /// Response status code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Response message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Contains response data
    /// </summary>
    [JsonPropertyName("data")]
    public CartTransformFunctionListData? Data { get; set; }
}

/// <summary>
/// Cart transform function list data container
/// </summary>
public class CartTransformFunctionListData
{
    /// <summary>
    /// Total number of records
    /// </summary>
    [JsonPropertyName("count")]
    public ulong Count { get; set; }

    /// <summary>
    /// List of cart-transform bindings
    /// </summary>
    [JsonPropertyName("cart-transforms")]
    public List<CartTransformFunction> CartTransforms { get; set; } = new();
}
