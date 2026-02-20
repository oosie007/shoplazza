# Cart-Transform Refactor Plan: JavaScript to Native Shoplazza Functions

## **CONTEXT: Why We're Refactoring**

### **Current Problem (JavaScript Approach):**
Our current implementation in `WidgetController.cs` uses complex JavaScript injection to:
- Intercept cart submissions
- Create virtual line items
- Manipulate cart state client-side
- Handle complex fallback scenarios

**Issues with this approach:**
- ❌ Complex client-side JavaScript that can break
- ❌ Potential conflicts with Shoplazza's cart system
- ❌ Difficult to debug and maintain
- ❌ Performance impact from heavy JavaScript execution
- ❌ Security concerns with client-side cart manipulation

### **Shoplazza's Native Solution:**
Shoplazza provides **native cart-transform functions** that:
- ✅ Run server-side on Shoplazza's infrastructure
- ✅ Execute automatically when cart operations happen
- ✅ Use WASM-based functions for performance
- ✅ Integrate seamlessly with Shoplazza's cart system
- ✅ Allow direct price adjustments via metafields

**Reference:** [Shoplazza Function API Documentation](https://www.shoplazza.dev/reference/tutorial-of-function-and-function-api)

## **TECHNICAL ARCHITECTURE CHANGE**

### **Before (Current Implementation):**
```
Customer → Widget (JavaScript) → Form Interception → Virtual Line Items → Cart
```

### **After (Native Approach):**
```
Customer → Widget (Metafield Setting) → Shoplazza Cart-Transform Function → Price Adjustment
```

## **IMPLEMENTATION STRATEGY**

### **Phase 1: Remove JavaScript Cart-Transform**
- Remove complex cart manipulation JavaScript from `WidgetController.cs`
- Keep basic form interception for metafield setting
- Simplify widget to focus on add-on selection only

### **Phase 2: Implement Metafield-Based Configuration**
- Use product metafields to store add-on configuration
- Set metafields when add-ons are selected
- Let Shoplazza's cart-transform function read metafields

### **Phase 3: Create Native Cart-Transform Function**
- Build WASM function using C# .NET or JavaScript
- Function reads metafields and adjusts prices
- Register function with Shoplazza's cart-transform system

## **DETAILED REFACTOR STEPS**

### **Step 1: Analyze Current WidgetController.cs**
**File:** `shoplazza-addon-system/shoplazza-addon-app/Controllers/WidgetController.cs`

**What to Remove:**
- `cartTransform` function (lines ~700-800)
- `getShoplazzaCart` function
- `createAddonLineItem` function  
- `updateCartTotals` function
- `triggerCartUpdateEvents` function
- `updateAddonQuantity` function
- `removeAddonFromCart` function
- `getAddonFromCart` function
- `getAllAddonsInCart` function

**What to Keep:**
- Basic form interception for setting metafields
- Add-on selection UI rendering
- Configuration loading
- Basic event handling

### **Step 2: Simplify Form Interception**
**Current Complex Implementation:**
```csharp
// Complex cart manipulation code to remove
window.shoplazzaAddonCartTransform = function() {
    // ... 100+ lines of cart manipulation
};
```

**New Simplified Implementation:**
```csharp
// Simple metafield setting
if (self.addOnPreference && self.addOnConfig) {
    // Set metafield for add-on selection
    var addonMetafieldInput = document.createElement('input');
    addonMetafieldInput.type = 'hidden';
    addonMetafieldInput.name = 'properties[_addon_selected]';
    addonMetafieldInput.value = 'true';
    form.appendChild(addonMetafieldInput);
    
    // Set add-on configuration metafield
    var addonConfigInput = document.createElement('input');
    addonConfigInput.type = 'hidden';
    addonConfigInput.name = 'properties[_addon_config]';
    addonConfigInput.value = JSON.stringify({
        title: self.addOnConfig.title,
        price: self.addOnConfig.price,
        sku: self.addOnConfig.sku
    });
    form.appendChild(addonConfigInput);
}
```

### **Step 3: Update Widget Generation**
**Remove from `GenerateWidgetScriptAsync`:**
- All cart manipulation JavaScript
- Complex cart state management
- Virtual line item creation logic

**Keep in `GenerateWidgetScriptAsync`:**
- Basic widget UI rendering
- Add-on selection handling
- Metafield setting
- Configuration loading

## **METAFIELD STRATEGY**

### **Metafield Structure:**
```json
{
  "namespace": "addon-system",
  "key": "protection-plan",
  "value": "true",
  "type": "boolean"
}
```

### **Add-On Configuration Metafields:**
- `_addon_selected`: Boolean indicating if add-on is selected
- `_addon_config`: JSON string with add-on details
- `_addon_price`: Add-on price for cart-transform function
- `_addon_title`: Add-on title for display

## **GIT COMMIT INSTRUCTIONS**

### **Commit 1: Remove Complex Cart-Transform JavaScript**
```bash
# Remove complex cart manipulation functions
git add shoplazza-addon-system/shoplazza-addon-app/Controllers/WidgetController.cs
git commit -m "refactor: remove complex JavaScript cart-transform in favor of native Shoplazza functions

- Remove cartTransform function and related cart manipulation code
- Remove virtual line item creation logic
- Remove complex cart state management
- Keep basic form interception for metafield setting
- Prepare for native cart-transform function implementation"
```

### **Commit 2: Simplify Form Interception**
```bash
# Simplify form interception to set metafields only
git add shoplazza-addon-system/shoplazza-addon-app/Controllers/WidgetController.cs
git commit -m "refactor: simplify form interception to use metafields for add-on configuration

- Replace complex cart manipulation with simple metafield setting
- Set _addon_selected and _addon_config metafields
- Remove cart state manipulation code
- Prepare for cart-transform function integration"
```

### **Commit 3: Clean Up Widget Generation**
```bash
# Remove unused JavaScript functions and clean up widget generation
git add shoplazza-addon-system/shoplazza-addon-app/Controllers/WidgetController.cs
git commit -m "refactor: clean up widget generation and remove unused cart functions

- Remove unused cart manipulation functions
- Clean up JavaScript code generation
- Optimize widget script size
- Focus on add-on selection and metafield setting"
```

## **ROLLBACK PROCEDURE**

### **If Refactor Causes Issues:**
```bash
# Find the last working commit before refactor
git log --oneline -10

# Rollback to last working state
git reset --hard <commit-hash>

# Force push if needed (be careful!)
git push --force-with-lease origin main
```

### **Rollback Checkpoints:**
- **Before Step 1:** Current working state
- **After Step 1:** Remove complex cart-transform
- **After Step 2:** Simplify form interception  
- **After Step 3:** Clean up widget generation

## **SUCCESS CRITERIA**

### **After Refactor:**
- ✅ Widget still renders add-on selection UI
- ✅ Add-on selection sets appropriate metafields
- ✅ No JavaScript errors in browser console
- ✅ Form submission includes metafield data
- ✅ Widget size reduced by removing complex cart logic
- ✅ Ready for native cart-transform function integration

### **What Should NOT Work After Refactor:**
- ❌ Cart price adjustments (handled by native function)
- ❌ Virtual line item creation (handled by native function)
- ❌ Complex cart state management (handled by native function)

## **NEXT STEPS AFTER REFACTOR**

1. **Widget Simplification** - Convert to HTML file streaming
2. **WASM Function Creation** - Build native cart-transform function
3. **Testing & Validation** - Test the new approach

## **RISKS AND MITIGATION**

### **Risk: Breaking Existing Functionality**
**Mitigation:** 
- Test each step thoroughly
- Keep rollback checkpoints
- Maintain basic add-on selection functionality

### **Risk: Metafield Compatibility**
**Mitigation:**
- Test metafield setting with Shoplazza
- Verify metafield reading in cart-transform function
- Use standard metafield types and namespaces

### **Risk: Performance Impact**
**Mitigation:**
- Remove heavy JavaScript execution
- Optimize widget script size
- Use efficient metafield operations

## **DEPENDENCIES**

- Shoplazza cart-transform function system
- Metafield support in Shoplazza
- WASM function development environment
- Function registration and binding APIs

---

**This refactor transforms our complex client-side cart manipulation into a clean, server-side native Shoplazza solution that will be more reliable, performant, and maintainable.**
