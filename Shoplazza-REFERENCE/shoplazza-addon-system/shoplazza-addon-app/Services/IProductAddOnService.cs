using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Dto;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service interface for managing product add-ons
/// </summary>
public interface IProductAddOnService
{
    /// <summary>
    /// Gets all product add-ons for a merchant
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <returns>List of product add-ons</returns>
    Task<IEnumerable<ProductAddOn>> GetMerchantAddOnsAsync(int merchantId);

    /// <summary>
    /// Gets all product add-ons for a merchant (alias for GetMerchantAddOnsAsync)
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <returns>List of product add-ons</returns>
    Task<IEnumerable<ProductAddOn>> GetAllProductAddOnsAsync(int merchantId);

    /// <summary>
    /// Gets a specific product add-on
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Product add-on if found</returns>
    Task<ProductAddOn?> GetProductAddOnAsync(int merchantId, string productId);

    /// <summary>
    /// Creates or updates a product add-on configuration
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="productId">Product ID</param>
    /// <param name="addOnConfig">Add-on configuration</param>
    /// <returns>Created or updated product add-on</returns>
    Task<ProductAddOn> CreateOrUpdateAddOnAsync(int merchantId, string productId, ProductAddOnConfigDto addOnConfig);

    /// <summary>
    /// Enables or disables a product add-on
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="productId">Product ID</param>
    /// <param name="isEnabled">Whether to enable or disable</param>
    /// <returns>True if successful</returns>
    Task<bool> ToggleAddOnAsync(int merchantId, string productId, bool isEnabled);

    /// <summary>
    /// Deletes a product add-on configuration
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="productId">Product ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteAddOnAsync(int merchantId, string productId);

    /// <summary>
    /// Gets products that have add-ons enabled for a merchant
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <returns>List of products with add-ons</returns>
    Task<IEnumerable<ProductAddOn>> GetEnabledAddOnsAsync(int merchantId);

    /// <summary>
    /// Synchronizes product information from Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID to sync</param>
    /// <returns>True if successful</returns>
    Task<bool> SyncProductInfoAsync(Merchant merchant, string productId);

    /// <summary>
    /// Bulk synchronizes all product add-ons for a merchant
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <returns>Number of products synchronized</returns>
    Task<int> BulkSyncProductsAsync(Merchant merchant);

    /// <summary>
    /// Validates that a product exists in Shoplazza
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="productId">Product ID</param>
    /// <returns>True if product exists</returns>
    Task<bool> ValidateProductExistsAsync(Merchant merchant, string productId);

    /// <summary>
    /// Creates an add-on product in Shoplazza (if needed)
    /// </summary>
    /// <param name="merchant">Merchant</param>
    /// <param name="addOnConfig">Add-on configuration</param>
    /// <returns>Created product information</returns>
    Task<ShoplazzaProductDto?> CreateAddOnProductAsync(Merchant merchant, ProductAddOnConfigDto addOnConfig);

    /// <summary>
    /// Gets statistics about add-ons for a merchant
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <returns>Add-on statistics</returns>
    Task<AddOnStatsDto> GetAddOnStatsAsync(int merchantId);

    /// <summary>
    /// Updates the addon_selected metadata field for a product
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="productId">Product ID</param>
    /// <param name="isSelected">Whether the add-on is selected</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateAddOnSelectionAsync(int merchantId, string productId, bool isSelected);
}

/// <summary>
/// DTO for product add-on configuration
/// </summary>
public class ProductAddOnConfigDto
{
    public string AddOnTitle { get; set; } = string.Empty;
    public string AddOnDescription { get; set; } = string.Empty;
    public int AddOnPriceCents { get; set; }
    public string Currency { get; set; } = "USD";
    public string DisplayText { get; set; } = string.Empty;
    public string AddOnSku { get; set; } = string.Empty;
    public bool RequiresShipping { get; set; } = true;
    public int WeightGrams { get; set; } = 0;
    public bool IsTaxable { get; set; } = true;
    public string? ImageUrl { get; set; }
    public int Position { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsEnabled { get; set; } = false;
    public string? AddOnProductId { get; set; }
    public string? AddOnVariantId { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// DTO for add-on statistics
/// </summary>
public class AddOnStatsDto
{
    public int TotalAddOns { get; set; }
    public int EnabledAddOns { get; set; }
    public int ActiveAddOns { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal ConversionRate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}