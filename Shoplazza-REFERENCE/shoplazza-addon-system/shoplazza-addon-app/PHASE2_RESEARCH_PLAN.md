# Phase 2: Shoplazza Cart API & Property Persistence Research Plan

## **🎯 RESEARCH OBJECTIVE**

**Understand how Shoplazza handles cart operations and property persistence** to implement robust add-on selection maintenance across all cart modifications.

## **🔍 CRITICAL RESEARCH QUESTIONS**

### **1. Shoplazza Cart API Endpoints**
- **What cart endpoints exist?**
  - `/cart/add` - Add items to cart ✅ **CONFIRMED**
  - `/api/cart/update.js` - Update cart via API ✅ **CONFIRMED**
  - `/cart/update` - Update cart quantities  
  - `/cart/change` - Modify cart contents
  - `/cart/clear` - Clear entire cart
  - Others?

- **How are these endpoints called?**
  - ✅ **Form submissions** with `properties[Custom_Field]` ✅ **CONFIRMED**
  - ✅ **API calls** to `/api/cart/update.js` ✅ **CONFIRMED**
  - Custom Shoplazza functions?

### **2. Property Persistence Behavior**
- **When do properties get lost?**
  - ✅ **Items removed and re-added** ✅ **CONFIRMED**
  - ✅ **Cart cleared programmatically** ✅ **CONFIRMED**
  - ✅ **Third-party apps modify cart** ✅ **CONFIRMED**
  - ✅ **Shoplazza doesn't auto-restore** properties after modifications ✅ **CONFIRMED**

- **Which operations preserve properties?**
  - ✅ **Direct form submissions** ✅ **CONFIRMED**
  - ✅ **API calls to `/api/cart/update.js`** ✅ **CONFIRMED**
  - Cart state changes?

### **3. Shoplazza Cart Events**
- **What events fire during cart operations?**
  - ✅ **`shoplazza:cart:updated`** ✅ **CONFIRMED**
  - `cart:requestComplete` - After cart operations finish
  - `cart:change` - When cart contents change
  - `product:added` - When items are added
  - `product:removed` - When items are removed
  - `cart:updated` - After cart updates

- **How can we listen for these events?**
  - ✅ **Custom event listeners** ✅ **CONFIRMED**
  - Shoplazza event system?
  - DOM mutation observers?

## **📋 RESEARCH METHODOLOGY**

### **Step 1: Analyze Shoplazza Storefront JavaScript**
- **Inspect Shoplazza storefront code** for cart-related functions
- **Look for cart API calls** in the browser's Network tab
- **Identify cart event triggers** and listeners
- **Find property handling logic** in cart operations

### **Step 2: Test Cart Operations**
- **Add items to cart** and monitor network requests
- **Modify cart quantities** and observe property behavior
- **Remove items** and check property persistence
- **Clear cart** and see what happens to properties

### **Step 3: Reverse Engineer Cart Flow**
- **Map the complete cart operation flow**
- **Identify all property injection points**
- **Find property validation and processing**
- **Understand error handling and fallbacks**

## **🔧 RESEARCH TOOLS & TECHNIQUES**

### **Browser Developer Tools**
- **Network Tab**: Monitor cart API calls
- **Console**: Look for cart-related JavaScript
- **Sources**: Inspect cart JavaScript files
- **Application**: Check localStorage and sessionStorage

### **Shoplazza Documentation**
- **Developer docs** for cart operations
- **API reference** for cart endpoints
- **Event system documentation**
- **Best practices** for cart modifications

### **Community Resources**
- **Shoplazza developer forums**
- **GitHub repositories** with Shoplazza integrations
- **Stack Overflow** questions about Shoplazza cart
- **Developer blogs** about Shoplazza customization

## **📊 EXPECTED RESEARCH OUTCOMES**

### **Technical Understanding**
- **Complete cart API map** with all endpoints
- **Property persistence rules** and edge cases
- **Event system architecture** and timing
- **Error handling patterns** and fallbacks

### **Implementation Strategy**
- **Best interception points** for cart operations
- **Property maintenance strategies** for each scenario
- **Event handling approach** for cart changes
- **Fallback mechanisms** for property loss

### **Risk Assessment**
- **Property loss scenarios** and frequency
- **Cart modification impact** on add-ons
- **Browser compatibility** issues
- **Performance implications** of interception

## **🚀 IMMEDIATE RESEARCH TASKS**

### **Task 1: Shoplazza Storefront Analysis**
- **Find a Shoplazza store** to analyze
- **Inspect cart JavaScript** for API calls
- **Monitor network requests** during cart operations
- **Document cart endpoint patterns**

### **Task 2: Cart Operation Testing**
- **Test add-to-cart** with properties
- **Test cart modifications** and property persistence
- **Test cart clearing** and property behavior
- **Document all findings** with examples

### **Task 3: Event System Research**
- **Look for cart event listeners** in storefront code
- **Test custom event dispatching** to Shoplazza
- **Find event documentation** or examples
- **Understand event timing** and order

## **📝 RESEARCH DELIVERABLES**

### **Technical Documentation**
- **Cart API reference** with all endpoints
- **Property persistence rules** and examples
- **Event system documentation** and usage
- **Interception strategy** recommendations

### **Implementation Guide**
- **Step-by-step cart interception** approach
- **Property maintenance code** examples
- **Event handling implementation** patterns
- **Testing and validation** procedures

### **Risk Mitigation Plan**
- **Property loss prevention** strategies
- **Fallback mechanism** implementations
- **Error handling** and recovery
- **Performance optimization** recommendations

## **⏰ RESEARCH TIMELINE**

### **Week 1: Storefront Analysis**
- Analyze Shoplazza storefront JavaScript
- Document cart API endpoints
- Identify property handling patterns

### **Week 2: Cart Operation Testing**
- Test all cart operations with properties
- Document property persistence behavior
- Map cart event system

### **Week 3: Strategy Development**
- Develop interception strategy
- Plan property maintenance approach
- Design fallback mechanisms

### **Week 4: Implementation Planning**
- Create implementation roadmap
- Define testing requirements
- Plan deployment strategy

## **🎯 SUCCESS CRITERIA**

- ✅ **Complete cart API understanding** with all endpoints documented
- ✅ **Property persistence rules** clearly defined with examples
- ✅ **Event system architecture** mapped and understood
- ✅ **Implementation strategy** developed with clear approach
- ✅ **Risk assessment** completed with mitigation plans
- ✅ **Testing plan** created for validation

---

## **🆕 SHOPLAZZA-SPECIFIC FINDINGS (CONFIRMED)**

### **Property Persistence Confirmed:**
- ✅ **Properties stored in cart session** via form submissions and API calls
- ✅ **Properties persist to orders** when checkout completes
- ✅ **Properties can be lost** during cart modifications (remove/re-add, clear, third-party apps)
- ✅ **Shoplazza doesn't auto-restore** properties after modifications

### **Shoplazza-Specific Solutions:**
- ✅ **Event listener**: `shoplazza:cart:updated` event for cart changes
- ✅ **API interception**: Override `window.fetch` for `/api/cart` calls
- ✅ **Backup system**: Use `localStorage` to store property backups
- ✅ **Form submission**: Use `properties[Custom_Field]` in forms

### **Implementation Approach:**
1. **Listen for `shoplazza:cart:updated` events**
2. **Intercept all `/api/cart` fetch calls**
3. **Store property backups in localStorage**
4. **Reapply properties after any cart modification**

---

**Next Action**: Begin implementing Shoplazza-specific cart interception and property persistence mechanisms based on confirmed findings.
