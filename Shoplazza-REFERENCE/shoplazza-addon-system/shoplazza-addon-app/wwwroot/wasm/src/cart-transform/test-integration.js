/**
 * Test script to verify WASM function integration with widget properties
 * This simulates the data flow from widget to WASM function
 */

// Simulate the cart data that would come from Shoplazza
const testCart = {
  "items": [
    {
      "id": "12345",
      "variant_id": "67890",
      "key": "item_12345",
      "title": "Test Product",
      "price": 2500,
      "quantity": 1,
      "properties": {
        "_addon_selected": "true",
        "_addon_config": "{\"title\":\"Premium Protection\",\"price\":1.50,\"sku\":\"PROTECTION-001\",\"description\":\"Protect your purchase\",\"isTaxable\":true,\"requiresShipping\":false}",
        "_add_on_type": "protection_plan",
        "_add_on_name": "Premium Protection",
        "_add_on_price": "1.50",
        "_add_on_sku": "PROTECTION-001",
        "_add_on_description": "Protect your purchase",
        "_compressed": "true",
        "_add_on_data": "eyJ0aXRsZSI6IlByZW1pdW0gUHJvdGVjdGlvbiIsInByaWNlIjoxLjUwLCJza3UiOiJQUk9URUNUSU9OLTAwMSIsImRlc2NyaXB0aW9uIjoiUHJvdGVjdCB5b3VyIHB1cmNoYXNlIiwiaXNUYXhhYmxlIjp0cnVlLCJyZXF1aXJlc1NoaXBwaW5nIjpmYWxzZX0="
      },
      "product_id": "11111",
      "handle": "test-product"
    }
  ],
  "items_subtotal_price": 2500,
  "total_price": 2500,
  "item_count": 1
};

// Simulate the WASM function processing (without Javy.IO)
function processCartWithAddons(cart) {
    const modifiedCart = JSON.parse(JSON.stringify(cart));
    const addonLineItems = [];
    
    if (modifiedCart.items && Array.isArray(modifiedCart.items)) {
        modifiedCart.items.forEach((item, itemIndex) => {
            // Check for new Shoplazza property names (Phase 2)
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
                    console.log('✅ Found Phase 2 add-on config:', addonConfig);
                    
                    if (addonConfig.title) {
                        // Calculate add-on cost (price in dollars, convert to cents)
                        const addonCost = Math.round(addonConfig.price * 100) * item.quantity;
                        console.log('✅ Calculated add-on cost:', addonCost, 'cents');
                        
                        // Create add-on line item
                        const addonLineItem = createAddonLineItem(addonConfig, addonCost, item);
                        addonLineItems.push(addonLineItem);
                        
                        // Mark original item
                        modifyOriginalItem(item, addonConfig);
                        console.log('✅ Modified original item properties:', item.properties);
                    }
                } catch (parseError) {
                    console.error('❌ Error processing Phase 2 add-on data:', parseError);
                }
            }
            
            // Handle compressed add-on data (Phase 2)
            else if (item.properties && 
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
                    console.log('✅ Found compressed add-on config:', addonConfig);
                    
                    if (addonConfig.title) {
                        // Calculate add-on cost (price in dollars, convert to cents)
                        const addonCost = Math.round(addonConfig.price * 100) * item.quantity;
                        console.log('✅ Calculated add-on cost:', addonCost, 'cents');
                        
                        // Create add-on line item
                        const addonLineItem = createAddonLineItem(addonConfig, addonCost, item);
                        addonLineItems.push(addonLineItem);
                        
                        // Mark original item
                        modifyOriginalItem(item, addonConfig);
                        console.log('✅ Modified original item properties:', item.properties);
                    }
                } catch (parseError) {
                    console.error('❌ Error processing compressed add-on data:', parseError);
                }
            }
            
            // Check for old widget properties (Phase 1 compatibility)
            else if (item.properties && 
                item.properties._addon_selected === 'true' && 
                item.properties._addon_config) {
                try {
                    const addonConfig = JSON.parse(item.properties._addon_config);
                    console.log('✅ Found Phase 1 add-on config:', addonConfig);
                    
                    if (addonConfig && addonConfig.title) {
                        // Calculate add-on cost (price in dollars, convert to cents)
                        const addonCost = Math.round(addonConfig.price * 100) * item.quantity;
                        console.log('✅ Calculated add-on cost:', addonCost, 'cents');
                        
                        // Create add-on line item
                        const addonLineItem = createAddonLineItem(addonConfig, addonCost, item);
                        addonLineItems.push(addonLineItem);
                        
                        // Mark original item
                        modifyOriginalItem(item, addonConfig);
                        console.log('✅ Modified original item properties:', item.properties);
                    }
                } catch (parseError) {
                    console.error('❌ Error parsing add-on config:', parseError);
                }
            }
        });
    }
    
    // Add add-on line items
    if (addonLineItems.length > 0) {
        if (!modifiedCart.items) {
            modifiedCart.items = [];
        }
        modifiedCart.items.push(...addonLineItems);
        recalculateCartTotals(modifiedCart);
        console.log('✅ Added', addonLineItems.length, 'add-on line items');
    }
    
    return modifiedCart;
}

function createAddonLineItem(addonConfig, addonCost, originalItem) {
    return {
        id: `addon_${originalItem.id}_${Date.now()}`,
        variant_id: `addon_variant_${originalItem.variant_id}_${Date.now()}`,
        key: `addon_${originalItem.key}_${Date.now()}`,
        title: addonConfig.title || 'Add-on',
        price: addonCost,
        quantity: originalItem.quantity,
        sku: addonConfig.sku || 'ADDON-001',
        product_type: 'add-on',
        properties: {
            _addon_type: 'shoplazza_addon',
            _addon_for_item: originalItem.key,
            _addon_config: JSON.stringify(addonConfig),
            _addon_source: 'widget_selection'
        }
    };
}

function modifyOriginalItem(item, addonConfig) {
    if (!item.properties) {
        item.properties = {};
    }
    
    item.properties._has_addon = 'true';
    item.properties._addon_applied = 'true';
    item.properties._addon_title = addonConfig.title || 'Add-on';
    item.properties._addon_processed = 'true';
}

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
        itemCount += 1;
    });
    
    cart.items_subtotal_price = itemsSubtotal;
    cart.total_price = totalPrice;
    cart.item_count = itemCount;
}

// Test the integration
console.log('🧪 Testing WASM function integration with widget properties...\n');

console.log('📥 Input cart:');
console.log(JSON.stringify(testCart, null, 2));

console.log('\n🔄 Processing cart...');
const result = processCartWithAddons(testCart);

console.log('\n📤 Output cart:');
console.log(JSON.stringify(result, null, 2));

console.log('\n✅ Integration test completed!');
console.log('📊 Cart summary:');
console.log('- Original total: $' + (testCart.total_price / 100).toFixed(2));
console.log('- New total: $' + (result.total_price / 100).toFixed(2));
console.log('- Add-ons added: ' + (result.items.length - testCart.items.length));
console.log('- Items in cart: ' + result.items.length);
