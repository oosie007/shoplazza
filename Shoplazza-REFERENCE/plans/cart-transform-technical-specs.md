# Cart-Transform Technical Specifications

## Architecture Overview
The cart-transform functionality will be implemented as a JavaScript function injected into the Shoplazza storefront that intercepts cart submissions and dynamically creates virtual line items for add-ons.

## Technical Implementation

### 1. Enhanced Form Interception
**Location**: `Controllers/WidgetController.cs` - `GenerateWidgetAsync` method

**Current Implementation**:
```csharp
// Existing form interception code
var formInterceptionScript = @"
  document.addEventListener('submit', function(e) {
    if (e.target.matches('form[action*=\""/cart/add\""]')) {
      // ... existing code ...
    }
  });
";
```

**Enhanced Implementation**:
```csharp
// Enhanced form interception with add-on state
var formInterceptionScript = @"
  document.addEventListener('submit', function(e) {
    if (e.target.matches('form[action*=\""/cart/add\""]')) {
      var addonWidget = document.getElementById('shoplazza-addon-widget');
      var addonCheckbox = addonWidget.querySelector('input[type=\""checkbox\""]');
      
      if (addonCheckbox && addonCheckbox.checked) {
        // Store add-on state for cart-transform
        window.shoplazzaAddonState = {
          selected: true,
          title: addonCheckbox.dataset.addonTitle,
          price: addonCheckbox.dataset.addonPrice,
          description: addonCheckbox.dataset.addonDescription
        };
        
        // Add properties to form
        var form = e.target;
        addHiddenField(form, 'properties[_addon_type]', 'product_addon');
        addHiddenField(form, 'properties[_addon_title]', addonCheckbox.dataset.addonTitle);
        addHiddenField(form, 'properties[_addon_price]', addonCheckbox.dataset.addonPrice);
        addHiddenField(form, 'properties[_addon_description]', addonCheckbox.dataset.addonDescription);
        addHiddenField(form, 'properties[_addon_sku]', addonCheckbox.dataset.addonSku);
      }
    }
  });
";
```

### 2. Cart-Transform JavaScript Function
**Location**: `Controllers/WidgetController.cs` - `GenerateWidgetAsync` method

**Implementation**:
```csharp
var cartTransformScript = @"
  // Cart-transform function for add-on line items
  window.shoplazzaAddonCartTransform = function() {
    if (!window.shoplazzaAddonState || !window.shoplazzaAddonState.selected) {
      return;
    }
    
    try {
      // Get current cart
      var cart = window.shoplazzaCart || {};
      
      // Create virtual line item for add-on
      var addonLineItem = {
        id: 'addon_' + Date.now(),
        product_id: 'virtual_addon_product',
        variant_id: 'virtual_addon_variant',
        title: window.shoplazzaAddonState.title,
        price: parseFloat(window.shoplazzaAddonState.price),
        quantity: 1,
        properties: {
          '_addon_type': 'product_addon',
          '_addon_title': window.shoplazzaAddonState.title
        },
        final_price: parseFloat(window.shoplazzaAddonState.price),
        final_line_price: parseFloat(window.shoplazzaAddonState.price)
      };
      
      // Add to cart line items
      if (!cart.line_items) {
        cart.line_items = [];
      }
      cart.line_items.push(addonLineItem);
      
      // Update cart totals
      if (!cart.total_price) cart.total_price = 0;
      cart.total_price += addonLineItem.final_line_price;
      
      // Store updated cart
      window.shoplazzaCart = cart;
      
      // Trigger cart update event
      var cartUpdateEvent = new CustomEvent('shoplazza:cart:updated', {
        detail: { cart: cart }
      });
      document.dispatchEvent(cartUpdateEvent);
      
    } catch (error) {
      console.error('Cart-transform error:', error);
      // Fallback to dialog
      window.shoplazzaAddonShowFallbackDialog();
    }
  };
  
  // Fallback dialog function
  window.shoplazzaAddonShowFallbackDialog = function() {
    if (window.shoplazzaAddonState && window.shoplazzaAddonState.selected) {
      var message = 'Add-on selected: ' + window.shoplazzaAddonState.title + 
                   ' (+$' + window.shoplazzaAddonState.price + ')';
      alert(message);
    }
  };
  
  // Intercept cart submissions and apply transform
  document.addEventListener('submit', function(e) {
    if (e.target.matches('form[action*=\""/cart/add\""]')) {
      // Apply cart-transform before form submission
      setTimeout(function() {
        window.shoplazzaAddonCartTransform();
      }, 100);
    }
  });
";
```

### 3. Widget Generation Integration
**Location**: `Controllers/WidgetController.cs` - `GenerateWidgetAsync` method

**Implementation**:
```csharp
// Combine all scripts in widget generation
var combinedScript = formInterceptionScript + "\n" + cartTransformScript;

var widgetHtml = $@"
  <div id=""shoplazza-addon-widget"" 
       data-product-id=""{productId}""
       data-addon-title=""{addonConfig.Title}""
       data-addon-price=""{addonConfig.Price}""
       data-addon-description=""{addonConfig.Description}""
       data-addon-sku=""{addonConfig.Sku}""
       class=""my-4"">
    
    <div class=""addon-selection"">
      <label class=""flex items-center cursor-pointer"">
        <input type=""checkbox"" 
               class=""addon-checkbox mr-2""
               data-addon-title=""{addonConfig.Title}""
               data-addon-price=""{addonConfig.Price}""
               data-addon-description=""{addonConfig.Description}""
               data-addon-sku=""{addonConfig.Sku}"">
        <span class=""addon-label"">
          {addonConfig.Title} (+${addonConfig.Price})
        </span>
      </label>
      <p class=""addon-description text-sm text-gray-600 mt-1"">
        {addonConfig.Description}
      </p>
    </div>
    
    <script>
      {combinedScript}
    </script>
  </div>
";
```

## Data Flow

### 1. Add-on Selection
1. User checks add-on checkbox
2. `handleAddOnToggle` function stores preference in localStorage
3. Confirmation popup shows add-on details

### 2. Cart Submission
1. Form submission is intercepted
2. Add-on state is stored in `window.shoplazzaAddonState`
3. Add-on properties are added to form as hidden fields
4. Cart-transform function is triggered

### 3. Cart-Transform Execution
1. Function checks if add-on is selected
2. Creates virtual line item with add-on details
3. Adds line item to cart structure
4. Updates cart totals
5. Triggers cart update event

### 4. Fallback Handling
1. If cart-transform fails, error is caught
2. Fallback dialog shows add-on selection details
3. User is informed of add-on selection

## Technical Requirements

### Browser Compatibility
- Modern browsers with ES6+ support
- Shoplazza storefront compatibility
- Mobile and desktop support

### Performance Considerations
- Minimal impact on page load time
- Efficient cart manipulation
- Proper error handling and fallbacks

### Security Considerations
- Validate add-on data before processing
- Sanitize user inputs
- Prevent XSS vulnerabilities

## Testing Strategy

### Unit Testing
- Test cart-transform function logic
- Test form interception functionality
- Test fallback dialog behavior

### Integration Testing
- Test with real Shoplazza cart
- Test add-on selection and cart updates
- Test error scenarios and fallbacks

### User Experience Testing
- Test add-on selection flow
- Test cart display and pricing
- Test mobile and desktop interfaces

## Dependencies
- Existing form interception code
- Add-on configuration from backend
- Shoplazza cart structure and events
- Browser JavaScript APIs

## Success Metrics
- Add-on selection works correctly
- Cart shows add-on as separate line item
- Cart totals include add-on pricing
- Fallback dialog works when needed
- No impact on existing functionality
