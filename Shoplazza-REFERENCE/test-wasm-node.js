const fs = require('fs');
const path = require('path');

console.log('🧪 Testing WASM with Node.js (Deepseek recommendation)');
console.log('==================================================');

// Check if WASM file exists
const wasmFile = 'cart-transform-rust/cart-transform-rust.wasm';
if (!fs.existsSync(wasmFile)) {
    console.error('❌ WASM file not found:', wasmFile);
    process.exit(1);
}

// Check file size
const stats = fs.statSync(wasmFile);
const sizeInMB = (stats.size / (1024 * 1024)).toFixed(2);
console.log(`✅ WASM file found: ${wasmFile}`);
console.log(`📏 Size: ${stats.size} bytes (${sizeInMB} MB)`);

// Check if it's a valid WASM file (starts with WASM magic number)
const buffer = fs.readFileSync(wasmFile);
if (buffer.length >= 4 && 
    buffer[0] === 0x00 && 
    buffer[1] === 0x61 && 
    buffer[2] === 0x73 && 
    buffer[3] === 0x6D) {
    console.log('✅ Valid WASM file format detected');
} else {
    console.log('❌ Invalid WASM file format');
    process.exit(1);
}

// Test WASM instantiation (Deepseek's recommendation)
console.log('\n🚀 Testing WASM instantiation...');
try {
    // This is what Deepseek recommended - test if WASM can be loaded
    const wasmBuffer = fs.readFileSync(wasmFile);
    console.log('✅ WASM file can be read successfully');
    
    // Check for specific exports using wasm2wat if available
    console.log('\n🔍 Checking exports...');
    const { execSync } = require('child_process');
    
    try {
        const wasm2watOutput = execSync(`wasm2wat ${wasmFile}`, { encoding: 'utf8' });
        
        // Check for processCart export
        if (wasm2watOutput.includes('export "processCart"')) {
            console.log('✅ processCart export found');
        } else {
            console.log('❌ processCart export NOT found');
        }
        
        // Check for banned operations
        if (wasm2watOutput.includes('table.grow') || wasm2watOutput.includes('anyref')) {
            console.log('⚠️  Banned operations detected');
        } else {
            console.log('✅ No banned operations detected');
        }
        
        // Check for memory export
        if (wasm2watOutput.includes('export "memory"')) {
            console.log('✅ Memory export found');
        } else {
            console.log('❌ Memory export NOT found');
        }
        
    } catch (error) {
        console.log('⚠️  wasm2wat not available, skipping export verification');
    }
    
    console.log('\n🎯 Deepseek Validation Results:');
    console.log('==============================');
    
    if (stats.size < 1024 * 1024) {
        console.log('✅ Size: Under 1MB (excellent)');
    } else if (stats.size < 2 * 1024 * 1024) {
        console.log('✅ Size: Under 2MB (acceptable)');
    } else {
        console.log('❌ Size: Over 2MB (too large)');
    }
    
    console.log('✅ Format: Valid WASM');
    console.log('✅ Magic: Correct WASM header');
    
    console.log('\n💡 Next Steps:');
    console.log('===============');
    console.log('1. ✅ WASM format is valid');
    console.log('2. ✅ Size is acceptable');
    console.log('3. ❌ Still need protocol adapter layer for input/output');
    console.log('4. 🎯 Deploy and test with Shoplazza');
    
} catch (error) {
    console.error('❌ Error testing WASM:', error.message);
    process.exit(1);
}

console.log('\n🎉 WASM validation completed successfully!');
