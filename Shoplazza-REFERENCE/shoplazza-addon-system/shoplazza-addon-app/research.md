# Shoplazza Add-On System - Research & Integration Plan

## **🎯 CURRENT SYSTEM ANALYSIS - WHAT WE ACTUALLY HAVE TODAY:**

### **✅ Widget (Frontend):**
1. **HTML Templates**: Dynamic widget generation via `WidgetController`
2. **Add-on Selection UI**: Checkbox with title, price, description
3. **Form Interception**: Intercepts Shoplazza cart forms and adds hidden inputs
4. **Properties Added**: 
   - `properties[_addon_selected]` = "true"
   - `properties[_addon_config]` = JSON string with title, price, SKU

### **✅ WASM Function (Backend):**
1. **Cart Processing**: Reads cart data from stdin, outputs modified cart to stdout
2. **Property Reading**: ✅ **NOW READS CORRECT PROPERTIES** (`_addon_selected` and `_addon_config`)
3. **Add-on Line Items**: Creates new line items for add-ons
4. **Cart Modification**: Adds add-on items and recalculates totals

### **✅ Data Flow TODAY:**
1. **Widget renders** → Shows add-on checkbox
2. **User selects add-on** → Hidden form inputs added
3. **Form submitted** → Properties sent to Shoplazza cart
4. **WASM function executes** → ✅ **NOW READS CORRECT PROPERTIES**
5. **Cart updated** → Add-ons appear as separate line items

### **✅ ISSUE 1 RESOLVED: Widget-WASM Property Mismatch**
**Problem**: Widget sends `_addon_config` but WASM read `cdh_shoplazza_addon`
**Solution**: ✅ **IMPLEMENTED** - WASM now reads the correct properties
**Status**: **FULLY INTEGRATED** - Widget and WASM function work together

---

## **🆕 NEW CRITICAL ISSUES IDENTIFIED:**

### **🚨 ISSUE 2: Property Persistence in Cart Modifications**
**Problem**: Line item properties can be **LOST** when cart is modified (remove/re-add items)
**DeepSeek Research**: Properties persist to orders but can be lost during cart modifications
**Impact**: User selects add-on → Cart modified → Add-on selection lost → WASM can't process

### **🚨 ISSUE 3: Form Interception vs API Interception**
**Problem**: Only intercepting form submissions is **INSUFFICIENT** for reliable add-on persistence
**DeepSeek Research**: Need to intercept cart API calls for comprehensive coverage
**Current Status**: Basic form interception only

### **🚨 ISSUE 4: Cart Event Handling**
**Problem**: Not listening for Shoplazza cart events that could invalidate add-on selections
**DeepSeek Research**: Need to listen for `cart:requestComplete`, `cart:change`, `product:added`
**Current Status**: No cart event handling

---

## **🔍 UPDATED RESEARCH PLAN:**

### **Phase 1: Research Shoplazza Cart Flow (Priority: CRITICAL)**
1. **✅ COMPLETED**: Basic property reading and WASM integration
2. **🆕 NEEDED**: Research Shoplazza cart API endpoints and events
3. **🆕 NEEDED**: Understand cart modification behaviors and property persistence

### **Phase 2: Implement Comprehensive Cart Interception (Priority: HIGH)**
1. **Form Interception**: ✅ **COMPLETED** - Basic form property injection
2. **API Interception**: 🆕 **NEEDED** - Intercept cart API calls
3. **Event Handling**: 🆕 **NEEDED** - Listen for Shoplazza cart events
4. **Property Recovery**: 🆕 **NEEDED** - Reapply properties after cart modifications

### **Phase 3: Robust Add-on Persistence (Priority: HIGH)**
1. **Local Storage Backup**: Store add-on selections in localStorage
2. **Property Reapplication**: Reapply properties after any cart modification
3. **Cart Fragment Handling**: Handle cart fragments that might lose properties
4. **Fallback Mechanisms**: Multiple strategies for maintaining add-on state

### **Phase 4: Clean Up and Optimization (Priority: MEDIUM)**
1. **Remove Static Metafield Confusion**: Use metafields only for static configuration
2. **Optimize Property Injection**: Efficient property management
3. **Error Handling**: Graceful degradation when properties are lost

---

## **📋 DETAILED ACTION PLAN:**

### **Step 1: Research Shoplazza Cart API (Priority: CRITICAL)**
- **Research Shoplazza cart endpoints**: `/cart/add`, `/cart/update`, `/cart/change`
- **Identify cart events**: `cart:requestComplete`, `cart:change`, `product:added`
- **Understand property persistence**: When and how properties are lost

### **Step 2: Implement API Interception (Priority: HIGH)**
- **Intercept cart API calls**: Override or wrap Shoplazza's cart functions
- **Maintain property state**: Track add-on selections across cart modifications
- **Reapply properties**: Ensure properties are present after any cart change

### **Step 3: Add Cart Event Handling (Priority: HIGH)**
- **Listen for cart events**: Implement event listeners for Shoplazza cart events
- **Handle property loss**: Detect when properties are lost and reapply them
- **Maintain consistency**: Keep add-on selections synchronized with cart state

### **Step 4: Implement Fallback Mechanisms (Priority: MEDIUM)**
- **Local storage backup**: Store add-on selections in browser storage
- **Property recovery**: Multiple strategies for maintaining add-on state
- **Graceful degradation**: Handle cases where properties can't be maintained

---

## **❓ KEY RESEARCH QUESTIONS (UPDATED):**

1. **How does Shoplazza handle cart modifications?**
   - What triggers property loss?
   - Which cart operations preserve properties?
   - How can we detect property loss?

2. **What cart API endpoints exist?**
   - `/cart/add` - Add items to cart
   - `/cart/update` - Update cart quantities
   - `/cart/change` - Modify cart contents
   - Are there others we need to intercept?

3. **How do Shoplazza cart events work?**
   - `cart:requestComplete` - When cart operations finish
   - `cart:change` - When cart contents change
   - `product:added` - When items are added
   - How can we listen for these events?

4. **What's the best strategy for property persistence?**
   - API interception vs form interception?
   - Local storage backup strategies?
   - Property reapplication timing?

---

## **🔗 RELEVANT FILES (UPDATED):**

### **Widget Implementation:**
- `Controllers/WidgetController.cs` - Serves dynamic widget HTML
- `wwwroot/widget-templates/` - HTML templates for widget UI
- `wwwroot/widget-templates/widget-script.html` - JavaScript logic (needs enhancement)

### **WASM Function:**
- `cart-transform-function/cart-transform.js` - ✅ **WORKING** - Cart transformation logic
- `Services/CartTransformFunctionService.cs` - WASM management service

### **Data Models:**
- `Models/Configuration/AddOnConfiguration.cs` - Add-on configuration model
- `Data/Entities/ProductAddOn.cs` - Database entity for product add-ons

### **🆕 NEW FILES NEEDED:**
- **Cart Interception Service**: Handle API interception and event listening
- **Property Persistence Manager**: Manage add-on selections across cart modifications
- **Cart Event Handler**: Listen for and respond to Shoplazza cart events

---

## **📝 NOTES (UPDATED):**

- **Current Status**: ✅ **Widget and WASM are connected and working**
- **Main Issue**: 🚨 **Property persistence during cart modifications**
- **Research Priority**: **Shoplazza cart API and event system**
- **Architecture Goal**: **Robust add-on persistence across all cart operations**

---

## **🚀 NEXT STEPS:**

1. **✅ COMPLETED**: Fix widget-WASM integration
2. **🆕 IMMEDIATE**: Research Shoplazza cart API endpoints
3. **🆕 NEXT**: Implement cart API interception
4. **🆕 THEN**: Add cart event handling
5. **🆕 FINALLY**: Implement fallback mechanisms for property persistence

---

## **🎯 SUCCESS CRITERIA:**

- ✅ **Basic Integration**: Widget and WASM work together (ACHIEVED)
- 🆕 **Property Persistence**: Add-on selections survive cart modifications
- 🆕 **API Interception**: Cart API calls maintain add-on properties
- 🆕 **Event Handling**: Cart events trigger appropriate property management
- 🆕 **Robust Operation**: Add-ons work reliably across all cart scenarios
