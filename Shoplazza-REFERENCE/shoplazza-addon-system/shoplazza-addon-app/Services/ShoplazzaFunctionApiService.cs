using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Api;
using ShoplazzaAddonApp.Models.Configuration;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service for interacting with Shoplazza's Function API
/// </summary>
public class ShoplazzaFunctionApiService : IShoplazzaFunctionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShoplazzaFunctionApiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IShoplazzaAuthService _authService;

    public ShoplazzaFunctionApiService(
        HttpClient httpClient,
        ILogger<ShoplazzaFunctionApiService> logger,
        IConfiguration configuration,
        IShoplazzaAuthService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _authService = authService;
    }

    /// <summary>
    /// Creates a new function on Shoplazza's platform using partner token, or updates existing one if creation fails
    /// </summary>
    public async Task<(string? FunctionId, string? ErrorDetails)> CreateFunctionAsync(Merchant merchant, FunctionRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Creating/updating function {FunctionName} for merchant {Shop}", request.Name, merchant.Shop);

            // Check if function already exists with this name
                                _logger.LogDebug("🔍 PROACTIVE CHECK: Looking for existing function with name: {FunctionName}", request.Name);
                    var existingFunctionId = await FindExistingFunctionByNameAsync(request.Name);
                    _logger.LogDebug("🔍 PROACTIVE CHECK: Result - existingFunctionId: {ExistingFunctionId}", existingFunctionId ?? "NULL");
            
            if (!string.IsNullOrEmpty(existingFunctionId))
            {
                _logger.LogInformation("Function {FunctionName} already exists with ID {FunctionId}, updating instead of creating", 
                    request.Name, existingFunctionId);
                
                // Update existing function
                var updateRequest = new FunctionUpdateRequest
                {
                    Namespace = "cart_transform",
                    Name = request.Name,
                    File = request.WasmBase64,
                    SourceCode = request.SourceCode
                };
                
                var updateSuccess = await UpdateFunctionAsync(merchant, existingFunctionId, updateRequest);
                if (updateSuccess)
                {
                    _logger.LogInformation("Successfully updated existing function {FunctionName} with ID {FunctionId}", 
                        request.Name, existingFunctionId);
                    return (existingFunctionId, null);
                }
                else
                {
                    _logger.LogWarning("Failed to update existing function {FunctionName}, falling back to create", request.Name);
                }
            }

            // Get partner token for Function API
            var partnerToken = await _authService.GetPartnerTokenAsync();
            _logger.LogInformation("🔑 Partner token obtained, length: {TokenLength}", partnerToken?.Length ?? 0);
            
            var endpoint = $"{GetApiBase()}/functions";
            _logger.LogInformation("🌐 Function creation endpoint: {Endpoint}", endpoint);
            
            // Create multipart form data according to Shoplazza Function API spec
            // HTTP spec: multipart/form-data with proper boundary handling
            var content = new MultipartFormDataContent();
            
            // Add form fields as per Shoplazza API requirements
            content.Add(new StringContent("cart_transform"), "namespace");
            content.Add(new StringContent(request.Name), "name");
            
            var wasmBytes = Convert.FromBase64String(request.WasmBase64);
            content.Add(new ByteArrayContent(wasmBytes), "file");
            content.Add(new StringContent(request.SourceCode), "source_code");

            _logger.LogInformation("📦 Request payload details:");
            _logger.LogInformation("   - Namespace: cart_transform");
            _logger.LogInformation("   - Name: {FunctionName}", request.Name);
            _logger.LogInformation("   - WASM file size: {WasmSize} bytes", wasmBytes.Length);
            _logger.LogInformation("   - Source code length: {SourceCodeLength} characters", request.SourceCode?.Length ?? 0);

            // Clear any existing headers and set partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            _logger.LogInformation("🔧 Request headers:");
            _logger.LogInformation("   - Access-Token: {TokenLength} characters", partnerToken?.Length ?? 0);
            _logger.LogInformation("   - app-client-id: {ClientId}", _configuration["Shoplazza:ClientId"]);

            _logger.LogInformation("🚀 Sending function creation request to Shoplazza...");
            var response = await _httpClient.PostAsync(endpoint, content);
            
            _logger.LogInformation("📡 Response received:");
            _logger.LogInformation("   - Status Code: {StatusCode}", response.StatusCode);
            _logger.LogInformation("   - Is Success: {IsSuccess}", response.IsSuccessStatusCode);
            _logger.LogInformation("   - Response Headers: {ResponseHeaders}", string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}")));
            
            // Read response body
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("📄 Response Content: {ResponseContent}", responseContent);
            
            _logger.LogDebug("CREATE Response: {StatusCode} (Success: {IsSuccess}) - {ResponseContent}", 
                response.StatusCode, response.IsSuccessStatusCode, responseContent);
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // Try to parse the response according to Shoplazza's documented format
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    // Parse response structure
                    var serializedResponse = JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true });
                    _logger.LogDebug("Parsed Response: {ResponseData}", serializedResponse);
                    
                    // Extract function ID according to Shoplazza's documented format
                    string? functionId = null;
                    try
                    {
                        // Use JsonElement for safer property access
                        if (responseData.TryGetProperty("data", out var dataElement) && 
                            dataElement.TryGetProperty("function_id", out var functionIdElement))
                        {
                            functionId = functionIdElement.GetString();
                            _logger.LogInformation("✅ Function ID extracted from data.function_id: {FunctionId}", functionId);
                        }
                        else if (responseData.TryGetProperty("function_id", out var directFunctionIdElement))
                        {
                            functionId = directFunctionIdElement.GetString();
                            _logger.LogInformation("✅ Function ID extracted from function_id: {FunctionId}", functionId);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Function ID not found in expected locations. Full response structure logged above.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Error accessing response properties: {Error}", ex.Message);
                    }
                    
                    _logger.LogInformation("Successfully created function {FunctionName} with ID {FunctionId}", 
                        request.Name, functionId);
                    
                    return (functionId, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing successful response from Shoplazza");
                    return (null, $"Error parsing response: {ex.Message}. Raw response: {responseContent}");
                }
            }
            else
            {
                _logger.LogError("❌ Function creation failed!");
                _logger.LogError("   - Function Name: {FunctionName}", request.Name);
                _logger.LogError("   - Status Code: {StatusCode}", response.StatusCode);
                _logger.LogError("   - Error Response: {Error}", responseContent);
                _logger.LogError("   - Request URL: {Endpoint}", endpoint);
                _logger.LogError("   - Request Payload: namespace=cart_transform, name={FunctionName}, file={WasmSize} bytes, source_code={SourceCodeLength} chars", 
                    request.Name, wasmBytes.Length, request.SourceCode?.Length ?? 0);
                return (null, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating function {FunctionName} for merchant {Shop}", request.Name, merchant.Shop);
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing function on Shoplazza's platform using partner token
    /// </summary>
    public async Task<bool> UpdateFunctionAsync(Merchant merchant, string functionId, FunctionUpdateRequest request)
    {
        try
        {
            _logger.LogInformation("Updating function {FunctionId} with name {FunctionName}", functionId, request.Name);

            // Get partner token for Function API
            var partnerToken = await _authService.GetPartnerTokenAsync();
            
            var endpoint = $"{GetApiBase()}/functions/{functionId}";

            // Create multipart form data according to Shoplazza Update Function API spec
            // HTTP spec: multipart/form-data with proper boundary handling
            var content = new MultipartFormDataContent();
            
            // Add form fields as per Shoplazza API requirements
            content.Add(new StringContent(request.Namespace ?? ""), "namespace");
            content.Add(new StringContent(request.Name ?? ""), "name");
            
            // CRITICAL FIX: Send WASM as binary data, not base64 string (same fix as CREATE endpoint)
            if (!string.IsNullOrEmpty(request.File))
            {
                var wasmBytes = Convert.FromBase64String(request.File);
                content.Add(new ByteArrayContent(wasmBytes), "file");
            }
            else
            {
                content.Add(new StringContent(""), "file");
            }
            
            content.Add(new StringContent(request.SourceCode ?? ""), "source_code");

            // Clear any existing headers and set partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            var response = await _httpClient.PatchAsync(endpoint, content);
            
            // Log response details for UPDATE endpoint (reduced logging)
            _logger.LogDebug("UPDATE Status: {StatusCode} (Success: {IsSuccess})", response.StatusCode, response.IsSuccessStatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("UPDATE Response: {ResponseContent}", responseContent);
                
                var updateResponse = JsonSerializer.Deserialize<FunctionUpdateResponse>(responseContent);
                
                _logger.LogInformation("Successfully updated function {FunctionId} with name {FunctionName}", 
                    functionId, request.Name);
                
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update function {FunctionId}. Status: {StatusCode}, Error: {Error}", 
                    functionId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating function {FunctionId} with name {FunctionName}", functionId, request.Name);
            return false;
        }
    }

    /// <summary>
    /// Binds a function to a specific shop's cart using Partner API authentication
    /// </summary>
    public async Task<bool> BindFunctionToShopAsync(Merchant merchant, string functionId, string merchantToken)
    {
        try
        {
            _logger.LogInformation("Binding function {FunctionId} to shop {Shop}", functionId, merchant.Shop);

            // Use shop-specific API endpoint for binding
            var endpoint = $"https://{merchant.Shop}/openapi/2024-07/function/cart-transform";

            // Create the binding payload
            var payload = new
            {
                function_id = functionId
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Validate merchant token
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Merchant token is null or empty for shop {Shop}", merchant.Shop);
                return false;
            }
            
            // Clear any existing headers and set Merchant API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", merchantToken);

            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully bound function {FunctionId} to shop {Shop}", functionId, merchant.Shop);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to bind function {FunctionId} to shop {Shop}. Status: {StatusCode}, Error: {Error}", 
                    functionId, merchant.Shop, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error binding function {FunctionId} to shop {Shop}", functionId, merchant.Shop);
            return false;
        }
    }

    /// <summary>
    /// Lists cart transform functions bound to a specific shop using Merchant API authentication
    /// </summary>
    public async Task<List<CartTransformFunction>?> GetCartTransformFunctionsAsync(Merchant merchant, string merchantToken)
    {
        try
        {
            _logger.LogInformation("Getting cart transform functions for shop {Shop}", merchant.Shop);

            // Use shop-specific API endpoint for listing cart transform functions
            var endpoint = $"https://{merchant.Shop}/openapi/2024-07/function/cart-transform/";

            // Validate merchant token
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Merchant token is null or empty for shop {Shop}", merchant.Shop);
                return null;
            }
            
            // Clear any existing headers and set Merchant API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", merchantToken);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<CartTransformFunctionListResponse>(responseContent);
                
                if (apiResponse?.Data?.CartTransforms != null)
                {
                    _logger.LogInformation("Successfully retrieved {Count} cart transform functions for shop {Shop}", 
                        apiResponse.Data.CartTransforms.Count, merchant.Shop);
                    return apiResponse.Data.CartTransforms;
                }
                
                _logger.LogInformation("No cart transform functions found for shop {Shop}", merchant.Shop);
                return new List<CartTransformFunction>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get cart transform functions for shop {Shop}. Status: {StatusCode}, Error: {Error}", 
                    merchant.Shop, response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart transform functions for shop {Shop}", merchant.Shop);
            return null;
        }
    }

    /// <summary>
    /// Updates a cart transform function bound to a specific shop
    /// </summary>
    public async Task<bool> UpdateCartTransformFunctionAsync(Merchant merchant, string functionBindingId, CartTransformFunctionUpdateRequest request, string merchantToken)
    {
        try
        {
            _logger.LogInformation("Updating cart transform function {FunctionBindingId} for shop {Shop}", functionBindingId, merchant.Shop);

            // Use shop-specific API endpoint for updating cart transform function
            var endpoint = $"https://{merchant.Shop}/openapi/2024-07/function/cart-transform/{functionBindingId}";

            // Create the request payload according to Shoplazza Update Cart Transform Function API spec
            var payload = new
            {
                block_on_failure = request.BlockOnFailure,
                input_query = request.InputQuery
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Validate merchant token
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Merchant token is null or empty for shop {Shop}", merchant.Shop);
                return false;
            }
            
            // Clear any existing headers and set Merchant API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", merchantToken);

            var response = await _httpClient.PatchAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated cart transform function {FunctionBindingId} for shop {Shop}", 
                    functionBindingId, merchant.Shop);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update cart transform function {FunctionBindingId} for shop {Shop}. Status: {StatusCode}, Error: {Error}", 
                    functionBindingId, merchant.Shop, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart transform function {FunctionBindingId} for shop {Shop}", functionBindingId, merchant.Shop);
            return false;
        }
    }

    /// <summary>
    /// Deletes a cart transform function from a specific shop using Merchant API authentication
    /// </summary>
    public async Task<bool> DeleteCartTransformFunctionAsync(Merchant merchant, string functionBindingId, string merchantToken)
    {
        try
        {
            _logger.LogInformation("Deleting cart transform function {FunctionBindingId} for shop {Shop}", functionBindingId, merchant.Shop);

            // Use shop-specific API endpoint for deleting cart transform function
            var endpoint = $"https://{merchant.Shop}/openapi/2024-07/function/cart-transform/{functionBindingId}";

            // Validate merchant token
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Merchant token is null or empty for shop {Shop}", merchant.Shop);
                return false;
            }
            
            // Clear any existing headers and set Merchant API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", merchantToken);

            var response = await _httpClient.DeleteAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted cart transform function {FunctionBindingId} for shop {Shop}", 
                    functionBindingId, merchant.Shop);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete cart transform function {FunctionBindingId} for shop {Shop}. Status: {StatusCode}, Error: {Error}", 
                    functionBindingId, merchant.Shop, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cart transform function {FunctionBindingId} for shop {Shop}", functionBindingId, merchant.Shop);
            return false;
        }
    }

    /// <summary>
    /// Binds a function to a specific shop's cart using Merchant API authentication
    /// </summary>
    public async Task<bool> BindCartTransformFunctionAsync(Merchant merchant, string functionId, string merchantToken)
    {
        try
        {
            _logger.LogInformation("Binding function {FunctionId} to shop {Shop}", functionId, merchant.Shop);

            // Use shop-specific API endpoint for binding
            var endpoint = $"https://{merchant.Shop}/openapi/2024-07/function/cart-transform";

            // Create the binding payload
            var payload = new
            {
                function_id = functionId
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Validate merchant token
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("Merchant token is null or empty for shop {Shop}", merchant.Shop);
                return false;
            }
            
            // Clear any existing headers and set Merchant API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", merchantToken);

            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully bound function {FunctionId} to shop {Shop}", functionId, merchant.Shop);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to bind function {FunctionId} to shop {Shop}. Status: {StatusCode}, Error: {Error}", 
                    functionId, merchant.Shop, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error binding function {FunctionId} to shop {Shop}", functionId, merchant.Shop);
            return false;
        }
    }

    /// <summary>
    /// Activates a function on Shoplazza's platform
    /// </summary>
    public async Task<bool> ActivateFunctionAsync(Merchant merchant, string functionId)
    {
        try
        {
            _logger.LogInformation("Activating function {FunctionId} for merchant {Shop}", functionId, merchant.Shop);

            var endpoint = $"{GetApiBase()}/functions/{functionId}/activate";

            // Get partner token for Partner API (Partner API authentication required)
            var partnerToken = await _authService.GetPartnerTokenAsync();
            
            // Clear any existing headers and set Partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            var response = await _httpClient.PostAsync(endpoint, null);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully activated function {FunctionId} for merchant {Shop}", functionId, merchant.Shop);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to activate function {FunctionId}. Status: {StatusCode}, Error: {Error}", 
                    functionId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating function {FunctionId} for merchant {Shop}", functionId, merchant.Shop);
            return false;
        }
    }

    /// <summary>
    /// Deletes a function from Shoplazza's platform using Partner API authentication
    /// </summary>
    public async Task<bool> DeleteFunctionAsync(Merchant merchant, string functionId)
    {
        try
        {
            _logger.LogInformation("Deleting function {FunctionId} for merchant {Shop}", functionId, merchant.Shop);

            var endpoint = $"{GetApiBase()}/functions/{functionId}";

            // Get partner token for Partner API (Partner API authentication required)
            var partnerToken = await _authService.GetPartnerTokenAsync();
            
            // Clear any existing headers and set Partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            var response = await _httpClient.DeleteAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted function {FunctionId} for merchant {Shop}", functionId, merchant.Shop);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete function {FunctionId}. Status: {StatusCode}, Error: {Error}", 
                    functionId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting function {FunctionId} for merchant {Shop}", functionId, merchant.Shop);
            return false;
        }
    }

    /// <summary>
    /// Gets the current status of a function using Partner API authentication
    /// </summary>
    public async Task<FunctionStatus> GetFunctionStatusAsync(Merchant merchant, string functionId)
    {
        try
        {
            _logger.LogDebug("Getting status for function {FunctionId} for merchant {Shop}", functionId, merchant.Shop);

            var endpoint = $"{GetApiBase()}/functions/{functionId}";

            // Get partner token for Partner API (Partner API authentication required)
            var partnerToken = await _authService.GetPartnerTokenAsync();
            
            // Clear any existing headers and set Partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var functionInfo = JsonSerializer.Deserialize<FunctionInfoResponse>(responseContent);
                
                var status = MapFunctionStatus(functionInfo?.Status);
                _logger.LogDebug("Function {FunctionId} status: {Status}", functionId, status);
                
                return status;
            }
            else
            {
                _logger.LogWarning("Failed to get function status {FunctionId}. Status: {StatusCode}", 
                    functionId, response.StatusCode);
                return FunctionStatus.Unknown;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting function status {FunctionId} for merchant {Shop}", functionId, merchant.Shop);
            return FunctionStatus.Unknown;
        }
    }

    /// <summary>
    /// Gets the API base URL from configuration
    /// </summary>
    private string GetApiBase()
    {
        return _configuration["ShoplazzaFunctionApi:BaseUrl"] ?? "https://partners.shoplazza.com/openapi/2024-07";
    }

    /// <summary>
    /// Maps Shoplazza function status to our FunctionStatus enum
    /// </summary>
    private FunctionStatus MapFunctionStatus(string? shoplazzaStatus)
    {
        return shoplazzaStatus?.ToLower() switch
        {
            "active" => FunctionStatus.Active,
            "inactive" => FunctionStatus.Pending,
            "failed" => FunctionStatus.Failed,
            "deleted" => FunctionStatus.Deleted,
            _ => FunctionStatus.Unknown
        };
    }

    /// <summary>
    /// Finds an existing function by name using the Partner API
    /// </summary>
    private async Task<string?> FindExistingFunctionByNameAsync(string functionName)
    {
        try
        {
            _logger.LogDebug("🔍 FIND EXISTING: Starting search for function name: {FunctionName}", functionName);
            
            // Get partner token for Function API
            var partnerToken = await _authService.GetPartnerTokenAsync();
            
            var endpoint = $"{GetApiBase()}/functions";
            
            // Clear any existing headers and set partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("🔍 FIND EXISTING: Partner API response content: {ResponseContent}", responseContent);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Look for functions with matching name
                if (responseData.TryGetProperty("data", out var dataElement) && 
                    dataElement.TryGetProperty("functions", out var functionsElement))
                {
                    _logger.LogDebug("🔍 FIND EXISTING: Found functions array with {Count} functions", functionsElement.GetArrayLength());
                    
                    foreach (var function in functionsElement.EnumerateArray())
                    {
                        // Try both field names: function_id (docs) and id (actual response)
                        string? functionId = null;
                        
                        if (function.TryGetProperty("function_id", out var functionIdElement))
                        {
                            functionId = functionIdElement.GetString();
                            _logger.LogDebug("🔍 FIND EXISTING: Found function_id field: {FunctionId}", functionId);
                        }
                        else if (function.TryGetProperty("id", out var idElement))
                        {
                            functionId = idElement.GetString();
                            _logger.LogDebug("🔍 FIND EXISTING: Found id field: {FunctionId}", functionId);
                        }
                        
                        if (function.TryGetProperty("name", out var nameElement))
                        {
                            var name = nameElement.GetString();
                            _logger.LogDebug("🔍 FIND EXISTING: Function name: {Name}", name);
                            
                            if (name == functionName && !string.IsNullOrEmpty(functionId))
                            {
                                _logger.LogDebug("🔍 FIND EXISTING: ✅ Found existing function {FunctionName} with ID {FunctionId}", functionName, functionId);
                                return functionId;
                            }
                        }
                    }
                }
            }
            
            _logger.LogDebug("🔍 FIND EXISTING: No existing function found with name: {FunctionName}", functionName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching for existing function with name: {FunctionName}", functionName);
            return null;
        }
    }

    /// <summary>
    /// Gets a list of all registered functions from Shoplazza (partner-level API)
    /// Note: This is a Partner API call that doesn't require merchant-specific information
    /// </summary>
    public async Task<List<FunctionDetails>?> GetFunctionDetailsAsync(string? functionId = null)
    {
        try
        {
            _logger.LogDebug("Getting function details from partner API");

            // Get partner token for Function API
            var partnerToken = await _authService.GetPartnerTokenAsync();
            
            var endpoint = $"{GetApiBase()}/functions";
            
            // Add query parameters if functionId is specified
            if (!string.IsNullOrEmpty(functionId))
            {
                endpoint += $"?function_id={functionId}";
            }

            // Clear any existing headers and set partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<FunctionListResponse>(responseContent);
                
                if (apiResponse?.Data?.Functions != null)
                {
                    _logger.LogDebug("Successfully retrieved {Count} functions from partner API", apiResponse.Data.Functions.Count);
                    return apiResponse.Data.Functions;
                }
                
                _logger.LogWarning("No functions found in partner API response");
                return new List<FunctionDetails>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get function details from partner API. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting function details from partner API");
            return null;
        }
    }

    /// <summary>
    /// Creates a global function using Partner API (no merchant required)
    /// </summary>
    public async Task<(string? FunctionId, string? ErrorDetails)> CreateGlobalFunctionAsync(FunctionRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Creating global function {FunctionName}", request.Name);

            // Get partner token for Function API
            var partnerToken = await _authService.GetPartnerTokenAsync();
            _logger.LogInformation("🔑 Partner token obtained, length: {TokenLength}", partnerToken?.Length ?? 0);
            
            var endpoint = $"{GetApiBase()}/functions";
            _logger.LogInformation("🌐 Global function creation endpoint: {Endpoint}", endpoint);
            
            // Create multipart form data according to Shoplazza Function API spec
            var content = new MultipartFormDataContent();
            
            // Add form fields as per Shoplazza API requirements
            content.Add(new StringContent("cart_transform"), "namespace");
            content.Add(new StringContent(request.Name), "name");
            
            var wasmBytes = Convert.FromBase64String(request.WasmBase64);
            content.Add(new ByteArrayContent(wasmBytes), "file");
            content.Add(new StringContent(request.SourceCode), "source_code");

            _logger.LogInformation("📦 Global function request payload details:");
            _logger.LogInformation("   - Namespace: cart_transform");
            _logger.LogInformation("   - Name: {FunctionName}", request.Name);
            _logger.LogInformation("   - WASM file size: {WasmSize} bytes", wasmBytes.Length);
            _logger.LogInformation("   - Source code length: {SourceCodeLength} characters", request.SourceCode?.Length ?? 0);

            // Clear any existing headers and set Partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            _logger.LogInformation("🔧 Global function request headers:");
            _logger.LogInformation("   - Access-Token: {TokenLength} characters", partnerToken?.Length ?? 0);
            _logger.LogInformation("   - app-client-id: {ClientId}", _configuration["Shoplazza:ClientId"]);

            _logger.LogInformation("🚀 Sending global function creation request to Shoplazza...");
            var response = await _httpClient.PostAsync(endpoint, content);
            
            _logger.LogInformation("📡 Response received:");
            _logger.LogInformation("   - Status Code: {StatusCode}", response.StatusCode);
            _logger.LogInformation("   - Is Success: {IsSuccess}", response.IsSuccessStatusCode);
            _logger.LogInformation("   - Response Headers: {Headers}", string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}")));

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📄 Response Content: {ResponseContent}", responseContent);
                
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseData.TryGetProperty("data", out var dataElement) && 
                    dataElement.TryGetProperty("function_id", out var functionIdElement))
                {
                    var functionId = functionIdElement.GetString();
                    _logger.LogInformation("✅ Global function created successfully with ID: {FunctionId}", functionId);
                    return (functionId, null);
                }
                else
                {
                    _logger.LogError("❌ Global function creation response missing function_id");
                    return (null, "Response missing function_id");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Global function creation failed!");
                _logger.LogError("   - Status Code: {StatusCode}", response.StatusCode);
                _logger.LogError("   - Error Content: {ErrorContent}", errorContent);
                _logger.LogError("   - Request Payload: namespace=cart_transform, name={FunctionName}, file={WasmSize} bytes, source_code={SourceCodeLength} chars",
                    request.Name, Convert.FromBase64String(request.WasmBase64).Length, request.SourceCode?.Length ?? 0);
                
                return (null, $"HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Global function creation failed with exception");
            return (null, $"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Activates a global function using Partner API
    /// </summary>
    public async Task<bool> ActivateGlobalFunctionAsync(string functionId)
    {
        try
        {
            _logger.LogInformation("Activating global function {FunctionId}", functionId);

            var endpoint = $"{GetApiBase()}/functions/{functionId}/activate";

            // Get partner token for Partner API (Partner API authentication required)
            var partnerToken = await _authService.GetPartnerTokenAsync();
            
            // Clear any existing headers and set Partner API headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Access-Token", partnerToken);
            _httpClient.DefaultRequestHeaders.Add("app-client-id", _configuration["Shoplazza:ClientId"]);

            var response = await _httpClient.PostAsync(endpoint, null);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Successfully activated global function {FunctionId}", functionId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Failed to activate global function {FunctionId}. Status: {StatusCode}, Error: {Error}", 
                    functionId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error activating global function {FunctionId}", functionId);
            return false;
        }
    }
}

/// <summary>
/// Response from Shoplazza Function API when creating a function
/// </summary>
public class FunctionCreateResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string? Code { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string? Message { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public FunctionCreateData? Data { get; set; }
}

/// <summary>
/// Function create response data
/// </summary>
public class FunctionCreateData
{
    [System.Text.Json.Serialization.JsonPropertyName("function_id")]
    public string? FunctionId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string? Version { get; set; }
}

/// <summary>
/// Response from Shoplazza Function API when getting function info
/// </summary>
public class FunctionInfoResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("function_id")]
    public string? FunctionId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response from Shoplazza Function API when updating a function
/// </summary>
public class FunctionUpdateResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string? Code { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string? Message { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public FunctionUpdateData? Data { get; set; }
}

/// <summary>
/// Function update response data
/// </summary>
public class FunctionUpdateData
{
    [System.Text.Json.Serialization.JsonPropertyName("function_id")]
    public string? FunctionId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string? Version { get; set; }
}
