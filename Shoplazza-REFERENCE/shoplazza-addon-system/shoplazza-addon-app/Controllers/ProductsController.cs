using Microsoft.AspNetCore.Mvc;
using ShoplazzaAddonApp.Services;

namespace ShoplazzaAddonApp.Controllers;

/// <summary>
/// API controller for product management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductAddOnService _productAddOnService;
    private readonly IMerchantService _merchantService;
    private readonly IShoplazzaApiService _shoplazzaApiService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductAddOnService productAddOnService,
        IMerchantService merchantService,
        IShoplazzaApiService shoplazzaApiService,
        ILogger<ProductsController> logger)
    {
        _productAddOnService = productAddOnService;
        _merchantService = merchantService;
        _shoplazzaApiService = shoplazzaApiService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all product add-ons for a merchant
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>List of product add-ons</returns>
    [HttpGet]
    public async Task<IActionResult> GetAddOns([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            // Update last login
            await _merchantService.UpdateLastLoginAsync(shop);

            var addOns = await _productAddOnService.GetMerchantAddOnsAsync(merchant.Id);

            return Ok(new
            {
                shop = shop,
                addOns = addOns.Select(a => new
                {
                    id = a.Id,
                    productId = a.ProductId,
                    productTitle = a.ProductTitle,
                    productHandle = a.ProductHandle,
                    isEnabled = a.IsEnabled,
                    isActive = a.IsActive,
                    addOn = new
                    {
                        title = a.AddOnTitle,
                        description = a.AddOnDescription,
                        price = a.FormattedPrice,
                        priceCents = a.AddOnPriceCents,
                        currency = a.Currency,
                        displayText = a.DisplayText,
                        sku = a.AddOnSku,
                        requiresShipping = a.RequiresShipping,
                        weightGrams = a.WeightGrams,
                        isTaxable = a.IsTaxable,
                        imageUrl = a.ImageUrl,
                        position = a.Position
                    },
                    createdAt = a.CreatedAt,
                    updatedAt = a.UpdatedAt
                }),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting add-ons for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific product add-on
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Product add-on details</returns>
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetAddOn([FromQuery] string shop, string productId)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var addOn = await _productAddOnService.GetProductAddOnAsync(merchant.Id, productId);
            if (addOn == null)
            {
                return NotFound(new { error = "Product add-on not found" });
            }

            return Ok(new
            {
                id = addOn.Id,
                productId = addOn.ProductId,
                productTitle = addOn.ProductTitle,
                productHandle = addOn.ProductHandle,
                isEnabled = addOn.IsEnabled,
                isActive = addOn.IsActive,
                addOn = new
                {
                    title = addOn.AddOnTitle,
                    description = addOn.AddOnDescription,
                    price = addOn.FormattedPrice,
                    priceCents = addOn.AddOnPriceCents,
                    currency = addOn.Currency,
                    displayText = addOn.DisplayText,
                    sku = addOn.AddOnSku,
                    requiresShipping = addOn.RequiresShipping,
                    weightGrams = addOn.WeightGrams,
                    isTaxable = addOn.IsTaxable,
                    imageUrl = addOn.ImageUrl,
                    position = addOn.Position,
                    addOnProductId = addOn.AddOnProductId,
                    addOnVariantId = addOn.AddOnVariantId
                },
                createdAt = addOn.CreatedAt,
                updatedAt = addOn.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting add-on for shop: {Shop}, product: {ProductId}", shop, productId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates or updates a product add-on
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Add-on configuration</param>
    /// <returns>Created or updated add-on</returns>
        [HttpPost("{productId}")]
    public async Task<IActionResult> CreateOrUpdateAddOn(
        [FromQuery] string shop, 
            string productId, 
        [FromBody] ProductAddOnConfigDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            // Check for demo mode - bypass product validation for demo requests
            var isDemoMode = IsDemoRequest();
            
            if (!isDemoMode)
            {
                // Validate that the product exists in Shoplazza for production
                var productExists = await _productAddOnService.ValidateProductExistsAsync(merchant, productId);
                if (!productExists)
                {
                    return BadRequest(new { error = "Product not found in Shoplazza" });
                }
            }
            else
            {
                _logger.LogInformation("Demo mode detected - bypassing product validation for shop: {Shop}, product: {ProductId}", shop, productId);
            }

            var addOn = await _productAddOnService.CreateOrUpdateAddOnAsync(merchant.Id, productId, request);

            if (!isDemoMode)
            {
                // Sync product info from Shoplazza for production
                await _productAddOnService.SyncProductInfoAsync(merchant, productId);
            }
            else
            {
                _logger.LogInformation("Demo mode - skipping product sync for shop: {Shop}, product: {ProductId}", shop, productId);
            }

            _logger.LogInformation("Created/updated add-on for shop: {Shop}, product: {ProductId}", shop, productId);

            return Ok(new
            {
                message = "Add-on configuration saved successfully",
                id = addOn.Id,
                productId = addOn.ProductId,
                isEnabled = addOn.IsEnabled,
                updatedAt = addOn.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating add-on for shop: {Shop}, product: {ProductId}", shop, productId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Toggles add-on enabled status
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Toggle request</param>
    /// <returns>Success response</returns>
        [HttpPatch("{productId}/toggle")]
    public async Task<IActionResult> ToggleAddOn(
        [FromQuery] string shop, 
            string productId, 
        [FromBody] ToggleAddOnRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var success = await _productAddOnService.ToggleAddOnAsync(merchant.Id, productId, request.IsEnabled);
            if (!success)
            {
                return NotFound(new { error = "Product add-on not found" });
            }

            _logger.LogInformation("Toggled add-on {Status} for shop: {Shop}, product: {ProductId}", 
                request.IsEnabled ? "enabled" : "disabled", shop, productId);

            return Ok(new
            {
                message = $"Add-on {(request.IsEnabled ? "enabled" : "disabled")} successfully",
                productId = productId,
                isEnabled = request.IsEnabled,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling add-on for shop: {Shop}, product: {ProductId}", shop, productId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a product add-on
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Success response</returns>
        [HttpDelete("{productId}")]
    public async Task<IActionResult> DeleteAddOn([FromQuery] string shop, string productId)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var success = await _productAddOnService.DeleteAddOnAsync(merchant.Id, productId);
            if (!success)
            {
                return NotFound(new { error = "Product add-on not found" });
            }

            _logger.LogInformation("Deleted add-on for shop: {Shop}, product: {ProductId}", shop, productId);

            return Ok(new
            {
                message = "Add-on deleted successfully",
                productId = productId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting add-on for shop: {Shop}, product: {ProductId}", shop, productId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets Shoplazza products for a merchant
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="limit">Number of products to retrieve</param>
    /// <param name="sinceId">Retrieve products after this ID</param>
    /// <returns>List of products</returns>
    [HttpGet("browse")]
    public async Task<IActionResult> BrowseProducts(
        [FromQuery] string shop, 
        [FromQuery] int limit = 50, 
        [FromQuery] long? sinceId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var products = await _shoplazzaApiService.GetProductsAsync(merchant, Math.Min(limit, 250), sinceId);

            return Ok(new
            {
                shop = shop,
                products = products.Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    handle = p.Handle,
                    status = p.Status,
                    productType = p.ProductType,
                    vendor = p.Vendor,
                    createdAt = p.CreatedAt,
                    updatedAt = p.UpdatedAt,
                    variants = p.Variants.Select(v => new
                    {
                        id = v.Id,
                        title = v.Title,
                        price = v.Price,
                        sku = v.Sku
                    }),
                    image = p.Images.FirstOrDefault()?.Src
                }),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing products for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Synchronizes product information from Shoplazza
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID to sync (optional, syncs all if not provided)</param>
    /// <returns>Sync result</returns>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncProducts([FromQuery] string shop, [FromQuery] string? productId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            if (!string.IsNullOrEmpty(productId))
            {
                // Sync single product
                var success = await _productAddOnService.SyncProductInfoAsync(merchant, productId);
                return Ok(new
                {
                    message = success ? "Product synchronized successfully" : "Product sync failed",
                    productId = productId,
                    success = success,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                // Bulk sync all products
                var syncCount = await _productAddOnService.BulkSyncProductsAsync(merchant);
                return Ok(new
                {
                    message = "Bulk product sync completed",
                    syncedCount = syncCount,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing products for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets add-on statistics for a merchant
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>Add-on statistics</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Shop parameter is required" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var stats = await _productAddOnService.GetAddOnStatsAsync(merchant.Id);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if the current request is from demo mode
    /// </summary>
    /// <returns>True if request is from demo mode</returns>
    private bool IsDemoRequest()
    {
        var request = HttpContext.Request;
        
        // Check for demo indicators
        var isDemoShop = request.Query["shop"].ToString().Contains("demo-store") ||
                        request.Headers["X-Demo-Mode"].Any() ||
                        request.Headers["User-Agent"].ToString().Contains("Demo");
        
        // Check if request is from localhost (development)
        var isLocalhost = HttpContext.Request.Host.Host.Contains("localhost") ||
                         HttpContext.Request.Host.Host.Contains("127.0.0.1");
        
        // Check for demo signature header
        var hasDemoSignature = request.Headers["X-Shoplazza-Hmac-Sha256"].ToString() == "demo-signature" ||
                              request.Headers["X-Shoplazza-Hmac-Sha256"].ToString() == "demo-hmac-signature";
        
        // Allow demo requests in development environment
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        return (isDemoShop || hasDemoSignature) && (isLocalhost || isDevelopment);
    }
}

/// <summary>
/// Request model for toggling add-on status
/// </summary>
public class ToggleAddOnRequest
{
    public bool IsEnabled { get; set; }
}