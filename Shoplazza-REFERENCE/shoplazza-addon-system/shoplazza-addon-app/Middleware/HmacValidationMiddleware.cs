using ShoplazzaAddonApp.Models.Auth;
using ShoplazzaAddonApp.Services;
using System.Text;

namespace ShoplazzaAddonApp.Middleware;

/// <summary>
/// Middleware to validate HMAC signatures on webhook and sensitive endpoints
/// </summary>
public class HmacValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HmacValidationMiddleware> _logger;
    private readonly IConfiguration _configuration;

    // Endpoints that require HMAC validation (broad prefixes)
    private static readonly string[] ProtectedPaths = {
        "/api/webhooks",
        "/api/auth"
    };

    // Endpoints that are explicitly exempt (even if they match a protected prefix)
    private static readonly string[] ExemptPaths = {
        "/api/auth/status"
    };

    public HmacValidationMiddleware(
        RequestDelegate next,
        ILogger<HmacValidationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, IShoplazzaAuthService authService)
    {
        // Check if this path requires HMAC validation
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var requiresValidation = ProtectedPaths.Any(p => path.StartsWith(p.ToLower()))
                                  && !ExemptPaths.Any(p => path.Equals(p.ToLower(), StringComparison.Ordinal));

        // Allow if endpoint is exempt, or it's a preflight
        if (!requiresValidation || context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        // If already authenticated via cookie/session, skip HMAC validation (for app-internal calls)
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        try
        {
            var isValid = await ValidateRequestAsync(context, authService);
            
            if (!isValid)
            {
                _logger.LogWarning("HMAC validation failed for path: {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid signature");
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HMAC validation middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    private async Task<bool> ValidateRequestAsync(HttpContext context, IShoplazzaAuthService authService)
    {
        var request = context.Request;
        var hmacHeader = request.Headers["X-Shoplazza-Hmac-Sha256"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(hmacHeader))
        {
            // For query string-based auth (like /api/auth), check query parameters
            hmacHeader = request.Query["hmac"].FirstOrDefault();
        }

        // Check for demo mode - bypass HMAC validation for demo requests
        var isDemoMode = IsDemoRequest(context);
        if (isDemoMode)
        {
            _logger.LogInformation("Demo mode detected - bypassing HMAC validation for path: {Path}", context.Request.Path);
            return true;
        }

        if (string.IsNullOrEmpty(hmacHeader))
        {
            _logger.LogWarning("No HMAC signature found in request");
            return false;
        }

        string rawData;

        if (request.Method == "GET")
        {
            // For GET requests, use the RAW query string (preserve encoding) per Shoplazza/Shopify-style HMAC rules
            var rawQuery = request.QueryString.Value ?? string.Empty; // includes leading '?'
            var query = rawQuery.StartsWith("?") ? rawQuery.Substring(1) : rawQuery;

            // Remove the hmac parameter without decoding/re-encoding
            var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            var filteredPairs = pairs.Where(p => !p.StartsWith("hmac=", StringComparison.OrdinalIgnoreCase));

            // Sort by key (left side of '=') using ordinal comparison
            var sortedPairs = filteredPairs
                .Select(p => new { Pair = p, Key = p.Split('=')[0] })
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => x.Pair);

            rawData = string.Join("&", sortedPairs);
        }
        else
        {
            // For POST/PUT requests, use request body
            request.EnableBuffering();
            request.Body.Position = 0;
            
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            rawData = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        // Use ClientSecret for OAuth query-based validation (/api/auth),
        // and WebhookSecret for webhook requests (headers path under /api/webhooks)
        var clientSecret = _configuration["Shoplazza:ClientSecret"];        
        var webhookSecret = _configuration["Shoplazza:WebhookSecret"];

        // Default to client secret if webhook secret is not set
        if (string.IsNullOrEmpty(webhookSecret))
        {
            webhookSecret = clientSecret ?? string.Empty;
        }
        
        // Determine which secret to use based on path
        var reqPath = request.Path.Value?.ToLower() ?? string.Empty;
        var secretToUse = reqPath.StartsWith("/api/webhooks") ? webhookSecret : (clientSecret ?? webhookSecret);
        if (string.IsNullOrEmpty(secretToUse))
        {
            _logger.LogError("Shoplazza secrets not configured");
            return false;
        }

        // Parse timestamp if available
        DateTime? timestamp = null;
        var timestampHeader = request.Headers["X-Shoplazza-Triggered-At"].FirstOrDefault() 
                            ?? request.Query["timestamp"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(timestampHeader) && DateTime.TryParse(timestampHeader, out var parsedTimestamp))
        {
            timestamp = parsedTimestamp;
        }

        var validationModel = new HmacValidationModel
        {
            RawData = rawData,
            ProvidedHmac = hmacHeader,
            SecretKey = secretToUse,
            RequestTimestamp = timestamp
        };

        var valid = await authService.ValidateHmacAsync(validationModel);
        if (valid)
        {
            // Mark request as already validated so controllers can skip duplicate validation
            context.Items["HmacValidated"] = true;
        }
        return valid;
    }

    /// <summary>
    /// Determines if this is a demo request that should bypass HMAC validation
    /// </summary>
    private bool IsDemoRequest(HttpContext context)
    {
        var request = context.Request;
        
        // Check for demo indicators
        var isDemoShop = request.Query["shop"].ToString().Contains("demo-store") ||
                        request.Headers["X-Demo-Mode"].Any() ||
                        request.Headers["User-Agent"].ToString().Contains("Demo");
        
        // Check if request is from localhost (development)
        var isLocalhost = context.Request.Host.Host.Contains("localhost") ||
                         context.Request.Host.Host.Contains("127.0.0.1");
        
        // Check for demo signature header
        var hasDemoSignature = request.Headers["X-Shoplazza-Hmac-Sha256"].ToString() == "demo-signature" ||
                              request.Headers["X-Shoplazza-Hmac-Sha256"].ToString() == "demo-hmac-signature";
        
        // Allow demo requests in development environment
        var isDevelopment = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";
        
        return (isDemoShop || hasDemoSignature) && (isLocalhost || isDevelopment);
    }
}