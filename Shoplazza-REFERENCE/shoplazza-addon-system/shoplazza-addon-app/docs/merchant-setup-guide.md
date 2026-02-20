# Shoplazza Add-On Widget - Merchant Setup Guide

## 🚀 Quick Start (Minimal Setup)

The add-on widget is designed to work with **minimal theme modifications**. You only need to add **one line of code** to your product page template.

### Step 1: Add Widget Anchor to Product Page

Find your main product page template (usually named one of these):
- `product-template.liquid`
- `main-product.liquid` 
- `product.liquid`
- `product-page.liquid`

**Add this single line where you want the add-on widget to appear:**

```liquid
<!-- Add-On Widget will render here -->
<div id="shoplazza-addon-widget" 
     data-product-id="{{ product.id }}" 
     data-product-handle="{{ product.handle }}" 
     data-product-title="{{ product.title | escape }}">
</div>
```

**Recommended placement:** Add this right after your product form or before the "Add to Cart" button.

### Step 2: That's It! 

The widget will automatically:
- ✅ Detect your product page
- ✅ Load add-on configuration from the app
- ✅ Render the add-on checkbox and pricing
- ✅ Handle cart integration when customers click "Add to Cart"
- ✅ Show confirmation popups for add-on selection

---

## �� Advanced Customization (Optional)

### Custom Widget Placement

If you want more control over placement, you can add the widget anchor in specific locations:

**Option A: After Product Form**
```liquid
<form action="/cart/add" method="post" enctype="multipart/form-data">
  <!-- Your existing product form fields -->
  
  <!-- Add-On Widget appears here -->
  <div id="shoplazza-addon-widget" 
       data-product-id="{{ product.id }}" 
       data-product-handle="{{ product.handle }}" 
       data-product-title="{{ product.title | escape }}">
  </div>
  
  <button type="submit" name="add">Add to Cart</button>
</form>
```

**Option B: Before Add to Cart Button**
```liquid
<!-- Product details -->
<div class="product-details">
  <!-- Add-On Widget appears here -->
  <div id="shoplazza-addon-widget" 
       data-product-id="{{ product.id }}" 
       data-product-handle="{{ product.handle }}" 
       data-product-title="{{ product.title | escape }}">
  </div>
  
  <!-- Your existing Add to Cart button -->
  <button type="submit" name="add">Add to Cart</button>
</div>
```

**Option C: In Product Description Area**
```liquid
<div class="product-description">
  {{ product.description }}
  
  <!-- Add-On Widget appears here -->
  <div id="shoplazza-addon-widget" 
       data-product-id="{{ product.id }}" 
       data-product-handle="{{ product.handle }}" 
       data-product-title="{{ product.title | escape }}">
  </div>
</div>
```

---

## 📱 How It Works

### 1. **Widget Detection**
The widget automatically finds the `#shoplazza-addon-widget` anchor and renders the add-on interface.

### 2. **Add-On Display**
- Shows a checkbox with add-on title and price
- Displays add-on description (if configured)
- Matches your theme's styling automatically

### 3. **Cart Integration**
- When customer selects add-on and clicks "Add to Cart"
- Add-on information is automatically added as line item properties
- **No separate cart operations** - everything happens in one transaction

### 4. **User Experience**
- Confirmation popup shows selected add-on details
- Clear pricing information before purchase
- Seamless integration with existing checkout flow

---

## 🎨 Styling Customization

The widget automatically adapts to your theme, but you can customize the appearance:

### Custom CSS (Optional)
Add this to your theme's CSS file if you want to customize the widget appearance:

```css
/* Customize widget container */
#shoplazza-addon-widget {
  margin: 20px 0;
  padding: 20px;
  border: 2px solid #your-theme-color;
  border-radius: 8px;
}

/* Customize checkbox */
#shoplazza-addon-widget .addon-checkbox {
  accent-color: #your-theme-color;
}

/* Customize add-on title */
#shoplazza-addon-widget .addon-title {
  color: #your-theme-text-color;
  font-weight: bold;
}

/* Customize add-on price */
#shoplazza-addon-widget .addon-price {
  color: #your-theme-accent-color;
  font-size: 1.1em;
}
```

---

## 🔍 Troubleshooting

### Widget Not Appearing?
1. **Check the anchor ID**: Make sure you have `<div id="shoplazza-addon-widget">`
2. **Verify product data**: Ensure `{{ product.id }}` is available in your template
3. **Check browser console**: Look for any JavaScript errors

### Add-On Not Working?
1. **Verify app configuration**: Make sure add-ons are configured in the app
2. **Check product ID**: Ensure the product ID matches between your store and the app
3. **Clear browser cache**: Sometimes cached JavaScript can cause issues

### Styling Issues?
1. **CSS conflicts**: Check if your theme CSS is overriding widget styles
2. **Z-index issues**: Ensure the widget has proper layering
3. **Responsive design**: Test on different screen sizes

---

## 📋 Template Examples

### Minimal Product Template
```liquid
{% comment %} Minimal product template with add-on widget {% endcomment %}

<div class="product-page">
  <h1>{{ product.title }}</h1>
  
  <form action="/cart/add" method="post" enctype="multipart/form-data">
    <input type="hidden" name="id" value="{{ product.selected_or_first_available_variant.id }}">
    
    <!-- Add-On Widget -->
    <div id="shoplazza-addon-widget" 
         data-product-id="{{ product.id }}" 
         data-product-handle="{{ product.handle }}" 
         data-product-title="{{ product.title | escape }}">
    </div>
    
    <button type="submit" name="add">Add to Cart</button>
  </form>
</div>
```

### Advanced Product Template
```liquid
{% comment %} Advanced product template with multiple sections {% endcomment %}

<div class="product-container">
  <!-- Product Images -->
  <div class="product-images">
    {% for image in product.images %}
      <img src="{{ image | img_url: 'medium' }}" alt="{{ image.alt }}">
    {% endfor %}
  </div>
  
  <!-- Product Details -->
  <div class="product-details">
    <h1>{{ product.title }}</h1>
    <p class="price">{{ product.price | money }}</p>
    <p class="description">{{ product.description }}</p>
    
    <!-- Add-On Widget -->
    <div id="shoplazza-addon-widget" 
         data-product-id="{{ product.id }}" 
         data-product-handle="{{ product.handle }}" 
         data-product-title="{{ product.title | escape }}">
    </div>
    
    <!-- Product Form -->
    <form action="/cart/add" method="post" enctype="multipart/form-data">
      <input type="hidden" name="id" value="{{ product.selected_or_first_available_variant.id }}">
      <button type="submit" name="add">Add to Cart</button>
    </form>
  </div>
</div>
```

---

## 🎯 Best Practices

### ✅ **Do's**
- Place the widget anchor near the "Add to Cart" button
- Use the exact `id="shoplazza-addon-widget"` 
- Include all three data attributes for best compatibility
- Test on both desktop and mobile devices

### ❌ **Don'ts**
- Don't change the widget ID
- Don't place the widget outside the product page
- Don't remove the data attributes
- Don't place multiple widget anchors on the same page

---

## 🆘 Need Help?

If you encounter any issues:

1. **Check the setup guide** above
2. **Verify your template syntax** matches the examples
3. **Check browser console** for error messages
4. **Contact support** with your specific error details

---

## 📚 Additional Resources

- [Shoplazza Liquid Documentation](https://www.shoplazza.dev/reference/overview-30)
- [App Configuration Guide](link-to-app-config)
- [Troubleshooting FAQ](troubleshooting-faq.md)

---

*Last updated: 2025-01-27*
*Widget Version: 1.0.0*
