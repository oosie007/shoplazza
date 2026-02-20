// Cart Transform Rust WASM Wrapper
// This wrapper loads the Rust-compiled WASM and provides the same API
// as our Javy version for seamless integration

let wasmModule = null;
let wasmInstance = null;

// Initialize WASM module
async function initWasm() {
    try {
        // Load the Rust WASM module
        const response = await fetch('./cart-transform-rust.wasm');
        const wasmBuffer = await response.arrayBuffer();
        
        // Initialize the WASM module
        const wasmModule = await WebAssembly.instantiate(wasmBuffer, {
            env: {
                // Provide any environment functions the WASM might need
                memory: new WebAssembly.Memory({ initial: 256, maximum: 512 }),
                abort: () => console.error('WASM abort called'),
            }
        });
        
        wasmInstance = wasmModule.instance;
        console.log('✅ Rust WASM module loaded successfully');
        return true;
    } catch (error) {
        console.error('❌ Failed to load Rust WASM module:', error);
        return false;
    }
}

// Main cart transformation function - same API as Javy version
async function processCartWithAddons(cart) {
    // Ensure WASM is initialized
    if (!wasmInstance) {
        const initialized = await initWasm();
        if (!initialized) {
            throw new Error('Failed to initialize Rust WASM module');
        }
    }
    
    try {
        // Convert cart to JSON string
        const cartJson = JSON.stringify(cart);
        
        // Call the Rust WASM function
        const result = wasmInstance.exports.process_cart(cartJson);
        
        // Parse the result back to JavaScript object
        const modifiedCart = JSON.parse(result);
        
        console.log('✅ Rust WASM cart transformation completed');
        return modifiedCart;
    } catch (error) {
        console.error('❌ Rust WASM cart transformation failed:', error);
        
        // Fallback: return original cart if WASM fails
        console.warn('⚠️  Falling back to original cart due to WASM error');
        return cart;
    }
}

// Export the main function for use in our system
if (typeof module !== 'undefined' && module.exports) {
    // Node.js environment
    module.exports = { processCartWithAddons, initWasm };
} else if (typeof window !== 'undefined') {
    // Browser environment
    window.processCartWithAddons = processCartWithAddons;
    window.initWasm = initWasm;
}

// Auto-initialize when loaded
if (typeof window !== 'undefined') {
    // Initialize WASM when the script loads
    initWasm().then(success => {
        if (success) {
            console.log('🚀 Rust WASM cart transform ready');
        } else {
            console.error('💥 Rust WASM initialization failed');
        }
    });
}
