# Shoplazza Add-On Widget - Troubleshooting FAQ

## 🔍 Common Issues & Solutions

### **Widget Not Appearing on Product Page**

#### **Problem**: The add-on widget doesn't show up on my product pages.

#### **Possible Causes & Solutions**:

1. **Missing Widget Anchor**
   - **Cause**: The widget anchor div is not present in your theme template
   - **Solution**: Add the widget anchor to your product page template:
   ```liquid
   <div id="shoplazza-addon-widget" 
        data-product-id="{{ product.id }}" 
        data-product-handle="{{ product.handle }}" 
        data-product-title="{{ product.title | escape }}">
   </div>
   ```

2. **Wrong Template File**
   - **Cause**: You added the widget to the wrong template file
   - **Solution**: Ensure you're editing the main product page template:
     - `product-template.liquid`
     - `main-product.liquid`
     - `product.liquid`
     - `product-page.liquid`

3. **JavaScript Errors**
   - **Cause**: JavaScript errors preventing widget initialization
   - **Solution**: 
     - Open browser Developer Tools (F12)
     - Check Console tab for error messages
     - Look for errors related to "Shoplazza Add-On Widget"

4. **App Not Installed/Configured**
   - **Cause**: The add-on app is not properly installed or configured
   - **Solution**: 
     - Verify app installation in your Shoplazza admin
     - Check that add-ons are configured for your products
     - Ensure the app has proper permissions

---

### **Add-On Not Working/Selecting**

#### **Problem**: I can see the widget but can't select the add-on or it's not working.

#### **Possible Causes & Solutions**:

1. **No Add-On Configuration**
   - **Cause**: No add-ons are configured for this product in the app
   - **Solution**: 
     - Go to the add-on app dashboard
     - Configure add-ons for your products
     - Set pricing, titles, and descriptions

2. **Product ID Mismatch**
   - **Cause**: Product ID in template doesn't match app configuration
   - **Solution**: 
     - Verify `{{ product.id }}` is correct
     - Check app dashboard for product mapping
     - Ensure product handle matches

3. **JavaScript Conflicts**
   - **Cause**: Other JavaScript code interfering with widget
   - **Solution**: 
     - Check for conflicting JavaScript libraries
     - Verify jQuery version compatibility
     - Look for CSS conflicts

---

### **Styling Issues**

#### **Problem**: The widget doesn't match my theme's styling.

#### **Possible Causes & Solutions**:

1. **CSS Conflicts**
   - **Cause**: Your theme CSS is overriding widget styles
   - **Solution**: Add custom CSS to override theme styles:
   ```css
   #shoplazza-addon-widget {
     /* Your custom styles here */
     margin: 20px 0;
     padding: 20px;
     border: 2px solid #your-theme-color;
   }
   ```

2. **Z-Index Issues**
   - **Cause**: Widget appears behind other elements
   - **Solution**: Add higher z-index to widget:
   ```css
   #shoplazza-addon-widget {
     z-index: 1000;
     position: relative;
   }
   ```

3. **Responsive Design Issues**
   - **Cause**: Widget doesn't adapt to mobile screens
   - **Solution**: Test on different screen sizes and add responsive CSS:
   ```css
   @media (max-width: 768px) {
     #shoplazza-addon-widget {
       padding: 15px;
       margin: 15px 0;
     }
   }
   ```

---

### **Cart Integration Issues**

#### **Problem**: Add-ons are not being added to cart properly.

#### **Possible Causes & Solutions**:

1. **Form Interception Failure**
   - **Cause**: Widget can't intercept the product form submission
   - **Solution**: 
     - Ensure widget anchor is near the form
     - Check that form has `action="/cart/add"`
     - Verify form structure matches expected format

2. **Missing Form Properties**
   - **Cause**: Add-on properties not being added to form
   - **Solution**: 
     - Check browser console for error messages
     - Verify widget is properly initialized
     - Ensure add-on configuration is loaded

3. **Cart API Errors**
   - **Cause**: Shoplazza cart API returning errors
   - **Solution**: 
     - Check browser Network tab for failed requests
     - Verify store permissions and API access
     - Contact Shoplazza support if API issues persist

---

### **Performance Issues**

#### **Problem**: Widget is slow to load or causes page lag.

#### **Possible Causes & Solutions**:

1. **Large Widget Size**
   - **Cause**: Widget JavaScript is too large
   - **Solution**: 
     - Check widget file size in Network tab
     - Ensure widget is minified
     - Consider lazy loading for non-critical features

2. **Multiple Widget Instances**
   - **Cause**: Widget is being initialized multiple times
   - **Solution**: 
     - Ensure only one widget anchor per page
     - Check for duplicate widget initialization
     - Verify widget cleanup on page changes

3. **External API Calls**
   - **Cause**: Widget making too many external requests
   - **Solution**: 
     - Check Network tab for excessive requests
     - Implement proper caching
     - Optimize API call frequency

---

## 🛠️ Debugging Steps

### **Step 1: Check Browser Console**
1. Open Developer Tools (F12)
2. Go to Console tab
3. Look for errors related to "Shoplazza Add-On Widget"
4. Note any JavaScript errors or warnings

### **Step 2: Check Network Tab**
1. Go to Network tab in Developer Tools
2. Refresh the page
3. Look for failed requests to widget or cart APIs
4. Check response status codes and error messages

### **Step 3: Verify Widget Anchor**
1. Inspect the page source
2. Search for `shoplazza-addon-widget`
3. Ensure the div is present with correct attributes
4. Verify data attributes have proper values

### **Step 4: Check App Configuration**
1. Go to add-on app dashboard
2. Verify product configuration
3. Check add-on settings and pricing
4. Ensure app permissions are correct

---

## 📱 Mobile-Specific Issues

### **Touch Events Not Working**
- **Cause**: Mobile touch events not properly handled
- **Solution**: Test on actual mobile devices, not just browser dev tools

### **Responsive Layout Issues**
- **Cause**: Widget not adapting to mobile screen sizes
- **Solution**: Add mobile-specific CSS media queries

### **Performance on Mobile**
- **Cause**: Widget too heavy for mobile devices
- **Solution**: Optimize widget size and implement lazy loading

---

## 🌐 Browser Compatibility Issues

### **Internet Explorer**
- **Issue**: Widget may not work in older IE versions
- **Solution**: Ensure compatibility with IE 11+ or recommend modern browsers

### **Safari Mobile**
- **Issue**: Some CSS properties may not work
- **Solution**: Test thoroughly on iOS devices and Safari

### **Firefox**
- **Issue**: CSS Grid or Flexbox compatibility
- **Solution**: Use fallback CSS for older Firefox versions

---

## �� Advanced Troubleshooting

### **Widget Initialization Debug**
Add this to your browser console to debug widget initialization:
```javascript
// Check if widget is loaded
console.log('Widget loaded:', typeof window.ShoplazzaAddonWidget !== 'undefined');

// Check widget configuration
if (window.ShoplazzaAddonWidget) {
  console.log('Widget config:', window.ShoplazzaAddonWidget.addOnConfig);
  console.log('Widget state:', window.ShoplazzaAddonWidget.addOnPreference);
}
```

### **Form Interception Debug**
Check if form interception is working:
```javascript
// Find product forms on page
var forms = document.querySelectorAll('form[action*="/cart/add"]');
console.log('Found forms:', forms.length);

// Check if widget is intercepting
if (window.ShoplazzaAddonWidget) {
  console.log('Form interception active:', window.ShoplazzaAddonWidget.initialized);
}
```

---

## 📞 Getting Help

### **Before Contacting Support**:
1. ✅ Check this troubleshooting guide
2. ✅ Verify widget anchor is properly placed
3. ✅ Check browser console for errors
4. ✅ Test on different browsers/devices
5. ✅ Verify app configuration

### **When Contacting Support**:
- **Store URL**: Your Shoplazza store URL
- **Theme Name**: Your current theme name and version
- **Error Messages**: Copy any console errors
- **Steps to Reproduce**: Detailed steps to recreate the issue
- **Screenshots**: Visual evidence of the problem
- **Browser/Device**: Browser version and device type

### **Support Channels**:
- **Email**: support@addon-app.com
- **Documentation**: [App Documentation](link-to-docs)
- **Community Forum**: [User Community](link-to-community)

---

## 📚 Additional Resources

- [Merchant Setup Guide](merchant-setup-guide.md)
- [Widget Configuration](widget-configuration.md)
- [API Documentation](api-documentation.md)
- [Theme Integration Examples](theme-examples.md)

---

*Last updated: 2025-01-27*
*FAQ Version: 1.0.0*
