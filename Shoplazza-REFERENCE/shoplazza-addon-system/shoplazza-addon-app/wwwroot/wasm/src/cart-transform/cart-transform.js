/**
 * 🚀 SHOPLAZZA CART-TRANSFORM FUNCTION WITH PROTOCOL ADAPTER
 * 
 * This function automatically adjusts cart prices based on add-on selections.
 * It now includes a protocol adapter layer to make it Shoplazza-compliant.
 * 
 * Functionality:
 * - Detects Shoplazza input format and uses protocol adapter
 * - Scans cart items for _addon_config and _addon_selected properties
 * - Calculates add-on costs based on property data
 * - Adjusts cart totals by adding line items or modifying prices
 * - Handles multiple add-ons across multiple products
 * - Returns Shoplazza-compliant operation.update format
 */

// Protocol adapter functions (inlined for Javy WASM compatibility)
// These functions convert between Shoplazza format and our internal format

// Read cart data from stdin
const cartData = Javy.IO.readSync(0, 0);

try {
    // Parse the cart data
    const inputData = JSON.parse(cartData);
    
    // Detect if this is Shoplazza format or our legacy format
    const isShoplazzaFormat = inputData.cart && inputData.cart.line_items && inputData.currency_settings;
    
    let outputData;
    
    if (isShoplazzaFormat) {
        console.log('🔄 Detected Shoplazza input format, using protocol adapter...');
        
        // Use protocol adapter for Shoplazza format
        outputData = processCartShoplazza(inputData);
        
        console.log('✅ Shoplazza processing completed, output format:', JSON.stringify(outputData, null, 2));
    } else {
        console.log('🔄 Detected legacy input format, using existing logic...');
        
        // Use existing logic for legacy format (backward compatibility)
        outputData = processCartWithAddons(inputData);
        
        console.log('✅ Legacy processing completed');
    }
    
    // Output the result to stdout
    Javy.IO.writeSync(1, JSON.stringify(outputData));
    
} catch (error) {
    console.error('❌ Cart-transform function error:', error.message);
    
    // Return appropriate error format based on detected input type
    const inputData = JSON.parse(cartData);
    const isShoplazzaFormat = inputData.cart && inputData.cart.line_items && inputData.currency_settings;
    
    if (isShoplazzaFormat) {
        // Return Shoplazza-compliant error format
        const errorOutput = { operation: { update: [] } };
        Javy.IO.writeSync(1, JSON.stringify(errorOutput));
    } else {
        // Return original cart unchanged for legacy format
        Javy.IO.writeSync(1, cartData);
    }
}

/**
 * 🚀 PROTOCOL ADAPTER FUNCTIONS (INLINED FOR JAVY WASM)
 * 
 * These functions implement the protocol adapter pattern to make our existing
 * cart transform logic Shoplazza-compliant without rewriting business logic.
 */

/**
 * STEP 1.1: INPUT ADAPTER (Shoplazza → Our Format)
 * 
 * Converts Shoplazza's input format to our internal cart format
 */
function adaptShoplazzaInput(shoplazzaInput) {
    try {
        console.log('🔄 Adapting Shoplazza input to our format...');
        
        const { cart, currency_settings } = shoplazzaInput;
        
        if (!cart || !cart.line_items) {
            throw new Error('Invalid Shoplazza input: missing cart.line_items');
        }
        
        // Convert to our expected format, handling Shoplazza's exact field types
        const adaptedCart = {
            items: cart.line_items.map((item, index) => {
                console.log(`📦 Processing line item ${index}: ${item.id}`);
                
                // CRITICAL: properties is a JSON string, not an object
                let properties = {};
                try {
                    if (item.properties && typeof item.properties === 'string') {
                        properties = JSON.parse(item.properties || '{}');
                    } else if (item.properties && typeof item.properties === 'object') {
                        properties = item.properties;
                    }
                } catch (e) {
                    console.warn(`⚠️ Failed to parse properties JSON string for item ${item.id}:`, e);
                    properties = {};
                }
                
                // CRITICAL: quantity is a string, we need number
                let quantity = 1;
                try {
                    quantity = parseInt(item.quantity, 10);
                    if (isNaN(quantity) || quantity < 1) {
                        console.warn(`⚠️ Invalid quantity for item ${item.id}: ${item.quantity}, defaulting to 1`);
                        quantity = 1;
                    }
                } catch (e) {
                    console.warn(`⚠️ Failed to parse quantity for item ${item.id}: ${item.quantity}, defaulting to 1`);
                    quantity = 1;
                }
                
                // CRITICAL: price is a string, we need number in cents
                let price = 0;
                try {
                    const priceFloat = parseFloat(item.product.price);
                    if (isNaN(priceFloat) || priceFloat < 0) {
                        console.warn(`⚠️ Invalid price for item ${item.id}: ${item.product.price}, defaulting to 0`);
                        price = 0;
                    } else {
                        price = Math.round(priceFloat * 100); // Convert to cents
                    }
                } catch (e) {
                    console.warn(`⚠️ Failed to parse price for item ${item.id}: ${item.product.price}, defaulting to 0`);
                    price = 0;
                }
                
                // CRITICAL: IDs are strings, we need numbers for our system
                let itemId = 0;
                let variantId = 0;
                let productId = 0;
                
                try {
                    itemId = parseInt(item.id, 10);
                    if (isNaN(itemId)) {
                        itemId = index + 1; // Fallback to index-based ID
                        console.warn(`⚠️ Invalid item ID for item ${index}: ${item.id}, using fallback: ${itemId}`);
                    }
                } catch (e) {
                    itemId = index + 1;
                    console.warn(`⚠️ Failed to parse item ID for item ${index}: ${item.id}, using fallback: ${itemId}`);
                }
                
                try {
                    variantId = parseInt(item.product.variant_id, 10);
                    if (isNaN(variantId)) {
                        variantId = itemId; // Fallback to item ID
                        console.warn(`⚠️ Invalid variant ID for item ${index}: ${item.product.variant_id}, using fallback: ${variantId}`);
                    }
                } catch (e) {
                    variantId = itemId;
                    console.warn(`⚠️ Failed to parse variant ID for item ${index}: ${item.product.variant_id}, using fallback: ${variantId}`);
                }
                
                try {
                    productId = parseInt(item.product.product_id, 10);
                    if (isNaN(productId)) {
                        productId = itemId; // Fallback to item ID
                        console.warn(`⚠️ Invalid product ID for item ${index}: ${item.product.product_id}, using fallback: ${productId}`);
                    }
                } catch (e) {
                    productId = itemId;
                    console.warn(`⚠️ Failed to parse product ID for item ${index}: ${item.product.product_id}, using fallback: ${productId}`);
                }
                
                const adaptedItem = {
                    // Core fields (converted to our format)
                    id: itemId,
                    variant_id: variantId,
                    product_id: productId,
                    price: price,
                    product_title: item.product.product_title || 'Unknown Product',
                    quantity: quantity,
                    
                    // CRITICAL: properties as object (parsed from JSON string)
                    properties: properties,
                    
                    // Metafields (preserved as-is)
                    metafields: item.product.metafields || [],
                    
                    // Additional fields our system expects
                    key: `variant_${variantId}`,
                    title: item.product.product_title || 'Unknown Product',
                    original_price: price,
                    discounted_price: price,
                    line_price: price * quantity,
                    original_line_price: price * quantity,
                    final_price: price,
                    final_line_price: price * quantity,
                    
                    // Default values for required fields
                    sku: '',
                    grams: 0,
                    vendor: '',
                    taxable: true,
                    product_description: '',
                    product_type: '',
                    product_has_only_default_variant: true,
                    gift_card: false,
                    requires_shipping: true,
                    url: '',
                    image: '',
                    handle: '',
                    options_with_values: [],
                    line_level_discount_allocations: [],
                    line_level_total_discount: 0,
                    total_discount: 0,
                    discounts: []
                };
                
                console.log(`✅ Adapted item ${index}: ID=${adaptedItem.id}, Price=${adaptedItem.price} cents, Qty=${adaptedItem.quantity}`);
                return adaptedItem;
            }),
            
            // Handle currency settings (required by Shoplazza)
            currency: currency_settings?.actual_rate || '1',
            currency_rate: parseFloat(currency_settings?.actual_rate || '1')
        };
        
        console.log(`✅ Successfully adapted ${adaptedCart.items.length} items from Shoplazza format`);
        return adaptedCart;
        
    } catch (error) {
        console.error('❌ Error adapting Shoplazza input:', error);
        throw new Error(`Input adaptation failed: ${error.message}`);
    }
}

/**
 * STEP 1.2: OUTPUT ADAPTER (Our Format → Shoplazza)
 * 
 * Converts our output to Shoplazza's expected operation.update format
 */
function adaptShoplazzaOutput(ourOutput, originalInput) {
    try {
        console.log('🔄 Adapting our output to Shoplazza format...');
        
        const { cart } = originalInput;
        
        if (!ourOutput.items || !Array.isArray(ourOutput.items)) {
            console.warn('⚠️ Our output has no items array, returning empty operation');
            return { operation: { update: [] } };
        }
        
        // Extract price adjustments from our modified cart
        // Shoplazza expects: operation.update[].price.adjustment_fixed_price
        const priceUpdates = [];
        
        ourOutput.items.forEach((item, index) => {
            // Find corresponding original item
            const originalItem = cart.line_items[index];
            if (!originalItem) {
                console.warn(`⚠️ No original item found for index ${index}, skipping`);
                return;
            }
            
            // Calculate price difference: our price - original price
            const originalPriceCents = parseFloat(originalItem.product.price) * 100;
            const ourPriceCents = item.price || 0;
            const adjustment = (ourPriceCents - originalPriceCents) / 100;
            
            // Only add if there's a price change (Shoplazza requirement)
            if (Math.abs(adjustment) > 0.01) { // Allow for small floating point differences
                // Validate adjustment range [0, 999999999] as per Shoplazza docs
                const clampedAdjustment = Math.max(0, Math.min(999999999, adjustment));
                
                const priceUpdate = {
                    id: originalItem.id, // string (required)
                    price: {
                        adjustment_fixed_price: clampedAdjustment.toFixed(2) // string (required)
                    }
                };
                
                priceUpdates.push(priceUpdate);
                console.log(`💰 Price update for item ${originalItem.id}: adjustment=${clampedAdjustment.toFixed(2)}`);
            }
        });
        
        // Return Shoplazza-compliant output structure
        const shoplazzaOutput = {
            operation: {
                update: priceUpdates
            }
        };
        
        console.log(`✅ Successfully adapted output: ${priceUpdates.length} price updates`);
        return shoplazzaOutput;
        
    } catch (error) {
        console.error('❌ Error adapting output to Shoplazza format:', error);
        // Return empty operation on error (Shoplazza best practice)
        return { operation: { update: [] } };
    }
}

/**
 * STEP 1.3: MAIN FUNCTION WRAPPER
 * 
 * Orchestrates the adapter layer workflow:
 * 1. Adapt Shoplazza input to our format
 * 2. Call our existing business logic
 * 3. Adapt our output to Shoplazza format
 */
function processCartShoplazza(shoplazzaInput) {
    try {
        console.log('🚀 Starting Shoplazza cart processing...');
        
        // Step 1: Adapt Shoplazza input to our format
        const adaptedInput = adaptShoplazzaInput(shoplazzaInput);
        
        // Step 2: Call our existing business logic
        const adaptedOutput = processCartWithAddons(adaptedInput);
        
        // Step 3: Adapt our output to Shoplazza format
        const shoplazzaOutput = adaptShoplazzaOutput(adaptedOutput, shoplazzaInput);
        
        console.log('✅ Shoplazza cart processing completed successfully');
        return shoplazzaOutput;
        
    } catch (error) {
        console.error('❌ Cart transform error:', error);
        // Return empty operation on error (Shoplazza best practice)
        return { operation: { update: [] } };
    }
}

/**
 * Main function to process cart and apply add-on pricing
 * @param {Object} cart - The cart data from Shoplazza
 * @returns {Object} - Modified cart with add-on pricing applied
 */
function processCartWithAddons(cart) {
    // Create a copy of the cart to modify
    const modifiedCart = JSON.parse(JSON.stringify(cart));
    
    // Track add-on line items to add
    const addonLineItems = [];
    
    // Process each cart item
    if (modifiedCart.items && Array.isArray(modifiedCart.items)) {
        modifiedCart.items.forEach((item, itemIndex) => {
            // Skip if this item already has an add-on processed
            if (item.properties && item.properties._add_on_processed === 'true') {
                console.log(`Item ${itemIndex} already has add-on processed, skipping`);
                return; // Use return instead of continue in forEach
            }
            
            let addonProcessed = false;
            
                            // Check for new Shoplazza property names (Phase 2)
                if (!addonProcessed && item.properties && 
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
                            addonProcessed = true;
                            console.log(`Processed Phase 2 add-on for item ${itemIndex}: ${addonConfig.title}`);
                        }
                    } catch (parseError) {
                        console.error('Error processing Shoplazza add-on data for item:', itemIndex, parseError);
                    }
                }
                
                // Handle compressed add-on data (Phase 2)
                if (!addonProcessed && item.properties && 
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
                            addonProcessed = true;
                            console.log(`Processed compressed add-on for item ${itemIndex}: ${addonConfig.title}`);
                        }
                    } catch (parseError) {
                        console.error('Error processing compressed Shoplazza add-on data for item:', itemIndex, parseError);
                    }
                }
                
                // Check if this item has add-on properties (widget sends these) - Phase 1 compatibility
                if (!addonProcessed && item.properties && 
                    item.properties._addon_selected === 'true' && 
                    item.properties._addon_config) {
                    try {
                        const addonConfig = JSON.parse(item.properties._addon_config);
                        
                        if (addonConfig && addonConfig.title) {
                            // Calculate add-on cost for this item
                            const addonCost = calculateAddonCost(addonConfig, item.quantity);
                            
                            // Create add-on line item
                            const addonLineItem = createAddonLineItem(addonConfig, addonCost, item);
                            
                            // Add to our list of add-ons to add
                            addonLineItems.push(addonLineItem);
                            
                            // Mark the original item as having an add-on
                            modifyOriginalItem(item, addonConfig);
                            addonProcessed = true;
                            console.log(`Processed Phase 1 add-on for item ${itemIndex}: ${addonConfig.title}`);
                        }
                    } catch (parseError) {
                        console.error('Error parsing add-on config for item:', itemIndex, parseError);
                    }
                }
                
                // Backward compatibility: also check for old cdh_shoplazza_addon format
                if (!addonProcessed && item.properties && item.properties.cdh_shoplazza_addon) {
                    try {
                        const addonData = JSON.parse(item.properties.cdh_shoplazza_addon);
                        
                        if (addonData.isSelected && addonData.config) {
                            // Calculate add-on cost for this item
                            const addonCost = calculateAddonCost(addonData.config, item.quantity);
                            
                            // Create add-on line item
                            const addonLineItem = createAddonLineItem(addonData.config, addonCost, item);
                            
                            // Add to our list of add-ons to add
                            addonLineItems.push(addonLineItem);
                            
                            // Mark the original item as having an add-on
                            modifyOriginalItem(item, addonData.config);
                            addonProcessed = true;
                            console.log(`Processed legacy add-on for item ${itemIndex}: ${addonData.config.title}`);
                        }
                    } catch (parseError) {
                        console.error('Error parsing legacy add-on data for item:', itemIndex, parseError);
                    }
                }
                
                if (!addonProcessed) {
                    console.log(`Item ${itemIndex} has no add-on properties to process`);
                }
            });
        }
    
    // Add all add-on line items to the cart
    if (addonLineItems.length > 0) {
        if (!modifiedCart.items) {
            modifiedCart.items = [];
        }
        modifiedCart.items.push(...addonLineItems);
        
        // Recalculate cart totals
        recalculateCartTotals(modifiedCart);
    }
    
    return modifiedCart;
}

/**
 * Calculate the add-on cost for a given quantity
 * @param {Object} addonConfig - Add-on configuration data
 * @param {number} quantity - Quantity of the main product
 * @returns {number} - Total add-on cost in cents
 */
function calculateAddonCost(addonConfig, quantity) {
    const basePrice = addonConfig.price || 0;
    const priceInCents = Math.round(basePrice * 100); // Convert to cents if needed
    
    // Apply quantity multiplier
    return priceInCents * quantity;
}

/**
 * Create a new line item for the add-on
 * @param {Object} addonConfig - Add-on configuration
 * @param {number} addonCost - Calculated add-on cost in cents
 * @param {Object} originalItem - Original cart item
 * @returns {Object} - Add-on line item
 */
function createAddonLineItem(addonConfig, addonCost, originalItem) {
    const addonItem = {
        id: generateAddonId(originalItem.id),
        variant_id: generateAddonVariantId(originalItem.variant_id),
        key: `addon_${originalItem.key}_${Date.now()}`,
        title: addonConfig.title || 'Add-on',
        price: addonCost,
        original_price: addonCost,
        discounted_price: addonCost,
        line_price: addonCost,
        original_line_price: addonCost,
        final_price: addonCost,
        final_line_price: addonCost,
        quantity: originalItem.quantity,
        sku: addonConfig.sku || 'ADDON-001',
        grams: addonConfig.weightGrams || 0,
        vendor: 'Add-on System',
        taxable: addonConfig.isTaxable !== false,
        product_id: generateAddonProductId(originalItem.product_id),
        product_title: addonConfig.title || 'Add-on',
        product_description: addonConfig.description || '',
        product_type: 'add-on',
        product_has_only_default_variant: true,
        gift_card: false,
        requires_shipping: addonConfig.requiresShipping !== false,
        url: '',
        image: addonConfig.imageUrl || '',
        handle: `addon-${originalItem.handle}`,
        properties: {
            _addon_type: 'shoplazza_addon',
            _addon_for_item: originalItem.key,
            _addon_config: JSON.stringify(addonConfig),
            _addon_source: 'widget_selection'
        },
        options_with_values: [],
        line_level_discount_allocations: [],
        line_level_total_discount: 0,
        total_discount: 0,
        discounts: []
    };
    
    return addonItem;
}

/**
 * Modify the original item to reference the add-on
 * @param {Object} item - Cart item to modify
 * @param {Object} addonConfig - Add-on configuration
 */
function modifyOriginalItem(item, addonConfig) {
    // Add reference to the add-on in the original item
    if (!item.properties) {
        item.properties = {};
    }
    
    // Phase 2: New Shoplazza property names
    item.properties._add_on_applied = 'true';
    item.properties._add_on_processed = 'true';
    item.properties._add_on_title = addonConfig.title || 'Add-on';
    
    // Phase 1: Backward compatibility
    item.properties._has_addon = 'true';
    item.properties._addon_applied = 'true';
    item.properties._addon_title = addonConfig.title || 'Add-on';
    item.properties._addon_processed = 'true';
}

/**
 * Recalculate cart totals after adding add-ons
 * @param {Object} cart - Cart to recalculate
 */
function recalculateCartTotals(cart) {
    if (!cart.items || !Array.isArray(cart.items)) {
        return;
    }
    
    let itemsSubtotal = 0;
    let totalPrice = 0;
    let itemCount = 0;
    
    cart.items.forEach(item => {
        const itemTotal = item.final_line_price || (item.price * item.quantity);
        itemsSubtotal += itemTotal;
        totalPrice += itemTotal;
        itemCount += 1; // Count line items, not quantity
    });
    
    // Update cart totals
    cart.items_subtotal_price = itemsSubtotal;
    cart.total_price = totalPrice;
    cart.item_count = itemCount;
    
    // Preserve original total for discount calculations
    if (!cart.original_total_price) {
        cart.original_total_price = cart.total_price;
    }
}

/**
 * Generate a unique ID for add-on items
 * @param {string|number} originalId - Original item ID
 * @returns {string} - Unique add-on ID
 */
function generateAddonId(originalId) {
    return `addon_${originalId}_${Date.now()}`;
}

/**
 * Generate a unique variant ID for add-on items
 * @param {string|number} originalVariantId - Original variant ID
 * @returns {string} - Unique add-on variant ID
 */
function generateAddonVariantId(originalVariantId) {
    return `addon_variant_${originalVariantId}_${Date.now()}`;
}

/**
 * Generate a unique product ID for add-on items
 * @param {string|number} originalProductId - Original product ID
 * @returns {string} - Unique add-on product ID
 */
function generateAddonProductId(originalProductId) {
    return `addon_product_${originalProductId}_${Date.now()}`;
}
