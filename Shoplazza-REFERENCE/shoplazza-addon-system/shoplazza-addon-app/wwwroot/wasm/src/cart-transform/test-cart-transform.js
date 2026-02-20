/**
 * Test script for the cart-transform function
 * 
 * This simulates how the function would work when called by Shoplazza
 * by providing sample cart data and checking the output.
 */

// Mock Javy.IO for testing
global.Javy = {
    IO: {
        readSync: () => JSON.stringify(sampleCart),
        writeSync: (fd, data) => {
            if (fd === 1) { // stdout
                console.log('=== FUNCTION OUTPUT ===');
                console.log(data);
                console.log('=== END OUTPUT ===');
                
                // Parse and validate the output
                try {
                    const outputCart = JSON.parse(data);
                    validateOutput(outputCart);
                } catch (error) {
                    console.error('Error parsing function output:', error.message);
                }
            }
        }
    }
};

// Sample cart data that would come from Shoplazza
const sampleCart = {
    id: "cart_12345",
    note: "",
    attributes: {},
    original_total_price: 2999,
    total_price: 2999,
    total_discount: 0,
    total_weight: 500,
    item_count: 1,
    items: [
        {
            id: 1,
            properties: {
                cdh_shoplazza_addon: JSON.stringify({
                    isSelected: true,
                    config: {
                        title: "Premium Protection",
                        price: 9.99,
                        sku: "PROTECTION-001",
                        description: "Protect your purchase with premium coverage",
                        weightGrams: 0,
                        isTaxable: true,
                        requiresShipping: false,
                        imageUrl: ""
                    }
                }),
                _add_on_type: "protection_plan",
                _add_on_name: "Premium Protection",
                _add_on_price: "9.99",
                _add_on_sku: "PROTECTION-001",
                _add_on_description: "Protect your purchase with premium coverage",
                _compressed: "true",
                _add_on_data: "eyJ0aXRsZSI6IlByZW1pdW0gUHJvdGVjdGlvbiIsInByaWNlIjo5Ljk5LCJza3UiOiJQUk9URUNUSU9OLTAwMSIsImRlc2NyaXB0aW9uIjoiUHJvdGVjdCB5b3VyIHB1cmNoYXNlIHdpdGggcHJlbWl1bSBjb3ZlcmFnZSIsIndlaWdodEdyYW1zIjowLCJpc1RheGFibGUiOnRydWUsInJlcXVpcmVzU2hpcHBpbmciOmZhbHNlLCJpbWFnZVVybCI6IiJ9"
            },
            quantity: 2,
            variant_id: 123,
            key: "variant_123",
            title: "Premium T-Shirt",
            price: 1499,
            original_price: 1499,
            discounted_price: 1499,
            line_price: 2998,
            original_line_price: 2998,
            final_price: 1499,
            final_line_price: 2998,
            sku: "TSHIRT-001",
            grams: 250,
            vendor: "Demo Store",
            taxable: true,
            product_id: 456,
            product_title: "Premium T-Shirt",
            product_description: "High-quality cotton t-shirt",
            product_type: "clothing",
            product_has_only_default_variant: true,
            gift_card: false,
            requires_shipping: true,
            url: "/products/premium-t-shirt",
            image: "https://example.com/tshirt.jpg",
            handle: "premium-t-shirt",
            options_with_values: [],
            line_level_discount_allocations: [],
            line_level_total_discount: 0,
            total_discount: 0,
            discounts: []
        }
    ],
    requires_shipping: true,
    currency: "USD",
    items_subtotal_price: 2998,
    cart_level_discount_applications: []
};

// Import the cart-transform function logic
const fs = require('fs');
const path = require('path');

// Read and execute the cart-transform function
try {
    const functionPath = path.join(__dirname, 'cart-transform.js');
    const functionCode = fs.readFileSync(functionPath, 'utf8');
    
    console.log('=== TESTING CART-TRANSFORM FUNCTION ===');
    console.log('Input cart has 1 item with add-on selected');
    console.log('Expected: Add-on line item added, cart totals updated');
    console.log('');
    
    // Execute the function
    eval(functionCode);
    
} catch (error) {
    console.error('Error testing function:', error.message);
}

/**
 * Validate the function output
 * @param {Object} outputCart - The cart returned by the function
 */
function validateOutput(outputCart) {
    console.log('');
    console.log('=== VALIDATION RESULTS ===');
    
    // Check if add-on line item was added
    const addonItems = outputCart.items.filter(item => 
        item.properties && item.properties._addon_type === 'shoplazza_addon'
    );
    
    if (addonItems.length > 0) {
        console.log('✅ Add-on line items added:', addonItems.length);
        
        addonItems.forEach((addon, index) => {
            console.log(`  Add-on ${index + 1}: ${addon.title} - $${(addon.price / 100).toFixed(2)}`);
        });
    } else {
        console.log('❌ No add-on line items found');
    }
    
    // Check if cart totals were updated
    const originalTotal = sampleCart.total_price;
    const newTotal = outputCart.total_price;
    const expectedAddonCost = 1998; // 2 items × $9.99 × 100 cents
    
    // Allow for small rounding differences (within 1 cent)
    const difference = Math.abs(newTotal - (originalTotal + expectedAddonCost));
    const isCorrect = difference <= 1;
    
    if (isCorrect) {
        console.log('✅ Cart total correctly updated');
        console.log(`  Original: $${(originalTotal / 100).toFixed(2)}`);
        console.log(`  Add-on cost: $${(expectedAddonCost / 100).toFixed(2)}`);
        console.log(`  New total: $${(newTotal / 100).toFixed(2)}`);
        if (difference > 0) {
            console.log(`  Note: 1 cent rounding difference (acceptable)`);
        }
    } else {
        console.log('❌ Cart total not correctly updated');
        console.log(`  Expected: $${((originalTotal + expectedAddonCost) / 100).toFixed(2)}`);
        console.log(`  Actual: $${(newTotal / 100).toFixed(2)}`);
        console.log(`  Difference: ${difference} cents`);
    }
    
    // Check if item count was updated
    const expectedItemCount = sampleCart.item_count + addonItems.length;
    if (outputCart.item_count === expectedItemCount) {
        console.log('✅ Item count correctly updated');
        console.log(`  Original: ${sampleCart.item_count}`);
        console.log(`  Add-ons: ${addonItems.length}`);
        console.log(`  New total: ${outputCart.item_count}`);
    } else {
        console.log('❌ Item count not correctly updated');
        console.log(`  Expected: ${expectedItemCount}`);
        console.log(`  Actual: ${outputCart.item_count}`);
    }
    
    // Check if original item was modified
    const originalItem = outputCart.items.find(item => item.id === 1);
    if (originalItem && originalItem.properties._has_addon === 'true') {
        console.log('✅ Original item properly marked with add-on');
    } else {
        console.log('❌ Original item not properly marked');
    }
    
    console.log('');
    console.log('=== TEST COMPLETE ===');
}
