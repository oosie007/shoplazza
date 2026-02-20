using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Api;
using ShoplazzaAddonApp.Models.Configuration;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service for interacting with Shoplazza's Function API
/// </summary>
public interface IShoplazzaFunctionApiService
{
    /// <summary>
    /// Creates a new function on Shoplazza's platform
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function will be created for</param>
    /// <param name="request">Function registration request details</param>
    /// <returns>A tuple containing the function ID if successful (or null) and error details if failed (or null)</returns>
    Task<(string? FunctionId, string? ErrorDetails)> CreateFunctionAsync(Merchant merchant, FunctionRegistrationRequest request);

    /// <summary>
    /// Updates an existing function on Shoplazza's platform
    /// </summary>
    /// <param name="merchant">Dummy parameter for compatibility (not used in partner API)</param>
    /// <param name="functionId">The ID of the function to update</param>
    /// <param name="request">Function update request details</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateFunctionAsync(Merchant merchant, string functionId, FunctionUpdateRequest request);

    /// <summary>
    /// Binds a function to a specific shop's cart using merchant token
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function will be bound to</param>
    /// <param name="functionId">The ID of the function to bind</param>
    /// <param name="merchantToken">The merchant's access token for API calls</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> BindFunctionToShopAsync(Merchant merchant, string functionId, string merchantToken);

    /// <summary>
    /// Lists cart transform functions bound to a specific shop
    /// </summary>
    /// <param name="merchant">The merchant whose shop to list functions for</param>
    /// <param name="merchantToken">The merchant's access token for API calls</param>
    /// <returns>List of cart transform functions if successful, null otherwise</returns>
    Task<List<CartTransformFunction>?> GetCartTransformFunctionsAsync(Merchant merchant, string merchantToken);

    /// <summary>
    /// Updates a cart transform function bound to a specific shop
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function belongs to</param>
    /// <param name="functionBindingId">The ID of the cart transform function binding</param>
    /// <param name="request">Update request details</param>
    /// <param name="merchantToken">The merchant's access token for API calls</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateCartTransformFunctionAsync(Merchant merchant, string functionBindingId, CartTransformFunctionUpdateRequest request, string merchantToken);

    /// <summary>
    /// Deletes a cart transform function from a specific shop
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function belongs to</param>
    /// <param name="functionBindingId">The ID of the cart transform function binding to delete</param>
    /// <param name="merchantToken">The merchant's access token for API calls</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> DeleteCartTransformFunctionAsync(Merchant merchant, string functionBindingId, string merchantToken);

    /// <summary>
    /// Binds a function to a specific shop's cart using merchant token
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function will be bound to</param>
    /// <param name="functionId">The ID of the function to bind</param>
    /// <param name="merchantToken">The merchant's access token for API calls</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> BindCartTransformFunctionAsync(Merchant merchant, string functionId, string merchantToken);

    /// <summary>
    /// Activates a function on Shoplazza's platform
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function belongs to</param>
    /// <param name="functionId">The ID of the function to activate</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> ActivateFunctionAsync(Merchant merchant, string functionId);

    /// <summary>
    /// Deletes a function from Shoplazza's platform
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function belongs to</param>
    /// <param name="functionId">The ID of the function to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> DeleteFunctionAsync(Merchant merchant, string functionId);

    /// <summary>
    /// Gets the current status of a function
    /// </summary>
    /// <param name="merchant">The merchant whose shop the function belongs to</param>
    /// <param name="functionId">The ID of the function to check</param>
    /// <returns>The current function status</returns>
    Task<FunctionStatus> GetFunctionStatusAsync(Merchant merchant, string functionId);

    /// <summary>
    /// Gets a list of all registered functions from Shoplazza (partner-level API)
    /// Note: This is a Partner API call that doesn't require merchant-specific information
    /// </summary>
    /// <param name="functionId">Optional function ID to filter results</param>
    /// <returns>List of function details if successful, null otherwise</returns>
    Task<List<FunctionDetails>?> GetFunctionDetailsAsync(string? functionId = null);

    /// <summary>
    /// Creates a global function using Partner API (no merchant required)
    /// </summary>
    /// <param name="request">The function registration request</param>
    /// <returns>Tuple of (functionId, errorDetails)</returns>
    Task<(string? FunctionId, string? ErrorDetails)> CreateGlobalFunctionAsync(FunctionRegistrationRequest request);

    /// <summary>
    /// Activates a global function using Partner API
    /// </summary>
    /// <param name="functionId">The function ID to activate</param>
    /// <returns>True if activation was successful, false otherwise</returns>
    Task<bool> ActivateGlobalFunctionAsync(string functionId);
}
