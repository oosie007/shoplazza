# **Shoplazza Add-On System Implementation Prompt**

## **🎯 CONTEXT & OBJECTIVE**

You are implementing **Phase 2** of a Shoplazza Add-On System that automatically adds protection plans to customer carts. The basic integration (Widget + WASM) is already working, but we need to implement **comprehensive cart interception and property persistence** to ensure add-on selections survive all cart modifications.

## **📁 WORKING DIRECTORY - CRITICAL**

**ONLY work in the `shoplazza-addon-app` folder.** This is the ONLY working directory. Do NOT attempt to work in other folders or make assumptions about file locations.

## **�� CRITICAL IMPLEMENTATION PLAN FILE**

**READ THIS FILE FIRST**: `IMPLEMENTATION_PLAN_PHASE2.md` - This contains the complete, detailed implementation plan with all code examples, requirements, and step-by-step instructions.

**This file is your blueprint** - follow it exactly and refer to it constantly during implementation.

## **✅ CURRENT STATUS (What's Working)**

1. **Widget (Frontend)**: ✅ Working - Serves dynamic HTML with add-on checkboxes
2. **WASM Function (Backend)**: ✅ Working - Processes cart data and adds add-on line items
3. **Basic Integration**: ✅ Working - Widget and WASM communicate correctly
4. **Form Interception**: ✅ Basic form property injection working

## **🚨 THE PROBLEM (What Needs Fixing)**

**Line item properties can be LOST** during cart modifications (remove/re-add, clear, third-party apps). Shoplazza doesn't auto-restore properties after modifications, so our add-on selections disappear.

## **🔧 SOLUTION STRATEGY (Multi-Layer Approach)**

### **Layer 1: Enhanced Form Interception** ✅ Already implemented
### **Layer 2: SessionStorage Backup System** �� NEW - Store add-on selections with expiration
### **Layer 3: API Interception** 🆕 NEW - Override fetch for `/api/cart` endpoints with property merging
### **Layer 4: Event-Driven Recovery** �� NEW - Listen for `shoplazza:cart:updated` and `shoplazza:section:unload`
### **Layer 5: Checkout Protection** �� NEW - Validate and restore add-ons before checkout

## **📁 KEY FILES TO READ AND WORK WITH**

### **IMPLEMENTATION PLAN (READ FIRST)**
- `IMPLEMENTATION_PLAN_PHASE2.md` - **CRITICAL: Your complete implementation blueprint**

### **CURRENT WORKING FILES**
- `wwwroot/widget-templates/widget-script.html` - Main widget JavaScript
- `cart-transform-function/cart-transform.js` - WASM function (needs updates for new property names)
- `Controllers/WidgetController.cs` - Widget serving logic
- `Services/CartTransformFunctionService.cs` - WASM management

### **RESEARCH & PLANNING FILES**
- `research.md` - Original research and problem analysis
- `PHASE2_RESEARCH_PLAN.md` - Research findings and methodology

## **📋 IMPLEMENTATION REQUIREMENTS**

### **1. Shoplazza-Specific Optimizations**
- Use `sessionStorage` (not localStorage) with store-specific keys and 1-hour expiration
- Include `X-Shoplazza-Cart-Token` in all API requests
- Handle cart token mismatch errors with regeneration
- Implement 300ms rate limiting for API calls
- Use `_add_on_` property naming to avoid conflicts

### **2. Property Persistence Features**
- **Property Merging**: Preserve existing properties during cart updates
- **Event Listening**: `shoplazza:cart:updated` and `shoplazza:section:unload`
- **Cart Modification Protection**: Monitor cart changes and backup properties
- **Checkout Validation**: Ensure add-ons persist to checkout

### **3. Production-Ready Features**
- **Error Recovery**: Handle API failures gracefully
- **Analytics Integration**: Track property persistence success rates
- **Multi-Currency Support**: Handle dynamic currency changes
- **Mobile PWA Compatibility**: Ensure cross-platform reliability

## **🚫 CRITICAL RULES - NO EXCEPTIONS**

1. **NO SHORTCUTS**: Always implement the full solution, don't skip steps
2. **NO ASSUMPTIONS**: If you're unsure about anything, ASK first
3. **NO "I SEE IT NOW"**: If you say this, you haven't given it proper thought
4. **NO ENDLESS LOOPS**: If you get stuck, ask for help immediately
5. **WORKING DIRECTORY ONLY**: Only work in `shoplazza-addon-app` folder
6. **ALWAYS ASK**: When in doubt, ask clarifying questions
7. **READ THE PLAN FIRST**: Always start by reading `IMPLEMENTATION_PLAN_PHASE2.md`

## **🧪 TESTING REQUIREMENTS**

Test all scenarios:
1. Basic add-to-cart with add-on
2. Cart item removal and re-addition
3. Cart clearing and property backup
4. API cart updates with property merging
5. Checkout flow with add-on persistence
6. Cart drawer close and state backup
7. Multi-currency changes
8. Third-party app conflicts

## **🎯 SUCCESS CRITERIA**

- ✅ Properties survive ALL cart modifications
- ✅ API calls maintain and merge properties automatically
- ✅ Event-driven property recovery works reliably
- ✅ SessionStorage backup provides fallback protection
- ✅ Checkout integration ensures add-ons persist
- ✅ Error handling is graceful with fallback mechanisms

## **🚀 IMPLEMENTATION APPROACH**

1. **READ THE PLAN**: Start by reading `IMPLEMENTATION_PLAN_PHASE2.md` completely
2. **Start with SessionStorage system** - Implement backup/restore functionality
3. **Add event listeners** - Listen for Shoplazza cart events
4. **Implement API interception** - Override fetch with property merging
5. **Add cart modification protection** - Monitor and backup properties
6. **Implement checkout validation** - Ensure add-ons persist to checkout
7. **Update WASM function** - Handle new property names
8. **Add production features** - Error recovery, analytics, rate limiting
9. **Comprehensive testing** - Validate all scenarios work correctly

## **❓ WHEN TO ASK QUESTIONS**

- If you're unsure about Shoplazza-specific behavior
- If you need clarification on any implementation step
- If you encounter unexpected errors or behavior
- If you need help with testing scenarios
- If you want to verify your approach before proceeding
- If you haven't read the implementation plan yet

## **📝 IMPLEMENTATION NOTES**

- This is a **production system** - implement robust error handling
- **Performance matters** - keep overhead under 100ms
- **User experience is critical** - add-ons must persist seamlessly
- **Testing is mandatory** - verify every scenario works
- **Documentation is required** - comment your code thoroughly
- **Follow the plan exactly** - the implementation plan is your source of truth

---

**Remember**: This is a complex, production-ready system. **Start by reading `IMPLEMENTATION_PLAN_PHASE2.md` completely**, then take your time implementing each layer properly, and always ask if you're unsure. The goal is a robust add-on system that works reliably across all Shoplazza cart scenarios. 