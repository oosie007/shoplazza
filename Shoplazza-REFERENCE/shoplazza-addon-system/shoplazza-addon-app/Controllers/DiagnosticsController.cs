using Microsoft.AspNetCore.Mvc;
using ShoplazzaAddonApp.Services;

namespace ShoplazzaAddonApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMerchantService _merchantService;
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly IShoplazzaFunctionApiService _functionApiService;
    private readonly IShoplazzaAuthService _authService;

    public DiagnosticsController(
        IConfiguration configuration,
        IMerchantService merchantService,
        ILogger<DiagnosticsController> logger,
        IShoplazzaFunctionApiService functionApiService,
        IShoplazzaAuthService authService)
    {
        _configuration = configuration;
        _merchantService = merchantService;
        _logger = logger;
        _functionApiService = functionApiService;
        _authService = authService;
    }

    // GET /api/diagnostics/token?shop=...
    [HttpGet("token")]
    public async Task<IActionResult> GetDecryptedToken([FromQuery] string shop)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics token access denied for shop {Shop}", shop);
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(shop))
            {
                return BadRequest(new { error = "Missing shop parameter" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var token = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(token))
            {
                return NotFound(new { error = "No token stored for merchant" });
            }

            return Ok(new { shop = merchant.Shop, token = token, length = token.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics token retrieval failed for shop {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/cleanup-database
    [HttpPost("cleanup-database")]
    public async Task<IActionResult> CleanupDatabase()
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics database cleanup access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            _logger.LogWarning("ADMIN DATABASE CLEANUP REQUESTED - This will delete ALL data!");

            // Check if any merchants are still active
            var activeMerchants = await _merchantService.GetActiveMerchantsAsync();
            if (activeMerchants.Any())
            {
                var activeShops = activeMerchants.Select(m => m.Shop).ToList();
                _logger.LogError("Database cleanup DENIED - Found {Count} active merchants: {Shops}", 
                    activeShops.Count, string.Join(", ", activeShops));
                
                return StatusCode(400, new { 
                    error = "Cannot cleanup database while merchants are active",
                    activeMerchants = activeShops.Count,
                    activeShops = activeShops
                });
            }

            // All merchants are inactive - proceed with cleanup
            _logger.LogWarning("All merchants are inactive. Proceeding with database cleanup...");
            
            var cleanupResult = await _merchantService.CleanupDatabaseAsync();
            
            if (cleanupResult.Success)
            {
                _logger.LogWarning("Database cleanup completed successfully. All data has been removed.");
                return Ok(new { 
                    message = "Database cleanup completed successfully",
                    details = cleanupResult.Details,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Database cleanup failed: {Error}", cleanupResult.Error);
                return StatusCode(500, new { 
                    error = "Database cleanup failed",
                    details = cleanupResult.Error
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics database cleanup failed");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/create-function
    [HttpPost("create-function")]
    public async Task<IActionResult> CreateFunction([FromBody] CreateFunctionRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics create function access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.WasmBase64))
            {
                return BadRequest(new { error = "Missing name or wasmBase64" });
            }

            _logger.LogWarning("ADMIN CREATE FUNCTION REQUESTED - Name: {Name}, WASM Size: {Size} bytes", 
                request.Name, request.WasmBase64.Length);

            // Create a dummy merchant for the function creation (partner API doesn't need shop-specific data)
            var dummyMerchant = new Data.Entities.Merchant { Shop = "admin" };
            var functionRequest = new Models.Api.FunctionRegistrationRequest
            {
                Name = request.Name,
                WasmBase64 = request.WasmBase64,
                Type = "cart-transform",
                Description = "Cart transform function created via diagnostics",
                Triggers = new List<string> { "cart-update" },
                Settings = new Models.Api.FunctionSettings
                {
                    Timeout = 30,
                    MemoryLimit = "128MB",
                    AutoEnable = true,
                    Retry = new Models.Api.RetrySettings
                    {
                        MaxAttempts = 3,
                        DelayMs = 1000,
                        UseExponentialBackoff = false
                    }
                },
                Metadata = new Dictionary<string, object>()
            };

            var (functionId, errorDetails) = await _functionApiService.CreateFunctionAsync(dummyMerchant, functionRequest);
            
            if (!string.IsNullOrEmpty(functionId))
            {
                _logger.LogWarning("Function created successfully with ID: {FunctionId}", functionId);
                return Ok(new { 
                    message = "Function created successfully",
                    functionId = functionId,
                    name = request.Name,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Function creation failed. Error: {ErrorDetails}", errorDetails);
                return StatusCode(500, new { error = "Function creation failed", details = errorDetails });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics create function failed");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/bind-function
    [HttpPost("bind-function")]
    public async Task<IActionResult> BindFunction([FromBody] BindFunctionRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics bind function access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Shop) || string.IsNullOrWhiteSpace(request.FunctionId))
            {
                return BadRequest(new { error = "Missing shop or functionId" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(request.Shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            _logger.LogWarning("ADMIN BIND FUNCTION REQUESTED - Shop: {Shop}, FunctionId: {FunctionId}", 
                request.Shop, request.FunctionId);

            var merchantToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Failed to get merchant token for shop {Shop}", request.Shop);
                return StatusCode(500, new { error = "Failed to get merchant token" });
            }
            var success = await _functionApiService.BindFunctionToShopAsync(merchant, request.FunctionId, merchantToken);
            
            if (success)
            {
                _logger.LogWarning("Function bound successfully to shop: {Shop}", request.Shop);
                return Ok(new { 
                    message = "Function bound successfully",
                    shop = request.Shop,
                    functionId = request.FunctionId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Function binding failed for shop: {Shop}", request.Shop);
                return StatusCode(500, new { error = "Function binding failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics bind function failed for shop {Shop}", request.Shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/delete-function
    [HttpPost("delete-function")]
    public async Task<IActionResult> DeleteFunction([FromBody] DeleteFunctionRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics__Enable"] ?? _configuration["Diagnostics:Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics delete function access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.FunctionId))
            {
                return BadRequest(new { error = "Missing functionId" });
            }

            _logger.LogWarning("ADMIN DELETE FUNCTION REQUESTED - FunctionId: {FunctionId}", request.FunctionId);

            // Create a dummy merchant for the function deletion (partner API doesn't need shop-specific data)
            var dummyMerchant = new Data.Entities.Merchant { Shop = "admin" };
            var success = await _functionApiService.DeleteFunctionAsync(dummyMerchant, request.FunctionId);
            
            if (success)
            {
                _logger.LogWarning("Function deleted successfully: {FunctionId}", request.FunctionId);
                return Ok(new { 
                    message = "Function deleted successfully",
                    functionId = request.FunctionId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Function deletion failed: {FunctionId}", request.FunctionId);
                return StatusCode(500, new { error = "Function deletion failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics delete function failed for functionId {FunctionId}", request.FunctionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // GET /api/diagnostics/function-details?shop=...&functionId=...
    [HttpGet("function-details")]
    public async Task<IActionResult> GetFunctionDetails([FromQuery] string shop, [FromQuery] string functionId)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics function details access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(shop) || string.IsNullOrWhiteSpace(functionId))
            {
                return BadRequest(new { error = "Missing shop or functionId parameter" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            _logger.LogInformation("ADMIN GET FUNCTION DETAILS REQUESTED - Shop: {Shop}, FunctionId: {FunctionId}", 
                shop, functionId);

            var functionDetails = await _functionApiService.GetFunctionDetailsAsync(functionId);
            
            if (functionDetails != null && functionDetails.Any())
            {
                return Ok(new { 
                    message = "Function details retrieved successfully",
                    shop = shop,
                    functionId = functionId,
                    totalFunctions = functionDetails.Count,
                    functions = functionDetails,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("No functions found for shop: {Shop}, functionId: {FunctionId}", shop, functionId);
                return Ok(new { 
                    message = "No functions found",
                    shop = shop,
                    functionId = functionId,
                    totalFunctions = 0,
                    functions = new List<object>(),
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics get function details failed for shop {Shop}, functionId {FunctionId}", shop, functionId);
            return StatusCode(500, new { error = "Internal server error" });
        }       
    }

    // POST /api/diagnostics/update-function
    [HttpPost("update-function")]
    public async Task<IActionResult> UpdateFunction([FromBody] UpdateFunctionRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics update function access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.FunctionId) || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Missing functionId or name" });
            }

            _logger.LogWarning("ADMIN UPDATE FUNCTION REQUESTED - FunctionId: {FunctionId}, Name: {Name}", 
                request.FunctionId, request.Name);

            // Create a dummy merchant for the function update (partner API doesn't need shop-specific data)
            var dummyMerchant = new Data.Entities.Merchant { Shop = "admin" };
            var updateRequest = new Models.Api.FunctionUpdateRequest
            {
                Namespace = "cart-transform",
                Name = request.Name,
                File = request.WasmBase64, // Optional - can be null
                SourceCode = request.SourceCode // Optional - can be null
            };

            var success = await _functionApiService.UpdateFunctionAsync(dummyMerchant, request.FunctionId, updateRequest);
            
            if (success)
            {
                _logger.LogWarning("Function updated successfully: {FunctionId}", request.FunctionId);
                return Ok(new { 
                    message = "Function updated successfully",
                    functionId = request.FunctionId,
                    name = request.Name,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Function update failed: {FunctionId}", request.FunctionId);
                return StatusCode(500, new { error = "Function update failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics update function failed for functionId {FunctionId}", request.FunctionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // GET /api/diagnostics/cart-transform-functions?shop=...
    [HttpGet("cart-transform-functions")]
    public async Task<IActionResult> GetCartTransformFunctions([FromQuery] string shop)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics cart transform functions access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(shop))
            {
                return BadRequest(new { error = "Missing shop parameter" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            _logger.LogInformation("ADMIN GET CART TRANSFORM FUNCTIONS REQUESTED - Shop: {Shop}", shop);

            var merchantToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Failed to get merchant token for shop {Shop}", shop);
                return StatusCode(500, new { error = "Failed to get merchant token" });
            }
            var cartTransformFunctions = await _functionApiService.GetCartTransformFunctionsAsync(merchant, merchantToken);
            
            if (cartTransformFunctions != null)
            {
                return Ok(new { 
                    message = "Cart transform functions retrieved successfully",
                    shop = shop,
                    totalFunctions = cartTransformFunctions.Count,
                    functions = cartTransformFunctions,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Failed to retrieve cart transform functions for shop: {Shop}", shop);
                return StatusCode(500, new { error = "Failed to retrieve cart transform functions" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics get cart transform functions failed for shop {Shop}", shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/update-cart-transform-function
    [HttpPost("update-cart-transform-function")]
    public async Task<IActionResult> UpdateCartTransformFunction([FromBody] UpdateCartTransformFunctionRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics update cart transform function access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Shop) || string.IsNullOrWhiteSpace(request.FunctionBindingId))
            {
                return BadRequest(new { error = "Missing shop or functionBindingId" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(request.Shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            _logger.LogWarning("ADMIN UPDATE CART TRANSFORM FUNCTION REQUESTED - Shop: {Shop}, FunctionBindingId: {FunctionBindingId}", 
                request.Shop, request.FunctionBindingId);

            var updateRequest = new Models.Api.CartTransformFunctionUpdateRequest
            {
                BlockOnFailure = request.BlockOnFailure,
                InputQuery = request.InputQuery
            };

            var merchantToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Failed to get merchant token for shop {Shop}", request.Shop);
                return StatusCode(500, new { error = "Failed to get merchant token" });
            }
            var success = await _functionApiService.UpdateCartTransformFunctionAsync(merchant, request.FunctionBindingId, updateRequest, merchantToken);
            
            if (success)
            {
                _logger.LogWarning("Cart transform function updated successfully for shop: {Shop}", request.Shop);
                return Ok(new { 
                    message = "Cart transform function updated successfully",
                    shop = request.Shop,
                    functionBindingId = request.FunctionBindingId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Cart transform function update failed for shop: {Shop}", request.Shop);
                return StatusCode(500, new { error = "Cart transform function update failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics update cart transform function failed for shop {Shop}", request.Shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/delete-cart-transform-function
    [HttpPost("delete-cart-transform-function")]
    public async Task<IActionResult> DeleteCartTransformFunction([FromBody] DeleteCartTransformFunctionRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics delete cart transform function access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Shop) || string.IsNullOrWhiteSpace(request.FunctionBindingId))
            {
                return BadRequest(new { error = "Missing shop or functionBindingId" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(request.Shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            _logger.LogWarning("ADMIN DELETE CART TRANSFORM FUNCTION REQUESTED - Shop: {Shop}, FunctionBindingId: {FunctionBindingId}", 
                request.Shop, request.FunctionBindingId);

            var merchantToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Failed to get merchant token for shop {Shop}", request.Shop);
                return StatusCode(500, new { error = "Failed to get merchant token" });
            }
            var success = await _functionApiService.DeleteCartTransformFunctionAsync(merchant, request.FunctionBindingId, merchantToken);
            
            if (success)
            {
                _logger.LogWarning("Cart transform function deleted successfully for shop: {Shop}", request.Shop);
                return Ok(new { 
                    message = "Cart transform function deleted successfully",
                    shop = request.Shop,
                    functionBindingId = request.FunctionBindingId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Cart transform function deletion failed for shop: {Shop}", request.Shop);
                return StatusCode(500, new { error = "Cart transform function deletion failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics delete cart transform function failed for shop {Shop}", request.Shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/bind-cart-transform-function
    [HttpPost("bind-cart-transform-function")]
    public async Task<IActionResult> BindCartTransformFunction([FromBody] BindCartTransformFunctionRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics bind cart transform function access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Shop) || string.IsNullOrWhiteSpace(request.FunctionId))
            {
                return BadRequest(new { error = "Missing shop or functionId" });
            }

            var merchant = await _merchantService.GetMerchantByShopAsync(request.Shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            _logger.LogWarning("ADMIN BIND CART TRANSFORM FUNCTION REQUESTED - Shop: {Shop}, FunctionId: {FunctionId}", 
                request.Shop, request.FunctionId);

            var merchantToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Failed to get merchant token for shop {Shop}", request.Shop);
                return StatusCode(500, new { error = "Failed to get merchant token" });
            }
            var success = await _functionApiService.BindCartTransformFunctionAsync(merchant, request.FunctionId, merchantToken);
            
            if (success)
            {
                _logger.LogWarning("Cart transform function bound successfully for shop: {Shop}", request.Shop);
                return Ok(new { 
                    message = "Cart transform function bound successfully",
                    shop = request.Shop,
                    functionId = request.FunctionId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Cart transform function binding failed for shop: {Shop}", request.Shop);
                return StatusCode(500, new { error = "Cart transform function binding failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics bind cart transform function failed for shop {Shop}", request.Shop);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // POST /api/diagnostics/test-cart-function-binding
    [HttpPost("test-cart-function-binding")]
    public async Task<IActionResult> TestCartFunctionBinding([FromBody] TestCartFunctionBindingRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics test cart function binding access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Shop) || string.IsNullOrWhiteSpace(request.FunctionId))
            {
                return BadRequest(new { error = "Missing shop or functionId" });
            }

            _logger.LogWarning("ADMIN TEST CART FUNCTION BINDING REQUESTED - Shop: {Shop}, FunctionId: {FunctionId}", 
                request.Shop, request.FunctionId);

            // Test Merchant API authentication first
            var merchant = await _merchantService.GetMerchantByShopAsync(request.Shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var merchantToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                return BadRequest(new { error = "No valid merchant token available" });
            }
            _logger.LogInformation("✅ Merchant token obtained successfully, length: {TokenLength}", merchantToken.Length);

            // Test the cart function binding endpoint directly
            var endpoint = $"https://{request.Shop}/openapi/2024-07/function/cart-transform";
            var payload = new { function_id = request.FunctionId };
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Access-Token", merchantToken);

            _logger.LogInformation("🔍 Testing cart function binding endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("🔍 Payload: {Payload}", jsonContent);
            _logger.LogInformation("🔍 Headers: Access-Token (length: {TokenLength})", merchantToken.Length);

            var response = await httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("🔍 Response Status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("🔍 Response Content: {ResponseContent}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { 
                    message = "Cart function binding test successful",
                    shop = request.Shop,
                    functionId = request.FunctionId,
                    statusCode = (int)response.StatusCode,
                    response = responseContent,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode((int)response.StatusCode, new { 
                    error = "Cart function binding test failed",
                    shop = request.Shop,
                    functionId = request.FunctionId,
                    statusCode = (int)response.StatusCode,
                    response = responseContent,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics test cart function binding failed for shop {Shop}", request.Shop);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // POST /api/diagnostics/test-cart-function-listing
    [HttpPost("test-cart-function-listing")]
    public async Task<IActionResult> TestCartFunctionListing([FromBody] TestCartFunctionListingRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics test cart function listing access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Shop))
            {
                return BadRequest(new { error = "Missing shop parameter" });
            }

            _logger.LogWarning("ADMIN TEST CART FUNCTION LISTING REQUESTED - Shop: {Shop}", request.Shop);

            // Test Merchant API authentication first
            var merchant = await _merchantService.GetMerchantByShopAsync(request.Shop);
            if (merchant == null)
            {
                return NotFound(new { error = "Merchant not found" });
            }

            var merchantToken = await _merchantService.DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                return BadRequest(new { error = "No valid merchant token available" });
            }
            _logger.LogInformation("✅ Merchant token obtained successfully, length: {TokenLength}", merchantToken.Length);

            // Test the cart function listing endpoint directly
            var endpoint = $"https://{request.Shop}/openapi/2024-07/function/cart-transform/";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Access-Token", merchantToken);

            _logger.LogInformation("🔍 Testing cart function listing endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("🔍 Headers: Access-Token (length: {TokenLength})", merchantToken.Length);

            var response = await httpClient.GetAsync(endpoint);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("🔍 Response Status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("🔍 Response Content: {ResponseContent}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { 
                    message = "Cart function listing test successful",
                    shop = request.Shop,
                    statusCode = (int)response.StatusCode,
                    response = responseContent,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode((int)response.StatusCode, new { 
                    error = "Cart function listing test failed",
                    shop = request.Shop,
                    statusCode = (int)response.StatusCode,
                    response = responseContent,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics test cart function listing failed for shop {Shop}", request.Shop);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // POST /api/diagnostics/test-partner-function-creation
    [HttpPost("test-partner-function-creation")]
    public async Task<IActionResult> TestPartnerFunctionCreation([FromBody] TestPartnerFunctionCreationRequest request)
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics test partner function creation access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Missing name parameter" });
            }

            _logger.LogWarning("ADMIN TEST PARTNER FUNCTION CREATION REQUESTED - Name: {Name}", request.Name);

            // Test Partner API authentication first
            var partnerToken = await _authService.GetPartnerTokenAsync();
            _logger.LogInformation("✅ Partner token obtained successfully, length: {TokenLength}", partnerToken.Length);

            // Test the partner function creation endpoint directly
            var endpoint = "https://partners.shoplazza.com/openapi/2024-07/functions";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            // Create a minimal test payload
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("cart_transform"), "namespace");
            content.Add(new StringContent(request.Name), "name");
            content.Add(new StringContent("Test function for diagnostics"), "description");
            
            // Add a minimal WASM file (1 byte) for testing
            var testWasmBytes = new byte[] { 0x00 };
            content.Add(new ByteArrayContent(testWasmBytes), "file");
            content.Add(new StringContent("// Test source code"), "source_code");

            _logger.LogInformation("🔍 Testing partner function creation endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("🔍 Headers: Access-Token (length: {TokenLength}), app-client-id: {ClientId}", 
                partnerToken.Length, _configuration["Shoplazza:ClientId"]);
            _logger.LogInformation("🔍 Payload: namespace=cart_transform, name={Name}, file size={FileSize} bytes", 
                request.Name, testWasmBytes.Length);

            var response = await httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("🔍 Response Status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("🔍 Response Content: {ResponseContent}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { 
                    message = "Partner function creation test successful",
                    name = request.Name,
                    statusCode = (int)response.StatusCode,
                    response = responseContent,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode((int)response.StatusCode, new { 
                    error = "Partner function creation test failed",
                    name = request.Name,
                    statusCode = (int)response.StatusCode,
                    response = responseContent,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics test partner function creation failed for name {Name}", request.Name);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // GET /api/diagnostics/list-partner-functions
    [HttpGet("list-partner-functions")]
    public async Task<IActionResult> ListPartnerFunctions()
    {
        try
        {
            var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
            var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                _logger.LogWarning("Diagnostics list partner functions access denied");
                return StatusCode(403, new { error = "Forbidden" });
            }

            _logger.LogWarning("ADMIN LIST PARTNER FUNCTIONS REQUESTED");

            // Get partner token for Partner API
            var partnerToken = await _authService.GetPartnerTokenAsync();
            _logger.LogInformation("✅ Partner token obtained successfully, length: {TokenLength}", partnerToken.Length);

            // Get all functions from Partner API
            var functions = await _functionApiService.GetFunctionDetailsAsync();
            
            if (functions != null)
            {
                _logger.LogInformation("✅ Successfully retrieved {Count} functions from Partner API", functions.Count);
                return Ok(new { 
                    message = "Partner functions retrieved successfully",
                    count = functions.Count,
                    functions = functions,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("⚠️ No functions found or error occurred");
                return Ok(new { 
                    message = "No functions found or error occurred",
                    count = 0,
                    functions = new List<object>(),
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics list partner functions failed");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}

public class CreateFunctionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string WasmBase64 { get; set; } = string.Empty;
    }

public class BindFunctionRequest
{
    public string Shop { get; set; } = string.Empty;
    public string FunctionId { get; set; } = string.Empty;
}

public class DeleteFunctionRequest
{
    public string FunctionId { get; set; } = string.Empty;
}

public class UpdateFunctionRequest
{
    public string FunctionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? WasmBase64 { get; set; }
    public string? SourceCode { get; set; }
}

public class UpdateCartTransformFunctionRequest
{
    public string Shop { get; set; } = string.Empty;
    public string FunctionBindingId { get; set; } = string.Empty;
    public bool? BlockOnFailure { get; set; }
    public string? InputQuery { get; set; }
}

public class DeleteCartTransformFunctionRequest
{
    public string Shop { get; set; } = string.Empty;
    public string FunctionBindingId { get; set; } = string.Empty;
}

public class BindCartTransformFunctionRequest
{
    public string Shop { get; set; } = string.Empty;
    public string FunctionId { get; set; } = string.Empty;
}

public class TestCartFunctionBindingRequest
{
    public string Shop { get; set; } = string.Empty;
    public string FunctionId { get; set; } = string.Empty;
}

public class TestCartFunctionListingRequest
{
    public string Shop { get; set; } = string.Empty;
}

public class TestPartnerFunctionCreationRequest
{
    public string Name { get; set; } = string.Empty;
}