using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Dto;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service implementation for managing product add-ons
/// </summary>
public class ProductAddOnService : IProductAddOnService
{
    private readonly IMerchantService _merchantService;
    private readonly IRepository<ProductAddOn> _productAddOnRepository;
    private readonly IShoplazzaApiService _shoplazzaApiService;
    private readonly ILogger<ProductAddOnService> _logger;

    public ProductAddOnService(
        IRepository<ProductAddOn> productAddOnRepository,
        IShoplazzaApiService shoplazzaApiService,
        IMerchantService merchantService,
        ILogger<ProductAddOnService> logger)
    {
        _productAddOnRepository = productAddOnRepository;
        _shoplazzaApiService = shoplazzaApiService;
        _merchantService = merchantService;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductAddOn>> GetMerchantAddOnsAsync(int merchantId)
    {
        try
        {
            return await _productAddOnRepository.GetAsync(
                filter: pa => pa.MerchantId == merchantId,
                orderBy: q => q.OrderBy(pa => pa.Position).ThenBy(pa => pa.ProductTitle),
                includeProperties: "Merchant");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting add-ons for merchant {MerchantId}", merchantId);
            throw;
        }
    }

    public async Task<IEnumerable<ProductAddOn>> GetAllProductAddOnsAsync(int merchantId)
    {
        // Alias for GetMerchantAddOnsAsync for consistency
        return await GetMerchantAddOnsAsync(merchantId);
    }

    public async Task<ProductAddOn?> GetProductAddOnAsync(int merchantId, string productId)
    {
        try
        {
            return await _productAddOnRepository.GetFirstOrDefaultAsync(
                filter: pa => pa.MerchantId == merchantId && pa.ProductId == productId,
                includeProperties: "Merchant");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting add-on for merchant {MerchantId} and product {ProductId}", 
                merchantId, productId);
            throw;
        }
    }

    public async Task<ProductAddOn> CreateOrUpdateAddOnAsync(int merchantId, string productId, ProductAddOnConfigDto addOnConfig)
    {
        try
        {
            var existingAddOn = await GetProductAddOnAsync(merchantId, productId);
            
            if (existingAddOn != null)
            {
                // Update existing add-on
                UpdateAddOnFromConfig(existingAddOn, addOnConfig);
                existingAddOn.UpdatedAt = DateTime.UtcNow;

                await _productAddOnRepository.UpdateAsync(existingAddOn);
                await _productAddOnRepository.SaveAsync();

                // Update metadata on the original product
                var merchant = await GetMerchantByIdAsync(merchantId);
                if (merchant != null)
                {
                    if (!await UpdateAddOnMetadataAsync(merchant, productId, addOnConfig))
                    {
                        throw new InvalidOperationException("Failed to update add-on metadata. Add-on update aborted.");
                    }
                }

                _logger.LogInformation("Updated add-on for merchant {MerchantId}, product {ProductId}", 
                    merchantId, productId);
                return existingAddOn;
            }
            else
            {
                // Create new add-on
                var newAddOn = new ProductAddOn
                {
                    MerchantId = merchantId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                UpdateAddOnFromConfig(newAddOn, addOnConfig);

                await _productAddOnRepository.AddAsync(newAddOn);
                await _productAddOnRepository.SaveAsync();

                // Update metadata on the original product
                var merchant = await GetMerchantByIdAsync(merchantId);
                if (merchant != null)
                {
                    if (!await AddAddOnMetadataAsync(merchant, productId, addOnConfig))
                    {
                        throw new InvalidOperationException("Failed to update add-on metadata. Add-on update aborted.");
                    }
                }

                _logger.LogInformation("Created add-on for merchant {MerchantId}, product {ProductId}", 
                    merchantId, productId);
                return newAddOn;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating add-on for merchant {MerchantId}, product {ProductId}", 
                merchantId, productId);
            throw;
        }
    }

    public async Task<bool> ToggleAddOnAsync(int merchantId, string productId, bool isEnabled)
    {
        try
        {
            var addOn = await GetProductAddOnAsync(merchantId, productId);
            if (addOn == null)
            {
                _logger.LogWarning("Add-on not found for merchant {MerchantId}, product {ProductId}", 
                    merchantId, productId);
                return false;
            }

            addOn.IsEnabled = isEnabled;
            addOn.UpdatedAt = DateTime.UtcNow;

            await _productAddOnRepository.UpdateAsync(addOn);
            await _productAddOnRepository.SaveAsync();

            _logger.LogInformation("Toggled add-on {Status} for merchant {MerchantId}, product {ProductId}", 
                isEnabled ? "enabled" : "disabled", merchantId, productId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling add-on for merchant {MerchantId}, product {ProductId}", 
                merchantId, productId);
            return false;
        }
    }

    public async Task<bool> DeleteAddOnAsync(int merchantId, string productId)
    {
        try
        {
            _logger.LogInformation("🔄 Starting add-on deletion process for merchant {MerchantId}, product {ProductId}", 
                merchantId, productId);
            
            var addOn = await GetProductAddOnAsync(merchantId, productId);
            if (addOn == null)
            {
                _logger.LogWarning("❌ Add-on not found for deletion: merchant {MerchantId}, product {ProductId}", 
                    merchantId, productId);
                return false;
            }

            _logger.LogInformation("✅ Found add-on to delete: ID {AddOnId}, Title: {AddOnTitle}, Product: {ProductId}", 
                addOn.Id, addOn.AddOnTitle, addOn.ProductId);

            // Remove metadata from the original product
            var merchant = await GetMerchantByIdAsync(merchantId);
            if (merchant != null)
            {
                _logger.LogInformation("🔄 Removing add-on metadata for shop {Shop}, product {ProductId}", 
                    merchant.Shop, productId);
                
                if (!await RemoveAddOnMetadataAsync(merchant, productId))
                {
                    _logger.LogError("❌ Failed to remove add-on metadata for shop {Shop}, product {ProductId}. Add-on deletion aborted.", 
                        merchant.Shop, productId);
                    throw new InvalidOperationException("Failed to remove add-on metadata. Add-on deletion aborted.");
                }
                
                _logger.LogInformation("✅ Successfully removed add-on metadata for shop {Shop}, product {ProductId}", 
                    merchant.Shop, productId);
            }
            else
            {
                _logger.LogWarning("⚠️ Merchant not found for ID {MerchantId} during add-on deletion", merchantId);
            }

            // Delete the add-on record from database
            _logger.LogInformation("🔄 Deleting add-on record from database: ID {AddOnId}", addOn.Id);
            await _productAddOnRepository.DeleteAsync(addOn);
            await _productAddOnRepository.SaveAsync();
            _logger.LogInformation("✅ Successfully deleted add-on record from database: ID {AddOnId}", addOn.Id);

            _logger.LogInformation("🎉 Add-on deletion completed successfully for merchant {MerchantId}, product {ProductId}", 
                merchantId, productId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting add-on for merchant {MerchantId}, product {ProductId}. Exception: {ExceptionMessage}", 
                merchantId, productId, ex.Message);
            return false;
        }
    }

    public async Task<IEnumerable<ProductAddOn>> GetEnabledAddOnsAsync(int merchantId)
    {
        try
        {
            return await _productAddOnRepository.GetAsync(
                filter: pa => pa.MerchantId == merchantId && pa.IsEnabled && pa.IsActive,
                orderBy: q => q.OrderBy(pa => pa.Position).ThenBy(pa => pa.ProductTitle),
                includeProperties: "Merchant");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enabled add-ons for merchant {MerchantId}", merchantId);
            throw;
        }
    }

    public async Task<bool> SyncProductInfoAsync(Merchant merchant, string productId)
    {
        try
        {
            var product = await _shoplazzaApiService.GetProductAsync(merchant, productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found in Shoplazza for merchant {Shop}", 
                    productId, merchant.Shop);
                return false;
            }

            var addOn = await GetProductAddOnAsync(merchant.Id, productId);
            if (addOn != null)
            {
                addOn.ProductTitle = product.Title;
                addOn.ProductHandle = product.Handle;
                addOn.UpdatedAt = DateTime.UtcNow;

                await _productAddOnRepository.UpdateAsync(addOn);
                await _productAddOnRepository.SaveAsync();

                _logger.LogInformation("Synced product info for product {ProductId} in merchant {Shop}", 
                    productId, merchant.Shop);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing product {ProductId} for merchant {Shop}", productId, merchant.Shop);
            return false;
        }
    }

    public async Task<int> BulkSyncProductsAsync(Merchant merchant)
    {
        try
        {
            var addOns = await GetMerchantAddOnsAsync(merchant.Id);
            var syncCount = 0;

            foreach (var addOn in addOns)
            {
                var success = await SyncProductInfoAsync(merchant, addOn.ProductId);
                if (success)
                {
                    syncCount++;
                }
            }

            _logger.LogInformation("Bulk synced {SyncCount} out of {TotalCount} products for merchant {Shop}", 
                syncCount, addOns.Count(), merchant.Shop);
            return syncCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk sync for merchant {Shop}", merchant.Shop);
            throw;
        }
    }

    public async Task<bool> ValidateProductExistsAsync(Merchant merchant, string productId)
    {
        try
        {
            var product = await _shoplazzaApiService.GetProductAsync(merchant, productId);
            return product != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product {ProductId} exists for merchant {Shop}", 
                productId, merchant.Shop);
            return false;
        }
    }

    public async Task<ShoplazzaProductDto?> CreateAddOnProductAsync(Merchant merchant, ProductAddOnConfigDto addOnConfig)
    {
        try
        {
            var productData = new
            {
                product = new
                {
                    title = addOnConfig.AddOnTitle,
                    body_html = addOnConfig.AddOnDescription,
                    vendor = "Add-On",
                    product_type = "Add-On",
                    status = "active",
                    variants = new[]
                    {
                        new
                        {
                            title = "Default Title",
                            price = (addOnConfig.AddOnPriceCents / 100.0m).ToString("F2"),
                            sku = addOnConfig.AddOnSku,
                            inventory_policy = "deny",
                            fulfillment_service = "manual",
                            inventory_management = "shopify",
                            requires_shipping = addOnConfig.RequiresShipping,
                            taxable = addOnConfig.IsTaxable,
                            grams = addOnConfig.WeightGrams
                        }
                    }
                }
            };

            var createdProduct = await _shoplazzaApiService.CreateProductAsync(merchant, productData);
            
            if (createdProduct != null)
            {
                _logger.LogInformation("Created add-on product {ProductId} for merchant {Shop}", 
                    createdProduct.Id, merchant.Shop);
            }

            return createdProduct;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating add-on product for merchant {Shop}", merchant.Shop);
            return null;
        }
    }

    public async Task<AddOnStatsDto> GetAddOnStatsAsync(int merchantId)
    {
        try
        {
            var addOns = await _productAddOnRepository.GetAsync(
                filter: pa => pa.MerchantId == merchantId);

            var stats = new AddOnStatsDto
            {
                TotalAddOns = addOns.Count(),
                EnabledAddOns = addOns.Count(a => a.IsEnabled),
                ActiveAddOns = addOns.Count(a => a.IsActive),
                LastUpdated = DateTime.UtcNow
            };

            // TODO: Implement revenue and order statistics
            // This would require integration with order data or analytics service
            stats.TotalRevenue = 0;
            stats.TotalOrders = 0;
            stats.AverageOrderValue = 0;
            stats.ConversionRate = 0;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting add-on stats for merchant {MerchantId}", merchantId);
            throw;
        }
    }

    /// <summary>
    /// Updates a ProductAddOn entity from configuration DTO
    /// </summary>
    private void UpdateAddOnFromConfig(ProductAddOn addOn, ProductAddOnConfigDto config)
    {
        addOn.AddOnTitle = config.AddOnTitle;
        addOn.AddOnDescription = config.AddOnDescription;
        addOn.AddOnPriceCents = config.AddOnPriceCents;
        addOn.Currency = config.Currency;
        addOn.DisplayText = config.DisplayText;
        addOn.AddOnSku = config.AddOnSku;
        addOn.RequiresShipping = config.RequiresShipping;
        addOn.WeightGrams = config.WeightGrams;
        addOn.IsTaxable = config.IsTaxable;
        addOn.ImageUrl = config.ImageUrl;
        addOn.Position = config.Position;
        addOn.IsActive = config.IsActive;
        addOn.IsEnabled = config.IsEnabled;
        addOn.AddOnProductId = config.AddOnProductId;
        addOn.AddOnVariantId = config.AddOnVariantId;

        if (config.Configuration != null)
        {
            addOn.ConfigurationJson = JsonConvert.SerializeObject(config.Configuration);
        }
    }

private const string METADATA_NAMESPACE = "cdh_shoplazza_addon";
private const string METADATA_KEY_TITLE = "addon_title";
private const string METADATA_KEY_DESCRIPTION = "addon_description";
private const string METADATA_KEY_PRICE = "addon_price";
private const string METADATA_KEY_SELECTED = "addon_selected";

/// <summary>
/// Updates the addon_selected metadata field for a product
/// </summary>
public async Task<bool> UpdateAddOnSelectionAsync(int merchantId, string productId, bool isSelected)
{
    try
    {
        var merchant = await GetMerchantByIdAsync(merchantId);
        if (merchant == null)
        {
            _logger.LogError("Merchant {MerchantId} not found", merchantId);
            return false;
        }

        // Get existing metafields to find the addon_selected one
        var existingMetafields = await _shoplazzaApiService.GetProductMetafieldsAsync(merchant, productId);
        var selectedMetafield = existingMetafields?.FirstOrDefault(m => m.Namespace == METADATA_NAMESPACE && m.Key == METADATA_KEY_SELECTED);

        if (selectedMetafield != null)
        {
            // Delete the old metafield
                            await _shoplazzaApiService.DeleteProductMetafieldAsync(merchant, productId, selectedMetafield.Id);
        }

        // Create new metafield with updated value
        var metafield = new ShoplazzaMetafieldDto
        {
            Namespace = METADATA_NAMESPACE,
            Key = METADATA_KEY_SELECTED,
            Value = isSelected.ToString().ToLower(),
            ValueType = "boolean"
        };

        var result = await _shoplazzaApiService.CreateProductMetafieldAsync(merchant, productId, metafield);
        return result != null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating add-on selection for product {ProductId}", productId);
        return false;
    }
}

/// <summary>
/// Adds add-on metadata fields to a product
/// </summary>
private async Task<bool> AddAddOnMetadataAsync(Merchant merchant, string productId, ProductAddOnConfigDto config)
{
    try
    {
        // Check if metafields already exist to prevent duplicates
        var existingMetafields = await _shoplazzaApiService.GetProductMetafieldsAsync(merchant, productId);
        var existingAddonMetafields = existingMetafields?.Where(m => m.Namespace == METADATA_NAMESPACE).ToList();
        
        if (existingAddonMetafields != null && existingAddonMetafields.Any())
        {
            _logger.LogWarning("Add-on metafields already exist for product {ProductId}. Skipping creation to prevent duplicates.", productId);
            return true; // Return true since the metafields are already there
        }

        var metadataFields = new[]
        {
            new { Key = METADATA_KEY_TITLE, Value = config.AddOnTitle, Type = "string" },
            new { Key = METADATA_KEY_DESCRIPTION, Value = config.AddOnDescription, Type = "string" },
            new { Key = METADATA_KEY_PRICE, Value = (config.AddOnPriceCents / 100.0m).ToString("F2"), Type = "string" },
            new { Key = METADATA_KEY_SELECTED, Value = "false", Type = "boolean" }
        };

        foreach (var field in metadataFields)
        {
            var metafield = new ShoplazzaMetafieldDto
            {
                Namespace = METADATA_NAMESPACE,
                Key = field.Key,
                Value = field.Value,
                ValueType = field.Type
            };

            var result = await _shoplazzaApiService.CreateProductMetafieldAsync(merchant, productId, metafield);
            if (result == null)
            {
                _logger.LogError("Failed to create metafield {Key} for product {ProductId}", field.Key, productId);
                return false;
            }
            
            // Small delay between creations to avoid overwhelming the API
            await Task.Delay(100);
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error adding add-on metadata for product {ProductId}", productId);
        return false;
    }
}

/// <summary>
/// Updates add-on metadata fields on a product
/// </summary>
private async Task<bool> UpdateAddOnMetadataAsync(Merchant merchant, string productId, ProductAddOnConfigDto config)
{
    try
    {
        // Remove existing metadata first
        if (!await RemoveAddOnMetadataAsync(merchant, productId))
        {
            return false;
        }

        // Wait a moment for Shoplazza to process the deletion
        await Task.Delay(1000);

        // Verify deletion was successful before recreating
        var remainingMetafields = await _shoplazzaApiService.GetProductMetafieldsAsync(merchant, productId);
        var remainingAddonMetafields = remainingMetafields?.Where(m => m.Namespace == METADATA_NAMESPACE).ToList();
        
        if (remainingAddonMetafields != null && remainingAddonMetafields.Any())
        {
            _logger.LogWarning("Some add-on metafields still exist after deletion for product {ProductId}. Waiting longer...", productId);
            
            // Wait longer and try deletion again
            await Task.Delay(2000);
            await RemoveAddOnMetadataAsync(merchant, productId);
            await Task.Delay(1000);
            
            // Final verification
            remainingMetafields = await _shoplazzaApiService.GetProductMetafieldsAsync(merchant, productId);
            remainingAddonMetafields = remainingMetafields?.Where(m => m.Namespace == METADATA_NAMESPACE).ToList();
            
            if (remainingAddonMetafields != null && remainingAddonMetafields.Any())
            {
                _logger.LogError("Failed to remove existing add-on metafields for product {ProductId}. Cannot update.", productId);
                return false;
            }
        }

        // Add updated metadata
        return await AddAddOnMetadataAsync(merchant, productId, config);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating add-on metadata for product {ProductId}", productId);
        return false;
    }
}

/// <summary>
/// Removes all add-on metadata fields from a product
/// </summary>
private async Task<bool> RemoveAddOnMetadataAsync(Merchant merchant, string productId)
{
    try
    {
        _logger.LogInformation("🔄 Starting metadata removal for shop {Shop}, product {ProductId}", 
            merchant.Shop, productId);
        
        var existingMetafields = await _shoplazzaApiService.GetProductMetafieldsAsync(merchant, productId);
        var addonMetafields = existingMetafields?.Where(m => m.Namespace == METADATA_NAMESPACE).ToList();

        if (addonMetafields != null && addonMetafields.Any())
        {
            _logger.LogInformation("📋 Found {Count} existing add-on metafields to remove for shop {Shop}, product {ProductId}", 
                addonMetafields.Count, merchant.Shop, productId);
            
            foreach (var metafield in addonMetafields)
            {
                _logger.LogInformation("🔄 Deleting metafield: ID {MetafieldId}, Key: {Key}, Value: {Value}", 
                    metafield.Id, metafield.Key, metafield.Value);
                
                var deleted = await _shoplazzaApiService.DeleteProductMetafieldAsync(merchant, productId, metafield.Id);
                if (!deleted)
                {
                    _logger.LogWarning("⚠️ Failed to delete metafield {MetafieldId} for shop {Shop}, product {ProductId}", 
                        metafield.Id, merchant.Shop, productId);
                }
                else
                {
                    _logger.LogInformation("✅ Successfully deleted metafield {MetafieldId} for shop {Shop}, product {ProductId}", 
                        metafield.Id, merchant.Shop, productId);
                }
                
                // Small delay between deletions to avoid overwhelming the API
                await Task.Delay(100);
            }
            
            // Wait for deletions to complete
            _logger.LogInformation("⏳ Waiting for metafield deletions to complete for shop {Shop}, product {ProductId}", 
                merchant.Shop, productId);
            await Task.Delay(500);
            
            // Verify deletions
            var remainingMetafields = await _shoplazzaApiService.GetProductMetafieldsAsync(merchant, productId);
            var remainingAddonMetafields = remainingMetafields?.Where(m => m.Namespace == METADATA_NAMESPACE).ToList();
            
            if (remainingAddonMetafields != null && remainingAddonMetafields.Any())
            {
                _logger.LogWarning("⚠️ {Count} add-on metafields still remain after deletion for shop {Shop}, product {ProductId}", 
                    remainingAddonMetafields.Count, merchant.Shop, productId);
            }
            else
            {
                _logger.LogInformation("✅ All add-on metafields successfully removed for shop {Shop}, product {ProductId}", 
                    merchant.Shop, productId);
            }
        }
        else
        {
            _logger.LogInformation("ℹ️ No existing add-on metafields found for shop {Shop}, product {ProductId}", 
                merchant.Shop, productId);
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error removing add-on metadata for shop {Shop}, product {ProductId}. Exception: {ExceptionMessage}", 
            merchant.Shop, productId, ex.Message);
        return false;
    }
}

/// <summary>
/// Gets merchant by ID
/// </summary>
private async Task<Merchant> GetMerchantByIdAsync(int merchantId)
{
    return await _merchantService.GetMerchantByIdAsync(merchantId);
}

}