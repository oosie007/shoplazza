/**
 * Comprehensive test scenarios for the cart-transform function
 * 
 * Tests various edge cases and real-world scenarios
 */

const fs = require('fs');
const path = require('path');

// Mock Javy.IO for testing
global.Javy = {
    IO: {
        readSync: () => '',
        writeSync: (fd, data) => {
            if (fd === 1) { // stdout
                return data;
            }
        }
    }
};

// Test scenarios
const testScenarios = [
    {
        name: "Single item with add-on",
        description: "Basic case: one product with add-on selected",
        cart: {
            id: "cart_001",
            total_price: 2999,
            item_count: 1,
            items: [{
                id: 1,
                properties: {
                    cdh_shoplazza_addon: JSON.stringify({
                        isSelected: true,
                        config: {
                            title: "Premium Protection",
                            price: 9.99,
                            sku: "PROTECTION-001"
                        }
                    })
                },
                quantity: 1,
                price: 2999,
                final_line_price: 2999
            }]
        },
        expected: {
            addonItems: 1,
            totalIncrease: 999, // $9.99 in cents
            itemCountIncrease: 1
        }
    },
    {
        name: "Multiple items with add-ons",
        description: "Cart with multiple products, some with add-ons",
        cart: {
            id: "cart_002",
            total_price: 5998,
            item_count: 2,
            items: [
                {
                    id: 1,
                    properties: {
                        cdh_shoplazza_addon: JSON.stringify({
                            isSelected: true,
                            config: {
                                title: "Premium Protection",
                                price: 9.99,
                                sku: "PROTECTION-001"
                            }
                        })
                    },
                    quantity: 2,
                    price: 1499,
                    final_line_price: 2998
                },
                {
                    id: 2,
                    properties: {
                        cdh_shoplazza_addon: JSON.stringify({
                            isSelected: true,
                            config: {
                                title: "Extended Warranty",
                                price: 19.99,
                                sku: "WARRANTY-001"
                            }
                        })
                    },
                    quantity: 1,
                    price: 3000,
                    final_line_price: 3000
                }
            ]
        },
        expected: {
            addonItems: 2,
            totalIncrease: 3997, // (2 × $9.99) + $19.99 in cents
            itemCountIncrease: 2
        }
    },
    {
        name: "Mixed cart - some with add-ons",
        description: "Cart with products that have and don't have add-ons",
        cart: {
            id: "cart_003",
            total_price: 4497,
            item_count: 2,
            items: [
                {
                    id: 1,
                    properties: {
                        cdh_shoplazza_addon: JSON.stringify({
                            isSelected: true,
                            config: {
                                title: "Premium Protection",
                                price: 9.99,
                                sku: "PROTECTION-001"
                            }
                        })
                    },
                    quantity: 1,
                    price: 1499,
                    final_line_price: 1499
                },
                {
                    id: 2,
                    properties: {}, // No add-on
                    quantity: 1,
                    price: 3000,
                    final_line_price: 3000
                }
            ]
        },
        expected: {
            addonItems: 1,
            totalIncrease: 1001, // $9.99 in cents (allowing for rounding)
            itemCountIncrease: 1
        }
    },
    {
        name: "Cart with no add-ons",
        description: "Cart with products but no add-ons selected",
        cart: {
            id: "cart_004",
            total_price: 4497,
            item_count: 2,
            items: [
                {
                    id: 1,
                    properties: {}, // No add-on
                    quantity: 1,
                    price: 1499,
                    final_line_price: 1499
                },
                {
                    id: 2,
                    properties: {}, // No add-on
                    quantity: 1,
                    price: 3000,
                    final_line_price: 3000
                }
            ]
        },
        expected: {
            addonItems: 0,
            totalIncrease: 0,
            itemCountIncrease: 0
        }
    },
    {
        name: "Add-on with quantity > 1",
        description: "Product with quantity > 1 and add-on selected",
        cart: {
            id: "cart_005",
            total_price: 5998,
            item_count: 1,
            items: [{
                id: 1,
                properties: {
                    cdh_shoplazza_addon: JSON.stringify({
                        isSelected: true,
                        config: {
                            title: "Premium Protection",
                            price: 9.99,
                            sku: "PROTECTION-001"
                        }
                    })
                },
                quantity: 4,
                price: 1499,
                final_line_price: 5998
            }]
        },
        expected: {
            addonItems: 1,
            totalIncrease: 3996, // 4 × $9.99 in cents
            itemCountIncrease: 1
        }
    },
    {
        name: "Phase 2: New Shoplazza property names",
        description: "Cart with new _add_on_ property format (Phase 2)",
        cart: {
            id: "cart_006",
            total_price: 2500,
            item_count: 1,
            items: [{
                id: 1,
                properties: {
                    _add_on_type: "protection_plan",
                    _add_on_name: "Premium Protection",
                    _add_on_price: "1.50",
                    _add_on_sku: "PROTECTION-001",
                    _add_on_description: "Protect your purchase"
                },
                quantity: 1,
                price: 2500,
                final_line_price: 2500
            }]
        },
        expected: {
            addonItems: 1,
            totalIncrease: 150, // $1.50 in cents
            itemCountIncrease: 1
        }
    },
    {
        name: "Phase 2: Compressed add-on data",
        description: "Cart with compressed _add_on_data property (Phase 2)",
        cart: {
            id: "cart_007",
            total_price: 2500,
            item_count: 1,
            items: [{
                id: 1,
                properties: {
                    _compressed: "true",
                    _add_on_data: "eyJ0aXRsZSI6IlByZW1pdW0gUHJvdGVjdGlvbiIsInByaWNlIjoxLjUwLCJza3UiOiJQUk9URUNUSU9OLTAwMSIsImRlc2NyaXB0aW9uIjoiUHJvdGVjdCB5b3VyIHB1cmNoYXNlIn0="
                },
                quantity: 1,
                price: 2500,
                final_line_price: 2500
            }]
        },
        expected: {
            addonItems: 1,
            totalIncrease: 150, // $1.50 in cents
            itemCountIncrease: 1
        }
    },
    {
        name: "Phase 2: Mixed property formats",
        description: "Cart with both old and new property formats for backward compatibility",
        cart: {
            id: "cart_008",
            total_price: 2500,
            item_count: 1,
            items: [{
                id: 1,
                properties: {
                    _addon_selected: "true",
                    _addon_config: "{\"title\":\"Premium Protection\",\"price\":1.50,\"sku\":\"PROTECTION-001\"}",
                    _add_on_type: "protection_plan",
                    _add_on_name: "Premium Protection",
                    _add_on_price: "1.50",
                    _add_on_sku: "PROTECTION-001"
                },
                quantity: 1,
                price: 2500,
                final_line_price: 2500
            }]
        },
        expected: {
            addonItems: 1,
            totalIncrease: 150, // $1.50 in cents
            itemCountIncrease: 1
        }
    }
];

/**
 * Run all test scenarios
 */
function runAllTests() {
    console.log('🧪 Running Comprehensive Cart-Transform Function Tests');
    console.log('=====================================================\n');
    
    let passedTests = 0;
    let totalTests = testScenarios.length;
    
    testScenarios.forEach((scenario, index) => {
        console.log(`Test ${index + 1}: ${scenario.name}`);
        console.log(`Description: ${scenario.description}`);
        
        try {
            const result = testScenario(scenario);
            if (result.passed) {
                console.log('✅ PASSED');
                passedTests++;
            } else {
                console.log('❌ FAILED');
                console.log(`  Expected: ${JSON.stringify(scenario.expected)}`);
                console.log(`  Actual: ${JSON.stringify(result.actual)}`);
            }
        } catch (error) {
            console.log('❌ ERROR');
            console.log(`  ${error.message}`);
        }
        
        console.log('');
    });
    
    console.log('=== TEST SUMMARY ===');
    console.log(`Passed: ${passedTests}/${totalTests}`);
    console.log(`Success Rate: ${((passedTests / totalTests) * 100).toFixed(1)}%`);
    
    if (passedTests === totalTests) {
        console.log('🎉 All tests passed!');
    } else {
        console.log('⚠️  Some tests failed. Check the output above.');
    }
}

/**
 * Test a single scenario
 * @param {Object} scenario - Test scenario to run
 * @returns {Object} - Test result
 */
function testScenario(scenario) {
    // Set up the mock to return this scenario's cart data
    global.Javy.IO.readSync = () => JSON.stringify(scenario.cart);
    
    // Capture the output
    let outputData = '';
    global.Javy.IO.writeSync = (fd, data) => {
        if (fd === 1) {
            outputData = data;
        }
    };
    
    // Import and execute the cart-transform function
    const functionPath = path.join(__dirname, 'cart-transform.js');
    const functionCode = fs.readFileSync(functionPath, 'utf8');
    
    // Execute the function
    eval(functionCode);
    
    // Parse the output
    const outputCart = JSON.parse(outputData);
    
    // Analyze the results
    const addonItems = outputCart.items.filter(item => 
        item.properties && item.properties._addon_type === 'shoplazza_addon'
    );
    
    const totalIncrease = outputCart.total_price - scenario.cart.total_price;
    const itemCountIncrease = outputCart.item_count - scenario.cart.item_count;
    
    const actual = {
        addonItems: addonItems.length,
        totalIncrease: totalIncrease,
        itemCountIncrease: itemCountIncrease
    };
    
    // Check if results match expectations (allow for small rounding differences)
    const passed = 
        actual.addonItems === scenario.expected.addonItems &&
        Math.abs(actual.totalIncrease - scenario.expected.totalIncrease) <= 1 &&
        actual.itemCountIncrease === scenario.expected.itemCountIncrease;
    
    return {
        passed,
        actual,
        expected: scenario.expected
    };
}

// Run the tests if this file is executed directly
if (require.main === module) {
    runAllTests();
}

module.exports = {
    runAllTests,
    testScenario,
    testScenarios
};
