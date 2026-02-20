using Microsoft.AspNetCore.Mvc;
using ShoplazzaAddonApp.Services;
using ShoplazzaAddonApp.Models.Dto;
using Microsoft.AspNetCore.Authorization;

namespace ShoplazzaAddonApp.Controllers;

/// <summary>
/// Controller for the merchant dashboard/landing page
/// </summary>
[ApiController]
[Route("[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly IProductAddOnService _productAddOnService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMerchantService merchantService,
        IProductAddOnService productAddOnService,
        ITemplateService templateService,
        ILogger<DashboardController> logger)
    {
        _merchantService = merchantService;
        _productAddOnService = productAddOnService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Serves the main merchant dashboard page
    /// </summary>
    /// <param name="shop">Shop domain from query parameter</param>
    /// <returns>HTML dashboard page</returns>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string shop)
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

            // Generate the dashboard HTML using template
            var dashboardHtml = await GenerateDashboardHtmlAsync(merchant, addOns);

            return Content(dashboardHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving dashboard for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// API endpoint to get merchant dashboard data
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>JSON dashboard data</returns>
    [HttpGet("api/data")]
    [Authorize]
    public async Task<IActionResult> GetDashboardData([FromQuery] string shop)
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

            var addOnsList = addOns.ToList();
            var dashboardData = new
            {
                merchant = new
                {
                    id = merchant.Id,
                    shop = merchant.Shop,
                    isActive = merchant.IsActive,
                    createdAt = merchant.CreatedAt,
                    updatedAt = merchant.UpdatedAt
                },
                addOns = addOnsList.Select(a => new
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
                }).ToList(),
                stats = new
                {
                    totalAddOns = addOnsList.Count,
                    activeAddOns = addOnsList.Count(a => a.IsActive),
                    totalProducts = addOnsList.Select(a => a.ProductId).Distinct().Count()
                }
            };

            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// API endpoint to update merchant settings
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="settings">Settings to update</param>
    /// <returns>Updated settings</returns>
    [HttpPost("api/settings")]
    [Authorize]
    public async Task<IActionResult> UpdateSettings([FromQuery] string shop, [FromBody] object settings)
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

            // TODO: Implement settings update logic
            _logger.LogInformation("Settings update requested for shop: {Shop}", shop);

            return Ok(new { success = true, message = "Settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<string> GenerateDashboardHtmlAsync(Data.Entities.Merchant merchant, IEnumerable<Data.Entities.ProductAddOn> addOns)
    {
        var addOnsList = addOns.ToList();
        var activeAddOns = addOnsList.Count(a => a.IsActive);
        var totalProducts = addOnsList.Select(a => a.ProductId).Distinct().Count();

        // Prepare template variables
        var variables = new Dictionary<string, string>
        {
            { "SHOP_DOMAIN", merchant.Shop },
            { "TOTAL_ADDONS", addOnsList.Count.ToString() },
            { "ACTIVE_ADDONS", activeAddOns.ToString() },
            { "TOTAL_PRODUCTS", totalProducts.ToString() },
            { "APP_STATUS", merchant.IsActive ? "✅" : "❌" },
            { "ADDON_LIST_HTML", GenerateAddOnListHtml(addOnsList) }
        };

        // Load and process template
        return await _templateService.LoadAndProcessTemplateAsync("dashboard/index.html", variables);
    }

    private string GenerateAddOnListHtml(IEnumerable<Data.Entities.ProductAddOn> addOns)
    {
        var addOnsList = addOns.ToList();
        
        if (!addOnsList.Any())
        {
            return @"
                <div class=""empty-state"">
                    <h3>No Add-Ons Configured</h3>
                    <p>Start by creating your first product add-on to increase your revenue!</p>
                </div>";
        }

        return string.Join("", addOnsList.Select(a => $@"
            <div class=""addon-item {(a.IsActive ? "active" : "")}"">
                <div class=""addon-header"">
                    <div class=""addon-title"">{a.AddOnTitle}</div>
                    <div class=""addon-status {(a.IsActive ? "active" : "inactive")}"">
                        {(a.IsActive ? "Active" : "Inactive")}
                    </div>
                </div>
                <div class=""addon-details"">
                    <div><strong>Product:</strong> {a.ProductTitle}</div>
                    <div><strong>Price:</strong> <span class=""addon-price"">${(a.AddOnPriceCents / 100.0m).ToString("F2")}</span></div>
                    <div><strong>SKU:</strong> {a.AddOnSku}</div>
                    <div><strong>Description:</strong> {a.AddOnDescription}</div>
                </div>
            </div>"));
    }
} 