using Microsoft.EntityFrameworkCore;
using ShoplazzaAddonApp.Data;
using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models;
using ShoplazzaAddonApp.Models.Auth;
using ShoplazzaAddonApp.Models.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service implementation for merchant management operations
/// </summary>
public class MerchantService : IMerchantService
{
    private readonly IRepository<Merchant> _merchantRepository;
    private readonly IRepository<FunctionConfiguration> _functionConfigurationRepository;
    private readonly IRepository<ProductAddOn> _productAddOnRepository;
    private readonly IRepository<Configuration> _configurationRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MerchantService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IShoplazzaFunctionApiService _shoplazzaFunctionApiService;
    private readonly ICartTransformFunctionService _cartTransformFunctionService;
    private readonly IServiceProvider _serviceProvider;

    public MerchantService(
        IRepository<Merchant> merchantRepository,
        IRepository<FunctionConfiguration> functionConfigurationRepository,
        IRepository<ProductAddOn> productAddOnRepository,
        IRepository<Configuration> configurationRepository,
        IConfiguration configuration,
        ILogger<MerchantService> logger,
        HttpClient httpClient,
        IShoplazzaFunctionApiService shoplazzaFunctionApiService,
        ICartTransformFunctionService cartTransformFunctionService,
        IServiceProvider serviceProvider)
    {
        _merchantRepository = merchantRepository;
        _functionConfigurationRepository = functionConfigurationRepository;
        _productAddOnRepository = productAddOnRepository;
        _configurationRepository = configurationRepository;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _shoplazzaFunctionApiService = shoplazzaFunctionApiService;
        _cartTransformFunctionService = cartTransformFunctionService;
        _serviceProvider = serviceProvider;
    }

    public async Task<Merchant?> GetMerchantByShopAsync(string shop)
    {
        try
        {
            return await _merchantRepository.GetFirstOrDefaultAsync(
                m => m.Shop == shop && m.IsActive,
                "ProductAddOns,Configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting merchant by shop: {Shop}", shop);
            throw;
        }
    }

    public async Task<Merchant> CreateOrUpdateMerchantAsync(string shop, ShoplazzaAuthResponse authResponse)
    {
        try
        {
            var existingMerchant = await _merchantRepository.GetFirstOrDefaultAsync(m => m.Shop == shop);
            
            if (existingMerchant != null)
            {
                // Update existing merchant
                existingMerchant.IsActive = true;
                existingMerchant.Scopes = authResponse.Scope;
                existingMerchant.TokenCreatedAt = authResponse.CreatedAt;
                existingMerchant.TokenExpiresAt = authResponse.ExpiresAt;
                existingMerchant.UpdatedAt = DateTime.UtcNow;

                // Encrypt and store the access token
                await EncryptAndStoreTokenAsync(existingMerchant, authResponse.AccessToken);

                await _merchantRepository.UpdateAsync(existingMerchant);
                await _merchantRepository.SaveAsync();

                _logger.LogInformation("Updated existing merchant: {Shop}", shop);
                return existingMerchant;
            }
            else
            {
                // Create new merchant
                var newMerchant = new Merchant
                {
                    Shop = shop,
                    IsActive = true,
                    Scopes = authResponse.Scope,
                    TokenCreatedAt = authResponse.CreatedAt,
                    TokenExpiresAt = authResponse.ExpiresAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Encrypt and store the access token
                await EncryptAndStoreTokenAsync(newMerchant, authResponse.AccessToken);

                await _merchantRepository.AddAsync(newMerchant);
                await _merchantRepository.SaveAsync();

                _logger.LogInformation("Created new merchant: {Shop}", shop);
                return newMerchant;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating merchant: {Shop}", shop);
            throw;
        }
    }

    public async Task<Merchant> UpdateMerchantInfoAsync(Merchant merchant, dynamic storeData)
    {
        try
        {
            // Update merchant information from Shoplazza store data
            if (storeData?.name != null)
            {
                merchant.StoreName = storeData.name.ToString();
            }

            if (storeData?.email != null)
            {
                merchant.StoreEmail = storeData.email.ToString();
            }

            merchant.UpdatedAt = DateTime.UtcNow;

            await _merchantRepository.UpdateAsync(merchant);
            await _merchantRepository.SaveAsync();

            _logger.LogInformation("Updated merchant info for shop: {Shop}", merchant.Shop);
            return merchant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating merchant info for shop: {Shop}", merchant.Shop);
            throw;
        }
    }

    public async Task<bool> RemoveMerchantDataAsync(string shop)
    {
        try
        {
            var merchant = await _merchantRepository.GetFirstOrDefaultAsync(m => m.Shop == shop);
            if (merchant == null)
            {
                _logger.LogWarning("Merchant not found for uninstall: {Shop}", shop);
                return false;
            }

            _logger.LogInformation("Starting complete data cleanup for uninstalled merchant: {Shop}", shop);

            // Delete all add-on configurations for this merchant
            var addOns = await _productAddOnRepository.GetAsync(pa => pa.MerchantId == merchant.Id);
            foreach (var addOn in addOns)
            {
                await _productAddOnRepository.DeleteAsync(addOn);
                _logger.LogInformation("Deleted add-on configuration: {AddOnTitle} for merchant: {Shop}", addOn.AddOnTitle, shop);
            }

            // Delete all function configurations for this merchant
            var functionConfigs = await _functionConfigurationRepository.GetAsync(fc => fc.MerchantId == merchant.Id);
            foreach (var config in functionConfigs)
            {
                await _functionConfigurationRepository.DeleteAsync(config);
                _logger.LogInformation("Deleted function configuration: {ConfigId} for merchant: {Shop}", config.Id, shop);
            }

            // Delete all general configurations for this merchant
            var configs = await _configurationRepository.GetAsync(c => c.MerchantId == merchant.Id);
            foreach (var config in configs)
            {
                await _configurationRepository.DeleteAsync(config);
                _logger.LogInformation("Deleted configuration: {ConfigId} for merchant: {Shop}", config.Id, shop);
            }

            // Finally, delete the merchant record itself
            await _merchantRepository.DeleteAsync(merchant);
            await _merchantRepository.SaveAsync();

            _logger.LogInformation("Completely removed all data for uninstalled merchant: {Shop}", shop);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completely removing merchant data for uninstall: {Shop}", shop);
            return false;
        }
    }

    public async Task<bool> UpdateLastLoginAsync(string shop)
    {
        try
        {
            var merchant = await _merchantRepository.GetFirstOrDefaultAsync(m => m.Shop == shop && m.IsActive);
            if (merchant == null)
            {
                return false;
            }

            merchant.LastLoginAt = DateTime.UtcNow;
            merchant.UpdatedAt = DateTime.UtcNow;

            await _merchantRepository.UpdateAsync(merchant);
            await _merchantRepository.SaveAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for merchant: {Shop}", shop);
            return false;
        }
    }

    public async Task<IEnumerable<Merchant>> GetActiveMerchantsAsync()
    {
        try
        {
            return await _merchantRepository.GetAsync(
                m => m.IsActive,
                orderBy: q => q.OrderBy(m => m.Shop));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active merchants");
            throw;
        }
    }

    public async Task EncryptAndStoreTokenAsync(Merchant merchant, string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                merchant.AccessToken = null;
                return;
            }

            // Get encryption key from configuration
            var encryptionKey = _configuration["Encryption:Key"];
            if (string.IsNullOrEmpty(encryptionKey))
            {
                // For demo purposes, store as plain text with warning
                _logger.LogWarning("No encryption key configured. Storing access token as plain text (NOT RECOMMENDED FOR PRODUCTION)");
                merchant.AccessToken = accessToken;
                return;
            }

            // Encrypt the access token
            var encryptedToken = EncryptString(accessToken, encryptionKey);
            merchant.AccessToken = encryptedToken;

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting access token for merchant: {Shop}", merchant.Shop);
            throw;
        }
    }

    public async Task<string?> DecryptTokenAsync(Merchant merchant)
    {
        try
        {
            if (string.IsNullOrEmpty(merchant.AccessToken))
            {
                return null;
            }

            // Get encryption key from configuration
            var encryptionKey = _configuration["Encryption:Key"];
            if (string.IsNullOrEmpty(encryptionKey))
            {
                // If no encryption key, assume plain text storage
                return merchant.AccessToken;
            }

            // Decrypt the access token
            var decryptedToken = DecryptString(merchant.AccessToken, encryptionKey);
            return decryptedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting access token for merchant: {Shop}", merchant.Shop);
            return null;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    public async Task<bool> ValidateTokenAsync(Merchant merchant)
    {
        try
        {
            var accessToken = await DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            // Check if token has expired
            if (merchant.TokenExpiresAt.HasValue && merchant.TokenExpiresAt.Value <= DateTime.UtcNow)
            {
                _logger.LogWarning("Access token expired for merchant: {Shop}", merchant.Shop);
                return false;
            }

            // Make a simple API call to validate the token
            var version = _configuration["Shoplazza:ApiVersion"] ?? "2022-01";
            var apiUrl = $"https://{merchant.Shop}/openapi/{version}/shop";
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("Access-Token", accessToken);

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Token validation failed for merchant: {Shop}. Status: {StatusCode}", 
                merchant.Shop, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token for merchant: {Shop}", merchant.Shop);
            return false;
        }
    }

    /// <summary>
    /// Encrypts a string using AES encryption
    /// </summary>
    private string EncryptString(string plainText, string key)
    {
        byte[] iv = new byte[16];
        byte[] array;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream memoryStream = new();
            using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);
            using (StreamWriter streamWriter = new(cryptoStream))
            {
                streamWriter.Write(plainText);
            }
            array = memoryStream.ToArray();
        }

        return Convert.ToBase64String(array);
    }

    /// <summary>
    /// Decrypts a string using AES encryption
    /// </summary>
    private string DecryptString(string cipherText, string key)
    {
        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(cipherText);

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.IV = iv;
        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using MemoryStream memoryStream = new(buffer);
        using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
        using StreamReader streamReader = new(cryptoStream);
        return streamReader.ReadToEnd();
    }

    public async Task<Merchant?> GetMerchantByIdAsync(int merchantId)
    {
        try
        {
            return await _merchantRepository.GetFirstOrDefaultAsync(
                m => m.Id == merchantId && m.IsActive,
                "ProductAddOns,Configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting merchant by ID: {MerchantId}", merchantId);
            throw;
        }
    }

    public async Task<bool> RegisterCartTransformFunctionAsync(Merchant merchant)
    {
        try
        {
            _logger.LogInformation("Registering cart-transform function for merchant {Shop}", merchant.Shop);

            // Get the global function ID from the database (created at startup)
            var globalFunction = await GetGlobalFunctionAsync();
            if (globalFunction == null)
            {
                _logger.LogError("❌ No global cart-transform function found. App startup may have failed.");
                return false;
            }

            _logger.LogInformation("✅ Found global cart-transform function: {FunctionId}", globalFunction.FunctionId);

            // Check if function already exists for this merchant
            var existingConfig = await GetFunctionConfigurationAsync(merchant.Id);
            if (existingConfig != null)
            {
                _logger.LogInformation("Function already registered for merchant {Shop}, updating existing function", merchant.Shop);
                
                // Update existing configuration to use global function ID
                existingConfig.FunctionId = globalFunction.FunctionId;
                existingConfig.FunctionName = globalFunction.FunctionName;
                existingConfig.Status = FunctionStatus.Pending;
                existingConfig.UpdatedAt = DateTime.UtcNow;
                existingConfig.ErrorMessage = null;
                
                await _functionConfigurationRepository.UpdateAsync(existingConfig);
                await _functionConfigurationRepository.SaveAsync();
            }
            else
            {
                // Create new configuration using global function
                var functionConfig = new FunctionConfiguration
                {
                    MerchantId = merchant.Id,
                    FunctionId = globalFunction.FunctionId,
                    FunctionName = globalFunction.FunctionName,
                    FunctionType = globalFunction.FunctionType,
                    Status = FunctionStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _functionConfigurationRepository.AddAsync(functionConfig);
                await _functionConfigurationRepository.SaveAsync();
            }

            // Bind the global function to the merchant's shop
            _logger.LogInformation("🔄 Binding global function {FunctionId} to shop's cart for merchant {Shop}", globalFunction.FunctionId, merchant.Shop);
            var merchantToken = await DecryptTokenAsync(merchant);
            if (string.IsNullOrEmpty(merchantToken))
            {
                _logger.LogError("❌ Failed to get merchant token for binding function {FunctionId} to shop {Shop}", globalFunction.FunctionId, merchant.Shop);
                return false;
            }

            var bound = await _shoplazzaFunctionApiService.BindCartTransformFunctionAsync(merchant, globalFunction.FunctionId, merchantToken);
            if (bound)
            {
                _logger.LogInformation("✅ Global function {FunctionId} bound successfully to shop's cart for merchant {Shop}", globalFunction.FunctionId, merchant.Shop);
                
                // Update status to active
                var functionConfig = await GetFunctionConfigurationAsync(merchant.Id);
                if (functionConfig != null)
                {
                    functionConfig.Status = FunctionStatus.Active;
                    functionConfig.ActivatedAt = DateTime.UtcNow;
                    functionConfig.UpdatedAt = DateTime.UtcNow;
                    
                    await _functionConfigurationRepository.UpdateAsync(functionConfig);
                    await _functionConfigurationRepository.SaveAsync();
                }
                
                _logger.LogInformation("🎉 Successfully bound global cart-transform function {FunctionId} for merchant {Shop}", globalFunction.FunctionId, merchant.Shop);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to bind global function {FunctionId} to shop's cart for merchant {Shop}", globalFunction.FunctionId, merchant.Shop);
                
                // Update status to failed
                var functionConfig = await GetFunctionConfigurationAsync(merchant.Id);
                if (functionConfig != null)
                {
                    functionConfig.Status = FunctionStatus.Failed;
                    functionConfig.ErrorMessage = "Global function binding failed";
                    functionConfig.UpdatedAt = DateTime.UtcNow;
                    
                    await _functionConfigurationRepository.UpdateAsync(functionConfig);
                    await _functionConfigurationRepository.SaveAsync();
                }
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering cart-transform function for merchant {Shop}", merchant.Shop);
            throw;
        }
    }

        private async Task<string> GetSourceCodeForFunctionAsync()
        {
            try
            {
                // Get the actual source code from the bundled source files in wwwroot/wasm/src
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var wasmSrcPath = Path.Combine(webRootPath, "wasm", "src");
                var sourceFiles = new List<string>();
                
                // Read source files from the bundled source directory
                if (Directory.Exists(wasmSrcPath))
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
                return "// Cart transform function source code\n// Source files not found in wwwroot/wasm/src";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read source code files from wwwroot/wasm/src, using fallback");
                return "// Cart transform function - source code unavailable";
            }
        }

    public async Task<FunctionConfiguration?> GetFunctionConfigurationAsync(int merchantId)
    {
        try
        {
            return await _functionConfigurationRepository.GetFirstOrDefaultAsync(
                fc => fc.MerchantId == merchantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting function configuration for merchant {MerchantId}", merchantId);
            return null;
        }
    }

    /// <summary>
    /// Gets the global cart-transform function configuration
    /// </summary>
    public async Task<Models.Configuration.GlobalFunctionConfiguration?> GetGlobalFunctionAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            return await dbContext.GlobalFunctionConfigurations
                .FirstOrDefaultAsync(f => f.FunctionType == "cart-transform" && f.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global function configuration");
            return null;
        }
    }

    public async Task<bool> UpdateFunctionStatusAsync(int merchantId, FunctionStatus status, string? errorMessage = null)
    {
        try
        {
            var config = await GetFunctionConfigurationAsync(merchantId);
            if (config == null)
            {
                _logger.LogWarning("No function configuration found for merchant {MerchantId}", merchantId);
                return false;
            }

            config.Status = status;
            config.UpdatedAt = DateTime.UtcNow;
            
            if (status == FunctionStatus.Active)
            {
                config.ActivatedAt = DateTime.UtcNow;
                config.ErrorMessage = null;
            }
            else if (status == FunctionStatus.Failed)
            {
                config.ErrorMessage = errorMessage;
            }

            await _functionConfigurationRepository.UpdateAsync(config);
            await _functionConfigurationRepository.SaveAsync();

            _logger.LogInformation("Updated function status to {Status} for merchant {MerchantId}", status, merchantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating function status for merchant {MerchantId}", merchantId);
            return false;
        }
    }

    public async Task<DatabaseCleanupResult> CleanupDatabaseAsync()
    {
        try
        {
            _logger.LogWarning("Starting complete database cleanup - this will delete ALL data!");

            var recordsDeleted = new Dictionary<string, int>();
            var totalDeleted = 0;

            // Get all merchants (including inactive ones)
            var allMerchants = await _merchantRepository.GetAsync(m => true);
            var merchantIds = allMerchants.Select(m => m.Id).ToList();

            // Delete ProductAddOns first (foreign key constraint)
            if (merchantIds.Any())
            {
                var addOns = await _productAddOnRepository.GetAsync(pa => merchantIds.Contains(pa.MerchantId));
                var addOnCount = addOns.Count();
                if (addOnCount > 0)
                {
                    await _productAddOnRepository.DeleteRangeAsync(addOns);
                    await _productAddOnRepository.SaveAsync();
                    recordsDeleted["ProductAddOns"] = addOnCount;
                    totalDeleted += addOnCount;
                    _logger.LogWarning("Deleted {Count} ProductAddOn records", addOnCount);
                }
            }

            // Delete FunctionConfigurations
            var functionConfigs = await _functionConfigurationRepository.GetAsync(fc => merchantIds.Contains(fc.MerchantId));
            var functionConfigCount = functionConfigs.Count();
            if (functionConfigCount > 0)
            {
                await _functionConfigurationRepository.DeleteRangeAsync(functionConfigs);
                await _functionConfigurationRepository.SaveAsync();
                recordsDeleted["FunctionConfigurations"] = functionConfigCount;
                totalDeleted += functionConfigCount;
                _logger.LogWarning("Deleted {Count} FunctionConfiguration records", functionConfigCount);
            }

            // Delete Configurations
            var configs = await _configurationRepository.GetAsync(c => merchantIds.Contains(c.MerchantId));
            var configCount = configs.Count();
            if (configCount > 0)
            {
                await _configurationRepository.DeleteRangeAsync(configs);
                await _configurationRepository.SaveAsync();
                recordsDeleted["Configurations"] = configCount;
                totalDeleted += configCount;
                _logger.LogWarning("Deleted {Count} Configuration records", configCount);
            }

            // Finally delete all Merchants
            var merchantCount = allMerchants.Count();
            if (merchantCount > 0)
            {
                await _merchantRepository.DeleteRangeAsync(allMerchants);
                await _merchantRepository.SaveAsync();
                recordsDeleted["Merchants"] = merchantCount;
                totalDeleted += merchantCount;
                _logger.LogWarning("Deleted {Count} Merchant records", merchantCount);
            }

            var details = $"Successfully deleted {totalDeleted} total records across {recordsDeleted.Count} tables";
            _logger.LogWarning("Database cleanup completed successfully: {Details}", details);

            return DatabaseCleanupResult.SuccessResult(details, recordsDeleted);
        }
        catch (Exception ex)
        {
            var error = $"Database cleanup failed: {ex.Message}";
            _logger.LogError(ex, "Database cleanup failed");
            return DatabaseCleanupResult.FailureResult(error);
        }
    }
}