using Microsoft.AspNetCore.Mvc;
using ShoplazzaAddonApp.Services;
using ShoplazzaAddonApp.Models.Dto;
using Microsoft.AspNetCore.Authorization;

namespace ShoplazzaAddonApp.Controllers;

/// <summary>
/// Controller for merchant configuration and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MerchantController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly IProductAddOnService _productAddOnService;
    private readonly ITemplateService _templateService;
    private readonly IShoplazzaApiService _shoplazzaApiService;
    private readonly ILogger<MerchantController> _logger;

    public MerchantController(
        IMerchantService merchantService,
        IProductAddOnService productAddOnService,
        ITemplateService templateService,
        IShoplazzaApiService shoplazzaApiService,
        ILogger<MerchantController> logger)
    {
        _merchantService = merchantService;
        _productAddOnService = productAddOnService;
        _templateService = templateService;
        _shoplazzaApiService = shoplazzaApiService;
        _logger = logger;
    }

    /// <summary>
    /// Serves the merchant configuration page
    /// </summary>
    /// <param name="shop">Shop domain from query parameter</param>
    /// <returns>HTML configuration page</returns>
    [HttpGet("config")]
    public async Task<IActionResult> ConfigPage([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest("Shop parameter is required");
            }

            // Get merchant information
            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound("Merchant not found. Please install the app first.");
            }

            // Check if merchant is active
            if (!merchant.IsActive)
            {
                return BadRequest("Merchant account is inactive. Please contact support.");
            }

            // Get merchant's add-on configurations
            var addOns = await _productAddOnService.GetAllProductAddOnsAsync(merchant.Id);

            // Generate the configuration page HTML using template
            var configHtml = await GenerateConfigPageHtmlAsync(merchant, addOns);

            return Content(configHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving config page for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get merchant's products from Shoplazza
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>List of products</returns>
    [HttpGet("products")]
    [Authorize]
    public async Task<IActionResult> GetProducts([FromQuery] string shop, [FromQuery] int limit = 50, [FromQuery] long? sinceId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest("Shop parameter is required");
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound("Merchant not found");
            }

            var items = await _shoplazzaApiService.GetProductsAsync(merchant, Math.Clamp(limit, 1, 250), sinceId);

            var products = items.Select(p => new
            {
                id = p.Id,
                title = p.Title,
                status = p.Status,
                handle = p.Handle,
                image = p.Images.FirstOrDefault()?.Src,
                variantCount = p.Variants?.Count ?? 0
            }).ToList();

            string? nextSinceId = products.Count > 0 ? products.Max(p => p.id) : null;
            return Ok(new { products, nextSinceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a single product (with variants) from Shoplazza
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Product detail</returns>
    [HttpGet("products/{productId}")]
    [Authorize]
    public async Task<IActionResult> GetProduct([FromQuery] string shop, string productId)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest("Shop parameter is required");
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound("Merchant not found");
            }

            var product = await _shoplazzaApiService.GetProductAsync(merchant, productId);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            var result = new
            {
                id = product.Id,
                title = product.Title,
                handle = product.Handle,
                status = product.Status,
                images = product.Images?.Select(i => i.Src).ToList() ?? new List<string>(),
                variants = (product.Variants ?? new List<ShoplazzaVariantDto>()).Select(v => new
                {
                    id = v.Id,
                    title = string.IsNullOrEmpty(v.Title) ? "Default" : v.Title,
                    price = v.Price,
                    sku = v.Sku
                }).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId} for shop: {Shop}", productId, shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get merchant's add-on configurations
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>List of add-on configurations</returns>
    [HttpGet("addons")]
    [Authorize]
    public async Task<IActionResult> GetAddOns([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest("Shop parameter is required");
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound("Merchant not found");
            }

            var addOns = await _productAddOnService.GetAllProductAddOnsAsync(merchant.Id);

            var addOnData = addOns.Select(a => new
            {
                id = a.Id,
                productId = a.ProductId,
                productTitle = a.ProductTitle,
                isActive = a.IsActive,
                addOnTitle = a.AddOnTitle,
                addOnDescription = a.AddOnDescription,
                addOnPriceCents = a.AddOnPriceCents,
                currency = a.Currency,
                sku = a.AddOnSku,
                createdAt = a.CreatedAt,
                updatedAt = a.UpdatedAt
            }).ToList();

            return Ok(new { addOns = addOnData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting add-ons for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create or update an add-on configuration
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <param name="addOnConfig">Add-on configuration</param>
    /// <returns>Updated add-on configuration</returns>
    [HttpPost("addons/{productId}")]
    [Authorize]
    public async Task<IActionResult> CreateOrUpdateAddOn([FromQuery] string shop, string productId, [FromBody] ProductAddOnConfigDto addOnConfig)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest("Shop parameter is required");
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound("Merchant not found");
            }

            // Create or update the add-on configuration
            var addOn = await _productAddOnService.CreateOrUpdateAddOnAsync(merchant.Id, productId, addOnConfig);

            var result = new
            {
                id = addOn.Id,
                productId = addOn.ProductId,
                productTitle = addOn.ProductTitle,
                isActive = addOn.IsActive,
                addOnTitle = addOn.AddOnTitle,
                addOnDescription = addOn.AddOnDescription,
                addOnPriceCents = addOn.AddOnPriceCents,
                currency = addOn.Currency,
                sku = addOn.AddOnSku,
                createdAt = addOn.CreatedAt,
                updatedAt = addOn.UpdatedAt
            };

            return Ok(new { success = true, addOn = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating add-on for shop: {Shop}, product: {ProductId}", shop, productId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an add-on configuration
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("addons/{productId}")]
    [Authorize]
    public async Task<IActionResult> DeleteAddOn([FromQuery] string shop, string productId)
    {
        try
        {
            _logger.LogInformation("🔄 Delete add-on request received for shop: {Shop}, product: {ProductId}", shop, productId);
            
            if (string.IsNullOrEmpty(shop))
            {
                _logger.LogWarning("❌ Delete add-on request missing shop parameter for product: {ProductId}", productId);
                return BadRequest("Shop parameter is required");
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                _logger.LogWarning("❌ Merchant not found for shop: {Shop} during add-on deletion", shop);
                return NotFound("Merchant not found");
            }

            _logger.LogInformation("✅ Found merchant for shop: {Shop}, ID: {MerchantId} during add-on deletion", 
                shop, merchant.Id);

            // Delete the add-on configuration
            var deleteResult = await _productAddOnService.DeleteAddOnAsync(merchant.Id, productId);
            
            if (deleteResult)
            {
                _logger.LogInformation("🎉 Successfully deleted add-on for shop: {Shop}, product: {ProductId}", shop, productId);
                return Ok(new { success = true, message = "Add-on configuration deleted successfully" });
            }
            else
            {
                _logger.LogWarning("⚠️ Add-on deletion returned false for shop: {Shop}, product: {ProductId}", shop, productId);
                return StatusCode(500, "Add-on deletion failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting add-on for shop: {Shop}, product: {ProductId}. Exception: {ExceptionMessage}", 
                shop, productId, ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<string> GenerateConfigPageHtmlAsync(Data.Entities.Merchant merchant, IEnumerable<Data.Entities.ProductAddOn> addOns)
    {
        var addOnsList = addOns.ToList();

        // Prepare template variables
        var variables = new Dictionary<string, string>
        {
            { "SHOP_DOMAIN", merchant.Shop },
            { "ADDON_LIST_HTML", GenerateAddOnListHtml(addOnsList) }
        };

        // Load and process template
        return await _templateService.LoadAndProcessTemplateAsync("merchant/config.html", variables);
    }

    private string GenerateAddOnListHtml(IEnumerable<Data.Entities.ProductAddOn> addOns)
    {
        var addOnsList = addOns.ToList();
        
        if (!addOnsList.Any())
        {
            return @"
                <div class=""empty-state"">
                    <h3>No Add-Ons Configured</h3>
                    <p>Create your first add-on above to get started!</p>
                </div>";
        }

        return string.Join("", addOnsList.Select(a => $@"
            <div class=""addon-item {(a.IsActive ? "active" : "")}"" data-product-id=""{a.ProductId}"">
                <div class=""addon-header"">
                    <div class=""addon-title"">{a.AddOnTitle}</div>
                    <div class=""addon-actions"">
                        <button class=""btn btn-success"" onclick=""editAddOn({a.ProductId})"">Edit</button>
                        <button class=""btn btn-danger"" onclick=""deleteAddOn({a.ProductId})"">Delete</button>
                    </div>
                </div>
                <div class=""addon-details"">
                    <div><strong>Product ID:</strong> {a.ProductId}</div>
                    <div><strong>Product:</strong> {a.ProductTitle}</div>
                    <div><strong>Price:</strong> <span class=""addon-price"">${a.AddOnPriceCents / 100.0m:F2}</span></div>
                    <div><strong>SKU:</strong> {a.AddOnSku}</div>
                    <div><strong>Status:</strong> {(a.IsActive ? "Active" : "Inactive")}</div>
                    <div><strong>Description:</strong> {a.AddOnDescription}</div>
                </div>
            </div>"));
    }
} 