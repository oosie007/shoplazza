using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShoplazzaAddonApp.Models.Dto;
using ShoplazzaAddonApp.Services;

namespace ShoplazzaAddonApp.Controllers;

/// <summary>
/// Controller for handling Shoplazza webhooks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly IProductAddOnService _productAddOnService;
    private readonly IShoplazzaApiService _shoplazzaApiService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IMerchantService merchantService,
        IProductAddOnService productAddOnService,
        IShoplazzaApiService shoplazzaApiService,
        ILogger<WebhooksController> logger)
    {
        _merchantService = merchantService;
        _productAddOnService = productAddOnService;
        _shoplazzaApiService = shoplazzaApiService;
        _logger = logger;
    }

    /// <summary>
    /// Handles product creation webhooks
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("products/create")]
    public async Task<IActionResult> ProductCreated([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var product = JsonConvert.DeserializeObject<ShoplazzaProductDto>(body);

            if (product == null)
            {
                _logger.LogWarning("Failed to parse product creation webhook body");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            _logger.LogInformation("Product created webhook received for shop {Shop}, product {ProductId}: {ProductTitle}", 
                shop, product.Id, product.Title);

            // Log the webhook for future processing if needed
            // We don't automatically create add-ons for new products
            
            return Ok(new { message = "Webhook received successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product created webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles product update webhooks
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("products/update")]
    public async Task<IActionResult> ProductUpdated([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var product = JsonConvert.DeserializeObject<ShoplazzaProductDto>(body);

            if (product == null)
            {
                _logger.LogWarning("Failed to parse product update webhook body");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            _logger.LogInformation("Product updated webhook received for shop {Shop}, product {ProductId}: {ProductTitle}", 
                shop, product.Id, product.Title);

            // Find the merchant
            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant != null)
            {
                // Check if this product has an add-on configuration
                var addOn = await _productAddOnService.GetProductAddOnAsync(merchant.Id, product.Id);
                if (addOn != null)
                {
                    await _productAddOnService.SyncProductInfoAsync(merchant, product.Id);
                    _logger.LogInformation("Updated add-on product info for product {ProductId} in shop {Shop}", 
                        product.Id, shop);
                }
            }

            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product updated webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles product deletion webhooks
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("products/delete")]
    public async Task<IActionResult> ProductDeleted([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var productData = JsonConvert.DeserializeObject<dynamic>(body);

            if (productData?.id == null)
            {
                _logger.LogWarning("Failed to parse product deletion webhook body");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            string productIdStr = (productData.id != null) ? productData.id.ToString() : string.Empty;

            _logger.LogInformation("Product deleted webhook received for shop {Shop}, product {ProductId}", 
                shop, productIdStr);

            // Find the merchant
            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant != null)
            {
                // Check if this product has an add-on configuration and remove it
                var success = await _productAddOnService.DeleteAddOnAsync(merchant.Id, productIdStr);
                if (success)
                {
                    _logger.LogInformation("Removed add-on configuration for deleted product {ProductId} in shop {Shop}", 
                        productIdStr, shop);
                }
            }

            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product deleted webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles app installation webhook from Shoplazza
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("app/installed")]
    public async Task<IActionResult> AppInstalled([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            LoggerExtensions.LogInformation(_logger, "App installed webhook received for shop: {Shop}", shop);

            if (IsDemoRequest())
            {
                LoggerExtensions.LogInformation(_logger, "Demo mode - simulating app installation for shop: {Shop}", shop);
                return Ok(new { success = true, message = "App installation webhook processed (demo)" });
            }

            // TODO: Process app installation
            // - Initialize default settings for merchant
            // - Set up initial configurations
            // - Send welcome emails or setup guides

            return Ok(new { success = true, message = "App installation webhook processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing app installation webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles app uninstallation webhooks
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("app/uninstalled")]
    public async Task<IActionResult> AppUninstalled([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            _logger.LogInformation("App uninstalled webhook received for shop {Shop}", shop);

            // Get merchant before marking as uninstalled
            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            
            if (merchant != null)
            {
                // Clean up webhooks
                try
                {
                    var webhookSuccess = await _shoplazzaApiService.UnregisterWebhooksAsync(merchant);
                    if (webhookSuccess)
                    {
                        _logger.LogInformation("Webhooks unregistered successfully for merchant: {Shop}", shop);
                    }
                    else
                    {
                        // This is expected during uninstall - app access is revoked
                        _logger.LogInformation("Webhook cleanup skipped for merchant: {Shop} (app access revoked)", shop);
                    }
                }
                catch (Exception ex)
                {
                    // This is expected during uninstall - app access is revoked
                    _logger.LogInformation("Webhook cleanup failed for merchant: {Shop} (app access revoked): {Message}", shop, ex.Message);
                }

                // Clean up script tag
                if (!string.IsNullOrEmpty(merchant.ScriptTagId))
                {
                    try
                    {
                        var scriptTagSuccess = await _shoplazzaApiService.DeleteScriptTagAsync(merchant, merchant.ScriptTagId!);
                        if (scriptTagSuccess)
                        {
                            _logger.LogInformation("Script tag deleted successfully for merchant: {Shop}", shop);
                        }
                        else
                        {
                            // This is expected during uninstall - app access is revoked
                            _logger.LogInformation("Script tag cleanup skipped for merchant: {Shop} (app access revoked)", shop);
                        }
                    }
                    catch (Exception ex)
                    {
                        // This is expected during uninstall - app access is revoked
                        _logger.LogInformation("Script tag cleanup failed for merchant: {Shop} (app access revoked): {Message}", shop, ex.Message);
                    }
                }
            }

            // Completely remove all merchant data
            var success = await _merchantService.RemoveMerchantDataAsync(shop);
            if (success)
            {
                _logger.LogInformation("Completely removed all data for uninstalled merchant: {Shop}", shop);
            }

            return Ok(new { message = "App uninstallation processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing app uninstalled webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles order creation webhooks (for analytics)
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("orders/create")]
    public async Task<IActionResult> OrderCreated([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var order = JsonConvert.DeserializeObject<dynamic>(body);

            if (order == null)
            {
                _logger.LogWarning("Failed to parse order creation webhook body");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            var orderId = order.id?.ToString() ?? "unknown";
            LoggerExtensions.LogInformation(_logger, "Order created webhook received for shop {Shop}, order {OrderId}", 
                shop, orderId);

            // TODO: Process order for add-on analytics
            // - Check if order contains add-on items
            // - Update revenue and conversion statistics
            // - Track add-on performance

            return Ok(new { message = "Webhook received successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order created webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles order update webhook from Shoplazza
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("orders/update")]
    public async Task<IActionResult> OrderUpdated([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var order = JsonConvert.DeserializeObject<dynamic>(body);

            if (order == null)
            {
                _logger.LogWarning("Failed to parse order update webhook body");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            var orderId = order.id?.ToString() ?? "unknown";
            LoggerExtensions.LogInformation(_logger, "Order updated webhook received for shop {Shop}, order {OrderId}", 
                shop, orderId);

            // TODO: Process order update
            // - Update order status in analytics
            // - Track order lifecycle changes

            return Ok(new { success = true, message = "Order update webhook processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order update webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles order paid webhook from Shoplazza
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("orders/paid")]
    public async Task<IActionResult> OrderPaid([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var order = JsonConvert.DeserializeObject<dynamic>(body);

            if (order == null)
            {
                _logger.LogWarning("Failed to parse order paid webhook body");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            var orderId = order.id?.ToString() ?? "unknown";
            LoggerExtensions.LogInformation(_logger, "Order paid webhook received for shop {Shop}, order {OrderId}", 
                shop, orderId);

            // TODO: Process order payment
            // - Finalize add-on revenue tracking
            // - Update conversion analytics
            // - Trigger post-purchase workflows

            return Ok(new { success = true, message = "Order paid webhook processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order paid webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles cart updates webhooks (for real-time analytics)
    /// </summary>
    /// <param name="shop">Shop domain from headers</param>
    /// <returns>Webhook response</returns>
    [HttpPost("carts/update")]
    public async Task<IActionResult> CartUpdated([FromHeader(Name = "X-Shoplazza-Shop-Domain")] string shop)
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var cart = JsonConvert.DeserializeObject<CartDto>(body);

            if (cart == null)
            {
                _logger.LogWarning("Failed to parse cart update webhook body");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            _logger.LogDebug("Cart updated webhook received for shop {Shop} with {ItemCount} items", 
                shop, cart.ItemCount);

            // TODO: Process cart updates for add-on analytics
            // - Track add-on additions/removals
            // - Monitor conversion funnel
            // - Update real-time statistics

            return Ok(new { message = "Webhook received successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cart updated webhook for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generic webhook endpoint for testing
    /// </summary>
    /// <returns>Webhook response</returns>
    [HttpPost("test")]
    public async Task<IActionResult> TestWebhook()
    {
        try
        {
            var body = await ReadRequestBodyAsync();
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

            _logger.LogInformation("Test webhook received with body length: {BodyLength}", body.Length);
            _logger.LogDebug("Test webhook headers: {Headers}", JsonConvert.SerializeObject(headers));

            return Ok(new
            {
                message = "Test webhook received successfully",
                bodyLength = body.Length,
                headers = headers,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing test webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Reads the request body as a string
    /// </summary>
    /// <returns>Request body content</returns>
    private async Task<string> ReadRequestBodyAsync()
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;
        
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        
        Request.Body.Position = 0;
        return body;
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