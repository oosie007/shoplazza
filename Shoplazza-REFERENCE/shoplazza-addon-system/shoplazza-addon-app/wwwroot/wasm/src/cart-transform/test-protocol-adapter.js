/**
 * 🧪 PROTOCOL ADAPTER TESTING SCRIPT
 * Tests the protocol adapter layer for Shoplazza compliance
 */

// Import the protocol adapter functions
const { adaptShoplazzaInput, adaptShoplazzaOutput, processCartShoplazza } = require('./protocol-adapter.js');

/**
 * Test 1: Input Format Validation
 */
function testInputFormatValidation() {
    console.log('\n🧪 TEST 1: Input Format Validation');
    
    const testInput = {
        "cart": {
            "line_items": [
                {
                    "product": {
                        "product_id": "test-123",
                        "variant_id": "variant-456", 
                        "price": "25.00",
                        "product_title": "Test Product",
                        "metafields": []
                    },
                    "id": "1",
                    "properties": "{\"Color\":\"Red\"}",
                    "quantity": "2"
                }
            ]
        },
        "currency_settings": {
            "actual_rate": "1.0"
        }
    };
    
    try {
        const adaptedInput = adaptShoplazzaInput(testInput);
        console.log('✅ Input validation passed');
        return true;
    } catch (error) {
        console.error('❌ Input validation failed:', error.message);
        return false;
    }
}

/**
 * Test 2: Output Format Validation
 */
function testOutputFormatValidation() {
    console.log('\n🧪 TEST 2: Output Format Validation');
    
    const ourOutput = {
        items: [
            {
                id: 1,
                price: 3500, // 35.00 in cents (original 25.00 + 10.00 addon)
                quantity: 2
            }
        ]
    };
    
    const originalInput = {
        cart: {
            line_items: [
                {
                    product: { price: "25.00" },
                    id: "1"
                }
            ]
        }
    };
    
    try {
        const shoplazzaOutput = adaptShoplazzaOutput(ourOutput, originalInput);
        
        if (!shoplazzaOutput.operation || !Array.isArray(shoplazzaOutput.operation.update)) {
            throw new Error('Invalid output structure');
        }
        
        console.log('✅ Output format validation passed');
        return true;
    } catch (error) {
        console.error('❌ Output validation failed:', error.message);
        return false;
    }
}

/**
 * Main test runner
 */
function runAllTests() {
    console.log('🚀 PROTOCOL ADAPTER TESTING SUITE');
    
    const tests = [testInputFormatValidation, testOutputFormatValidation];
    let passedTests = 0;
    
    for (const test of tests) {
        if (test()) {
            passedTests++;
        }
    }
    
    console.log(`\n📊 RESULTS: ${passedTests}/${tests.length} tests passed`);
    return passedTests === tests.length;
}

// Run tests if this file is executed directly
if (require.main === module) {
    runAllTests();
}

module.exports = { runAllTests };
