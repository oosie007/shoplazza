# WASM Function Implementation Plan: Native Shoplazza Cart-Transform

## **CONTEXT: Why We Need WASM Functions**

### **Shoplazza's Native Cart-Transform System:**
According to the [Shoplazza Function API documentation](https://www.shoplazza.dev/reference/tutorial-of-function-and-function-api), Shoplazza provides:

- ✅ **Native cart-transform functions** that run server-side
- ✅ **WASM-based execution** for performance
- ✅ **Automatic triggering** when cart operations happen
- ✅ **Direct price adjustments** via metafields
- ✅ **Seamless integration** with Shoplazza's cart system

### **What We're Building:**
A **WASM function** that:
1. **Reads add-on metafields** from cart items
2. **Calculates price adjustments** based on add-on selection
3. **Applies price changes** to cart totals
4. **Handles multiple add-ons** and complex scenarios

## **TECHNICAL ARCHITECTURE**

### **Function Flow:**
```
Customer Adds Product → Cart-Transform Function Triggered → Read Metafields → Calculate Price → Update Cart
```

### **Function Types Available:**
- **Cart-Transform Functions** - Modify cart prices and totals
- **Checkout Functions** - Handle checkout process
- **Order Functions** - Process completed orders

## **IMPLEMENTATION STRATEGY**

### **Phase 1: Function Development Environment**
- Set up WASM development environment
- Choose between C# .NET and JavaScript
- Create function structure and testing framework

### **Phase 2: Core Function Logic**
- Implement metafield reading logic
- Build price calculation engine
- Add error handling and validation

### **Phase 3: Function Registration & Testing**
- Register function with Shoplazza
- Test in development environment
- Validate cart price adjustments

## **DETAILED IMPLEMENTATION STEPS**

### **Step 1: Choose Function Technology Stack**

**Option A: C# .NET WASM (Recommended)**
```csharp
// Advantages:
// - Familiar language for our team
// - Strong typing and error handling
// - Good performance characteristics
// - Easy integration with existing codebase

// Disadvantages:
// - Larger bundle size
// - More complex build process
```

**Option B: JavaScript WASM**
```javascript
// Advantages:
// - Smaller bundle size
// - Faster execution
// - Easier debugging
// - Better tooling support

// Disadvantages:
// - Less familiar for our team
// - Dynamic typing challenges
```

**Recommendation: C# .NET WASM** for better integration with existing codebase.

### **Step 2: Create Function Project Structure**

**Directory:** `shoplazza-addon-system/shoplazza-addon-functions/`

**Files to Create:**
1. **`CartTransformFunction.cs`** - Main function logic
2. **`AddOnPriceCalculator.cs`** - Price calculation engine
3. **`MetafieldReader.cs`** - Metafield parsing utilities
4. **`function.json`** - Function configuration
5. **`host.json`** - Host configuration

### **Step 3: Implement Core Cart-Transform Function**

**File:** `shoplazza-addon-functions/CartTransformFunction.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ShoplazzaAddonFunctions
{
    public class CartTransformFunction
    {
        private readonly ILogger<CartTransformFunction> _logger;
        private readonly AddOnPriceCalculator _priceCalculator;
        private readonly MetafieldReader _metafieldReader;

        public CartTransformFunction(
            ILogger<CartTransformFunction> logger,
            AddOnPriceCalculator priceCalculator,
            MetafieldReader metafieldReader)
        {
            _logger = logger;
            _priceCalculator = priceCalculator;
            _metafieldReader = metafieldReader;
        }

        [FunctionName("CartTransform")]
        public async Task<CartTransformResponse> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] CartTransformRequest request)
        {
            try
            {
                _logger.LogInformation("Cart-transform function triggered for cart: {CartId}", request.CartId);

                // Parse cart data from request
                var cart = JsonSerializer.Deserialize<CartData>(request.CartData);
                if (cart == null)
                {
                    _logger.LogError("Failed to deserialize cart data");
                    return new CartTransformResponse { Success = false, Error = "Invalid cart data" };
                }

                // Process each line item for add-ons
                var updatedItems = new List<CartLineItem>();
                var totalAddOnPrice = 0m;

                foreach (var item in cart.LineItems)
                {
                    var processedItem = await ProcessLineItemAsync(item);
                    updatedItems.Add(processedItem);

                    // Calculate add-on price if present
                    if (processedItem.AddOnPrice > 0)
                    {
                        totalAddOnPrice += processedItem.AddOnPrice;
                    }
                }

                // Create response with updated cart
                var response = new CartTransformResponse
                {
                    Success = true,
                    CartId = request.CartId,
                    UpdatedItems = updatedItems,
                    TotalAddOnPrice = totalAddOnPrice,
                    CartSubtotal = cart.Subtotal + totalAddOnPrice,
                    CartTotal = cart.Total + totalAddOnPrice
                };

                _logger.LogInformation("Cart-transform completed successfully. Add-on total: {AddOnTotal}", totalAddOnPrice);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cart-transform function");
                return new CartTransformResponse 
                { 
                    Success = false, 
                    Error = "Internal function error" 
                };
            }
        }

        private async Task<CartLineItem> ProcessLineItemAsync(CartLineItem item)
        {
            try
            {
                // Check if item has add-on metafields
                var addOnMetafield = _metafieldReader.GetAddOnMetafield(item.Properties);
                if (addOnMetafield == null)
                {
                    return item; // No add-on, return unchanged
                }

                // Parse add-on configuration
                var addOnConfig = _metafieldReader.ParseAddOnConfig(addOnMetafield);
                if (addOnConfig == null)
                {
                    _logger.LogWarning("Invalid add-on config for item {ItemId}", item.Id);
                    return item;
                }

                // Calculate add-on price
                var addOnPrice = _priceCalculator.CalculateAddOnPrice(addOnConfig, item.Quantity);
                
                // Update item with add-on information
                var updatedItem = item.Clone();
                updatedItem.AddOnPrice = addOnPrice;
                updatedItem.AddOnTitle = addOnConfig.Title;
                updatedItem.AddOnSku = addOnConfig.Sku;
                updatedItem.Properties["_addon_processed"] = "true";
                updatedItem.Properties["_addon_price"] = addOnPrice.ToString("F2");

                return updatedItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing line item {ItemId}", item.Id);
                return item; // Return unchanged item on error
            }
        }
    }
}
```

### **Step 4: Implement Price Calculator**

**File:** `shoplazza-addon-functions/AddOnPriceCalculator.cs`
```csharp
using System;
using System.Collections.Generic;

namespace ShoplazzaAddonFunctions
{
    public class AddOnPriceCalculator
    {
        private readonly Dictionary<string, AddOnPricingRule> _pricingRules;

        public AddOnPriceCalculator()
        {
            _pricingRules = InitializePricingRules();
        }

        public decimal CalculateAddOnPrice(AddOnConfig config, int quantity)
        {
            try
            {
                // Get pricing rule for this add-on type
                if (!_pricingRules.TryGetValue(config.Type, out var rule))
                {
                    // Default pricing: flat rate per item
                    return config.BasePrice * quantity;
                }

                // Apply pricing rule
                return rule.CalculatePrice(config, quantity);
            }
            catch (Exception)
            {
                // Fallback to base price
                return config.BasePrice * quantity;
            }
        }

        private Dictionary<string, AddOnPricingRule> InitializePricingRules()
        {
            return new Dictionary<string, AddOnPricingRule>
            {
                ["protection-plan"] = new ProtectionPlanPricingRule(),
                ["warranty"] = new WarrantyPricingRule(),
                ["installation"] = new InstallationPricingRule(),
                ["custom"] = new CustomPricingRule()
            };
        }
    }

    public abstract class AddOnPricingRule
    {
        public abstract decimal CalculatePrice(AddOnConfig config, int quantity);
    }

    public class ProtectionPlanPricingRule : AddOnPricingRule
    {
        public override decimal CalculatePrice(AddOnConfig config, int quantity)
        {
            // Protection plans: percentage of product price + base fee
            var percentagePrice = config.ProductPrice * (config.PercentageRate / 100m);
            var basePrice = config.BasePrice;
            return (percentagePrice + basePrice) * quantity;
        }
    }

    public class WarrantyPricingRule : AddOnPricingRule
    {
        public override decimal CalculatePrice(AddOnConfig config, int quantity)
        {
            // Warranties: tiered pricing based on product value
            var tier = GetWarrantyTier(config.ProductPrice);
            var tierPrice = config.TierPrices.GetValueOrDefault(tier, config.BasePrice);
            return tierPrice * quantity;
        }

        private string GetWarrantyTier(decimal productPrice)
        {
            if (productPrice <= 100m) return "basic";
            if (productPrice <= 500m) return "standard";
            if (productPrice <= 1000m) return "premium";
            return "extended";
        }
    }
}
```

### **Step 5: Implement Metafield Reader**

**File:** `shoplazza-addon-functions/MetafieldReader.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ShoplazzaAddonFunctions
{
    public class MetafieldReader
    {
        public AddOnMetafield GetAddOnMetafield(Dictionary<string, string> properties)
        {
            if (properties == null) return null;

            // Check for add-on selection metafield
            if (properties.TryGetValue("_addon_selected", out var selected) && 
                selected == "true")
            {
                // Get add-on configuration
                if (properties.TryGetValue("_addon_config", out var configJson))
                {
                    try
                    {
                        var config = JsonSerializer.Deserialize<AddOnConfig>(configJson);
                        if (config != null)
                        {
                            return new AddOnMetafield
                            {
                                IsSelected = true,
                                Config = config
                            };
                        }
                    }
                    catch (JsonException)
                    {
                        // Invalid JSON, return null
                    }
                }
            }

            return null;
        }

        public AddOnConfig ParseAddOnConfig(string configJson)
        {
            try
            {
                return JsonSerializer.Deserialize<AddOnConfig>(configJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
```

### **Step 6: Create Function Configuration**

**File:** `shoplazza-addon-functions/function.json`
```json
{
  "scriptFile": "ShoplazzaAddonFunctions.dll",
  "entryPoint": "ShoplazzaAddonFunctions.CartTransformFunction.Run",
  "bindings": [
    {
      "authLevel": "function",
      "name": "req",
      "type": "httpTrigger",
      "direction": "in",
      "route": "cart-transform",
      "methods": ["POST"]
    },
    {
      "name": "$return",
      "type": "http",
      "direction": "out"
    }
  ],
  "disabled": false
}
```

### **Step 7: Create Data Models**

**File:** `shoplazza-addon-functions/Models/CartModels.cs`
```csharp
using System.Collections.Generic;

namespace ShoplazzaAddonFunctions.Models
{
    public class CartTransformRequest
    {
        public string CartId { get; set; }
        public string CartData { get; set; }
        public string ShopDomain { get; set; }
        public string AccessToken { get; set; }
    }

    public class CartTransformResponse
    {
        public bool Success { get; set; }
        public string CartId { get; set; }
        public string Error { get; set; }
        public List<CartLineItem> UpdatedItems { get; set; }
        public decimal TotalAddOnPrice { get; set; }
        public decimal CartSubtotal { get; set; }
        public decimal CartTotal { get; set; }
    }

    public class CartData
    {
        public string Id { get; set; }
        public List<CartLineItem> LineItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; }
    }

    public class CartLineItem
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string VariantId { get; set; }
        public string Title { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public decimal AddOnPrice { get; set; }
        public string AddOnTitle { get; set; }
        public string AddOnSku { get; set; }

        public CartLineItem Clone()
        {
            return new CartLineItem
            {
                Id = this.Id,
                ProductId = this.ProductId,
                VariantId = this.VariantId,
                Title = this.Title,
                Quantity = this.Quantity,
                Price = this.Price,
                Properties = new Dictionary<string, string>(this.Properties),
                AddOnPrice = this.AddOnPrice,
                AddOnTitle = this.AddOnTitle,
                AddOnSku = this.AddOnSku
            };
        }
    }

    public class AddOnMetafield
    {
        public bool IsSelected { get; set; }
        public AddOnConfig Config { get; set; }
    }

    public class AddOnConfig
    {
        public string Title { get; set; }
        public decimal BasePrice { get; set; }
        public string Type { get; set; }
        public string Sku { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal PercentageRate { get; set; }
        public Dictionary<string, decimal> TierPrices { get; set; }
    }
}
```

## **FUNCTION REGISTRATION WITH SHOPLAZZA**

### **Step 1: Build and Deploy Function**
```bash
# Build the function project
cd shoplazza-addon-system/shoplazza-addon-functions
dotnet build --configuration Release

# Publish to Azure Functions
func azure functionapp publish shoplazza-addon-functions
```

### **Step 2: Register Function with Shoplazza**
```javascript
// Function registration payload
const functionRegistration = {
  name: "cart-transform-addon",
  type: "cart-transform",
  endpoint: "https://your-function-app.azurewebsites.net/api/cart-transform",
  triggers: ["cart.add", "cart.update"],
  metadata: {
    description: "Add-on price adjustment for cart items",
    version: "1.0.0",
    author: "Your Company"
  }
};

// Register via Shoplazza API
fetch('https://api.shoplazza.com/v1/functions', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer YOUR_ACCESS_TOKEN'
  },
  body: JSON.stringify(functionRegistration)
});
```

### **Step 3: Bind Function to Cart Events**
```javascript
// Bind function to cart operations
const binding = {
  functionId: "cart-transform-addon",
  eventType: "cart.transform",
  shopId: "your-shop-id",
  enabled: true
};

fetch('https://api.shoplazza.com/v1/functions/bindings', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer YOUR_ACCESS_TOKEN'
  },
  body: JSON.stringify(binding)
});
```

## **TESTING STRATEGY**

### **Local Testing:**
```bash
# Start function locally
cd shoplazza-addon-system/shoplazza-addon-functions
func start

# Test with sample cart data
curl -X POST http://localhost:7071/api/cart-transform \
  -H "Content-Type: application/json" \
  -d '{
    "cartId": "test-cart-123",
    "cartData": "{\"lineItems\":[{\"id\":\"1\",\"properties\":{\"_addon_selected\":\"true\",\"_addon_config\":\"{\\\"title\\\":\\\"Protection Plan\\\",\\\"price\\\":9.99}\"}}]}"
  }'
```

### **Integration Testing:**
- Test with real Shoplazza cart data
- Verify metafield reading and parsing
- Validate price calculations
- Test error handling scenarios

### **End-to-End Testing:**
- Complete add-on selection flow
- Cart submission and transformation
- Price adjustment verification
- Checkout process validation

## **GIT COMMIT INSTRUCTIONS**

### **Commit 1: Create Function Project Structure**
```bash
# Create function project and core files
git add shoplazza-addon-system/shoplazza-addon-functions/
git commit -m "feat: create WASM function project for native cart-transform

- Add CartTransformFunction.cs with core logic
- Add AddOnPriceCalculator.cs for price calculations
- Add MetafieldReader.cs for metafield parsing
- Add function.json and host.json configuration
- Prepare for native Shoplazza cart-transform integration"
```

### **Commit 2: Implement Data Models and Utilities**
```bash
# Add data models and utility classes
git add shoplazza-addon-system/shoplazza-addon-functions/Models/
git commit -m "feat: add data models and utility classes for cart-transform function

- Add CartTransformRequest/Response models
- Add CartData and CartLineItem models
- Add AddOnMetafield and AddOnConfig models
- Implement utility classes for metafield processing
- Complete function infrastructure"
```

### **Commit 3: Add Function Registration and Testing**
```bash
# Add function registration and testing infrastructure
git add shoplazza-addon-system/shoplazza-addon-functions/
git commit -m "feat: add function registration and testing infrastructure

- Add function registration with Shoplazza API
- Add local testing setup and sample data
- Add integration testing framework
- Complete WASM function implementation"
```

## **ROLLBACK PROCEDURE**

### **If Function Implementation Causes Issues:**
```bash
# Find the last working commit before function implementation
git log --oneline -10

# Rollback to last working state
git reset --hard <commit-hash>

# Force push if needed (be careful!)
git push --force-with-lease origin main
```

### **Rollback Checkpoints:**
- **Before Step 1:** Current working state
- **After Step 1:** Function project structure created
- **After Step 2:** Core function logic implemented
- **After Step 3:** Function registration and testing added

## **SUCCESS CRITERIA**

### **After WASM Function Implementation:**
- ✅ Function builds and deploys successfully
- ✅ Function registers with Shoplazza API
- ✅ Function triggers on cart operations
- ✅ Metafields are read and parsed correctly
- ✅ Price calculations are accurate
- ✅ Cart totals are updated properly
- ✅ Error handling works correctly

### **What Should Work:**
- ✅ Automatic cart price adjustments
- ✅ Add-on price calculations
- ✅ Multiple add-on support
- ✅ Complex pricing rules
- ✅ Error recovery and fallbacks

## **NEXT STEPS AFTER WASM FUNCTION**

1. **Testing & Validation** - Comprehensive testing of the new system
2. **Performance Optimization** - Monitor function execution times
3. **Monitoring & Logging** - Add production monitoring
4. **Documentation** - Update technical documentation

## **RISKS AND MITIGATION**

### **Risk: Function Performance Issues**
**Mitigation:**
- Optimize WASM execution
- Monitor function execution times
- Implement caching where appropriate

### **Risk: Shoplazza API Changes**
**Mitigation:**
- Use stable API endpoints
- Implement version compatibility
- Monitor API documentation updates

### **Risk: Complex Pricing Rules**
**Mitigation:**
- Start with simple pricing
- Add complexity gradually
- Comprehensive testing of edge cases

---

**This WASM function implementation provides a native, server-side solution for cart price adjustments that integrates seamlessly with Shoplazza's cart system while maintaining performance and reliability.**
