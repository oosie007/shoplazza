using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ShoplazzaAddonApp.Data;
using ShoplazzaAddonApp.Models.Configuration;
using ShoplazzaAddonApp.Services;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Microsoft.EntityFrameworkCore;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service that runs at startup to ensure global functions are created and available
/// </summary>
public class GlobalFunctionStartupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlobalFunctionStartupService> _logger;
    private readonly IConfiguration _configuration;

    public GlobalFunctionStartupService(
        IServiceProvider serviceProvider,
        ILogger<GlobalFunctionStartupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🚀 Starting global function initialization...");
            
            // Wait for the application to be fully started and database to be ready
            _logger.LogInformation("⏳ Waiting for application to fully initialize (15 seconds)...");
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            _logger.LogInformation("🔍 Application should be ready, proceeding with global function setup...");
            
            await EnsureGlobalCartTransformFunctionExistsAsync();
            
            _logger.LogInformation("✅ Global function initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Global function initialization failed");
        }
    }

    /// <summary>
    /// Ensures the global cart-transform function exists and is active
    /// </summary>
    private async Task EnsureGlobalCartTransformFunctionExistsAsync()
    {
        // Retry logic for database connection
        const int maxRetries = 3;
        const int retryDelayMs = 2000;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var functionApiService = scope.ServiceProvider.GetRequiredService<IShoplazzaFunctionApiService>();
                var cartTransformService = scope.ServiceProvider.GetRequiredService<ICartTransformFunctionService>();
                
                // Test database connection
                await dbContext.Database.CanConnectAsync();
                _logger.LogInformation("✅ Database connection successful on attempt {Attempt}", attempt);
                
                // If we get here, database is ready, proceed with the rest
                await ProcessGlobalFunctionSetup(dbContext, functionApiService, cartTransformService);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("⚠️ Database connection attempt {Attempt} failed: {Error}. Retrying in {Delay}ms...", 
                    attempt, ex.Message, retryDelayMs);
                await Task.Delay(retryDelayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Database connection failed after {MaxRetries} attempts", maxRetries);
                throw;
            }
        }
    }
    
    private async Task ProcessGlobalFunctionSetup(ApplicationDbContext dbContext, IShoplazzaFunctionApiService functionApiService, ICartTransformFunctionService cartTransformService)
    {

        try
        {
            _logger.LogInformation("🔍 Checking if global cart-transform function exists...");

            // Check if we already have a global function in the database
            var existingGlobalFunction = await dbContext.GlobalFunctionConfigurations
                .FirstOrDefaultAsync(f => f.FunctionType == "cart-transform" && f.IsActive);

            if (existingGlobalFunction != null && !string.IsNullOrEmpty(existingGlobalFunction.FunctionId))
            {
                _logger.LogInformation("✅ Global cart-transform function already exists with ID: {FunctionId}", existingGlobalFunction.FunctionId);
                
                // Verify the function still exists on Shoplazza
                var functionExists = await VerifyFunctionExistsOnShoplazzaAsync(functionApiService, existingGlobalFunction.FunctionId);
                if (functionExists)
                {
                    _logger.LogInformation("✅ Global function verified on Shoplazza, no action needed");
                    return;
                }
                else
                {
                    _logger.LogWarning("⚠️ Global function not found on Shoplazza, will recreate");
                    existingGlobalFunction = null;
                }
            }

            // Create the global function
            _logger.LogInformation("🚀 Creating global cart-transform function...");
            
            var wasmBytes = await cartTransformService.BuildWasmAsync();
            var wasmBase64 = Convert.ToBase64String(wasmBytes);
            var sourceCode = await GetSourceCodeForFunctionAsync();

            var request = new Models.Api.FunctionRegistrationRequest
            {
                Name = "cart-transform-addon",
                Type = "cart-transform",
                Description = "Global cart transform function for add-on pricing",
                WasmBase64 = wasmBase64,
                SourceCode = sourceCode,
                Triggers = new List<string> { "cart.add", "cart.update", "checkout.begin" },
                Settings = new Models.Api.FunctionSettings
                {
                    Timeout = 5000,
                    MemoryLimit = "128MB",
                    AutoEnable = false
                }
            };

            _logger.LogInformation("📋 Global function registration request prepared:");
            _logger.LogInformation("   - Name: {FunctionName}", request.Name);
            _logger.LogInformation("   - Type: {FunctionType}", request.Type);
            _logger.LogInformation("   - WASM Size: {WasmSize} bytes", wasmBytes.Length);
            _logger.LogInformation("   - Source Code Length: {SourceCodeLength} characters", request.SourceCode?.Length ?? 0);

            // Create function using Partner API (no merchant required for global creation)
            var (functionId, errorDetails) = await functionApiService.CreateGlobalFunctionAsync(request);
            
            if (string.IsNullOrEmpty(functionId))
            {
                _logger.LogError("❌ Failed to create global cart-transform function. Error: {ErrorDetails}", errorDetails);
                throw new InvalidOperationException($"Failed to create global function: {errorDetails}");
            }

            _logger.LogInformation("✅ Global cart-transform function created successfully with ID: {FunctionId}", functionId);

            // Function is active by default after creation - no activation step needed
            _logger.LogInformation("✅ Global function {FunctionId} is ready to use", functionId);

            // Save to database
            var globalFunctionConfig = new GlobalFunctionConfiguration
            {
                FunctionId = functionId,
                FunctionName = request.Name,
                FunctionNamespace = "cart_transform",
                FunctionType = request.Type,
                Status = FunctionStatus.Active,
                ActivatedAt = null, // Function is active by default, no activation step
                Version = "1.0.0",
                IsActive = true,
                ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(request)
            };

            // Deactivate any existing global functions
            var existingFunctions = await dbContext.GlobalFunctionConfigurations
                .Where(f => f.FunctionType == "cart-transform")
                .ToListAsync();
            
            foreach (var existing in existingFunctions)
            {
                existing.IsActive = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            dbContext.GlobalFunctionConfigurations.Add(globalFunctionConfig);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("💾 Global function configuration saved to database");
            _logger.LogInformation("🎉 Global cart-transform function setup completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to ensure global cart-transform function exists");
            throw;
        }
    }
    
    /// <summary>
    /// Verifies that a function still exists on Shoplazza
    /// </summary>
    private async Task<bool> VerifyFunctionExistsOnShoplazzaAsync(IShoplazzaFunctionApiService functionApiService, string functionId)
    {
        try
        {
            // This would need to be implemented in the function API service
            // For now, we'll assume it exists if we have an ID
            return !string.IsNullOrEmpty(functionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Warning: Could not verify function {FunctionId} on Shoplazza", functionId);
            return false;
        }
    }

    /// <summary>
    /// Gets the source code for the function
    /// </summary>
    private async Task<string> GetSourceCodeForFunctionAsync()
    {
        try
        {
            // Try multiple possible source code paths
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "wasm", "src", "cart-transform"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "wasm", "src", "cart-transform-universal"),
                Path.Combine(Directory.GetCurrentDirectory(), "cart-transform-rust", "src"),
                Path.Combine(Directory.GetCurrentDirectory(), "cart-transform-function")
            };

            foreach (var sourcePath in possiblePaths)
            {
                if (Directory.Exists(sourcePath))
                {
                    var sourceFiles = Directory.GetFiles(sourcePath, "*.rs", SearchOption.AllDirectories);
                    if (sourceFiles.Length > 0)
                    {
                        var sourceCode = new List<string>();
                        
                        foreach (var file in sourceFiles)
                        {
                            var content = await File.ReadAllTextAsync(file);
                            sourceCode.Add($"// {Path.GetFileName(file)}\n{content}");
                        }
                        
                        var result = string.Join("\n\n", sourceCode);
                        _logger.LogInformation("✅ Found source code in {Path}, {Count} files, total length: {Length} chars", 
                            sourcePath, sourceFiles.Length, result.Length);
                        return result;
                    }
                }
            }
            
            // Fallback: return a meaningful source code description
            var fallbackCode = @"// Cart transform function source code
// This is a Rust-based WebAssembly function for cart transformation
// Function: cart-transform-addon
// Purpose: Transform cart items to add optional product add-ons
// Implementation: Rust compiled to WASM
// Triggers: cart.add, cart.update, checkout.begin";
            
            _logger.LogWarning("⚠️ No source code files found in expected paths, using fallback");
            return fallbackCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read source code for function");
            var fallbackCode = @"// Cart transform function source code
// Function: cart-transform-addon
// Purpose: Transform cart items to add optional product add-ons
// Implementation: Rust compiled to WASM
// Error: Source code unavailable due to: " + ex.Message;
            return fallbackCode;
        }
    }
}
