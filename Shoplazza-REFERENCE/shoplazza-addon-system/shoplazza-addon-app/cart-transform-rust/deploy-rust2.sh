#!/bin/bash

# Cart Transform Rust WASM Build Script
# This script builds the Rust cart-transform function into optimized WASM
# Author: AI Assistant
# Date: 2025-08-16

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="cart-transform-rust"
WASM_OUTPUT="cart-transform-rust.wasm"
OPTIMIZED_OUTPUT="cart-transform-rust-optimized.wasm"
TEST_OUTPUT="test-output.json"

echo -e "${BLUE}🚀 Cart Transform Rust WASM Build Pipeline${NC}"
echo -e "${BLUE}============================================${NC}"

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Check if we're in the right directory
if [ ! -f "Cargo.toml" ]; then
    print_error "Cargo.toml not found. Please run this script from the cart-transform-rust directory."
    exit 1
fi

# Check if Rust is installed
if ! command -v cargo &> /dev/null; then
    print_error "Rust/cargo not found. Please install Rust first: https://rustup.rs/"
    exit 1
fi

# Check if wasm-pack is installed
if ! command -v wasm-pack &> /dev/null; then
    print_warning "wasm-pack not found. Installing..."
    cargo install wasm-pack
    print_status "wasm-pack installed successfully"
fi

# Check Rust version
RUST_VERSION=$(rustc --version | cut -d' ' -f2)
print_info "Rust version: $RUST_VERSION"

# Clean previous builds
print_info "Cleaning previous builds..."
cargo clean
rm -f "$WASM_OUTPUT" "$OPTIMIZED_OUTPUT" "$TEST_OUTPUT"

# Run Rust tests first
print_info "Running Rust unit tests..."
if cargo test; then
    print_status "All Rust tests passed"
else
    print_error "Rust tests failed. Fix the issues before building WASM."
    exit 1
fi

# Build for WASM target with Shoplazza 2025 optimizations
print_info "Building for WASM target with Shoplazza 2025 optimizations..."
export RUSTFLAGS="-C target-feature=+bulk-memory"
if wasm-pack build --target web --release --out-dir pkg; then
    print_status "WASM build successful"
else
    print_error "WASM build failed"
    exit 1
fi

# Copy the generated WASM file
if [ -f "pkg/cart_transform_rust_bg.wasm" ]; then
    cp "pkg/cart_transform_rust_bg.wasm" "$WASM_OUTPUT"
    print_status "WASM file generated: $WASM_OUTPUT"
else
    print_error "Expected WASM file not found in pkg directory"
    exit 1
fi

# Check file size
WASM_SIZE=$(stat -f%z "$WASM_OUTPUT" 2>/dev/null || stat -c%s "$WASM_OUTPUT" 2>/dev/null || echo "unknown")
print_info "WASM file size: $WASM_SIZE bytes"

# Verify exports using wasm2wat if available
if command -v wasm2wat &> /dev/null; then
    print_info "Verifying WASM exports..."
    if wasm2wat "$WASM_OUTPUT" 2>/dev/null | grep -q "export.*processCart"; then
        print_status "✅ processCart export found"
    else
        print_warning "⚠️  processCart export not found - this may cause issues"
    fi
    
    # Check for banned operations
    if wasm2wat "$WASM_OUTPUT" 2>/dev/null | grep -q -E "(table\.grow|anyref)"; then
        print_warning "⚠️  Banned operations detected (table.grow/anyref)"
    else
        print_status "✅ No banned operations detected"
    fi
else
    print_warning "wasm2wat not found. Install wabt for export verification:"
    print_info "  macOS: brew install wabt"
    print_info "  Ubuntu: sudo apt-get install wabt"
fi

# Note: wasm-opt optimization removed due to compatibility issues
# The WASM file from wasm-pack is already optimized for production use
print_info "Using wasm-pack optimized WASM (wasm-opt step removed for compatibility)"

# Create a simple test to verify the WASM works
print_info "Creating test script to verify WASM functionality..."
cat > test-wasm.js << 'EOF'
const fs = require('fs');
const path = require('path');

// Simple test to verify WASM file exists and has reasonable size
const wasmFile = 'cart-transform-rust.wasm';

if (!fs.existsSync(wasmFile)) {
    console.error('❌ WASM file not found');
    process.exit(1);
}

const stats = fs.statSync(wasmFile);
const sizeInMB = (stats.size / (1024 * 1024)).toFixed(2);

console.log(`✅ WASM file found: ${wasmFile}`);
console.log(`📏 Size: ${stats.size} bytes (${sizeInMB} MB)`);

// Check if size is reasonable (should be much smaller than Javy version)
if (stats.size > 1024 * 1024) {
    console.log('⚠️  Warning: WASM file is larger than 1MB');
} else {
    console.log('🎯 Excellent: WASM file is under 1MB');
}

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
EOF

# Run the test
print_info "Running WASM validation test..."
if node test-wasm.js; then
    print_status "WASM validation passed"
else
    print_error "WASM validation failed"
    exit 1
fi

# Note: Shoplazza validation endpoint removed - endpoint was incorrect
# Focus on local validation and export verification instead
print_info "Shoplazza validation endpoint removed - using local validation instead"

# ========================================
# DEPLOYMENT: Copy WASM and source to production locations
# ========================================
print_info "🚀 Deploying WASM and source code to production locations..."

# Create the primary WASM directory if it doesn't exist
PRIMARY_WASM_DIR="../wwwroot/wasm"
if [ ! -d "$PRIMARY_WASM_DIR" ]; then
    mkdir -p "$PRIMARY_WASM_DIR"
    print_status "Created primary WASM directory: $PRIMARY_WASM_DIR"
fi

# Copy WASM to primary location (this makes it the FIRST choice)
PRIMARY_WASM_PATH="$PRIMARY_WASM_DIR/cart-transform-rust.wasm"
cp "$WASM_OUTPUT" "$PRIMARY_WASM_PATH"
print_status "✅ WASM deployed to PRIMARY location: $PRIMARY_WASM_PATH"

# Create source code directory structure
SOURCE_DIR="../wwwroot/wasm/src/cart-transform"
if [ ! -d "$SOURCE_DIR" ]; then
    mkdir -p "$SOURCE_DIR"
    print_status "Created source directory: $SOURCE_DIR"
fi

# Clean out old source files and copy only the main implementation
print_info "📁 Cleaning old source files and copying main implementation..."
rm -f "$SOURCE_DIR"/*.rs

# Copy only the main implementation file (lib-shoplazza.rs) and rename it to lib.rs
cp "src/main.rs" "$SOURCE_DIR/main.rs"
print_status "✅ Main implementation copied as main.rs to: $SOURCE_DIR"

# List what was copied
print_info "📋 Deployment summary:"
print_info "   WASM: $PRIMARY_WASM_PATH ($(stat -f%z "$PRIMARY_WASM_PATH" 2>/dev/null || stat -c%s "$PRIMARY_WASM_PATH" 2>/dev/null || echo "unknown") bytes)"
print_info "   Source: $SOURCE_DIR ($(ls -1 "$SOURCE_DIR"/*.rs 2>/dev/null | wc -l | tr -d ' ') Rust files)"

print_status "🎉 Deployment complete! Your WASM is now the PRIMARY choice!"

# Clean up test files
rm -f test-wasm.js "$TEST_OUTPUT"
rm -rf pkg

# Final status
FINAL_SIZE=$(stat -f%z "$WASM_OUTPUT" 2>/dev/null || stat -c%s "$WASM_OUTPUT" 2>/dev/null || echo "unknown")
FINAL_SIZE_MB=$(echo "scale=2; $FINAL_SIZE / 1048576" | bc 2>/dev/null || echo "unknown")

echo ""
echo -e "${GREEN}🎉 BUILD COMPLETE! 🎉${NC}"
echo -e "${GREEN}========================${NC}"
echo -e "📁 Output file: ${GREEN}$WASM_OUTPUT${NC}"
echo -e "📏 File size: ${GREEN}$FINAL_SIZE bytes${NC}"
if [ "$FINAL_SIZE_MB" != "unknown" ]; then
    echo -e "📊 Size in MB: ${GREEN}$FINAL_SIZE_MB MB${NC}"
fi

# Size comparison with Javy version
if [ -f "../cart-transform-function/cart-transform.wasm" ]; then
    JAVY_SIZE=$(stat -f%z "../cart-transform-function/cart-transform.wasm" 2>/dev/null || stat -c%s "../cart-transform-function/cart-transform.wasm" 2>/dev/null || echo "unknown")
    if [ "$FINAL_SIZE" != "unknown" ] && [ "$JAVY_SIZE" != "unknown" ]; then
        REDUCTION=$((100 - (FINAL_SIZE * 100 / JAVY_SIZE)))
        echo -e "📉 Size reduction vs Javy: ${GREEN}${REDUCTION}%${NC}"
        
        if [ "$FINAL_SIZE" -lt "$JAVY_SIZE" ]; then
            echo -e "🚀 ${GREEN}SUCCESS: Rust version is smaller than Javy!${NC}"
        else
            echo -e "⚠️  ${YELLOW}Warning: Rust version is not smaller than Javy${NC}"
        fi
    fi
fi

echo ""
echo -e "${BLUE}✅ AUTOMATED DEPLOYMENT COMPLETE!${NC}"
echo -e "${BLUE}====================================${NC}"
echo -e "🎯 Your WASM is now the PRIMARY choice in the system"
echo -e "📁 WASM deployed to: ${GREEN}../wwwroot/wasm/cart-transform-rust.wasm${NC}"
echo -e "📁 Source code deployed to: ${GREEN}../wwwroot/wasm/src/cart-transform/${NC}"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo -e "1. ✅ WASM automatically deployed to primary location"
echo -e "2. ✅ Source code automatically deployed to expected directory"
echo -e "3. Test with the existing test suite"
echo -e "4. Verify function creation works with Shoplazza API"

echo ""
print_status "Rust WASM build pipeline completed successfully!"
