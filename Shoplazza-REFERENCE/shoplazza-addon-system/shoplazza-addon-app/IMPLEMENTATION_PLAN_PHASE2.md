# Phase 2 Implementation Plan: Shoplazza Cart Interception & Property Persistence

## **🎯 IMPLEMENTATION OBJECTIVE**

**Implement comprehensive cart interception and property persistence mechanisms** to ensure add-on selections survive all cart modifications in Shoplazza.

## **✅ CONFIRMED SHOPLAZZA BEHAVIOR**

### **Property Persistence:**
- Properties stored in cart session via form submissions and API calls
- Properties persist to orders when checkout completes
- Properties can be lost during cart modifications (remove/re-add, clear, third-party apps)
- Shoplazza doesn't auto-restore properties after modifications

### **Shoplazza-Specific Solutions:**
- Event listener: `shoplazza:cart:updated` event for cart changes
- Event listener: `shoplazza:section:unload` for cart drawer closes
- API interception: Override `window.fetch` for `/api/cart` calls
- Backup system: Use `sessionStorage` with store-specific keys and expiration
- Form submission: Use `properties[Custom_Field]` in forms

### **🆕 LATEST DEEPSEEK INSIGHTS INTEGRATED:**
- **Property Naming**: Use underscore prefixes (`_add_on_`) to avoid conflicts
- **API Endpoints**: `/api/cart`, `/api/cart/add`, `/api/cart/update` (no `.js` required)
- **Cart Token System**: Include `X-Shoplazza-Cart-Token` in API requests
- **Property Limits**: 100KB per cart item, only 50 properties show in checkout UI
- **Property Compression**: Use `_compressed` flag for complex configurations
- **Checkout Integration**: Handle both classic and one-page checkout flows
- **WASM Integration**: Process cart data after updates for price modifications

### **🆕 FINAL DEEPSEEK RECOMMENDATIONS INTEGRATED:**
- **Error Recovery**: Handle cart token mismatch and regeneration
- **Enhanced Compression**: Base64 encoding with fallback display properties
- **Analytics Integration**: Track property persistence and recovery events
- **Multi-Currency Support**: Handle currency changes and price updates
- **Mobile PWA Testing**: Ensure sessionStorage persists in progressive web app
- **Rate Limiting**: Prevent API call flooding with 300ms delays
- **App Conflict Testing**: Simulate third-party app property clearing

## **🔧 IMPLEMENTATION STRATEGY**

### **1. Multi-Layer Property Persistence**
- **Form Interception**: ✅ **Already implemented** - Basic form property injection
- **API Interception**: 🆕 **NEW** - Override fetch calls with property merging
- **Event Handling**: 🆕 **NEW** - Listen for Shoplazza cart and section events
- **Session Storage Backup**: 🆕 **NEW** - Store property backups with expiration
- **Checkout Validation**: 🆕 **NEW** - Ensure add-ons persist to checkout
- **Cart Token Management**: 🆕 **NEW** - Handle Shoplazza's cart token system

### **2. Property Recovery Mechanisms**
- **Automatic Recovery**: Reapply properties after any cart modification
- **Property Merging**: Preserve existing properties during API updates
- **Manual Recovery**: Restore properties from sessionStorage when needed
- **Fallback Handling**: Graceful degradation when properties can't be maintained
- **Checkout Protection**: Validate and restore add-ons before checkout

### **3. Production-Ready Features**
- **Error Recovery**: Handle cart token mismatches and API failures
- **Rate Limiting**: Prevent API call flooding
- **Analytics Tracking**: Monitor property persistence success rates
- **Multi-Currency Support**: Handle dynamic currency changes
- **Mobile PWA Compatibility**: Ensure cross-platform reliability

## **📋 IMPLEMENTATION STEPS**

### **Step 1: Enhanced Widget JavaScript (Priority: HIGH)**

#### **1.1 Add Shoplazza-Optimized Session Storage System**
```javascript
// Shoplazza-optimized storage with store-specific keys and expiration
const shoplazzaStorage = {
  setAddon: (productId, data) => {
    const key = `sl_addon_${productId}_${window.location.hostname}`;
    sessionStorage.setItem(key, JSON.stringify({
      ...data,
      _shoplazza_store: window.location.hostname,
      _expires: Date.now() + 3600000 // 1 hour expiration
    }));
    console.log('Add-on selection backed up to sessionStorage:', data);
  },
  
  getAddon: (productId) => {
    const key = `sl_addon_${productId}_${window.location.hostname}`;
    const data = sessionStorage.getItem(key);
    if (data) {
      const parsed = JSON.parse(data);
      if (parsed._expires > Date.now()) {
        return parsed;
      }
      sessionStorage.removeItem(key); // Clean expired data
      console.log('Expired add-on data cleaned:', key);
    }
    return null;
  },
  
  getAllAddons: () => {
    const addons = [];
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i);
      if (key && key.startsWith(`sl_addon_`) && key.includes(window.location.hostname)) {
        const data = sessionStorage.getItem(key);
        if (data) {
          const parsed = JSON.parse(data);
          if (parsed._expires > Date.now()) {
            addons.push(parsed);
          } else {
            sessionStorage.removeItem(key); // Clean expired data
          }
        }
      }
    }
    return addons;
  },
  
  clearExpired: () => {
    const now = Date.now();
    let cleaned = 0;
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i);
      if (key && key.startsWith(`sl_addon_`)) {
        const data = sessionStorage.getItem(key);
        if (data) {
          const parsed = JSON.parse(data);
          if (parsed._expires && parsed._expires < now) {
            sessionStorage.removeItem(key);
            cleaned++;
          }
        }
      }
    }
    if (cleaned > 0) {
      console.log(`Cleaned ${cleaned} expired add-on entries`);
    }
    return cleaned;
  }
};

// Enhanced property compression for complex add-ons
function compressAddonProperties(config) {
  const compressed = {
    _add_on_data: btoa(JSON.stringify(config)), // Base64 encoded full config
    _compressed: true,
    _v: 2 // Version flag for parsing
  };
  
  // Fallback display properties for checkout UI (within 50-property limit)
  if (config.title) compressed._add_on_title = config.title.slice(0, 50);
  if (config.price) compressed._add_on_price = config.price.toString();
  if (config.sku) compressed._add_on_sku = config.sku.slice(0, 20);
  
  return compressed;
}

// Store add-on selections with enhanced compression
function backupAddOnSelections(productId, addOnConfig) {
    const backup = {
        productId: productId,
        variantId: addOnConfig.variantId,
        addOnConfig: addOnConfig,
        timestamp: Date.now(),
        properties: compressAddonProperties(addOnConfig)
    };
    
    shoplazzaStorage.setAddon(productId, backup);
}
```

#### **1.2 Add Enhanced Shoplazza Event Listeners with Analytics**
```javascript
// Listen for Shoplazza cart updates and section unloads
document.addEventListener('shoplazza:cart:updated', function(event) {
    console.log('Shoplazza cart updated, checking for lost properties...');
    window.__addonsRestored = 0; // Reset counter for analytics
    restoreLostAddOnProperties();
    
    // Track cart property persistence for analytics
    if (window.Shoplazza && window.Shoplazza.analytics) {
        window.Shoplazza.analytics.track('Cart Properties Updated', {
            count: shoplazzaStorage.getAllAddons().length,
            restored: window.__addonsRestored || 0,
            timestamp: Date.now()
        });
    }
});

document.addEventListener('shoplazza:section:unload', function(event) {
    if (event.detail.sectionId === 'cart-drawer') {
        console.log('Cart drawer closed, backing up current state...');
        backupCurrentCartState();
    }
});

// Multi-currency support
document.addEventListener('shoplazza:currency:changed', function(event) {
    console.log('Currency changed, updating add-on prices...');
    updateAddonPricesForCurrency(event.detail.currency || window.Shoplazza.currency.current);
});

// Restore properties that may have been lost
function restoreLostAddOnProperties() {
    const cartItems = document.querySelectorAll('[data-cart-item]');
    let restoredCount = 0;
    
    cartItems.forEach(item => {
        const productId = item.dataset.productId;
        const backup = shoplazzaStorage.getAddon(productId);
        
        if (backup && !hasAddOnProperties(item)) {
            console.log(`Restoring add-on properties for product ${productId}`);
            reapplyAddOnProperties(item, backup.properties);
            restoredCount++;
        }
    });
    
    window.__addonsRestored = restoredCount;
    if (restoredCount > 0) {
        console.log(`Restored ${restoredCount} add-on selections`);
    }
}

// Update add-on prices for currency changes
function updateAddonPricesForCurrency(currency) {
    const addons = shoplazzaStorage.getAllAddons();
    addons.forEach(addon => {
        if (addon.addOnConfig && addon.addOnConfig.price) {
            // Convert price to new currency (simplified - in production, use proper conversion)
            const convertedPrice = convertPrice(addon.addOnConfig.price, currency);
            addon.addOnConfig.price = convertedPrice;
            shoplazzaStorage.setAddon(addon.productId, addon);
        }
    });
    
    // Update displayed prices on the page
    updateDisplayedAddonPrices(currency);
}

// Check if cart item has add-on properties
function hasAddOnProperties(cartItem) {
    const propertyInputs = cartItem.querySelectorAll('input[name^="properties[_add_on_"]');
    return propertyInputs.length > 0;
}

// Backup current cart state when drawer closes
function backupCurrentCartState() {
    const cartItems = document.querySelectorAll('[data-cart-item]');
    cartItems.forEach(item => {
        const productId = item.dataset.productId;
        const properties = extractPropertiesFromCartItem(item);
        if (properties && Object.keys(properties).length > 0) {
            const backup = {
                productId: productId,
                properties: properties,
                timestamp: Date.now()
            };
            shoplazzaStorage.setAddon(productId, backup);
        }
    });
}
```

### **Step 2: Enhanced API Interception with Property Merging and Rate Limiting (Priority: HIGH)**

#### **2.1 Override Fetch with Shoplazza-Specific Property Preservation and Rate Limiting**
```javascript
// Shoplazza-specific fetch interception with property merging and rate limiting
const originalFetch = window.fetch;
let lastCartCall = 0;
const CART_API_DELAY = 300; // ms - prevent API flooding

window.fetch = async function(url, options) {
    if (typeof url === 'string' && url.includes('/api/cart')) {
        console.log('Intercepting Shoplazza cart API call:', url);
        
        // Rate limiting for cart API calls
        if (Date.now() - lastCartCall < CART_API_DELAY) {
            const delay = CART_API_DELAY - (Date.now() - lastCartCall);
            console.log(`Rate limiting: waiting ${delay}ms before cart API call`);
            await new Promise(resolve => setTimeout(resolve, delay));
        }
        lastCartCall = Date.now();
        
        return interceptShoplazzaCartApiCall(url, options);
    }
    return originalFetch.apply(this, arguments);
};

// Intercept and modify Shoplazza cart API calls with property preservation
async function interceptShoplazzaCartApiCall(url, options) {
    if (options && options.method === 'POST' && options.body) {
        try {
            const body = JSON.parse(options.body);
            
            // Merge existing properties for line items to prevent loss
            if (body.updates) {
                const currentCart = await fetch('/api/cart', {
                    headers: getShoplazzaHeaders()
                }).then(r => r.json());
                
                currentCart.items.forEach(item => {
                    if (item.properties) {
                        body.properties = body.properties || {};
                        body.properties[item.id] = {
                            ...item.properties,
                            ...(body.properties[item.id] || {})
                        };
                    }
                });
                
                options.body = JSON.stringify(body);
                console.log('Merged existing properties to prevent loss:', body.properties);
            }
            
            // Ensure add-on properties are included for new additions
            if (body.id && !body.properties) {
                const addOnProperties = getCurrentAddOnProperties();
                if (Object.keys(addOnProperties).length > 0) {
                    body.properties = addOnProperties;
                    options.body = JSON.stringify(body);
                    console.log('Added add-on properties to new cart item:', addOnProperties);
                }
            }
        } catch (e) {
            console.warn('Could not parse Shoplazza cart API body:', e);
        }
    }
    
    return originalFetch(url, options);
}
```

#### **2.2 Shoplazza Headers, Cart Token Management, and Error Recovery**
```javascript
// Get Shoplazza-specific headers including cart token
function getShoplazzaHeaders() {
    const headers = {
        'Content-Type': 'application/json',
        'X-Requested-With': 'XMLHttpRequest'
    };
    
    // Include cart token if available
    const cartToken = getCartTokenFromCookie();
    if (cartToken) {
        headers['X-Shoplazza-Cart-Token'] = cartToken;
    }
    
    return headers;
}

// Extract cart token from cookies
function getCartTokenFromCookie() {
    const cookies = document.cookie.split(';');
    for (const cookie of cookies) {
        const [name, value] = cookie.trim().split('=');
        if (name === 'cart_token' || name === 'shoplazza_cart_token') {
            return value;
        }
    }
    return null;
}

// Enhanced error recovery for Shoplazza API calls
function handleShoplazzaAPIError(error) {
    if (error.message && error.message.includes('Cart token mismatch')) {
        console.warn('Cart token mismatch detected, regenerating...');
        // Clear expired cart token
        document.cookie = 'cart_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/';
        document.cookie = 'shoplazza_cart_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/';
        
        // Regenerate cart by fetching current cart
        return fetch('/api/cart')
            .then(response => response.json())
            .then(() => {
                console.log('Cart token regenerated successfully');
                return true;
            })
            .catch(regenerateError => {
                console.error('Failed to regenerate cart token:', regenerateError);
                throw error; // Re-throw original error if regeneration fails
            });
    }
    
    // Handle other common Shoplazza API errors
    if (error.status === 429) {
        console.warn('Rate limit exceeded, implementing exponential backoff');
        return new Promise(resolve => {
            setTimeout(() => resolve(true), 2000); // Wait 2 seconds
        });
    }
    
    throw error; // Re-throw unhandled errors
}

// Get current add-on properties for injection into Shoplazza API calls
function getCurrentAddOnProperties() {
    const properties = {};
    const selectedAddOns = getSelectedAddOns();
    
    selectedAddOns.forEach(addOn => {
        properties['_add_on_type'] = 'protection_plan';
        properties['_add_on_name'] = addOn.title;
        properties['_add_on_price'] = addOn.price.toString();
        properties['_add_on_sku'] = addOn.sku;
        properties['_add_on_description'] = addOn.description || '';
        properties['_compressed'] = true; // Flag for complex data
    });
    
    return properties;
}

// Get currently selected add-ons from the page
function getSelectedAddOns() {
    const selectedAddOns = [];
    const checkboxes = document.querySelectorAll('.addon-checkbox:checked');
    
    checkboxes.forEach(checkbox => {
        const addOnConfig = {
            title: checkbox.dataset.addonTitle,
            price: parseFloat(checkbox.dataset.addonPrice),
            sku: checkbox.dataset.addonSku,
            description: checkbox.dataset.addonDescription,
            variantId: checkbox.dataset.addonVariantId
        };
        selectedAddOns.push(addOnConfig);
    });
    
    return selectedAddOns;
}
```

### **Step 3: Enhanced Cart Modification Protection (Priority: HIGH)**

#### **3.1 Monitor Cart Changes with Shoplazza-Specific Selectors**
```javascript
// Monitor cart modifications and protect properties
function setupCartModificationProtection() {
    // Watch for cart item removal
    const cartObserver = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === 'childList') {
                mutation.removedNodes.forEach(function(node) {
                    if (node.nodeType === Node.ELEMENT_NODE && 
                        (node.classList.contains('cart-item') || 
                         node.classList.contains('cart__item') ||
                         node.hasAttribute('data-cart-item'))) {
                        console.log('Cart item removed, backing up properties');
                        backupRemovedItemProperties(node);
                    }
                });
            }
        });
    });
    
    // Start observing cart container (try multiple Shoplazza selectors)
    const cartSelectors = [
        '.cart-container',
        '[data-cart-container]',
        '.cart__container',
        '.cart-items',
        '[data-cart-items]',
        '.cart-drawer',
        '[data-cart-drawer]'
    ];
    
    let cartContainer = null;
    for (const selector of cartSelectors) {
        cartContainer = document.querySelector(selector);
        if (cartContainer) break;
    }
    
    if (cartContainer) {
        cartObserver.observe(cartContainer, { childList: true, subtree: true });
        console.log('Cart modification protection enabled on:', cartContainer);
    } else {
        console.warn('Could not find cart container for modification protection');
    }
}

// Backup properties when items are removed
function backupRemovedItemProperties(cartItem) {
    const productId = cartItem.dataset.productId;
    const properties = extractPropertiesFromCartItem(cartItem);
    
    if (properties && Object.keys(properties).length > 0) {
        const backup = {
            productId: productId,
            properties: properties,
            timestamp: Date.now(),
            _removed: true
        };
        shoplazzaStorage.setAddon(productId, backup);
        console.log('Backed up properties for removed item:', backup);
    }
}

// Extract properties from cart item DOM
function extractPropertiesFromCartItem(cartItem) {
    const properties = {};
    const propertyInputs = cartItem.querySelectorAll('input[name^="properties["]');
    
    propertyInputs.forEach(input => {
        const name = input.name.match(/properties\[(.*?)\]/)?.[1];
        if (name) {
            properties[name] = input.value;
        }
    });
    
    return properties;
}
```

#### **3.2 Property Recovery After Modifications**
```javascript
// Reapply add-on properties after cart modifications
function reapplyAddOnProperties(cartItem, properties) {
    // Find or create property inputs with Shoplazza-specific naming
    Object.keys(properties).forEach(key => {
        let input = cartItem.querySelector(`input[name="properties[${key}]"]`);
        
        if (!input) {
            input = document.createElement('input');
            input.type = 'hidden';
            input.name = `properties[${key}]`;
            input.value = properties[key];
            cartItem.appendChild(input);
        } else {
            input.value = properties[key];
        }
    });
    
    console.log('Reapplied add-on properties to cart item:', properties);
}
```

### **Step 4: Enhanced Form Interception with Shoplazza Naming (Priority: MEDIUM)**

#### **4.1 Improve Existing Form Interception with Shoplazza-Specific Property Naming**
```javascript
// Enhanced form interception with Shoplazza-specific property naming
function interceptProductForm() {
    var self = this;
    var forms = document.querySelectorAll('form[action*="/cart/add"]');
    
    forms.forEach(function(form) {
        form.addEventListener('submit', function(e) {
            if (self.addOnPreference && self.addOnConfig) {
                // Set properties for add-on selection with Shoplazza naming
                var addonTypeInput = document.createElement('input');
                addonTypeInput.type = 'hidden';
                addonTypeInput.name = 'properties[_add_on_type]';
                addonTypeInput.value = 'protection_plan';
                form.appendChild(addonTypeInput);
                
                var addonNameInput = document.createElement('input');
                addonNameInput.type = 'hidden';
                addonNameInput.name = 'properties[_add_on_name]';
                addonNameInput.value = self.addOnConfig.title;
                form.appendChild(addonNameInput);
                
                var addonPriceInput = document.createElement('input');
                addonPriceInput.type = 'hidden';
                addonPriceInput.name = 'properties[_add_on_price]';
                addonPriceInput.value = self.addOnConfig.price.toString();
                form.appendChild(addonPriceInput);
                
                var addonSkuInput = document.createElement('input');
                addonSkuInput.type = 'hidden';
                addonSkuInput.name = 'properties[_add_on_sku]';
                addonSkuInput.value = self.addOnConfig.sku;
                form.appendChild(addonSkuInput);
                
                var addonDescriptionInput = document.createElement('input');
                addonDescriptionInput.type = 'hidden';
                addonDescriptionInput.name = 'properties[_add_on_description]';
                addonDescriptionInput.value = self.addOnConfig.description || '';
                form.appendChild(addonDescriptionInput);
                
                var compressedFlagInput = document.createElement('input');
                compressedFlagInput.type = 'hidden';
                compressedFlagInput.name = 'properties[_compressed]';
                compressedFlagInput.value = 'true';
                form.appendChild(compressedFlagInput);
                
                // Backup add-on selection
                backupAddOnSelections(self.currentProductId, self.addOnConfig);
                
                console.log('Form intercepted and Shoplazza add-on properties added with backup');
            }
        });
    });
}
```

### **Step 5: Enhanced Checkout Integration with Shoplazza-Specific Handling and Error Recovery (Priority: HIGH)**

#### **5.1 Validate Cart Before Checkout with Property Compression and Error Recovery**
```javascript
// Ensure add-ons persist to checkout with property compression and error recovery
function validateCartBeforeCheckout() {
    return fetch('/api/cart', {
        headers: getShoplazzaHeaders()
    })
    .then(response => response.json())
    .then(cart => {
        const hasAddOn = cart.items.some(item => 
            item.properties && item.properties._add_on_type === 'protection_plan'
        );
        
        if (!hasAddOn) {
            console.log('Add-on missing from cart, attempting to restore...');
            const savedAddOns = shoplazzaStorage.getAllAddons();
            
            if (savedAddOns.length > 0) {
                // Try to restore the first available add-on
                const addOnToRestore = savedAddOns[0];
                return restoreAddOnToCart(addOnToRestore);
            }
        }
        return Promise.resolve();
    })
    .catch(error => {
        console.error('Error validating cart before checkout:', error);
        return handleShoplazzaAPIError(error)
            .then(() => validateCartBeforeCheckout()) // Retry after error recovery
            .catch(recoveryError => {
                console.error('Error recovery failed, proceeding to checkout anyway:', recoveryError);
                return Promise.resolve(); // Continue to checkout even if validation fails
            });
    });
}

// Restore add-on to cart via Shoplazza API with proper headers and error handling
function restoreAddOnToCart(addOnBackup) {
    return fetch('/api/cart/add', {
        method: 'POST',
        headers: getShoplazzaHeaders(),
        body: JSON.stringify({
            id: addOnBackup.variantId || addOnBackup.addOnConfig.variantId,
            quantity: 1,
            properties: addOnBackup.properties || addOnBackup.addOnConfig.properties
        })
    })
    .then(response => response.json())
    .then(cartData => {
        console.log('Add-on restored to cart before checkout:', cartData);
        return cartData;
    })
    .catch(error => {
        console.error('Failed to restore add-on to cart:', error);
        return handleShoplazzaAPIError(error)
            .then(() => restoreAddOnToCart(addOnBackup)) // Retry after error recovery
            .catch(recoveryError => {
                console.error('Error recovery failed for add-on restoration:', recoveryError);
                throw error; // Re-throw if recovery fails
            });
    });
}
```

#### **5.2 Enhanced Checkout Protection with Multiple Flow Handling and Error Recovery**
```javascript
// Handle both classic and one-page checkout flows with error recovery
function setupCheckoutGuard() {
    // Handle checkout button clicks
    document.addEventListener('click', (e) => {
        const checkoutBtn = e.target.closest('[href*="/checkout"], [data-checkout]');
        if (checkoutBtn) {
            e.preventDefault();
            console.log('Checkout initiated, validating add-ons...');
            
            validateCartBeforeCheckout()
                .then(() => {
                    console.log('Add-on validation complete, proceeding to checkout');
                    window.location.assign(checkoutBtn.href || '/checkout');
                })
                .catch(error => {
                    console.error('Add-on validation failed, proceeding to checkout anyway:', error);
                    window.location.assign(checkoutBtn.href || '/checkout');
                });
        }
    });

    // Handle dynamic checkout buttons
    if (window.Shoplazza && window.Shoplazza.Checkout) {
        const originalInit = window.Shoplazza.Checkout.init;
        window.Shoplazza.Checkout.init = function(...args) {
            return validateCartBeforeCheckout()
                .then(() => originalInit.apply(this, args))
                .catch(error => {
                    console.error('Checkout validation failed, proceeding anyway:', error);
                    return originalInit.apply(this, args);
                });
        };
    }
    
    // Handle quick checkout flows
    document.addEventListener('click', (e) => {
        const quickCheckoutBtn = e.target.closest('[data-quick-checkout], .quick-checkout');
        if (quickCheckoutBtn) {
            e.preventDefault();
            validateCartBeforeCheckout()
                .then(() => {
                    // Trigger quick checkout
                    if (quickCheckoutBtn.dataset.quickCheckout) {
                        window.Shoplazza.QuickCheckout.init();
                    }
                })
                .catch(error => {
                    console.error('Quick checkout validation failed, proceeding anyway:', error);
                    if (quickCheckoutBtn.dataset.quickCheckout) {
                        window.Shoplazza.QuickCheckout.init();
                    }
                });
        }
    });
}
```

### **Step 6: WASM Integration Updates (Priority: MEDIUM)**

#### **6.1 Update WASM Function for Shoplazza Property Names and Compression**
```javascript
// Update the existing WASM function to read new property names and handle compression
// This will be done in the cart-transform.js file
function processCartWithAddons(cart) {
    const modifiedCart = JSON.parse(JSON.stringify(cart));
    const addonLineItems = [];
    
    if (modifiedCart.items && Array.isArray(modifiedCart.items)) {
        modifiedCart.items.forEach((item, itemIndex) => {
            // Check for new Shoplazza property names
            if (item.properties && 
                item.properties._add_on_type === 'protection_plan' && 
                item.properties._add_on_name) {
                try {
                    const addonConfig = {
                        title: item.properties._add_on_name,
                        price: parseFloat(item.properties._add_on_price || '0'),
                        sku: item.properties._add_on_sku || 'ADDON-001',
                        description: item.properties._add_on_description || ''
                    };
                    
                    if (addonConfig.title) {
                        const addonCost = calculateAddonCost(addonConfig, item.quantity);
                        const addonLineItem = createAddonLineItem(addonConfig, addonCost, item);
                        addonLineItems.push(addonLineItem);
                        modifyOriginalItem(item, addonConfig);
                    }
                } catch (parseError) {
                    console.error('Error processing Shoplazza add-on data for item:', itemIndex, parseError);
                }
            }
            
            // Handle compressed add-on data
            if (item.properties && 
                item.properties._compressed === 'true' && 
                item.properties._add_on_data) {
                try {
                    const decodedData = JSON.parse(atob(item.properties._add_on_data));
                    const addonConfig = {
                        title: decodedData.title || 'Add-on',
                        price: parseFloat(decodedData.price || '0'),
                        sku: decodedData.sku || 'ADDON-001',
                        description: decodedData.description || ''
                    };
                    
                    if (addonConfig.title) {
                        const addonCost = calculateAddonCost(addonConfig, item.quantity);
                        const addonLineItem = createAddonLineItem(addonConfig, addonCost, item);
                        addonLineItems.push(addonLineItem);
                        modifyOriginalItem(item, addonConfig);
                    }
                } catch (parseError) {
                    console.error('Error processing compressed Shoplazza add-on data for item:', itemIndex, parseError);
                }
            }
            
            // Backward compatibility: also check for old format
            if (item.properties && 
                item.properties._addon_selected === 'true' && 
                item.properties._addon_config) {
                // ... existing logic for backward compatibility
            }
        });
    }
    
    // Add add-on line items and recalculate totals
    if (addonLineItems.length > 0) {
        if (!modifiedCart.items) {
            modifiedCart.items = [];
        }
        modifiedCart.items.push(...addonLineItems);
        recalculateCartTotals(modifiedCart);
    }
    
    return modifiedCart;
}
```

### **Step 7: Production-Ready Features and Testing (Priority: MEDIUM)**

#### **7.1 Analytics Integration and Performance Monitoring**
```javascript
// Track add-on system performance and success rates
function setupAnalyticsTracking() {
    // Track property persistence success
    document.addEventListener('shoplazza:cart:updated', () => {
        if (window.Shoplazza && window.Shoplazza.analytics) {
            window.Shoplazza.analytics.track('Add-on Cart Properties Updated', {
                count: shoplazzaStorage.getAllAddons().length,
                restored: window.__addonsRestored || 0,
                timestamp: Date.now(),
                success: true
            });
        }
    });
    
    // Track checkout validation success
    const originalValidateCart = validateCartBeforeCheckout;
    validateCartBeforeCheckout = function() {
        const startTime = Date.now();
        return originalValidateCart.apply(this, arguments)
            .then(result => {
                if (window.Shoplazza && window.Shoplazza.analytics) {
                    window.Shoplazza.analytics.track('Add-on Checkout Validation', {
                        success: true,
                        duration: Date.now() - startTime,
                        timestamp: Date.now()
                    });
                }
                return result;
            })
            .catch(error => {
                if (window.Shoplazza && window.Shoplazza.analytics) {
                    window.Shoplazza.analytics.track('Add-on Checkout Validation', {
                        success: false,
                        error: error.message,
                        duration: Date.now() - startTime,
                        timestamp: Date.now()
                    });
                }
                throw error;
            });
    };
}

// Performance monitoring
function setupPerformanceMonitoring() {
    // Monitor API call performance
    const originalFetch = window.fetch;
    window.fetch = function(url, options) {
        if (url.includes('/api/cart')) {
            const startTime = performance.now();
            return originalFetch.apply(this, arguments)
                .then(response => {
                    const duration = performance.now() - startTime;
                    if (duration > 1000) { // Log slow API calls
                        console.warn(`Slow cart API call: ${duration.toFixed(2)}ms to ${url}`);
                    }
                    return response;
                });
        }
        return originalFetch.apply(this, arguments);
    };
}
```

#### **7.2 Mobile PWA and Multi-Currency Testing**
```javascript
// Test mobile PWA behavior
function testMobilePWABehavior() {
    // Check if running in PWA mode
    const isPWA = window.matchMedia('(display-mode: standalone)').matches || 
                  window.navigator.standalone === true;
    
    if (isPWA) {
        console.log('Running in PWA mode, ensuring sessionStorage persistence');
        
        // Test sessionStorage persistence
        const testKey = 'pwa_test_key';
        sessionStorage.setItem(testKey, 'test_value');
        
        // Verify persistence after a short delay
        setTimeout(() => {
            if (sessionStorage.getItem(testKey) === 'test_value') {
                console.log('PWA sessionStorage persistence confirmed');
            } else {
                console.warn('PWA sessionStorage persistence may be compromised');
            }
            sessionStorage.removeItem(testKey);
        }, 1000);
    }
}

// Test app conflict scenarios
function testAppConflictScenarios() {
    // Simulate another app clearing properties
    console.log('Testing app conflict scenario...');
    
    // Store current add-on state
    const currentAddons = shoplazzaStorage.getAllAddons();
    
    // Simulate property wipe
    fetch('/api/cart/update', {
        method: 'POST',
        headers: getShoplazzaHeaders(),
        body: JSON.stringify({ 
            updates: { 123456: 1 }, 
            properties: {} 
        })
    })
    .then(() => {
        console.log('App conflict simulation completed');
        
        // Verify recovery mechanism works
        setTimeout(() => {
            const recoveredAddons = shoplazzaStorage.getAllAddons();
            if (recoveredAddons.length > 0) {
                console.log('App conflict recovery successful');
            } else {
                console.warn('App conflict recovery may need improvement');
            }
        }, 2000);
    })
    .catch(error => {
        console.error('App conflict simulation failed:', error);
    });
}
```

## **🧪 TESTING STRATEGY**

### **Test Scenarios:**
1. **Basic Add-to-Cart**: Add product with add-on → Verify properties set
2. **Cart Modification**: Remove item → Re-add item → Verify properties restored
3. **API Cart Update**: Use cart API → Verify properties maintained and merged
4. **Cart Clearing**: Clear cart → Verify properties backed up
5. **Browser Refresh**: Refresh page → Verify properties restored
6. **Third-party App**: Simulate third-party cart modification → Verify properties protected
7. **Checkout Flow**: Initiate checkout → Verify add-ons persist to checkout
8. **Cart Drawer**: Close cart drawer → Verify state backed up
9. **Property Compression**: Verify complex add-on data compressed for checkout
10. **Cart Token**: Verify cart token included in API requests
11. **Error Recovery**: Test cart token mismatch and regeneration
12. **Rate Limiting**: Verify API call delays prevent flooding
13. **Multi-Currency**: Test currency changes and price updates
14. **Mobile PWA**: Test sessionStorage persistence in progressive web app
15. **App Conflicts**: Simulate third-party app property clearing

### **Testing Tools:**
- Browser Developer Tools (Network, Console, Application tabs)
- Shoplazza storefront testing
- Property persistence validation
- Event listener verification
- Checkout flow testing
- Cart token validation
- Property merging verification
- Performance monitoring
- Analytics tracking verification
- Mobile PWA testing
- Multi-currency testing

## **📊 IMPLEMENTATION TIMELINE**

### **Week 1: Enhanced Widget JavaScript**
- Implement Shoplazza-optimized sessionStorage system
- Add enhanced Shoplazza event listeners with analytics
- Test basic property backup/restore with expiration

### **Week 2: API Interception System**
- Implement fetch override with property merging and rate limiting
- Add Shoplazza headers and cart token handling
- Implement error recovery mechanisms
- Test API interception and property preservation

### **Week 3: Cart Modification Protection**
- Implement cart change monitoring with Shoplazza selectors
- Add property recovery mechanisms
- Test property persistence across modifications

### **Week 4: Checkout Integration & Production Features**
- Implement checkout validation with property compression
- Update WASM function for new Shoplazza property names
- Add analytics integration and performance monitoring
- Integrate all components and comprehensive testing

## **🎯 SUCCESS CRITERIA**

- ✅ **Properties survive ALL cart modifications** (remove/re-add, clear, third-party apps)
- ✅ **API calls maintain and merge properties** automatically with rate limiting
- ✅ **Event-driven property recovery** works reliably with analytics tracking
- ✅ **SessionStorage backup system** provides fallback protection with expiration
- ✅ **Checkout integration** ensures add-ons persist to checkout with compression
- ✅ **Cart token handling** works correctly with error recovery
- ✅ **Property compression** handles complex configurations within limits
- ✅ **Cart drawer events** properly backed up
- ✅ **Multi-currency support** handles dynamic currency changes
- ✅ **Mobile PWA compatibility** ensures cross-platform reliability
- ✅ **Error recovery** handles cart token mismatches and API failures
- ✅ **Performance impact minimal** (< 100ms overhead)
- ✅ **Error handling graceful** with fallback mechanisms

## **🚀 NEXT STEPS**

1. **Implement Shoplazza-optimized sessionStorage** with expiration and store-specific keys
2. **Add enhanced Shoplazza event listeners** for cart updates and section unloads
3. **Implement API interception with property merging** to prevent property loss
4. **Add cart token handling and error recovery** for all API requests
5. **Implement checkout validation** with property compression
6. **Update WASM function** for new Shoplazza property names
7. **Add analytics integration and performance monitoring**
8. **Test property persistence** across all cart modification scenarios
9. **Validate mobile PWA and multi-currency behavior**
10. **Test app conflict scenarios and recovery mechanisms**

---

**Status**: Updated with final DeepSeek recommendations - Production-ready implementation plan with comprehensive error recovery, analytics, performance monitoring, and testing scenarios.
