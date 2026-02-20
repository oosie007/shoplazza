using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service for building and managing cart-transform WASM functions
/// </summary>
public class CartTransformFunctionService : ICartTransformFunctionService
{
    private readonly ILogger<CartTransformFunctionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _wasmSourcePath;

    public CartTransformFunctionService(
        ILogger<CartTransformFunctionService> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _wasmSourcePath = Path.Combine(Directory.GetCurrentDirectory(), "cart-transform-function");
    }

    /// <summary>
    /// Builds the cart-transform WASM file from source
    /// </summary>
    public async Task<byte[]> BuildWasmAsync()
    {
        try
        {
            _logger.LogInformation("Building WASM file from source in {WasmSourcePath}", _wasmSourcePath);

            // Always try to use pre-built WASM first (production deployment)
            var fallbackPath = await GetFallbackWasmPathAsync();
            if (fallbackPath != null && File.Exists(fallbackPath))
            {
                _logger.LogInformation("Using pre-built WASM file from {FallbackPath}", fallbackPath);
                var preBuiltWasmBytes = await File.ReadAllBytesAsync(fallbackPath);
                
                // Validate the pre-built WASM file
                if (await ValidateWasmAsync(preBuiltWasmBytes))
                {
                    _logger.LogInformation("✅ Successfully loaded pre-built WASM file, size: {Size} bytes, path: {WasmPath}", preBuiltWasmBytes.Length, fallbackPath);
                    
                    // Log which type of WASM we're using
                    if (fallbackPath.Contains("cart-transform-shoplazza.wasm"))
                    {
                        _logger.LogInformation("🎯 Using NEWLY BUILT WASM with latest Rust implementation for cart transform functionality");
                    }
                    else if (fallbackPath.Contains("cart-transform-universal.wasm"))
                    {
                        _logger.LogInformation("🔄 Using FALLBACK UNIVERSAL WASM with correct Rust implementation");
                    }
                    else if (fallbackPath.Contains("cart-transform-rust.wasm"))
                    {
                        _logger.LogInformation("🔄 Using LEGACY RUST WASM implementation");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Using unexpected WASM file: {WasmPath}", fallbackPath);
                    }
                    
                    return preBuiltWasmBytes;
                }
                else
                {
                    _logger.LogWarning("Pre-built WASM file failed validation, attempting to build from source");
                }
            }

            // Check if build environment is ready for building from source
            if (!await IsBuildEnvironmentReadyAsync())
            {
                _logger.LogWarning("Build environment not ready and no valid fallback WASM available");
                throw new InvalidOperationException("Build environment not ready and no valid fallback WASM available");
            }

            // Build WASM using the existing deploy.sh script
            var wasmPath = await BuildWasmFileAsync();
            
            if (!File.Exists(wasmPath))
            {
                throw new InvalidOperationException($"WASM file not generated at expected path: {wasmPath}");
            }

            var builtWasmBytes = await File.ReadAllBytesAsync(wasmPath);
            _logger.LogInformation("Successfully built WASM file from source, size: {Size} bytes", builtWasmBytes.Length);

            // Validate the generated WASM file
            if (!await ValidateWasmAsync(builtWasmBytes))
            {
                throw new InvalidOperationException("Generated WASM file failed validation");
            }

            return builtWasmBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building WASM file");
            throw;
        }
    }

    /// <summary>
    /// Builds the cart-transform WASM file and returns it as base64 string
    /// </summary>
    public async Task<string> BuildWasmBase64Async()
    {
        var wasmBytes = await BuildWasmAsync();
        return Convert.ToBase64String(wasmBytes);
    }

    /// <summary>
    /// Validates that a WASM file is properly formatted
    /// </summary>
    public async Task<bool> ValidateWasmAsync(byte[] wasmBytes)
    {
        try
        {
            if (wasmBytes == null || wasmBytes.Length == 0)
            {
                _logger.LogWarning("WASM file is null or empty");
                return false;
            }

            // Check file size (Shoplazza limit is 2MB)
            var maxSize = _configuration.GetValue<int>("ShoplazzaFunctionApi:MaxWasmSizeBytes", 2097152); // 2MB default (Shoplazza's limit)
            if (wasmBytes.Length > maxSize)
            {
                _logger.LogWarning("WASM file size {Size} bytes exceeds maximum allowed size {MaxSize} bytes (Shoplazza 2MB limit)", 
                    wasmBytes.Length, maxSize);
                return false;
            }

            if (wasmBytes.Length < 100)  // Allow ultra-minimal WASM files (like our 149-byte test file)
            {
                _logger.LogWarning("WASM file size {Size} bytes seems unusually small", wasmBytes.Length);
                // Don't reject small files - they might be valid minimal implementations
            }

            // Check WASM file header (WASM files start with specific bytes: 0x00 0x61 0x73 0x6D)
            if (wasmBytes.Length < 4 || 
                wasmBytes[0] != 0x00 || wasmBytes[1] != 0x61 || 
                wasmBytes[2] != 0x73 || wasmBytes[3] != 0x6D)
            {
                _logger.LogError("Invalid WASM file header - file does not appear to be a valid WebAssembly file");
                return false;
            }

            _logger.LogDebug("WASM file validation passed, size: {Size} bytes", wasmBytes.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating WASM file");
            return false;
        }
    }

    /// <summary>
    /// Gets the path to the pre-built WASM file as fallback
    /// </summary>
    public async Task<string?> GetFallbackWasmPathAsync()
    {
        try
        {
            // Get the web root path properly for both local and Azure environments
            var webRootPath = GetWebRootPath();
            
            // Check for pre-built WASM in wwwroot/wasm first (production deployment)
            var productionPaths = new[]
            {
                Path.Combine(webRootPath, "wwwroot", "wasm", "cart-transform-rust.wasm"), // PRIMARY: Working 180KB WASM
                Path.Combine(webRootPath, "wwwroot", "wasm", "cart-transform-universal.wasm"), // FALLBACK: Working WASM with correct source code
                Path.Combine(webRootPath, "wwwroot", "wasm", "cart-transform-rust.wasm"),       // LEGACY: Manual copy
                Path.Combine(webRootPath, "wasm", "cart-transform-shoplazza.wasm"),             // Local development - newly built WASM
                Path.Combine(webRootPath, "wasm", "cart-transform-universal.wasm"),             // Local fallback - working WASM
                Path.Combine(webRootPath, "wasm", "cart-transform-rust.wasm"),                 // Local legacy - manual copy
                // REMOVED: cart-transform.wasm (1.3MB - too large and problematic)
            };

            foreach (var path in productionPaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation("✅ Found production WASM file at {ProductionPath}", path);
                    return path;
                }
                else
                {
                    _logger.LogDebug("❌ WASM file not found at {ProductionPath}", path);
                }
            }

            // Check for pre-built WASM in the cart-transform-function directory (development)
            var developmentPath = Path.Combine(_wasmSourcePath, "cart-transform-rust.wasm");
            if (File.Exists(developmentPath))
            {
                _logger.LogInformation("Found development WASM file at {DevelopmentPath}", developmentPath);
                return developmentPath;
            }

            _logger.LogWarning("No pre-built WASM file found in wwwroot/wasm or cart-transform-function directory");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for pre-built WASM file");
            return null;
        }
    }

    /// <summary>
    /// Gets the web root path for both local and Azure environments
    /// </summary>
    private string GetWebRootPath()
    {
        // Try multiple possible paths for different environments
        var possiblePaths = new[]
        {
            // Azure App Service Linux path (most specific)
            "/home/site/wwwroot",
            // Azure App Service Windows path
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            // Local development path
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            // Fallback to current directory
            Directory.GetCurrentDirectory()
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                _logger.LogDebug("Using web root path: {WebRootPath}", path);
                return path;
            }
        }

        // Default fallback
        var fallbackPath = Directory.GetCurrentDirectory();
        _logger.LogWarning("Using fallback web root path: {FallbackPath}", fallbackPath);
        return fallbackPath;
    }

    /// <summary>
    /// Checks if the WASM build environment is properly configured
    /// </summary>
    public async Task<bool> IsBuildEnvironmentReadyAsync()
    {
        try
        {
            // Check if cart-transform-function directory exists
            if (!Directory.Exists(_wasmSourcePath))
            {
                _logger.LogWarning("WASM source directory does not exist: {WasmSourcePath}", _wasmSourcePath);
                return false;
            }

            // Check if package.json exists
            var packageJsonPath = Path.Combine(_wasmSourcePath, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                _logger.LogWarning("package.json not found in WASM source directory: {PackageJsonPath}", packageJsonPath);
                return false;
            }

            // Check if deploy.sh exists
            var deployScriptPath = Path.Combine(_wasmSourcePath, "deploy.sh");
            if (!File.Exists(deployScriptPath))
            {
                _logger.LogWarning("deploy.sh not found in WASM source directory: {DeployScriptPath}", deployScriptPath);
                return false;
            }

            // Check if Node.js is available
            if (!await IsNodeJsAvailableAsync())
            {
                _logger.LogWarning("Node.js is not available in the current environment");
                return false;
            }

            _logger.LogDebug("WASM build environment is ready");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking WASM build environment");
            return false;
        }
    }

    /// <summary>
    /// Builds the WASM file using the existing deploy.sh script
    /// </summary>
    private async Task<string> BuildWasmFileAsync()
    {
        try
        {
            _logger.LogInformation("Building WASM file using deploy.sh script");

            // Run the deploy.sh script to build the WASM file
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = "deploy.sh",
                    WorkingDirectory = _wasmSourcePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("deploy.sh script failed with exit code {ExitCode}. Error: {Error}", 
                    process.ExitCode, error);
                throw new InvalidOperationException($"deploy.sh script failed: {error}");
            }

            _logger.LogInformation("deploy.sh script completed successfully. Output: {Output}", output);

            // Check if WASM file was generated (try both possible names)
            var wasmPath = Path.Combine(_wasmSourcePath, "cart-transform-universal.wasm");
            if (!File.Exists(wasmPath))
            {
                // Fallback to old name
                wasmPath = Path.Combine(_wasmSourcePath, "cart-transform.wasm");
                if (!File.Exists(wasmPath))
                {
                    throw new InvalidOperationException("WASM file not generated by deploy.sh script");
                }
            }

            return wasmPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building WASM file using deploy.sh script");
            throw;
        }
    }

    /// <summary>
    /// Checks if Node.js is available in the current environment
    /// </summary>
    private async Task<bool> IsNodeJsAvailableAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("Node.js version detected: {Version}", output.Trim());
                return true;
            }

            _logger.LogWarning("Node.js not available. Exit code: {ExitCode}, Error: {Error}", 
                process.ExitCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Node.js check failed: {Error}", ex.Message);
            return false;
        }
    }
}
