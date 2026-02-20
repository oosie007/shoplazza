using Microsoft.AspNetCore.Mvc;
using ShoplazzaAddonApp.Data.Entities;
using System.Text.Json;
using ShoplazzaAddonApp.Models.Auth;
using ShoplazzaAddonApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace ShoplazzaAddonApp.Controllers;

/// <summary>
/// Handles Shoplazza OAuth authentication flow
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IShoplazzaAuthService _authService;
    private readonly IMerchantService _merchantService;
    private readonly IShoplazzaApiService _shoplazzaApiService;
    private readonly IRepository<Merchant> _merchantRepository;
    private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

    public AuthController(
        IShoplazzaAuthService authService, 
        IMerchantService merchantService,
        IShoplazzaApiService shoplazzaApiService,
        IRepository<Merchant> merchantRepository,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _merchantService = merchantService;
        _shoplazzaApiService = shoplazzaApiService;
        _merchantRepository = merchantRepository;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Initial authentication endpoint - called by Shoplazza before app installation
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="timestamp">Request timestamp</param>
    /// <param name="hmac">HMAC signature</param>
    /// <param name="install_source">Installation source</param>
    /// <returns>Redirect to OAuth authorization</returns>
    [HttpGet]
    public async Task<IActionResult> Auth(
        [FromQuery] string shop,
        [FromQuery] string? timestamp = null,
        [FromQuery] string? hmac = null,
        [FromQuery] string? install_source = null)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                _logger.LogWarning("Auth request missing shop parameter");
                return BadRequest(new { error = "Missing shop parameter" });
            }

            // Create auth request model
            var authRequest = new ShoplazzaAuthRequest
            {
                Shop = shop,
                Timestamp = timestamp ?? string.Empty,
                Hmac = hmac ?? string.Empty,
                InstallSource = install_source
            };

            // Add any additional query parameters
            foreach (var param in Request.Query)
            {
                if (!new[] { "shop", "timestamp", "hmac", "install_source" }.Contains(param.Key.ToLower()))
                {
                    authRequest.AdditionalParams[param.Key] = param.Value.ToString();
                }
            }

            // Check for demo mode - bypass validation for demo requests
            var isDemoMode = IsDemoRequest();
            if (!isDemoMode)
            {
                // If middleware already validated HMAC, skip duplicate validation
                var skipValidation = HttpContext.Items.TryGetValue("HmacValidated", out var validatedObj)
                                      && validatedObj is bool validated && validated;

                if (!skipValidation)
                {
                    // Validate the request when middleware didn't or couldn't
                    var isValid = await _authService.ValidateAuthRequestAsync(authRequest);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid auth request for shop: {Shop}", shop);
                        return Unauthorized(new { error = "Invalid authentication request" });
                    }
                }
            }
            else
            {
                _logger.LogInformation("Demo mode detected - bypassing auth request validation for shop: {Shop}", shop);
            }

            // Generate OAuth URL (scopes from configuration only; prefer single env string to override arrays)
            string[]? scopes = null;
            var scopesStr = _configuration["Shoplazza:RequiredScopes"]; // supports App Settings env var override
            if (!string.IsNullOrWhiteSpace(scopesStr))
            {
                var trimmed = scopesStr.Trim();
                // If env value looks like JSON (e.g., ["read_product","write_product"]) try to parse
                if (trimmed.StartsWith("["))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<string[]>(trimmed);
                        if (parsed != null && parsed.Length > 0)
                        {
                            scopes = parsed;
                        }
                    }
                    catch
                    {
                        // ignore and fall back to sanitizing string below
                    }
                }

                if (scopes == null)
                {
                    // Sanitize any brackets/quotes and split by common delimiters
                    var sanitized = trimmed
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty)
                        .Replace("\"", string.Empty)
                        .Replace("'", string.Empty);

                    scopes = sanitized
                        .Split(new[] { ' ', ',', ';', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
                }
            }
            else
            {
                scopes = _configuration.GetSection("Shoplazza:RequiredScopes").Get<string[]>();
            }

            if (scopes == null || scopes.Length == 0)
            {
                _logger.LogError("Required scopes are not configured. Set Shoplazza:RequiredScopes in app settings.");
                return StatusCode(500, new { error = "Required scopes are not configured. Set Shoplazza__RequiredScopes in app settings." });
            }
            var state = Guid.NewGuid().ToString(); // Generate unique state for CSRF protection
            var oauthUrl = _authService.GenerateOAuthUrl(shop, scopes, state);

            _logger.LogInformation("Redirecting shop {Shop} to OAuth authorization", shop);

            // Store state for validation (in production, use secure storage)
            HttpContext.Session.SetString($"oauth_state_{shop}", state);

            // For demo mode, return state directly instead of redirecting
            if (isDemoMode)
            {
                _logger.LogInformation("Demo mode - returning state directly instead of redirect");
                return Ok(new { 
                    message = "OAuth URL generated", 
                    oauthUrl = oauthUrl,
                    state = state,
                    shop = shop 
                });
            }

            return Redirect(oauthUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auth request for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// OAuth callback endpoint - called by Shoplazza after user authorization
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="code">Authorization code</param>
    /// <param name="state">State parameter for CSRF protection</param>
    /// <param name="hmac">HMAC signature</param>
    /// <param name="timestamp">Request timestamp</param>
    /// <returns>Redirect to app dashboard</returns>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string shop,
        [FromQuery] string code,
        [FromQuery] string? state = null,
        [FromQuery] string? hmac = null,
        [FromQuery] string? timestamp = null)
    {
        try
        {
            if (string.IsNullOrEmpty(shop) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("OAuth callback missing required parameters");
                return BadRequest(new { error = "Missing required parameters" });
            }

            // Check for demo mode - bypass validation and token exchange for demo requests
            var isDemoMode = IsDemoRequest();
            
            if (!isDemoMode)
            {
                // Validate state parameter (CSRF protection) for production
                if (!string.IsNullOrEmpty(state))
                {
                    var storedState = HttpContext.Session.GetString($"oauth_state_{shop}");
                    if (storedState != state)
                    {
                        _logger.LogWarning("OAuth state mismatch for shop: {Shop}", shop);
                        return BadRequest(new { error = "Invalid state parameter" });
                    }
                    // Clear the stored state
                    HttpContext.Session.Remove($"oauth_state_{shop}");
                }

                // Exchange authorization code for access token
                var authResponse = await _authService.ExchangeCodeForTokenAsync(shop, code);
                
                // Create or update merchant with authentication data
                var merchant = await _merchantService.CreateOrUpdateMerchantAsync(shop, authResponse);
                
                // Create widget script tag for production merchant
                try
                {
                    var created = await CreateWidgetScriptTagAsync(merchant);
                    if (created)
                    {
                        _logger.LogInformation("Widget script tag created for merchant: {Shop}", shop);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create script tag for merchant {Shop}", shop);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create widget script tag for merchant: {Shop}", shop);
                    // Continue with OAuth flow even if script tag creation fails
                }

                // Register webhooks for production merchant
                try
                {
                    var webhookBaseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                    var webhookSuccess = await _shoplazzaApiService.RegisterWebhooksAsync(merchant, webhookBaseUrl);
                    
                    if (webhookSuccess)
                    {
                        _logger.LogInformation("Webhooks registered successfully for merchant: {Shop}", shop);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to register webhooks for merchant: {Shop}", shop);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register webhooks for merchant: {Shop}", shop);
                    // Continue with OAuth flow even if webhook registration fails
                }

                // Register cart-transform function for production merchant
                try
                {
                    var functionSuccess = await _merchantService.RegisterCartTransformFunctionAsync(merchant);
                    if (functionSuccess)
                    {
                        _logger.LogInformation("Cart-transform function registered successfully for merchant: {Shop}", shop);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to register cart-transform function for merchant {Shop}", shop);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register cart-transform function for merchant: {Shop}", shop);
                    // Continue with OAuth flow even if function registration fails
                }

                // Sign-in merchant session for [Authorize] endpoints
                var claims = new List<Claim> { new Claim("shop", shop) };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }
            else
            {
                _logger.LogInformation("Demo mode detected - bypassing token exchange and creating demo merchant for shop: {Shop}", shop);
                
                // Create demo merchant data
                var demoAuthResponse = new ShoplazzaAuthResponse
                {
                    AccessToken = "demo-access-token-" + Guid.NewGuid().ToString("N")[..16],
                    Scope = "read_products,write_products,read_orders,write_orders",
                    TokenType = "bearer"
                };
                
                var merchant = await _merchantService.CreateOrUpdateMerchantAsync(shop, demoAuthResponse);
                
                // For demo mode, don't create actual script tag since we're not connected to real Shoplazza
                _logger.LogInformation("Demo mode - skipping script tag creation for shop: {Shop}", shop);
                
                // For demo mode, don't register cart-transform function since we're not connected to real Shoplazza
                _logger.LogInformation("Demo mode - skipping cart-transform function registration for shop: {Shop}", shop);
                
                // Clear demo state if it exists
                HttpContext.Session.Remove($"oauth_state_{shop}");
            }

            _logger.LogInformation("Successfully authenticated shop: {Shop}", shop);

            // For demo mode, return success JSON instead of redirecting
            if (isDemoMode)
            {
                _logger.LogInformation("Demo mode - returning success JSON instead of redirect");
                return Ok(new { 
                    message = "OAuth callback completed successfully", 
                    shop = shop,
                    success = true
                });
            }

            // Redirect to app dashboard with shop parameter
            var dashboardUrl = $"/dashboard?shop={Uri.EscapeDataString(shop)}";
            return Redirect(dashboardUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Authentication failed" });
        }
    }

    /// <summary>
    /// Validates if a shop is currently authenticated
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>Authentication status</returns>
    [HttpGet("status")]
    public async Task<IActionResult> GetAuthStatus([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Missing shop parameter" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            var isAuthenticated = merchant != null && await _merchantService.ValidateTokenAsync(merchant);

            return Ok(new
            {
                shop = shop,
                authenticated = isAuthenticated,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auth status for shop: {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles app uninstallation - removes stored authentication data
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>Success response</returns>
    [HttpPost("uninstall")]
    public async Task<IActionResult> Uninstall([FromQuery] string shop)
    {
        try
        {
            if (string.IsNullOrEmpty(shop))
            {
                return BadRequest(new { error = "Missing shop parameter" });
            }

            // Completely remove all merchant data
            await _merchantService.RemoveMerchantDataAsync(shop);
            
            _logger.LogInformation("App uninstalled for shop: {Shop}", shop);

            return Ok(new { message = "App successfully uninstalled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uninstall for shop: {Shop}", shop);
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

    /// <summary>
    /// Creates a script tag for the widget in the merchant's store
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>True if successful</returns>
    private async Task<bool> CreateWidgetScriptTagAsync(Merchant merchant)
    {
        try
        {
            // Check if script tag already exists
            if (!string.IsNullOrEmpty(merchant.ScriptTagId))
            {
                _logger.LogInformation("Script tag already exists for merchant {Shop}: {ScriptTagId}", 
                    merchant.Shop, merchant.ScriptTagId ?? string.Empty);
                return true;
            }

            // Get the widget script URL
            var baseUrl = Request.Scheme + "://" + Request.Host.Value;
            var widgetUrl = $"{baseUrl}/api/widget/widget.js?shop={Uri.EscapeDataString(merchant.Shop)}";

            // Create script tag via Shoplazza API
            // Use display_scope "product" so the widget loads on product pages
            var scriptTagId = await _shoplazzaApiService.CreateScriptTagAsync(merchant, widgetUrl, "product", "app");

            if (!string.IsNullOrEmpty(scriptTagId))
            {
                // Update merchant with script tag ID
                merchant.ScriptTagId = scriptTagId;
                merchant.UpdatedAt = DateTime.UtcNow;
                
                await _merchantRepository.UpdateAsync(merchant);
                
                _logger.LogInformation("Created script tag {ScriptTagId} for merchant {Shop}", 
                    scriptTagId ?? string.Empty, merchant.Shop);
                return true;
            }

            _logger.LogWarning("Failed to create script tag for merchant {Shop}", merchant.Shop);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating widget script tag for merchant {Shop}", merchant.Shop);
            return false;
        }
    }
}