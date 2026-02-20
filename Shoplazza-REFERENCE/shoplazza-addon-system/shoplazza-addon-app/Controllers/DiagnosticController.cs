using Microsoft.AspNetCore.Mvc;
using System.Text;
using ShoplazzaAddonApp.Services;
using ShoplazzaAddonApp.Models.Api;
using ShoplazzaAddonApp.Data.Entities;
using Microsoft.Extensions.Configuration;
using ShoplazzaAddonApp.Data;
using Microsoft.EntityFrameworkCore;
using ShoplazzaAddonApp.Models.Configuration;

namespace ShoplazzaAddonApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticController : ControllerBase
    {
        private readonly ILogger<DiagnosticController> _logger;
        private readonly string _wasmDirectory;
        private readonly IShoplazzaFunctionApiService _shoplazzaService;
        private readonly IConfiguration _configuration;

        public DiagnosticController(
            ILogger<DiagnosticController> logger, 
            IWebHostEnvironment environment,
            IConfiguration configuration,
            IShoplazzaFunctionApiService shoplazzaService)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Check if we have a local development WASM directory configured
            var localWasmPath = configuration["LocalWasmDirectory"];
            if (!string.IsNullOrEmpty(localWasmPath) && Directory.Exists(localWasmPath))
            {
                _wasmDirectory = localWasmPath;
                _logger.LogInformation("Using local WASM directory: {LocalPath}", localWasmPath);
            }
            else
            {
                // Fallback: Check if we're in development and local wwwroot/wasm exists
                var devWasmPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "wasm");
                if (environment.IsDevelopment() && Directory.Exists(devWasmPath))
                {
                    _wasmDirectory = devWasmPath;
                    _logger.LogInformation("Using development WASM directory: {DevPath}", devWasmPath);
                }
                else
                {
                    _wasmDirectory = Path.Combine(environment.WebRootPath, "wasm");
                    _logger.LogInformation("Using Azure WASM directory: {AzurePath}", _wasmDirectory);
                }
            }
            
            _shoplazzaService = shoplazzaService;
        }

        [HttpPost("upload-wasm")]
        public async Task<IActionResult> UploadWasm(IFormFile wasmFile)
        {
            try
            {
                if (wasmFile == null || wasmFile.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                if (!wasmFile.FileName.EndsWith(".wasm"))
                {
                    return BadRequest("File must be a .wasm file");
                }

                // Ensure wasm directory exists
                Directory.CreateDirectory(_wasmDirectory);

                // Save the file
                var filePath = Path.Combine(_wasmDirectory, wasmFile.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await wasmFile.CopyToAsync(stream);
                }

                _logger.LogInformation("WASM file uploaded: {FileName} ({Size} bytes)", 
                    wasmFile.FileName, wasmFile.Length);

                return Ok(new
                {
                    message = "WASM file uploaded successfully",
                    fileName = wasmFile.FileName,
                    size = wasmFile.Length,
                    path = filePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading WASM file");
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }

        [HttpPost("test-wasm")]
        public async Task<IActionResult> TestWasm([FromBody] TestWasmRequest request)
        {
            try
            {
                var wasmPath = Path.Combine(_wasmDirectory, request.FileName);
                
                if (!System.IO.File.Exists(wasmPath))
                {
                    return NotFound($"WASM file not found: {request.FileName}");
                }

                var fileInfo = new FileInfo(wasmPath);
                var wasmBytes = await System.IO.File.ReadAllBytesAsync(wasmPath);

                // Basic WASM validation
                var validationResult = ValidateWasmFile(wasmBytes);

                return Ok(new
                {
                    fileName = request.FileName,
                    fileSize = fileInfo.Length,
                    filePath = wasmPath,
                    validation = validationResult,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing WASM file");
                return StatusCode(500, $"Error testing file: {ex.Message}");
            }
        }



        [HttpPost("test-shoplazza")]
        public async Task<IActionResult> TestWasmWithShoplazza([FromBody] TestShoplazzaRequest request)
        {
            try
            {
                var wasmPath = Path.Combine(_wasmDirectory, request.FileName);
                
                if (!System.IO.File.Exists(wasmPath))
                {
                    return NotFound($"WASM file not found: {request.FileName}");
                }

                var wasmBytes = await System.IO.File.ReadAllBytesAsync(wasmPath);
                var wasmBase64 = Convert.ToBase64String(wasmBytes);

                // Create a test merchant for the API call
                var testMerchant = new Merchant
                {
                    Shop = request.TestShop ?? "test-shop.myshoplaza.com",
                    AccessToken = "test-token",
                    Id = 1
                };

                // Create function registration request with consistent descriptive names
                var functionName = GetConsistentFunctionName(request.FileName);
                var functionRequest = new FunctionRegistrationRequest
                {
                    Name = functionName,
                    WasmBase64 = wasmBase64,
                    SourceCode = await GetSourceCodeForWasmAsync(request.FileName)
                };

                _logger.LogInformation("Testing WASM file {FileName} with Shoplazza API", request.FileName);

                // Call Shoplazza's create function API
                var (functionId, errorDetails) = await _shoplazzaService.CreateFunctionAsync(testMerchant, functionRequest);

                if (!string.IsNullOrEmpty(functionId))
                {
                    return Ok(new
                    {
                        success = true,
                        message = "WASM file successfully created on Shoplazza!",
                        fileName = request.FileName,
                        functionId = functionId,
                        fileSize = wasmBytes.Length,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    // Return the actual error details from Shoplazza
                    return Ok(new
                    {
                        success = false,
                        message = "WASM file failed to create on Shoplazza",
                        fileName = request.FileName,
                        fileSize = wasmBytes.Length,
                        timestamp = DateTime.UtcNow,
                        errorDetails = errorDetails,
                        note = "This is the actual error message from Shoplazza's API"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing WASM file with Shoplazza");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error testing WASM file with Shoplazza",
                    error = ex.Message,
                    fileName = request.FileName,
                    timestamp = DateTime.UtcNow
                });
            }
                }

        [HttpPut("update-wasm")]
        public async Task<IActionResult> UpdateWasm(IFormFile wasmFile, string? existingFileName = null)
        {
            try
            {
                if (wasmFile == null || wasmFile.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                if (!wasmFile.FileName.EndsWith(".wasm"))
                {
                    return BadRequest("File must be a .wasm file");
                }

                // Determine target filename
                var targetFileName = existingFileName ?? wasmFile.FileName;
                var targetPath = Path.Combine(_wasmDirectory, targetFileName);

                // Ensure wasm directory exists
                Directory.CreateDirectory(_wasmDirectory);

                // Save the file (overwrites if exists)
                using (var stream = new FileStream(targetPath, FileMode.Create))
                {
                    await wasmFile.CopyToAsync(stream);
                }

                _logger.LogInformation("WASM file updated: {FileName} ({Size} bytes)", 
                    targetFileName, wasmFile.Length);

                return Ok(new
                {
                    message = "WASM file updated successfully",
                    fileName = targetFileName,
                    size = wasmFile.Length,
                    path = targetPath,
                    action = "updated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating WASM file");
                return StatusCode(500, $"Error updating file: {ex.Message}");
            }
        }

        [HttpPut("update-source")]
        public async Task<IActionResult> UpdateSource(IFormFile sourceFile, string wasmFileName)
        {
            try
            {
                if (sourceFile == null || sourceFile.Length == 0)
                {
                    return BadRequest("No source file uploaded");
                }

                // Determine source directory based on WASM filename
                string sourceDir;
                if (wasmFileName.Contains("cart-transform-universal") || wasmFileName.Contains("cart-transform-rust"))
                {
                    sourceDir = "cart-transform-universal";
                }
                else if (wasmFileName.Contains("cart-transform"))
                {
                    sourceDir = "cart-transform";
                }
                else
                {
                    return BadRequest("Cannot determine source directory for WASM file");
                }

                var sourcePath = Path.Combine(_wasmDirectory, "src", sourceDir);
                Directory.CreateDirectory(sourcePath);

                // Save the source file
                var targetPath = Path.Combine(sourcePath, sourceFile.FileName);
                using (var stream = new FileStream(targetPath, FileMode.Create))
                {
                    await sourceFile.CopyToAsync(stream);
                }

                _logger.LogInformation("Source file updated: {FileName} for WASM {WasmFile}", 
                    sourceFile.FileName, wasmFileName);

                return Ok(new
                {
                    message = "Source file updated successfully",
                    sourceFileName = sourceFile.FileName,
                    wasmFileName = wasmFileName,
                    sourceDirectory = sourceDir,
                    path = targetPath,
                    action = "updated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating source file");
                return StatusCode(500, $"Error updating source file: {ex.Message}");
            }
        }

        [HttpDelete("delete-wasm/{fileName}")]
        public async Task<IActionResult> DeleteWasm(string fileName)
        {
            try
            {
                var wasmPath = Path.Combine(_wasmDirectory, fileName);
                var sourceDir = Path.Combine(_wasmDirectory, "src");

                if (!System.IO.File.Exists(wasmPath))
                {
                    return NotFound($"WASM file not found: {fileName}");
                }

                // Delete WASM file
                System.IO.File.Delete(wasmPath);

                // Try to delete corresponding source directory
                string sourceSubDir;
                if (fileName.Contains("cart-transform-universal") || fileName.Contains("cart-transform-rust"))
                {
                    sourceSubDir = Path.Combine(sourceDir, "cart-transform-universal");
                }
                else if (fileName.Contains("cart-transform"))
                {
                    sourceSubDir = Path.Combine(sourceDir, "cart-transform");
                }
                else
                {
                    sourceSubDir = null;
                }

                if (sourceSubDir != null && Directory.Exists(sourceSubDir))
                {
                    Directory.Delete(sourceSubDir, true);
                    _logger.LogInformation("Deleted source directory: {SourceDir}", sourceSubDir);
                }

                _logger.LogInformation("WASM file and sources deleted: {FileName}", fileName);

                return Ok(new
                {
                    message = "WASM file and sources deleted successfully",
                    fileName = fileName,
                    action = "deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting WASM file");
                return StatusCode(500, $"Error deleting file: {ex.Message}");
            }
        }

        [HttpGet("list-wasm")]
        public async Task<IActionResult> ListWasm()
        {
            try
            {
                var wasmFiles = new List<object>();
                var sourceDir = Path.Combine(_wasmDirectory, "src");

                if (Directory.Exists(_wasmDirectory))
                {
                    var files = Directory.GetFiles(_wasmDirectory, "*.wasm");
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var fileInfo = new FileInfo(file);
                        
                        // Get source code info
                        string sourceCodeInfo = "No source code found";
                        try
                        {
                            var sourceCode = await GetSourceCodeForWasmAsync(fileName);
                            sourceCodeInfo = $"Source code length: {sourceCode.Length} characters";
                        }
                        catch
                        {
                            // Ignore errors getting source code
                        }

                        wasmFiles.Add(new
                        {
                            fileName = fileName,
                            size = fileInfo.Length,
                            lastModified = fileInfo.LastWriteTimeUtc,
                            sourceCodeInfo = sourceCodeInfo
                        });
                    }
                }

                return Ok(new
                {
                    wasmFiles = wasmFiles,
                    totalCount = wasmFiles.Count,
                    wasmDirectory = _wasmDirectory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing WASM files");
                return StatusCode(500, $"Error listing files: {ex.Message}");
            }
        }

        /// <summary>
        /// Test getting function details from Shoplazza
        /// </summary>
        [HttpGet("get-function-details/{functionId}")]
        public async Task<IActionResult> GetFunctionDetails(string functionId)
        {
            try
            {
                var functionDetails = await _shoplazzaService.GetFunctionDetailsAsync(functionId);
                if (functionDetails == null || !functionDetails.Any())
                {
                    return NotFound($"Function with ID {functionId} not found");
                }

                return Ok(functionDetails.First());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function details for ID: {FunctionId}", functionId);
                return StatusCode(500, $"Error getting function details: {ex.Message}");
            }
        }

        [HttpPost("clear-function-config")]
        public async Task<IActionResult> ClearFunctionConfig()
        {
            try
            {
                // Check if diagnostics are enabled
                var enabled = _configuration["Diagnostics:Enable"] ?? _configuration["Diagnostics__Enable"];
                if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound();
                }

                // Validate diagnostic key
                var providedKey = Request.Headers["X-Diag-Key"].FirstOrDefault() ?? string.Empty;
                var expectedKey = _configuration["Diagnostics__Key"] ?? _configuration["Diagnostics:Key"] ?? string.Empty;
                if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
                {
                    _logger.LogWarning("Diagnostics function config clear access denied");
                    return StatusCode(403, new { error = "Forbidden" });
                }

                _logger.LogWarning("ADMIN FUNCTION CONFIG CLEAR REQUESTED - This will remove function configuration from database!");

                // Use the proper service pattern instead of direct database manipulation
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Get the count before clearing for logging
                var functionConfigs = await dbContext.GlobalFunctionConfigurations
                    .Where(g => g.FunctionType == "cart-transform" && g.IsActive)
                    .ToListAsync();
                
                if (functionConfigs.Any())
                {
                    _logger.LogWarning("Found {Count} active function configurations to clear", functionConfigs.Count);
                    
                    // Use a transaction to ensure data consistency
                    using var transaction = await dbContext.Database.BeginTransactionAsync();
                    try
                    {
                        // Mark configurations as inactive instead of deleting them
                        foreach (var config in functionConfigs)
                        {
                            config.IsActive = false;
                            config.UpdatedAt = DateTime.UtcNow;
                            config.Status = FunctionStatus.Deleted;
                        }
                        
                        await dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        _logger.LogWarning("Successfully cleared {Count} function configurations from database", functionConfigs.Count);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                else
                {
                    _logger.LogInformation("No active function configurations found in database");
                }
                
                return Ok(new
                {
                    message = "Function configuration cleared successfully",
                    configsCleared = functionConfigs.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing function configuration");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Generates consistent function names for Partner API testing
        /// </summary>
        private string GetConsistentFunctionName(string fileName)
        {
            // Remove .wasm extension and create descriptive name
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            
            // Map to consistent function names
            return baseName switch
            {
                "cart-transform-rust" => "cart-transform-rust",
                "cart-transform-universal" => "cart-transform-universal", 
                "cart-transform" => "cart-transform",
                _ => $"cart-transform-{baseName}" // Fallback for other files
            };
        }

        private async Task<string> GetSourceCodeForWasmAsync(string fileName)
        {
            try
            {
                var sourceFiles = new List<string>();
                
                // Get source code from the bundled source files in wwwroot/wasm/src
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var wasmSrcPath = Path.Combine(webRootPath, "wasm", "src");
                
                if (!Directory.Exists(wasmSrcPath))
                {
                    return $"// Source code for {fileName}\n// Source directory not found: {wasmSrcPath}";
                }
                
                // Map WASM files to their corresponding source code directory
                if (fileName.Contains("cart-transform-universal") || fileName.Contains("cart-transform-rust"))
                {
                    // Read from cart-transform-universal (Rust) directory
                    var rustSrcPath = Path.Combine(wasmSrcPath, "cart-transform-universal");
                    if (Directory.Exists(rustSrcPath))
                    {
                        var rustFiles = System.IO.Directory.GetFiles(rustSrcPath, "*.rs", SearchOption.TopDirectoryOnly);
                        foreach (var file in rustFiles)
                        {
                            var content = await System.IO.File.ReadAllTextAsync(file);
                            sourceFiles.Add($"// {Path.GetFileName(file)}\n{content}");
                        }
                    }
                }
                else if (fileName.Contains("cart-transform"))
                {
                    // Read from cart-transform (JavaScript) directory
                    var jsSrcPath = Path.Combine(wasmSrcPath, "cart-transform");
                    if (Directory.Exists(jsSrcPath))
                    {
                        var jsFiles = System.IO.Directory.GetFiles(jsSrcPath, "*.js", SearchOption.TopDirectoryOnly);
                        foreach (var file in jsFiles)
                        {
                            var content = await System.IO.File.ReadAllTextAsync(file);
                            sourceFiles.Add($"// {Path.GetFileName(file)}\n{content}");
                        }
                    }
                }
                
                if (sourceFiles.Any())
                {
                    return string.Join("\n\n", sourceFiles);
                }
                
                // Fallback to a generic description if no source files found
                return $"// Source code for {fileName}\n// No source files found in corresponding directory";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read source code files for {FileName}, using fallback", fileName);
                return $"// Source code for {fileName}\n// Error reading source files: {ex.Message}";
            }
        }

        private object ValidateWasmFile(byte[] wasmBytes)
        {
            try
            {
                // Check WASM magic number
                if (wasmBytes.Length < 4 || 
                    wasmBytes[0] != 0x00 || wasmBytes[1] != 0x61 || 
                    wasmBytes[2] != 0x73 || wasmBytes[3] != 0x6D)
                {
                    return new { isValid = false, error = "Invalid WASM magic number" };
                }

                // Check file size - WASM files can be very small for minimal implementations
                if (wasmBytes.Length < 4)
                {
                    return new { isValid = false, error = "WASM file too small - must be at least 4 bytes" };
                }

                return new
                {
                    isValid = true,
                    magicNumber = "0x00 0x61 0x73 0x6D",
                    version = "1.0",
                    size = wasmBytes.Length
                };
            }
            catch (Exception ex)
            {
                return new { isValid = false, error = $"Validation error: {ex.Message}" };
            }
        }
    }

    public class TestWasmRequest
    {
        public string FileName { get; set; } = string.Empty;
    }

    public class TestShoplazzaRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string? TestShop { get; set; }
    }
}
