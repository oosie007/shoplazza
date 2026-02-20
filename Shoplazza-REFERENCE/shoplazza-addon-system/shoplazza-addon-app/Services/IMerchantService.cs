using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models;
using ShoplazzaAddonApp.Models.Auth;
using ShoplazzaAddonApp.Models.Configuration;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service interface for merchant management operations
/// </summary>
public interface IMerchantService
{
    /// <summary>
    /// Gets a merchant by shop domain
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>Merchant if found, null otherwise</returns>
    Task<Merchant?> GetMerchantByShopAsync(string shop);

    /// <summary>
    /// Creates or updates a merchant with authentication data
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <param name="authResponse">Authentication response from Shoplazza</param>
    /// <returns>The created/updated merchant</returns>
    Task<Merchant> CreateOrUpdateMerchantAsync(string shop, ShoplazzaAuthResponse authResponse);

    /// <summary>
    /// Updates merchant information from Shoplazza store data
    /// </summary>
    /// <param name="merchant">Merchant to update</param>
    /// <param name="storeData">Store data from Shoplazza API</param>
    /// <returns>Updated merchant</returns>
    Task<Merchant> UpdateMerchantInfoAsync(Merchant merchant, dynamic storeData);

    /// <summary>
    /// Completely removes all merchant data when app is uninstalled
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>True if successful</returns>
    Task<bool> RemoveMerchantDataAsync(string shop);

    /// <summary>
    /// Updates the last login timestamp for a merchant
    /// </summary>
    /// <param name="shop">Shop domain</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateLastLoginAsync(string shop);

    /// <summary>
    /// Gets all active merchants
    /// </summary>
    /// <returns>List of active merchants</returns>
    Task<IEnumerable<Merchant>> GetActiveMerchantsAsync();

    /// <summary>
    /// Encrypts and stores access token for a merchant
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="accessToken">Plain text access token</param>
    Task EncryptAndStoreTokenAsync(Merchant merchant, string accessToken);

    /// <summary>
    /// Decrypts and retrieves access token for a merchant
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>Plain text access token if available</returns>
    Task<string?> DecryptTokenAsync(Merchant merchant);

    /// <summary>
    /// Validates if a merchant's access token is still valid
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>True if token is valid</returns>
    Task<bool> ValidateTokenAsync(Merchant merchant);

    /// <summary>
    /// Gets a merchant by ID
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <returns>Merchant if found, null otherwise</returns>
    Task<Merchant?> GetMerchantByIdAsync(int merchantId);

    /// <summary>
    /// Registers a cart-transform function for a merchant
    /// </summary>
    /// <param name="merchant">Merchant to register function for</param>
    /// <returns>True if successful</returns>
    Task<bool> RegisterCartTransformFunctionAsync(Merchant merchant);

    /// <summary>
    /// Gets the function configuration for a merchant
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <returns>Function configuration if found, null otherwise</returns>
    Task<Models.Configuration.FunctionConfiguration?> GetFunctionConfigurationAsync(int merchantId);

    /// <summary>
    /// Updates the function status for a merchant
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="status">New function status</param>
    /// <param name="errorMessage">Error message if status is Failed</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateFunctionStatusAsync(int merchantId, Models.Configuration.FunctionStatus status, string? errorMessage = null);

    /// <summary>
    /// Completely cleans up the database when all merchants are inactive
    /// </summary>
    /// <returns>Cleanup result with success status and details</returns>
    Task<DatabaseCleanupResult> CleanupDatabaseAsync();
}