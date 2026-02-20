/**
 * 🚀 SHOPLAZZA PROTOCOL ADAPTER LAYER
 * 
 * This file implements the protocol adapter pattern to make our existing
 * cart transform logic Shoplazza-compliant without rewriting business logic.
 * 
 * Based on official Shoplazza documentation:
 * - Input: https://www.shoplazza.dev/v2024.07/reference/function-input-and-output-rules
 * - Examples: https://www.shoplazza.dev/v2024.07/reference/function-execution-logic
 */

/**
 * STEP 1.1: INPUT ADAPTER (Shoplazza → Our Format)
 * 
 * Converts Shoplazza's input format to our internal cart format
 * 
 * Shoplazza sends:
 * {
 *   "cart": {
 *     "line_items": [
 *       {
 *         "product": {
 *           "product_id": "string",
 *           "variant_id": "string", 
 *           "price": "string",
 *           "product_title": "string",
 *           "metafields": [...]
 *         },
 *         "id": "string",
 *         "properties": "string (JSON)",
 *         "quantity": "string"
 *       }
 *     ]
 *   },
 *   "currency_settings": {
 *     "actual_rate": "string"
 *   }
 * }
 * 
 * We need:
 * {
 *   "items": [
 *     {
 *       "id": number,
 *       "variant_id": number,
 *       "product_id": number,
 *       "price": number (cents),
 *       "product_title": "string",
 *       "quantity": number,
 *       "properties": object,
 *       "metafields": [...]
 *     }
 *   ],
 *   "currency": "string",
 *   "currency_rate": number
 * }
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
 * 
 * Our system returns modified cart with add-on line items
 * Shoplazza expects:
 * {
 *   "operation": {
 *     "update": [
 *       {
 *         "id": "string",
 *         "price": {
 *           "adjustment_fixed_price": "string"
 *         }
 *       }
 *     ]
 *   }
 * }
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
        // Note: This function will be imported from our existing cart-transform.js
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

// Export functions for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        adaptShoplazzaInput,
        adaptShoplazzaOutput,
        processCartShoplazza
    };
}
