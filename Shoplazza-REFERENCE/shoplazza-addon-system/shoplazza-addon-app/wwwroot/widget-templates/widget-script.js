(function() {
    'use strict';
    
    var WIDGET_CONFIG = {
        shop: '[[SHOP_DOMAIN]]',
        apiEndpoint: '[[API_ENDPOINT]]',
        debug: false
    };
    
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
                if (key && key.startsWith('sl_addon_')) {
                    const data = sessionStorage.getItem(key);
                    if (data) {
                        try {
                            const parsed = JSON.parse(data);
                            if (parsed._expires > Date.now()) {
                                addons.push(parsed);
                            } else {
                                sessionStorage.removeItem(key); // Clean expired
                            }
                        } catch (e) {
                            console.warn('Invalid add-on data in storage:', key);
                        }
                    }
                }
            }
            return addons;
        }
    };
    
    // Main widget functionality
    var ShoplazzaAddonWidget = {
        currentProductId: null,
        addOnConfig: null,
        isInitialized: false,
        
        init: function() {
            if (this.isInitialized) {
                console.log('Widget already initialized');
                return;
            }
            
            console.log('Initializing Shoplazza Add-On Widget for shop:', WIDGET_CONFIG.shop);
            
            // Detect current product
            this.detectCurrentProduct();
            
            // Load add-on configuration
            this.loadAddOnConfig();
            
            this.isInitialized = true;
        },
        
        detectCurrentProduct: function() {
            // Enhanced product detection for Shoplazza
            var variantInput = document.querySelector('form[action*="/cart/add"] input[name="id"]');
            if (variantInput && variantInput.value) {
                this.currentProductId = variantInput.value;
            }
            
            // Also try to detect from URL or other Shoplazza-specific selectors
            if (!this.currentProductId) {
                const productIdMatch = window.location.pathname.match(/\/products\/([^\/]+)/);
                if (productIdMatch) {
                    this.currentProductId = productIdMatch[1];
                }
            }
            
            console.log('Detected product handle:', this.currentProductId);
        },
        
        loadAddOnConfig: function() {
            // First resolve the product handle to get the real product ID
            this.resolveProductId();
        },
        
        resolveProductId: function() {
            var callbackName = 'shoplazzaAddOnResolveCallback_' + Date.now();
            window[callbackName] = function(response) {
                if (response.productId) {
                    console.log('Resolved product ID:', response.productId, 'for handle:', ShoplazzaAddonWidget.currentProductId);
                    // Now load the add-on config with the real product ID
                    ShoplazzaAddonWidget.loadAddOnConfigWithId(response.productId);
                } else {
                    console.warn('Could not resolve product ID for handle:', ShoplazzaAddonWidget.currentProductId);
                }
                // Clean up callback
                delete window[callbackName];
            };
            
            var script = document.createElement('script');
            script.src = WIDGET_CONFIG.apiEndpoint + '/api/widget/resolve?shop=' + 
                        encodeURIComponent(WIDGET_CONFIG.shop) + 
                        '&handle=' + this.currentProductId + 
                        '&callback=' + callbackName;
            document.head.appendChild(script);
        },
        
        loadAddOnConfigWithId: function(realProductId) {
            // Load configuration via JSONP with the real product ID
            var callbackName = 'shoplazzaAddOnCallback_' + Date.now();
            window[callbackName] = function(config) {
                if (config.success && config.hasAddOn && config.addOn.isActive) {
                    ShoplazzaAddonWidget.addOnConfig = config.addOn;
                    ShoplazzaAddonWidget.renderWidget();
                } else {
                    console.log('No active add-on found for product:', realProductId);
                }
                // Clean up callback
                delete window[callbackName];
            };
            
            var script = document.createElement('script');
            script.src = WIDGET_CONFIG.apiEndpoint + '/api/widget/config?shop=' + 
                        encodeURIComponent(WIDGET_CONFIG.shop) + 
                        '&productId=' + realProductId + 
                        '&callback=' + callbackName;
            document.head.appendChild(script);
        },
        
        renderWidget: function() {
            // Load widget HTML from separate endpoint
            this.loadWidgetHtml();
        },
        
        loadWidgetHtml: function() {
            var xhr = new XMLHttpRequest();
            xhr.open('GET', WIDGET_CONFIG.apiEndpoint + '/api/widget/widget.html?shop=' + 
                    encodeURIComponent(WIDGET_CONFIG.shop), true);
            
            xhr.onload = function() {
                if (xhr.status === 200) {
                    // Process template variables with actual add-on data
                    var processedHtml = ShoplazzaAddonWidget.processTemplate(xhr.responseText);
                    
                    // Find the best place to inject the widget
                    var injectionPoint = ShoplazzaAddonWidget.findInjectionPoint();
                    if (injectionPoint) {
                        injectionPoint.insertAdjacentHTML('beforeend', processedHtml);
                        ShoplazzaAddonWidget.initializeWidget();
                    }
                }
            };
            
            xhr.send();
        },
        
        processTemplate: function(htmlTemplate) {
            if (!this.addOnConfig) {
                console.warn('No add-on config available for template processing');
                return htmlTemplate;
            }
            
            // Note: HTML template placeholders are already processed by the controller
            // No need to replace them again here
            console.log('Template already processed by controller, no additional processing needed');
            return htmlTemplate;
        },
        
        findInjectionPoint: function() {
            // Try to find the best place to inject the widget
            var selectors = [
                'form[action*="/cart/add"]', // Cart form
                '.product-form', // Product form
                '.product-info', // Product info section
                '.product-details', // Product details
                '[data-product-id]', // Any element with product ID
                '.product' // Generic product class
            ];
            
            for (var i = 0; i < selectors.length; i++) {
                var element = document.querySelector(selectors[i]);
                if (element) {
                    return element;
                }
            }
            
            // Fallback: inject before the first form
            return document.querySelector('form');
        },
        
        initializeWidget: function() {
            // Add event listeners to the injected widget
            this.addEventListeners();
            
            // Restore any saved preferences
            this.restorePreference();
        },
        
        addEventListeners: function() {
            var widget = document.getElementById('shoplazza-addon-widget');
            if (!widget) return;
            
            var checkbox = widget.querySelector('.addon-checkbox');
            if (checkbox) {
                checkbox.addEventListener('change', function(e) {
                    ShoplazzaAddonWidget.handleAddOnToggle(e.target.checked);
                });
            }
            
            // Note: Form interception removed - now using fetch interception for /api/cart calls
        },
        
        handleAddOnToggle: function(isChecked) {
            if (isChecked) {
                this.setAddOnSelected(true);
            } else {
                this.setAddOnSelected(false);
            }
        },
        
        setAddOnSelected: function(selected) {
            if (!this.addOnConfig) return;
            
            var data = {
                productId: this.currentProductId,
                addOnId: this.addOnConfig.id,
                selected: selected,
                title: this.addOnConfig.title,
                price: this.addOnConfig.price,
                sku: this.addOnConfig.sku
            };
            
            // Save to storage
            shoplazzaStorage.setAddon(this.currentProductId, data);
            
            // Note: Cart form update removed - now using fetch interception
            
            // Set metafield on the product
            this.updateProductMetafield(selected);
            
            console.log('Add-on selection updated:', data);
        },
        
        updateProductMetafield: function(selected) {
            // This will be handled by the cart-transform function
            // We just need to set the metafield
            var metafieldData = {
                namespace: 'cdh_shoplazza_addon',
                key: 'addon_selected',
                value: selected.toString(),
                value_type: 'boolean'
            };
            
            // Store in session storage for cart-transform function to read
            sessionStorage.setItem('shoplazza_addon_metafield', JSON.stringify(metafieldData));
        },
        
        restorePreference: function() {
            var saved = shoplazzaStorage.getAddon(this.currentProductId);
            if (saved && saved.selected) {
                var checkbox = document.querySelector('#shoplazza-addon-widget .addon-checkbox');
                if (checkbox) {
                    checkbox.checked = true;
                }
            }
        },

        getCurrentAddOnSelection: function() {
            const saved = shoplazzaStorage.getAddon(this.currentProductId);
            if (saved) {
                return saved;
            }
            return null;
        }
    };
    
    // Initialize widget when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            ShoplazzaAddonWidget.init();
        });
    } else {
        ShoplazzaAddonWidget.init();
    }
    
    // Intercept fetch calls to /api/cart to inject add-on properties
    const originalFetch = window.fetch;
    window.fetch = function(url, options) {
        // Check if this is a cart API call
        if (typeof url === 'string' && url.includes('/api/cart') && options && options.method === 'POST') {
            console.log('Intercepting cart API call:', url);
            
            // Clone the options to avoid modifying the original
            const modifiedOptions = { ...options };
            
            // Parse and modify the body if it exists
            if (modifiedOptions.body) {
                try {
                    const body = JSON.parse(modifiedOptions.body);
                    
                    // Check if this is adding a product to cart
                    if (body.id || body.product_id) {
                        console.log('Cart add request intercepted, checking for add-on selection');
                        
                        // Get current add-on selection from the widget
                        const addOnSelection = ShoplazzaAddonWidget.getCurrentAddOnSelection();
                        
                        if (addOnSelection && addOnSelection.selected) {
                            console.log('Adding add-on properties to cart request:', addOnSelection);
                            
                            // Initialize properties if they don't exist
                            if (!body.properties) {
                                body.properties = {};
                            }
                            
                            // Add add-on properties as a raw object (will be stringified by the final JSON.stringify)
                            body.properties['addon'] = {
                                type: 'protection_plan',
                                name: addOnSelection.title,
                                price: addOnSelection.price,
                                sku: addOnSelection.sku,
                                description: addOnSelection.description || '',
                                variantId: addOnSelection.variantId,
                                compressed: true
                            };
                            
                            // Add-on properties are now clean and properly formatted
                            // No more duplicate backward compatibility properties
                            
                            // Update the body
                            modifiedOptions.body = JSON.stringify(body);
                            console.log('Modified cart request body:', body);
                        } else {
                            console.log('No add-on selected, leaving cart request unchanged');
                        }
                    }
                } catch (e) {
                    console.warn('Could not parse cart request body:', e);
                }
            }
            
            // Call the original fetch with modified options
            return originalFetch(url, modifiedOptions);
        }
        
        // For all other requests, use the original fetch
        return originalFetch.apply(this, arguments);
    };
    
    console.log('Fetch interception for /api/cart enabled');
    
    // Make widget globally accessible
    window.ShoplazzaAddonWidget = ShoplazzaAddonWidget;
    
    console.log('Shoplazza Add-On Widget script loaded for shop:', WIDGET_CONFIG.shop);
    
    // Inject the widget HTML and CSS into the page
    (function() {
        // Add CSS styles
        var styleElement = document.createElement('style');
        styleElement.textContent = `[[WIDGET_STYLES]]`;
        document.head.appendChild(styleElement);
        
        // Add widget HTML
        var widgetContainer = document.createElement('div');
        widgetContainer.innerHTML = `[[WIDGET_HTML]]`;
        document.body.appendChild(widgetContainer);
        
        // Initialize the widget
        if (ShoplazzaAddonWidget && typeof ShoplazzaAddonWidget.init === 'function') {
            ShoplazzaAddonWidget.init();
        }
    })();
})();
