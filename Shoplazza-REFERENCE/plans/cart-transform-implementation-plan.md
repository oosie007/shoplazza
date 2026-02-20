# Cart-Transform Implementation Plan

## Overview
This plan implements Shoplazza cart-transform functionality to dynamically add add-on products as separate line items when customers select the add-on checkbox and click "Add to Cart".

## Current State
- ✅ Add-on selection checkbox with confirmation popup
- ✅ Form interception that adds add-on properties to cart submissions
- ✅ Properties are being sent to Shoplazza cart (but price isn't being modified yet)
- ✅ Add-on configuration comes from backend via `_addon_price`, `_addon_title`, etc.

## Target State
- ✅ Cart-transform function intercepts cart submissions
- ✅ When add-on is selected, creates virtual line item for add-on
- ✅ Add-on appears as separate line item in cart with transparent pricing
- ✅ Fallback to dialog if cart-transform fails

## Technical Approach

### 1. Enhanced Form Interception
- Keep existing form interception in WidgetController.cs
- Add add-on selection state to form data
- Include add-on configuration data for cart-transform

### 2. Cart-Transform JavaScript Function
- Inject JavaScript function into widget generation
- Function intercepts cart submissions when add-on is selected
- Creates virtual line item with add-on details
- Uses `_addon_price` and `_addon_title` from form data

### 3. Virtual Line Item Creation
- Product ID: Virtual add-on product ID
- Quantity: 1
- Properties: `_addon_title` only
- Price: Add-on price from configuration
- Transparent display in cart

## Key Requirements
- **Q1**: Virtual line item approach ✅
- **Q2**: JavaScript implementation ✅  
- **Q3**: Only `_addon_title` as property ✅
- **Q4**: Separate line item for transparency ✅
- **Fallback**: Dialog if cart-transform fails ✅

## Files to Modify
1. `Controllers/WidgetController.cs` - Add cart-transform logic to widget generation
2. `Models/Dto/CartDto.cs` - May need updates for virtual line items
3. `Services/ShoplazzaApiService.cs` - May need cart modification methods

## Success Criteria
- Add-on checkbox selection works
- Cart submission includes add-on as separate line item
- Add-on price is correctly added to cart total
- User sees transparent pricing in cart
- Fallback dialog works if cart-transform fails

## Dependencies
- Shoplazza cart-transform API
- Existing form interception code
- Add-on configuration from backend
- Cart line item structure from liquid templates

## Risk Mitigation
- Test cart-transform functionality thoroughly
- Implement fallback dialog for edge cases
- Validate cart totals and line item creation
- Test with various add-on configurations
