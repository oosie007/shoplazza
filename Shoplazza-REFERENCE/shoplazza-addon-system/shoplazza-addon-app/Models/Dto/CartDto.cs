using Newtonsoft.Json;

namespace ShoplazzaAddonApp.Models.Dto;

/// <summary>
/// DTO for cart operations and responses
/// </summary>
public class CartDto
{
    [JsonProperty("note")]
    public string? Note { get; set; }

    [JsonProperty("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = new();

    [JsonProperty("original_total_price")]
    public decimal OriginalTotalPrice { get; set; }

    [JsonProperty("total_price")]
    public decimal TotalPrice { get; set; }

    [JsonProperty("total_discount")]
    public decimal TotalDiscount { get; set; }

    [JsonProperty("total_weight")]
    public decimal TotalWeight { get; set; }

    [JsonProperty("item_count")]
    public int ItemCount { get; set; }

    [JsonProperty("items")]
    public List<CartItemDto> Items { get; set; } = new();

    [JsonProperty("requires_shipping")]
    public bool RequiresShipping { get; set; }

    [JsonProperty("currency")]
    public string Currency { get; set; } = "USD";

    [JsonProperty("items_subtotal_price")]
    public decimal ItemsSubtotalPrice { get; set; }

    [JsonProperty("cart_level_discount_applications")]
    public List<object> CartLevelDiscountApplications { get; set; } = new();
}

/// <summary>
/// DTO for cart items
/// </summary>
public class CartItemDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("variant_id")]
    public long VariantId { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("original_price")]
    public decimal OriginalPrice { get; set; }

    [JsonProperty("discounted_price")]
    public decimal DiscountedPrice { get; set; }

    [JsonProperty("line_price")]
    public decimal LinePrice { get; set; }

    [JsonProperty("original_line_price")]
    public decimal OriginalLinePrice { get; set; }

    [JsonProperty("total_discount")]
    public decimal TotalDiscount { get; set; }

    [JsonProperty("discounts")]
    public List<object> Discounts { get; set; } = new();

    [JsonProperty("sku")]
    public string? Sku { get; set; }

    [JsonProperty("grams")]
    public int Grams { get; set; }

    [JsonProperty("vendor")]
    public string? Vendor { get; set; }

    [JsonProperty("taxable")]
    public bool Taxable { get; set; }

    [JsonProperty("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonProperty("product_has_only_default_variant")]
    public bool ProductHasOnlyDefaultVariant { get; set; }

    [JsonProperty("gift_card")]
    public bool GiftCard { get; set; }

    [JsonProperty("final_price")]
    public decimal FinalPrice { get; set; }

    [JsonProperty("final_line_price")]
    public decimal FinalLinePrice { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("featured_image")]
    public CartItemImageDto? FeaturedImage { get; set; }

    [JsonProperty("image")]
    public string? Image { get; set; }

    [JsonProperty("handle")]
    public string Handle { get; set; } = string.Empty;

    [JsonProperty("requires_shipping")]
    public bool RequiresShipping { get; set; }

    [JsonProperty("product_type")]
    public string? ProductType { get; set; }

    [JsonProperty("product_title")]
    public string ProductTitle { get; set; } = string.Empty;

    [JsonProperty("product_description")]
    public string? ProductDescription { get; set; }

    [JsonProperty("variant_title")]
    public string? VariantTitle { get; set; }

    [JsonProperty("variant_options")]
    public List<string> VariantOptions { get; set; } = new();

    [JsonProperty("options_with_values")]
    public List<CartItemOptionDto> OptionsWithValues { get; set; } = new();

    [JsonProperty("line_level_discount_allocations")]
    public List<object> LineLevelDiscountAllocations { get; set; } = new();

    [JsonProperty("line_level_total_discount")]
    public decimal LineLevelTotalDiscount { get; set; }
}

/// <summary>
/// DTO for cart item images
/// </summary>
public class CartItemImageDto
{
    [JsonProperty("aspect_ratio")]
    public decimal AspectRatio { get; set; }

    [JsonProperty("alt")]
    public string? Alt { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("width")]
    public int Width { get; set; }
}

/// <summary>
/// DTO for cart item options
/// </summary>
public class CartItemOptionDto
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// DTO for adding items to cart
/// </summary>
public class AddToCartDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("properties")]
    public Dictionary<string, string>? Properties { get; set; }
}

/// <summary>
/// DTO for updating cart items
/// </summary>
public class UpdateCartDto
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("quantity")]
    public int? Quantity { get; set; }

    [JsonProperty("line")]
    public int? Line { get; set; }

    [JsonProperty("properties")]
    public Dictionary<string, string>? Properties { get; set; }
}

/// <summary>
/// DTO for cart operations requests
/// </summary>
public class CartOperationDto
{
    [JsonProperty("items")]
    public List<AddToCartDto>? Items { get; set; }

    [JsonProperty("updates")]
    public Dictionary<string, int>? Updates { get; set; }

    [JsonProperty("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }

    [JsonProperty("note")]
    public string? Note { get; set; }
}