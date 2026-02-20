using System.Text.Json.Serialization;

namespace ShoplazzaAddonApp.Models.Api;

/// <summary>
/// Request model for updating a cart transform function
/// </summary>
public class CartTransformFunctionUpdateRequest
{
    /// <summary>
    /// Determines whether to block the cart/checkout process if the Function execution fails
    /// </summary>
    [JsonPropertyName("block_on_failure")]
    public bool? BlockOnFailure { get; set; }

    /// <summary>
    /// A JSON string specifying metafield queries for cart line items
    /// </summary>
    [JsonPropertyName("input_query")]
    public string? InputQuery { get; set; }
}

/// <summary>
/// Input query structure for cart transform function
/// </summary>
public class CartTransformInputQuery
{
    /// <summary>
    /// Defines rules to query Metafield data
    /// </summary>
    [JsonPropertyName("product_metafields_query")]
    public List<ProductMetafieldQuery>? ProductMetafieldsQuery { get; set; }
}

/// <summary>
/// Product metafield query rule
/// </summary>
public class ProductMetafieldQuery
{
    /// <summary>
    /// The namespace of the Metafield
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// The key of the Metafield
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}
