using Newtonsoft.Json;
using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Dto;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service implementation for Shoplazza API integration
/// </summary>
public class ShoplazzaApiService : IShoplazzaApiService
{
    private readonly HttpClient _httpClient;
    private readonly IMerchantService _merchantService;
    private readonly ILogger<ShoplazzaApiService> _logger;
    private readonly IConfiguration _configuration;

    public ShoplazzaApiService(
        HttpClient httpClient,
        IMerchantService merchantService,
        ILogger<ShoplazzaApiService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _merchantService = merchantService;
        _logger = logger;
        _configuration = configuration;
    }

    private string GetApiBase()
    {
        var version = _configuration["Shoplazza:ApiVersion"] ?? "2022-01";
        return $"/openapi/{version}";
    }

    public async Task<ShoplazzaProductDto?> GetProductAsync(Merchant merchant, string productId)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products/{productId}";
            var response = await MakeApiCallAsync<ShoplazzaProductResponse>(merchant, endpoint, HttpMethod.Get);
            return response?.Product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId} for merchant {Shop}", productId, merchant.Shop);
            return null;
        }
    }

    public async Task<IEnumerable<ShoplazzaProductDto>> GetProductsAsync(Merchant merchant, int limit = 50, long? sinceId = null)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products?limit={limit}";
            if (sinceId.HasValue)
            {
                endpoint += $"&since_id={sinceId}";
            }

            var response = await MakeApiCallAsync<ShoplazzaProductsResponse>(merchant, endpoint, HttpMethod.Get);
            return response?.Products ?? new List<ShoplazzaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for merchant {Shop}", merchant.Shop);
            return new List<ShoplazzaProductDto>();
        }
    }

    public async Task<ShoplazzaProductDto?> CreateProductAsync(Merchant merchant, object productData)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products";
            var response = await MakeApiCallAsync<ShoplazzaProductResponse>(merchant, endpoint, HttpMethod.Post, productData);
            return response?.Product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<ShoplazzaProductDto?> UpdateProductAsync(Merchant merchant, string productId, object productData)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products/{productId}";
            var response = await MakeApiCallAsync<ShoplazzaProductResponse>(merchant, endpoint, HttpMethod.Put, productData);
            return response?.Product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId} for merchant {Shop}", productId, merchant.Shop);
            return null;
        }
    }

    public async Task<bool> DeleteProductAsync(Merchant merchant, string productId)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products/{productId}";
            var response = await MakeApiCallAsync<object>(merchant, endpoint, HttpMethod.Delete);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId} for merchant {Shop}", productId, merchant.Shop);
            return false;
        }
    }

    public async Task<ShoplazzaVariantDto?> GetVariantAsync(Merchant merchant, string variantId)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/variants/{variantId}";
            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Get);
            return response?.variant != null ? JsonConvert.DeserializeObject<ShoplazzaVariantDto>(response.variant.ToString()) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variant {VariantId} for merchant {Shop}", variantId, merchant.Shop);
            return null;
        }
    }

    public async Task<ShoplazzaProductDto?> FindProductByHandleAsync(Merchant merchant, string handle)
    {
        try
        {
            // Some Shoplazza versions support querying by handle via products endpoint with handle param
            var endpoint = $"{GetApiBase()}/products?handle={Uri.EscapeDataString(handle)}&limit=1";
            var response = await MakeApiCallAsync<ShoplazzaProductsResponse>(merchant, endpoint, HttpMethod.Get);
            if (response?.Products != null && response.Products.Count > 0)
            {
                return response.Products[0];
            }
            // Fallback: list a page and match client-side
            var list = await GetProductsAsync(merchant, 50, null);
            return list.FirstOrDefault(p => string.Equals(p.Handle, handle, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding product by handle {Handle} for merchant {Shop}", handle, merchant.Shop);
            return null;
        }
    }

    public async Task<ShoplazzaMetafieldDto?> CreateProductMetafieldAsync(Merchant merchant, string productId, object metafield)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products/{productId}/metafields";
            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Post, metafield);
            return response?.metafield != null ? JsonConvert.DeserializeObject<ShoplazzaMetafieldDto>(response.metafield.ToString()) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating metafield for product {ProductId} for merchant {Shop}", productId, merchant.Shop);
            return null;
        }
    }

    public async Task<IEnumerable<ShoplazzaMetafieldDto>> GetProductMetafieldsAsync(Merchant merchant, string productId)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products/{productId}/metafields";
            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Get);
            
            if (response?.metafields != null)
            {
                return JsonConvert.DeserializeObject<List<ShoplazzaMetafieldDto>>(response.metafields.ToString()) ?? new List<ShoplazzaMetafieldDto>();
            }

            return new List<ShoplazzaMetafieldDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metafields for product {ProductId} for merchant {Shop}", productId, merchant.Shop);
            return new List<ShoplazzaMetafieldDto>();
        }
    }

    public async Task<bool> DeleteProductMetafieldAsync(Merchant merchant, string productId, string metafieldId)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/products/{productId}/metafields/{metafieldId}";
            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Delete);
            
            // Shoplazza API typically returns 200 or 204 for successful deletion
            // If we get here without an exception, the deletion was successful
            _logger.LogInformation("Successfully deleted metafield {MetafieldId} from product {ProductId} for merchant {Shop}", 
                metafieldId, productId, merchant.Shop);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metafield {MetafieldId} from product {ProductId} for merchant {Shop}", 
                metafieldId, productId, merchant.Shop);
            return false;
        }
    }

    public async Task<CartDto?> GetCartAsync(Merchant merchant)
    {
        try
        {
            // Note: Cart API typically works in storefront context, not admin API
            // This would be used by the frontend widget
            var endpoint = "/cart.js";
            return await MakeStorefrontApiCallAsync<CartDto>(merchant, endpoint, HttpMethod.Get);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<CartDto?> AddToCartAsync(Merchant merchant, long variantId, int quantity, Dictionary<string, string>? properties = null)
    {
        try
        {
            var endpoint = "/cart/add.js";
            var data = new
            {
                id = variantId,
                quantity = quantity,
                properties = properties
            };

            return await MakeStorefrontApiCallAsync<CartDto>(merchant, endpoint, HttpMethod.Post, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<CartDto?> UpdateCartAsync(Merchant merchant, Dictionary<string, int> updates)
    {
        try
        {
            var endpoint = "/cart/update.js";
            var data = new { updates = updates };

            return await MakeStorefrontApiCallAsync<CartDto>(merchant, endpoint, HttpMethod.Post, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<CartDto?> ChangeCartLineAsync(Merchant merchant, int line, int quantity, Dictionary<string, string>? properties = null)
    {
        try
        {
            var endpoint = "/cart/change.js";
            var data = new
            {
                line = line,
                quantity = quantity,
                properties = properties
            };

            return await MakeStorefrontApiCallAsync<CartDto>(merchant, endpoint, HttpMethod.Post, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing cart line for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<CartDto?> ClearCartAsync(Merchant merchant)
    {
        try
        {
            var endpoint = "/cart/clear.js";
            return await MakeStorefrontApiCallAsync<CartDto>(merchant, endpoint, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<dynamic?> GetStoreInfoAsync(Merchant merchant)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/shop";
            return await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Get);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store info for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<bool> ValidateCredentialsAsync(Merchant merchant)
    {
        try
        {
            var storeInfo = await GetStoreInfoAsync(merchant);
            return storeInfo != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for merchant {Shop}", merchant.Shop);
            return false;
        }
    }

    public async Task<T?> MakeApiCallAsync<T>(Merchant merchant, string endpoint, HttpMethod method, object? data = null) where T : class
    {
        try
        {
            var accessToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("No access token available for merchant {Shop}", merchant.Shop);
                return null;
            }

            var url = $"https://{merchant.Shop}{endpoint}";
            using var request = new HttpRequestMessage(method, url);

            // Shoplazza OpenAPI expects Access-Token header (per docs)
            request.Headers.Add("Access-Token", accessToken);
            request.Headers.Add("Accept", "application/json");

            // Add request body for POST/PUT requests
            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                var json = JsonConvert.SerializeObject(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (typeof(T) == typeof(object))
                {
                    return JsonConvert.DeserializeObject(responseContent) as T;
                }
                
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API call failed for merchant {Shop}. Status: {StatusCode}, Error: {Error}", 
                    merchant.Shop, response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making API call to {Endpoint} for merchant {Shop}", endpoint, merchant.Shop);
            return null;
        }
    }

    /// <summary>
    /// Makes API calls to storefront endpoints (cart operations)
    /// </summary>
    private async Task<T?> MakeStorefrontApiCallAsync<T>(Merchant merchant, string endpoint, HttpMethod method, object? data = null) where T : class
    {
        try
        {
            var url = $"https://{merchant.Shop}{endpoint}";
            using var request = new HttpRequestMessage(method, url);

            request.Headers.Add("Accept", "application/json");

            // Add request body for POST requests
            if (data != null && method == HttpMethod.Post)
            {
                var json = JsonConvert.SerializeObject(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Storefront API call failed for merchant {Shop}. Status: {StatusCode}, Error: {Error}", 
                    merchant.Shop, response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making storefront API call to {Endpoint} for merchant {Shop}", endpoint, merchant.Shop);
            return null;
        }
    }

    public async Task<string?> CreateScriptTagAsync(Merchant merchant, string src, string displayScope = "product", string eventType = "app")
    {
        try
        {
            var endpoint = $"{GetApiBase()}/script_tags_new";
            var data = new
            {
                src = src,
                display_scope = displayScope,
                event_type = eventType
            };

            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Post, data);
            return response?.script_tag?.id ?? response?.id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating script tag for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<IEnumerable<object>> GetScriptTagsAsync(Merchant merchant)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/script_tags_new";
            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Get);

            if (response != null)
            {
                var listJson = response.script_tags ?? response;
                return JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(listJson)) ?? new List<object>();
            }

            return new List<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting script tags for merchant {Shop}", merchant.Shop);
            return new List<object>();
        }
    }

    public async Task<bool> UpdateScriptTagAsync(Merchant merchant, string scriptTagId, string src, string displayScope = "product", string eventType = "app")
    {
        try
        {
            var endpoint = $"{GetApiBase()}/script_tags_new/{scriptTagId}";
            var data = new
            {
                id = scriptTagId,
                src = src,
                display_scope = displayScope,
                event_type = eventType
            };

            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Put, data);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating script tag {ScriptTagId} for merchant {Shop}", scriptTagId, merchant.Shop);
            return false;
        }
    }

    public async Task<bool> DeleteScriptTagAsync(Merchant merchant, string scriptTagId)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/script_tags_new/{scriptTagId}";
            var response = await MakeApiCallAsync<object>(merchant, endpoint, HttpMethod.Delete);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting script tag {ScriptTagId} for merchant {Shop}", scriptTagId, merchant.Shop);
            return false;
        }
    }

    public async Task<bool> RegisterWebhooksAsync(Merchant merchant, string webhookBaseUrl)
    {
        try
        {
            var basePath = GetApiBase();
            _logger.LogInformation("Using OpenAPI base {BasePath} for webhook registration (shop {Shop})", (object)basePath, (object)merchant.Shop);
            var webhooks = new[]
            {
                new { topic = "app/uninstalled", address = $"{webhookBaseUrl}/api/webhooks/app/uninstalled" },
                new { topic = "products/create", address = $"{webhookBaseUrl}/api/webhooks/products/create" },
                new { topic = "products/update", address = $"{webhookBaseUrl}/api/webhooks/products/update" },
                new { topic = "products/delete", address = $"{webhookBaseUrl}/api/webhooks/products/delete" },
                new { topic = "orders/create", address = $"{webhookBaseUrl}/api/webhooks/orders/create" },
                new { topic = "orders/update", address = $"{webhookBaseUrl}/api/webhooks/orders/update" },
                new { topic = "orders/paid", address = $"{webhookBaseUrl}/api/webhooks/orders/paid" }
            };

            var successCount = 0;
            foreach (var webhook in webhooks)
            {
                try
                {
                    var endpoint = $"{basePath}/webhooks";
                    // NOTE: Per working curl, payload should be the plain object { topic, address }
                    var data = webhook;
                    // TEMP VERBOSE LOG
                    _logger.LogInformation("Registering webhook {Topic} at {Endpoint} for shop {Shop}", (object)webhook.topic, (object)endpoint, (object)merchant.Shop);
                    var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Post, data);
                    
                    if (response != null)
                    {
                        successCount++;
                        var snippet = JsonConvert.SerializeObject(response);
                        if (snippet.Length > 300) snippet = snippet.Substring(0, 300) + "...";
                        _logger.LogDebug("Webhook register response for {Topic}: {Snippet}", (object)webhook.topic, (object)snippet);
                        LoggerExtensions.LogInformation(_logger, "Registered webhook {Topic} for merchant {Shop}", webhook.topic, merchant.Shop);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register webhook {Topic} for merchant {Shop}", webhook.topic, merchant.Shop);
                }
            }

            LoggerExtensions.LogInformation(_logger, "Registered {SuccessCount}/{TotalCount} webhooks for merchant {Shop}", 
                successCount, webhooks.Length, merchant.Shop);
            
            return successCount > 0; // Consider successful if at least one webhook was registered
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhooks for merchant {Shop}", merchant.Shop);
            return false;
        }
    }

    public async Task<bool> UnregisterWebhooksAsync(Merchant merchant)
    {
        try
        {
            // Get existing webhooks
            var endpoint = $"{GetApiBase()}/webhooks";
            _logger.LogInformation("Listing webhooks at {Endpoint} for shop {Shop}", (object)endpoint, (object)merchant.Shop);
            var response = await MakeApiCallAsync<dynamic>(merchant, endpoint, HttpMethod.Get);
            
            if (response?.webhooks == null)
            {
                LoggerExtensions.LogInformation(_logger, "No webhooks found for merchant {Shop}", merchant.Shop);
                return true;
            }

            var webhooks = JsonConvert.DeserializeObject<List<dynamic>>(response.webhooks.ToString());
            var listSnippet = JsonConvert.SerializeObject(webhooks);
            if (listSnippet.Length > 300) listSnippet = listSnippet.Substring(0, 300) + "...";
            _logger.LogDebug("Webhook list response snippet: {Snippet}", (object)listSnippet);
            var successCount = 0;

            foreach (var webhook in webhooks)
            {
                try
                {
                    var webhookId = webhook.id?.ToString();
                    if (!string.IsNullOrEmpty(webhookId))
                    {
                        var deleteEndpoint = $"{GetApiBase()}/webhooks/{webhookId}";
                        _logger.LogInformation("Deleting webhook {WebhookId} at {Endpoint} for shop {Shop}", (object)(webhookId ?? string.Empty), (object)deleteEndpoint, (object)merchant.Shop);
                        var deleteResponse = await MakeApiCallAsync<object>(merchant, deleteEndpoint, HttpMethod.Delete);
                        
                        if (deleteResponse != null)
                        {
                            var delSnippet = JsonConvert.SerializeObject(deleteResponse);
                            if (delSnippet.Length > 300) delSnippet = delSnippet.Substring(0, 300) + "...";
                            _logger.LogDebug("Webhook delete response for {WebhookId}: {Snippet}", (object)(webhookId ?? string.Empty), (object)delSnippet);
                            successCount++;
                            LoggerExtensions.LogInformation(_logger, "Unregistered webhook {WebhookId} for merchant {Shop}", webhookId, merchant.Shop);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to unregister webhook for merchant {Shop}", merchant.Shop);
                }
            }

            LoggerExtensions.LogInformation(_logger, "Unregistered {SuccessCount}/{TotalCount} webhooks for merchant {Shop}", 
                successCount, webhooks.Count, merchant.Shop);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering webhooks for merchant {Shop}", merchant.Shop);
            return false;
        }
    }

   
}