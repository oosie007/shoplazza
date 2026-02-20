using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Dto;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service interface for Shoplazza API integration
/// </summary>
public interface IShoplazzaApiService
{
    /// <summary>
    /// Gets a product from Shoplazza by ID
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Product data if found</returns>
    Task<ShoplazzaProductDto?> GetProductAsync(Merchant merchant, string productId);

    /// <summary>
    /// Gets multiple products from Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="limit">Number of products to retrieve (max 250)</param>
    /// <param name="sinceId">Retrieve products after this ID</param>
    /// <returns>List of products</returns>
    Task<IEnumerable<ShoplazzaProductDto>> GetProductsAsync(Merchant merchant, int limit = 50, long? sinceId = null);

    /// <summary>
    /// Creates a new product in Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productData">Product data</param>
    /// <returns>Created product</returns>
    Task<ShoplazzaProductDto?> CreateProductAsync(Merchant merchant, object productData);

    /// <summary>
    /// Updates a product in Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID</param>
    /// <param name="productData">Updated product data</param>
    /// <returns>Updated product</returns>
    Task<ShoplazzaProductDto?> UpdateProductAsync(Merchant merchant, string productId, object productData);

    /// <summary>
    /// Deletes a product from Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteProductAsync(Merchant merchant, string productId);

    /// <summary>
    /// Gets a variant by ID
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="variantId">Variant ID</param>
    /// <returns>Variant data if found</returns>
    Task<ShoplazzaVariantDto?> GetVariantAsync(Merchant merchant, string variantId);

    /// <summary>
    /// Finds a product by handle (slug)
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="handle">Product handle (slug)</param>
    /// <returns>Product if found</returns>
    Task<ShoplazzaProductDto?> FindProductByHandleAsync(Merchant merchant, string handle);

    /// <summary>
    /// Creates a metafield for a product
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID</param>
    /// <param name="metafield">Metafield data</param>
    /// <returns>Created metafield</returns>
    Task<ShoplazzaMetafieldDto?> CreateProductMetafieldAsync(Merchant merchant, string productId, object metafield);

    /// <summary>
    /// Gets metafields for a product
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID</param>
    /// <returns>List of metafields</returns>
    Task<IEnumerable<ShoplazzaMetafieldDto>> GetProductMetafieldsAsync(Merchant merchant, string productId);

    /// <summary>
    /// Deletes a metafield from a product
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID</param>
    /// <param name="metafieldId">Metafield ID to delete</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteProductMetafieldAsync(Merchant merchant, string productId, string metafieldId);

    /// <summary>
    /// Gets the current cart contents
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>Cart contents</returns>
    Task<CartDto?> GetCartAsync(Merchant merchant);

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="variantId">Variant ID to add</param>
    /// <param name="quantity">Quantity to add</param>
    /// <param name="properties">Line item properties</param>
    /// <returns>Updated cart</returns>
    Task<CartDto?> AddToCartAsync(Merchant merchant, long variantId, int quantity, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Updates cart items
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="updates">Updates to apply</param>
    /// <returns>Updated cart</returns>
    Task<CartDto?> UpdateCartAsync(Merchant merchant, Dictionary<string, int> updates);

    /// <summary>
    /// Changes a cart line item
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="line">Line number (1-based)</param>
    /// <param name="quantity">New quantity (0 to remove)</param>
    /// <param name="properties">Line item properties</param>
    /// <returns>Updated cart</returns>
    Task<CartDto?> ChangeCartLineAsync(Merchant merchant, int line, int quantity, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Clears the cart
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>Empty cart</returns>
    Task<CartDto?> ClearCartAsync(Merchant merchant);

    /// <summary>
    /// Gets store information
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>Store information</returns>
    Task<dynamic?> GetStoreInfoAsync(Merchant merchant);

    /// <summary>
    /// Validates API credentials
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>True if credentials are valid</returns>
    Task<bool> ValidateCredentialsAsync(Merchant merchant);

    /// <summary>
    /// Makes a generic API call to Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="method">HTTP method</param>
    /// <param name="data">Request data</param>
    /// <returns>API response</returns>
    Task<T?> MakeApiCallAsync<T>(Merchant merchant, string endpoint, HttpMethod method, object? data = null) where T : class;

    /// <summary>
    /// Creates a script tag in Shoplazza store
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="src">Script source URL (must be HTTPS)</param>
    /// <param name="displayScope">Display scope for where the script appears (e.g., "all", "index")</param>
    /// <param name="eventType">Optional tag type identifier. Defaults to "app" per API</param>
    /// <returns>Script tag ID if successful</returns>
    Task<string?> CreateScriptTagAsync(Merchant merchant, string src, string displayScope = "index", string eventType = "app");

    /// <summary>
    /// Gets all script tags for a store
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>List of script tags</returns>
    Task<IEnumerable<object>> GetScriptTagsAsync(Merchant merchant);

    /// <summary>
    /// Updates a script tag
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="scriptTagId">Script tag ID</param>
    /// <param name="src">New script source URL</param>
    /// <param name="displayScope">Display scope for where the script appears</param>
    /// <param name="eventType">Optional tag type identifier</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateScriptTagAsync(Merchant merchant, string scriptTagId, string src, string displayScope = "index", string eventType = "app");

    /// <summary>
    /// Deletes a script tag from Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="scriptTagId">Script tag ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteScriptTagAsync(Merchant merchant, string scriptTagId);

    /// <summary>
    /// Registers webhooks for a merchant
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="webhookBaseUrl">Base URL for webhook endpoints</param>
    /// <returns>True if successful</returns>
    Task<bool> RegisterWebhooksAsync(Merchant merchant, string webhookBaseUrl);

    /// <summary>
    /// Unregisters webhooks for a merchant
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>True if successful</returns>
    Task<bool> UnregisterWebhooksAsync(Merchant merchant);
}