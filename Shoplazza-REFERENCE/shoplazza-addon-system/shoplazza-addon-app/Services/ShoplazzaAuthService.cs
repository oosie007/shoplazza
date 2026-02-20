using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using Newtonsoft.Json;
using ShoplazzaAddonApp.Models.Auth;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Implementation of Shoplazza authentication and authorization services
/// </summary>
public class ShoplazzaAuthService : IShoplazzaAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShoplazzaAuthService> _logger;
    
    // In-memory storage for demo - replace with database in production
    private static readonly Dictionary<string, ShoplazzaAuthResponse> _tokenStorage = new();

    public ShoplazzaAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShoplazzaAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ValidateHmacAsync(HmacValidationModel validationModel)
    {
        try
        {
            if (string.IsNullOrEmpty(validationModel.RawData) || 
                string.IsNullOrEmpty(validationModel.ProvidedHmac) ||
                string.IsNullOrEmpty(validationModel.SecretKey))
            {
                _logger.LogWarning("HMAC validation failed: Missing required data");
                return false;
            }

            // Check timestamp if provided
            if (validationModel.RequestTimestamp.HasValue)
            {
                var timeDiff = Math.Abs((DateTime.UtcNow - validationModel.RequestTimestamp.Value).TotalSeconds);
                if (timeDiff > validationModel.TimestampTolerance)
                {
                    _logger.LogWarning("HMAC validation failed: Request timestamp too old");
                    return false;
                }
            }

            // Calculate HMAC-SHA256
            var keyBytes = Encoding.UTF8.GetBytes(validationModel.SecretKey);
            var dataBytes = Encoding.UTF8.GetBytes(validationModel.RawData);

            using var hmac = new HMACSHA256(keyBytes);
            var computedHash = hmac.ComputeHash(dataBytes);

            // Shoplazza may provide Base64 (headers) or hex (query param) encoded HMAC
            var computedBase64 = Convert.ToBase64String(computedHash);
            var computedHex = BitConverter.ToString(computedHash).Replace("-", string.Empty).ToLowerInvariant();

            var provided = validationModel.ProvidedHmac.Trim();
            var isValid = string.Equals(computedBase64, provided, StringComparison.Ordinal) ||
                          string.Equals(computedHex, provided.ToLowerInvariant(), StringComparison.Ordinal);
            
            if (!isValid)
            {
                _logger.LogWarning("HMAC validation failed: Signature mismatch");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating HMAC signature");
            return false;
        }
    }

    public string GenerateOAuthUrl(string shop, string[] scopes, string? state = null)
    {
        var clientId = _configuration["Shoplazza:ClientId"];
        var redirectUri = _configuration["Shoplazza:RedirectUri"];
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
        {
            throw new InvalidOperationException("Shoplazza OAuth configuration is missing");
        }

        // Shoplazza expects scopes space-delimited
        var scopeString = string.Join(" ", scopes);
        var encodedRedirectUri = UrlEncoder.Default.Encode(redirectUri);
        var encodedScopes = UrlEncoder.Default.Encode(scopeString);

        var url = $"https://{shop}/admin/oauth/authorize" +
                  $"?client_id={clientId}" +
                  $"&scope={encodedScopes}" +
                  $"&redirect_uri={encodedRedirectUri}" +
                  $"&response_type=code";

        if (!string.IsNullOrEmpty(state))
        {
            url += $"&state={UrlEncoder.Default.Encode(state)}";
        }

        return url;
    }

    public async Task<ShoplazzaAuthResponse> ExchangeCodeForTokenAsync(string shop, string code)
    {
        try
        {
            var clientId = _configuration["Shoplazza:ClientId"];
            var clientSecret = _configuration["Shoplazza:ClientSecret"];
            var redirectUri = _configuration["Shoplazza:RedirectUri"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Shoplazza OAuth configuration is missing");
            }

            var tokenUrl = $"https://{shop}/admin/oauth/token";

            // Per OAuth2 spec, send form-url-encoded with grant_type
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId ?? string.Empty,
                ["client_secret"] = clientSecret ?? string.Empty,
                ["code"] = code,
                ["redirect_uri"] = redirectUri ?? string.Empty
            };

            var content = new FormUrlEncodedContent(form);

            _logger.LogInformation("Token exchange POST {TokenUrl} (grant_type=authorization_code) for shop {Shop}", tokenUrl, shop);

            var response = await _httpClient.PostAsync(tokenUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var snippet = errorContent.Length > 300 ? errorContent.Substring(0, 300) + "..." : errorContent;
                _logger.LogError("Token exchange failed: {StatusCode} - {Content}", 
                    response.StatusCode, snippet);
                throw new HttpRequestException($"Token exchange failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

            return new ShoplazzaAuthResponse
            {
                AccessToken = tokenResponse?.access_token ?? string.Empty,
                TokenType = tokenResponse?.token_type ?? "Bearer",
                Scope = tokenResponse?.scope ?? string.Empty,
                Shop = shop,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for token");
            throw;
        }
    }

    public async Task<bool> ValidateAuthRequestAsync(ShoplazzaAuthRequest authRequest)
    {
        try
        {
            var webhookSecret = _configuration["Shoplazza:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("Webhook secret not configured");
                return false;
            }

            // Parse timestamp
            if (!DateTime.TryParse(authRequest.Timestamp, out var timestamp))
            {
                _logger.LogWarning("Invalid timestamp in auth request");
                return false;
            }

            // Build raw data string for HMAC validation
            var rawData = $"shop={authRequest.Shop}&timestamp={authRequest.Timestamp}";
            
            // Add additional parameters in alphabetical order
            if (authRequest.AdditionalParams.Any())
            {
                var sortedParams = authRequest.AdditionalParams.OrderBy(p => p.Key);
                foreach (var param in sortedParams)
                {
                    rawData += $"&{param.Key}={param.Value}";
                }
            }

            var validationModel = new HmacValidationModel
            {
                RawData = rawData,
                ProvidedHmac = authRequest.Hmac,
                SecretKey = webhookSecret,
                RequestTimestamp = timestamp
            };

            return await ValidateHmacAsync(validationModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating auth request");
            return false;
        }
    }

    public async Task<string?> GetAccessTokenAsync(string shop)
    {
        await Task.CompletedTask; // Async placeholder
        
        if (_tokenStorage.TryGetValue(shop, out var authResponse))
        {
            return authResponse.AccessToken;
        }

        return null;
    }

    public async Task StoreAccessTokenAsync(string shop, ShoplazzaAuthResponse authResponse)
    {
        await Task.CompletedTask; // Async placeholder
        
        _tokenStorage[shop] = authResponse;
        _logger.LogInformation("Stored access token for shop: {Shop}", shop);
    }

    public async Task RemoveAuthDataAsync(string shop)
    {
        await Task.CompletedTask; // Async placeholder
        
        if (_tokenStorage.Remove(shop))
        {
            _logger.LogInformation("Removed auth data for shop: {Shop}", shop);
        }
    }

    /// <summary>
    /// Gets a partner-level access token for Function API calls using client credentials
    /// </summary>
    public async Task<string> GetPartnerTokenAsync()
    {
        try
        {
            var clientId = _configuration["Shoplazza:ClientId"];
            var clientSecret = _configuration["Shoplazza:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Shoplazza ClientId or ClientSecret configuration is missing");
            }

            var tokenUrl = "https://partners.shoplazza.com/partner/oauth/token";

            // Partner OAuth uses client_credentials grant type
            var payload = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "client_credentials"
            };

            var jsonContent = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Requesting partner token from {TokenUrl}", tokenUrl);

            var response = await _httpClient.PostAsync(tokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var snippet = errorContent.Length > 300 ? errorContent.Substring(0, 300) + "..." : errorContent;
                _logger.LogError("Partner token request failed: {StatusCode} - {Content}", 
                    response.StatusCode, snippet);
                throw new HttpRequestException($"Partner token request failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

            var accessToken = tokenResponse?.access_token?.ToString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Partner token response missing access_token");
            }

            _logger.LogInformation("Successfully obtained partner token");
            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining partner token");
            throw;
        }
    }
}