using Microsoft.AspNetCore.Mvc;
using ShoplazzaAddonApp.Services;
using System.Text;

namespace ShoplazzaAddonApp.Controllers;

/// <summary>
/// Controller for serving dynamic widget scripts to Shoplazza storefronts
/// </summary>
[ApiController]
[Route("api/[controller]")]
    public class WidgetController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly IProductAddOnService _productAddOnService;
        private readonly IShoplazzaApiService _shoplazzaApiService;
    private readonly ILogger<WidgetController> _logger;

    public WidgetController(
        IMerchantService merchantService,
            IProductAddOnService productAddOnService,
            IShoplazzaApiService shoplazzaApiService,
            ILogger<WidgetController> logger)
    {
        _merchantService = merchantService;
        _productAddOnService = productAddOnService;
            _shoplazzaApiService = shoplazzaApiService;
        _logger = logger;
    }

    /// <summary>
    /// Serves the dynamic widget JavaScript file
    /// </summary>
    /// <param name="shop">Shop domain (from query parameter)</param>
    /// <returns>JavaScript content</returns>
    [HttpGet("widget.js")]
    public async Task<IActionResult> GetWidget([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                _logger.LogWarning("Widget request without shop parameter");
                return BadRequest("Shop parameter is required");
            }

            // Check if this is a demo request
            var isDemoRequest = IsDemoRequest();
            
            if (isDemoRequest)
            {
                _logger.LogInformation("Serving widget for demo shop: {Shop}", shop);
                // For demo, create a mock merchant object
                var demoMerchant = new Data.Entities.Merchant
                {
                    Id = 1,
                    Shop = shop,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var demoWidgetScript = await GenerateWidgetJavaScriptAsync(demoMerchant);
                return Content(demoWidgetScript, "application/javascript");
            }

            // Get merchant by shop domain for real shops
            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                _logger.LogWarning("Widget requested for unknown shop: {Shop}", shop);
                return NotFound("Shop not found");
            }

            // Check if merchant is active
            if (!merchant.IsActive)
            {
                _logger.LogWarning("Widget requested for inactive shop: {Shop}", shop);
                return NotFound("Shop inactive");
            }

            // Generate dynamic widget JavaScript
            var widgetScript = await GenerateWidgetJavaScriptAsync(merchant);

            return Content(widgetScript, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving widget for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Serves the widget HTML content for injection into product pages
    /// </summary>
    /// <param name="shop">Shop domain (from query parameter)</param>
    /// <returns>HTML content</returns>
    [HttpGet("widget.html")]
    public async Task<IActionResult> GetWidgetHtml([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                _logger.LogWarning("Widget HTML request without shop parameter");
                return BadRequest("Shop parameter is required");
            }

            // Check if this is a demo request
            var isDemoRequest = IsDemoRequest();
            
            if (isDemoRequest)
            {
                _logger.LogInformation("Serving widget HTML for demo shop: {Shop}", shop);
                // For demo, create a mock merchant object
                var demoMerchant = new Data.Entities.Merchant
                {
                    Id = 1,
                    Shop = shop,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                // For demo, create a mock add-on config
                var demoAddonConfig = new Data.Entities.ProductAddOn
                {
                    AddOnTitle = "Premium Protection",
                    AddOnPriceCents = 199,
                    AddOnDescription = "Protect your purchase with comprehensive coverage",
                    AddOnSku = "PROTECTION-001",
                    AddOnVariantId = "demo-variant-123"
                };
                
                var demoWidgetHtml = await GenerateWidgetHtmlAsync(demoMerchant, demoAddonConfig);
                return Content(demoWidgetHtml, "text/html");
            }

            // Get merchant by shop domain for real shops
            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                _logger.LogWarning("Widget HTML requested for unknown shop: {Shop}", shop);
                return NotFound("Shop not found");
            }

            // Check if merchant is active
            if (!merchant.IsActive)
            {
                _logger.LogWarning("Widget HTML requested for inactive shop: {Shop}", shop);
                return NotFound("Shop inactive");
            }

            // For widget.html, we need to get the add-on config for the specific product
            // Since this is a generic endpoint, we'll use the first available add-on config
            var addonConfigs = await _productAddOnService.GetMerchantAddOnsAsync(merchant.Id);
            var addonConfig = addonConfigs.FirstOrDefault();
            
            // Generate dynamic widget HTML with actual config
            var widgetHtml = await GenerateWidgetHtmlAsync(merchant, addonConfig);

            return Content(widgetHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving widget HTML for shop: {Shop}", shop);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// JSONP endpoint for widget configuration
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="productId">Product ID</param>
    /// <param name="callback">JSONP callback function name</param>
    /// <returns>JSONP response with configuration</returns>
    [HttpGet("config")]
    public async Task<IActionResult> GetWidgetConfig([FromQuery] string shop, [FromQuery] string productId, [FromQuery] string callback)
    {
        try
        {
            if (string.IsNullOrEmpty(shop) || string.IsNullOrEmpty(callback))
            {
                return BadRequest("Shop and callback parameters are required");
            }

            // Check if this is a demo request
            var isDemoRequest = IsDemoRequest();
            
            if (isDemoRequest)
            {
                _logger.LogInformation("Serving widget config for demo shop: {Shop}, product: {ProductId}", shop, productId);
                
                // For demo, try to get real configuration first, fallback to mock if none exists
                var demoMerchant = await _merchantService.GetMerchantByShopAsync(shop);
                if (demoMerchant != null)
                {
                var demoAddOn = await _productAddOnService.GetProductAddOnAsync(demoMerchant.Id, productId);
                    if (demoAddOn != null && demoAddOn.IsActive)
                    {
                        var realConfig = new
                        {
                            success = true,
                            shop = shop,
                            productId = productId,
                            hasAddOn = true,
                            addOn = new
                            {
                                id = demoAddOn.Id,
                                productId = demoAddOn.ProductId,
                                isActive = demoAddOn.IsActive,
                                title = demoAddOn.AddOnTitle,
                                description = demoAddOn.AddOnDescription,
                                price = demoAddOn.AddOnPriceCents / 100.0m,
                                currency = demoAddOn.Currency,
                                sku = demoAddOn.AddOnSku
                            }
                        };
                        
                        var demoJsonpResponse = $"{callback}({System.Text.Json.JsonSerializer.Serialize(realConfig)})";
                        return Content(demoJsonpResponse, "application/javascript");
                    }
                }
                
                // Fallback to mock configuration if no real config exists
                var mockConfig = new
                {
                    success = true,
                    shop = shop,
                    productId = productId,
                    hasAddOn = true,
                    addOn = new
                    {
                        id = 1,
                    productId = productId,
                        isActive = true,
                        title = "Premium Protection",
                        description = "Protect your purchase with our comprehensive coverage plan.",
                        price = 1.50m,
                        currency = "USD",
                        sku = "PROTECTION-001"
                    }
                };
                
                var fallbackJsonpResponse = $"{callback}({System.Text.Json.JsonSerializer.Serialize(mockConfig)})";
                return Content(fallbackJsonpResponse, "application/javascript");
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound("Shop not found");
            }

            // Get product add-on configuration
            var addOn = await _productAddOnService.GetProductAddOnAsync(merchant.Id, productId);
            
            var config = new
            {
                success = true,
                shop = shop,
                productId = productId,
                hasAddOn = addOn != null,
                addOn = addOn != null ? new
                {
                    id = addOn.Id,
                    productId = addOn.ProductId,
                    title = addOn.AddOnTitle,
                    description = addOn.AddOnDescription,
                    price = addOn.AddOnPriceCents / 100.0, // Convert cents to dollars
                    sku = addOn.AddOnSku,
                    variantId = addOn.AddOnVariantId,
                    isActive = addOn.IsActive,
                    displayText = addOn.DisplayText,
                    position = addOn.Position,
                    currency = addOn.Currency
                } : null
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(config);
            var jsonpResponse = $"window['{callback}']({jsonResponse});";

            return Content(jsonpResponse, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget config for shop: {Shop}, product: {ProductId}", shop, productId);
            
            var errorConfig = new { success = false, error = "Internal server error" };
            var errorJson = System.Text.Json.JsonSerializer.Serialize(errorConfig);
            var errorJsonp = $"window['{callback}']({errorJson});";
            
            return Content(errorJsonp, "application/javascript");
        }
    }

    /// <summary>
    /// Resolve productId by variantId or handle (slug)
    /// </summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] string shop, [FromQuery] string? variantId = null, [FromQuery] string? handle = null, [FromQuery] string? callback = null)
    {
        try
        {
            if (string.IsNullOrEmpty(shop)) return BadRequest(new { error = "Missing shop" });
            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null) return NotFound(new { error = "Merchant not found" });

            if (!string.IsNullOrEmpty(variantId))
            {
                var variant = await _shoplazzaApiService.GetVariantAsync(merchant, variantId);
                if (variant?.ProductId != null)
                {
                    var payload = new { productId = variant.ProductId };
                    if (!string.IsNullOrEmpty(callback))
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(payload);
                        return Content($"window['{callback}']({json});", "application/javascript");
                    }
                    return Ok(payload);
                }
            }

            if (!string.IsNullOrEmpty(handle))
            {
                var product = await _shoplazzaApiService.FindProductByHandleAsync(merchant, handle);
                if (product?.Id != null)
                {
                    var payload = new { productId = product.Id };
                    if (!string.IsNullOrEmpty(callback))
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(payload);
                        return Content($"window['{callback}']({json});", "application/javascript");
                    }
                    return Ok(payload);
                }
            }

            var error = new { error = "Unable to resolve product" };
            if (!string.IsNullOrEmpty(callback))
            {
                var jsonErr = System.Text.Json.JsonSerializer.Serialize(error);
                return Content($"window['{callback}']({jsonErr});", "application/javascript");
            }
            return NotFound(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resolve endpoint failed. shop={Shop}, variantId={VariantId}, handle={Handle}", shop, variantId ?? string.Empty, handle ?? string.Empty);
            var error = new { error = "Internal server error" };
            if (!string.IsNullOrEmpty(callback))
            {
                var jsonErr = System.Text.Json.JsonSerializer.Serialize(error);
                return Content($"window['{callback}']({jsonErr});", "application/javascript");
            }
            return StatusCode(500, error);
        }
    }
    /// <summary>
    /// Generates the dynamic widget HTML using templates
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="addonConfig">Add-on configuration for the product</param>
    /// <returns>Widget HTML content</returns>
    private async Task<string> GenerateWidgetHtmlAsync(Data.Entities.Merchant merchant, Data.Entities.ProductAddOn addonConfig)
    {
        try
        {
            // Load HTML templates
            var baseTemplate = await LoadTemplateAsync("widget-base.html");
            var addonSelectionTemplate = await LoadTemplateAsync("addon-selection.html");
            var widgetScriptTemplate = await LoadTemplateAsync("widget-script.js");  // Use the updated JS template
            var widgetStylesTemplate = await LoadTemplateAsync("widget-styles.html");
            
            // First, process the addon-selection template
            var processedAddonSelection = addonSelectionTemplate
                .Replace("[[ADDON_TITLE]]", addonConfig?.AddOnTitle ?? "Premium Protection")
                .Replace("[[ADDON_PRICE]]", ((addonConfig?.AddOnPriceCents ?? 0) / 100.0m).ToString("F2"))
                .Replace("[[ADDON_DESCRIPTION]]", addonConfig?.AddOnDescription ?? "Protect your purchase")
                .Replace("[[ADDON_SKU]]", addonConfig?.AddOnSku ?? "PROTECTION-001")
                .Replace("[[ADDON_VARIANT_ID]]", addonConfig?.AddOnVariantId?.ToString() ?? "");
            
            // Then, process the widget script template to replace its placeholders
            var processedWidgetScript = widgetScriptTemplate
                .Replace("[[SHOP_DOMAIN]]", merchant.Shop)
                .Replace("[[API_ENDPOINT]]", $"{Request.Scheme}://{Request.Host}")
                .Replace("[[WIDGET_STYLES]]", widgetStylesTemplate)
                .Replace("[[WIDGET_HTML]]", processedAddonSelection);
            
            // Finally, replace template placeholders in the base template
            var widgetHtml = baseTemplate
                .Replace("[[PRODUCT_ID]]", "default")
                .Replace("[[ADDON_TITLE]]", addonConfig?.AddOnTitle ?? "Premium Protection")
                .Replace("[[ADDON_PRICE]]", ((addonConfig?.AddOnPriceCents ?? 0) / 100.0m).ToString("F2"))
                .Replace("[[ADDON_DESCRIPTION]]", addonConfig?.AddOnDescription ?? "Protect your purchase")
                .Replace("[[ADDON_SKU]]", addonConfig?.AddOnSku ?? "PROTECTION-001")
                .Replace("[[ADDON_VARIANT_ID]]", addonConfig?.AddOnVariantId?.ToString() ?? "")
                .Replace("[[SHOP_DOMAIN]]", merchant.Shop)
                .Replace("[[API_ENDPOINT]]", $"{Request.Scheme}://{Request.Host}")
                .Replace("[[ADDON_SELECTION_UI]]", processedAddonSelection)
                .Replace("[[WIDGET_SCRIPT]]", processedWidgetScript)
                .Replace("[[WIDGET_STYLES]]", widgetStylesTemplate);
            
            return widgetHtml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating widget HTML");
            return GetFallbackWidget();
        }
    }

    /// <summary>
    /// Generates the dynamic widget JavaScript using templates
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>Widget JavaScript content</returns>
    private async Task<string> GenerateWidgetJavaScriptAsync(Data.Entities.Merchant merchant)
    {
        try
        {
            // Load all template pieces
            var widgetScriptTemplate = await LoadTemplateAsync("widget-script.js");
            var widgetStylesTemplate = await LoadTemplateAsync("widget-styles.html");
            var addonSelectionTemplate = await LoadTemplateAsync("addon-selection.html");
            
            // Check if this is a demo request
            var isDemoRequest = IsDemoRequest();
            
            if (isDemoRequest)
            {
                _logger.LogInformation("Serving widget JavaScript for demo shop: {Shop}", merchant.Shop);
                
                // For demo, use mock add-on configuration
                var mockAddOnConfig = new
                {
                    AddOnTitle = "Premium Protection",
                    AddOnPriceCents = 199,
                    AddOnDescription = "Protect your purchase with comprehensive coverage",
                    AddOnSku = "PROTECTION-001",
                    AddOnVariantId = "demo-variant-123"
                };
                
                // Create the complete widget HTML by combining templates
                var demoWidgetHtml = addonSelectionTemplate
                    .Replace("[[ADDON_TITLE]]", mockAddOnConfig.AddOnTitle)
                    .Replace("[[ADDON_PRICE]]", (mockAddOnConfig.AddOnPriceCents / 100.0m).ToString("F2"))
                    .Replace("[[ADDON_DESCRIPTION]]", mockAddOnConfig.AddOnDescription)
                    .Replace("[[ADDON_SKU]]", mockAddOnConfig.AddOnSku)
                    .Replace("[[ADDON_VARIANT_ID]]", mockAddOnConfig.AddOnVariantId);
                
                // Replace template placeholders in the JavaScript
                var demoWidgetScript = widgetScriptTemplate
                    .Replace("[[SHOP_DOMAIN]]", merchant.Shop)
                    .Replace("[[API_ENDPOINT]]", $"{Request.Scheme}://{Request.Host}")
                    .Replace("[[WIDGET_STYLES]]", widgetStylesTemplate)
                    .Replace("[[WIDGET_HTML]]", demoWidgetHtml);
                
                return demoWidgetScript;
            }

            // For non-demo requests, we need to get the actual add-on config
            var actualAddonConfig = await _productAddOnService.GetProductAddOnAsync(merchant.Id, "default");
            
            // Create the complete widget HTML by combining templates
            var widgetHtml = addonSelectionTemplate
                .Replace("[[ADDON_TITLE]]", actualAddonConfig?.AddOnTitle ?? "Premium Protection")
                .Replace("[[ADDON_PRICE]]", ((actualAddonConfig?.AddOnPriceCents ?? 0) / 100.0m).ToString("F2"))
                .Replace("[[ADDON_DESCRIPTION]]", actualAddonConfig?.AddOnDescription ?? "Protect your purchase")
                .Replace("[[ADDON_SKU]]", actualAddonConfig?.AddOnSku ?? "PROTECTION-001")
                .Replace("[[ADDON_VARIANT_ID]]", actualAddonConfig?.AddOnVariantId?.ToString() ?? "");
            
            // Replace template placeholders in the JavaScript
            var widgetScript = widgetScriptTemplate
                .Replace("[[SHOP_DOMAIN]]", merchant.Shop)
                .Replace("[[API_ENDPOINT]]", $"{Request.Scheme}://{Request.Host}")
                .Replace("[[WIDGET_STYLES]]", widgetStylesTemplate)
                .Replace("[[WIDGET_HTML]]", widgetHtml);
            
            return widgetScript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating widget JavaScript");
            return "console.error('Widget script generation failed.');";
        }
    }

    /// <summary>
    /// Loads an HTML template from the widget-templates directory
    /// </summary>
    /// <param name="templateName">Name of the template file</param>
    /// <returns>Template content as string</returns>
    private async Task<string> LoadTemplateAsync(string templateName)
    {
        // Try multiple possible paths for template loading
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "widget-templates", templateName),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "widget-templates", templateName),
            Path.Combine(Environment.CurrentDirectory, "wwwroot", "widget-templates", templateName)
        };
        
        _logger.LogInformation("Loading template {TemplateName}. Current directory: {CurrentDir}, Base directory: {BaseDir}, Environment directory: {EnvDir}", 
            templateName, Directory.GetCurrentDirectory(), AppContext.BaseDirectory, Environment.CurrentDirectory);
        
        foreach (var templatePath in possiblePaths)
        {
            _logger.LogInformation("Trying template path: {Path}", templatePath);
            if (System.IO.File.Exists(templatePath))
            {
                try
                {
                    var content = await System.IO.File.ReadAllTextAsync(templatePath);
                    _logger.LogInformation("Successfully loaded template {TemplateName} from {Path}, content length: {Length}", templateName, templatePath, content.Length);
                    return content;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading template {TemplateName} from {Path}", templateName, templatePath);
                    continue;
                }
            }
            else
            {
                _logger.LogInformation("Template file not found at: {Path}", templatePath);
            }
        }
        
        _logger.LogWarning("Template not found: {TemplateName}. Tried paths: {Paths}", templateName, string.Join(", ", possiblePaths));
        return string.Empty;
    }

    /// <summary>
    /// Returns a fallback widget HTML when template loading fails
    /// </summary>
    /// <returns>Fallback widget HTML</returns>
    private string GetFallbackWidget()
    {
        return @"
            <div class=""shoplazza-addon-widget"">
                <p>Add-on widget loading...</p>
                <p style=""color: red; font-size: 12px;"">Template loading failed. Check server logs for details.</p>
            </div>
        ";
    }
       
    /// <summary>
    /// Test endpoint to demonstrate the widget functionality
    /// </summary>
    /// <returns>HTML test page</returns>
    [HttpGet("test")]
    public IActionResult GetTestPage()
    {
        var testHtml = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Widget Test</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .test-section { margin: 20px 0; padding: 20px; border: 1px solid #ccc; }
        .widget-container { border: 2px dashed #999; padding: 20px; margin: 20px 0; }
        iframe { width: 100%; height: 400px; border: 1px solid #ddd; }
    </style>
</head>
<body>
    <h1>Shoplazza Add-On Widget Test</h1>
    
    <div class=""test-section"">
        <h2>Widget Endpoint Test</h2>
        <p>Testing the widget endpoint: <code>/api/widget/widget.js?shop=demo-store</code></p>
        
        <div class=""widget-container"">
            <h3>Widget Output:</h3>
            <iframe id=""widgetFrame"" src=""/api/widget/widget.js?shop=demo-store""></iframe>
        </div>
        
        <button onclick=""testWidgetEndpoint()"">Test Widget Endpoint</button>
        <div id=""testResult""></div>
    </div>
    
    <div class=""test-section"">
        <h2>Widget Config Test</h2>
        <p>Testing the config endpoint: <code>/api/widget/config?shop=demo-store&productId=test&callback=testCallback</code></p>
        
        <button onclick=""testConfigEndpoint()"">Test Config Endpoint</button>
        <div id=""configResult""></div>
    </div>
    
    <script>
        function testWidgetEndpoint() {
            const resultDiv = document.getElementById('testResult');
            resultDiv.innerHTML = 'Testing...';
            
            fetch('/api/widget/widget.js?shop=demo-store')
                .then(response => {
                    if (response.ok) {
                        return response.text();
                    }
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                })
                .then(html => {
                    resultDiv.innerHTML = `
                        <h4>Success!</h4>
                        <p>Response length: ${html.length} characters</p>
                        <details>
                            <summary>View HTML (first 500 chars)</summary>
                            <pre style=""background: #f5f5f5; padding: 10px; overflow-x: auto;"">${html.substring(0, 500)}...</pre>
                        </details>
                    `;
                })
                .catch(error => {
                    resultDiv.innerHTML = `
                        <h4>Error!</h4>
                        <p style=""color: red;"">${error.message}</p>
                    `;
                });
        }
        
        function testConfigEndpoint() {
            const resultDiv = document.getElementById('configResult');
            resultDiv.innerHTML = 'Testing...';
            
            // Create a unique callback name
            const callbackName = 'testCallback_' + Date.now();
            window[callbackName] = function(config) {
                resultDiv.innerHTML = `
                    <h4>Success!</h4>
                    <p>Config received via JSONP</p>
                    <pre style=""background: #f5f5f5; padding: 10px; overflow-x: auto;"">${JSON.stringify(config, null, 2)}</pre>
                `;
                
                // Clean up
                delete window[callbackName];
                document.head.removeChild(script);
            };
            
            const script = document.createElement('script');
            script.src = `/api/widget/config?shop=demo-store&productId=test&callback=${callbackName}`;
            script.onerror = function() {
                resultDiv.innerHTML = `
                    <h4>Error!</h4>
                    <p style=""color: red;"">Failed to load config script</p>
                `;
                delete window[callbackName];
            };
            
            document.head.appendChild(script);
        }
    </script>
</body>
</html>";
        
        return Content(testHtml, "text/html");
    }
    
    /// <summary>
    /// Check if request is from demo mode
    /// </summary>
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