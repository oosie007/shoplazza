using Newtonsoft.Json;

namespace ShoplazzaAddonApp.Models.Dto;

/// <summary>
/// DTO for Shoplazza Product API responses
/// </summary>
public class ShoplazzaProductDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("handle")]
    public string Handle { get; set; } = string.Empty;

    [JsonProperty("body_html")]
    public string? BodyHtml { get; set; }

    [JsonProperty("vendor")]
    public string? Vendor { get; set; }

    [JsonProperty("product_type")]
    public string? ProductType { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("published_at")]
    public DateTime? PublishedAt { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("variants")]
    public List<ShoplazzaVariantDto> Variants { get; set; } = new();

    [JsonProperty("images")]
    public List<ShoplazzaImageDto> Images { get; set; } = new();

    [JsonProperty("options")]
    public List<ShoplazzaOptionDto> Options { get; set; } = new();

    [JsonProperty("tags")]
    public string? Tags { get; set; }

    [JsonProperty("metafields")]
    public List<ShoplazzaMetafieldDto>? Metafields { get; set; }
}

/// <summary>
/// DTO for Shoplazza Product Variant
/// </summary>
public class ShoplazzaVariantDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("compare_at_price")]
    public decimal? CompareAtPrice { get; set; }

    [JsonProperty("sku")]
    public string? Sku { get; set; }

    [JsonProperty("position")]
    public int Position { get; set; }

    [JsonProperty("inventory_policy")]
    public string? InventoryPolicy { get; set; }

    [JsonProperty("fulfillment_service")]
    public string? FulfillmentService { get; set; }

    [JsonProperty("inventory_management")]
    public string? InventoryManagement { get; set; }

    [JsonProperty("option1")]
    public string? Option1 { get; set; }

    [JsonProperty("option2")]
    public string? Option2 { get; set; }

    [JsonProperty("option3")]
    public string? Option3 { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("taxable")]
    public bool Taxable { get; set; }

    [JsonProperty("barcode")]
    public string? Barcode { get; set; }

    [JsonProperty("grams")]
    public int Grams { get; set; }

    [JsonProperty("image_id")]
    public string? ImageId { get; set; }

    [JsonProperty("weight")]
    public decimal Weight { get; set; }

    [JsonProperty("weight_unit")]
    public string WeightUnit { get; set; } = "kg";

    [JsonProperty("inventory_item_id")]
    public string? InventoryItemId { get; set; }

    [JsonProperty("inventory_quantity")]
    public int InventoryQuantity { get; set; }

    [JsonProperty("old_inventory_quantity")]
    public int OldInventoryQuantity { get; set; }

    [JsonProperty("requires_shipping")]
    public bool RequiresShipping { get; set; }

    [JsonProperty("admin_graphql_api_id")]
    public string? AdminGraphqlApiId { get; set; }
}

/// <summary>
/// DTO for Shoplazza Product Image
/// </summary>
public class ShoplazzaImageDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonProperty("position")]
    public int Position { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("alt")]
    public string? Alt { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("src")]
    public string Src { get; set; } = string.Empty;

    [JsonProperty("variant_ids")]
    public List<string> VariantIds { get; set; } = new();

    [JsonProperty("admin_graphql_api_id")]
    public string? AdminGraphqlApiId { get; set; }
}

/// <summary>
/// DTO for Shoplazza Product Option
/// </summary>
public class ShoplazzaOptionDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("position")]
    public int Position { get; set; }

    [JsonProperty("values")]
    public List<string> Values { get; set; } = new();
}

/// <summary>
/// DTO for Shoplazza Metafield
/// </summary>
public class ShoplazzaMetafieldDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;

    [JsonProperty("value_type")]
    public string ValueType { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("owner_id")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonProperty("owner_resource")]
    public string OwnerResource { get; set; } = string.Empty;

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for Shoplazza API list responses
/// </summary>
public class ShoplazzaProductsResponse
{
    [JsonProperty("products")]
    public List<ShoplazzaProductDto> Products { get; set; } = new();
}

/// <summary>
/// DTO for single product responses
/// </summary>
public class ShoplazzaProductResponse
{
    [JsonProperty("product")]
    public ShoplazzaProductDto Product { get; set; } = new();
}