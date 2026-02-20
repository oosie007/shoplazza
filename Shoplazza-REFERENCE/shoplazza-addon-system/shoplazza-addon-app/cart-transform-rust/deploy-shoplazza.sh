#!/bin/bash

# Shoplazza 2024.07 WASM Build Script
# This builds the WASM using Shoplazza's ACTUAL protocol requirements
# Author: AI Assistant
# Date: 2025-08-16

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

echo -e "${BLUE}🚀 Shoplazza 2024.07 WASM Build Pipeline${NC}"
echo -e "${BLUE}============================================${NC}"

# Configuration
PROJECT_NAME="cart-transform-shoplazza"
WASM_OUTPUT="cart-transform-shoplazza.wasm"
OPTIMIZED_OUTPUT="cart-transform-shoplazza-optimized.wasm"

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

# Check Rust version
RUST_VERSION=$(rustc --version | cut -d' ' -f2)
print_info "Rust version: $RUST_VERSION"

# Clean previous builds
print_info "Cleaning previous builds..."
cargo clean
rm -f "$WASM_OUTPUT" "$OPTIMIZED_OUTPUT"

# Build for WASM target with Shoplazza-specific flags
print_info "Building for WASM target with Shoplazza 2024.07 protocol..."
export RUSTFLAGS="-C link-arg=--initial-memory=16777216 -C target-feature=+bulk-memory"

if cargo build --target wasm32-unknown-unknown --release; then
    print_status "WASM build successful"
else
    print_error "WASM build failed"
    exit 1
fi

# Copy the generated WASM file
if [ -f "target/wasm32-unknown-unknown/release/cart_transform_shoplazza.wasm" ]; then
    cp "target/wasm32-unknown-unknown/release/cart_transform_shoplazza.wasm" "$WASM_OUTPUT"
    print_status "WASM file generated: $WASM_OUTPUT"
else
    print_error "Expected WASM file not found in target directory"
    exit 1
fi

# Check file size
WASM_SIZE=$(stat -f%z "$WASM_OUTPUT" 2>/dev/null || stat -c%s "$WASM_OUTPUT" 2>/dev/null || echo "unknown")
print_info "WASM file size: $WASM_SIZE bytes"

# Verify exports using wasm2wat if available
if command -v wasm2wat &> /dev/null; then
    print_info "Verifying WASM exports for Shoplazza compatibility..."
    
    # Check for required exports
    if wasm2wat "$WASM_OUTPUT" 2>/dev/null | grep -q "export.*processCart"; then
        print_status "✅ processCart export found"
    else
        print_warning "⚠️  processCart export not found - this will cause issues"
    fi
    
    if wasm2wat "$WASM_OUTPUT" 2>/dev/null | grep -q "export.*_start"; then
        print_status "✅ _start export found"
    else
        print_warning "⚠️  _start export not found - this will cause issues"
    fi
    
    # Check for banned operations
    if wasm2wat "$WASM_OUTPUT" 2>/dev/null | grep -q -E "(table\.grow|anyref)"; then
        print_warning "⚠️  Banned operations detected (table.grow/anyref)"
    else
        print_status "✅ No banned operations detected"
    fi
    
    # Check memory export
    if wasm2wat "$WASM_OUTPUT" 2>/dev/null | grep -q "export.*memory"; then
        print_status "✅ Memory export found"
    else
        print_warning "⚠️  Memory export not found - this may cause issues"
    fi
else
    print_warning "wasm2wat not found. Install wabt for export verification:"
    print_info "  macOS: brew install wabt"
    print_info "  Ubuntu: sudo apt-get install binaryen"
fi

# Optimize the WASM file using wasm-opt if available
if command -v wasm-opt &> /dev/null; then
    print_info "Optimizing WASM file with wasm-opt for Shoplazza..."
    
    # Use Shoplazza-compatible optimization flags
    if wasm-opt -Oz --disable-reference-types --enable-mutable-globals \
        -o "$OPTIMIZED_OUTPUT" "$WASM_OUTPUT"; then
        OPTIMIZED_SIZE=$(stat -f%z "$OPTIMIZED_OUTPUT" 2>/dev/null || stat -c%s "$OPTIMIZED_OUTPUT" 2>/dev/null || echo "unknown")
        print_status "WASM optimized for Shoplazza: $OPTIMIZED_SIZE bytes"
        
        # Use optimized version if it's smaller
        if [ "$OPTIMIZED_SIZE" != "unknown" ] && [ "$WASM_SIZE" != "unknown" ] && [ "$OPTIMIZED_SIZE" -lt "$WASM_SIZE" ]; then
            cp "$OPTIMIZED_OUTPUT" "$WASM_OUTPUT"
            print_status "Using optimized version (${OPTIMIZED_SIZE} bytes)"
        fi
    else
        print_warning "WASM optimization failed, using original version"
    fi
else
    print_warning "wasm-opt not found. Install binaryen for additional optimization:"
    print_info "  macOS: brew install binaryen"
    print_info "  Ubuntu: sudo apt-get install binaryen"
fi

# Final validation
FINAL_SIZE=$(stat -f%z "$WASM_OUTPUT" 2>/dev/null || stat -c%s "$WASM_OUTPUT" 2>/dev/null || echo "unknown")
FINAL_SIZE_MB=$(echo "scale=2; $FINAL_SIZE / 1048576" | bc 2>/dev/null || echo "unknown")

echo ""
echo -e "${GREEN}🎉 SHOPLAZZA 2024.07 BUILD COMPLETE! 🎉${NC}"
echo -e "${GREEN}==========================================${NC}"
echo -e "📁 Output file: ${GREEN}$WASM_OUTPUT${NC}"
echo -e "📏 File size: ${GREEN}$FINAL_SIZE bytes${NC}"
if [ "$FINAL_SIZE_MB" != "unknown" ]; then
    echo -e "📊 Size in MB: ${GREEN}$FINAL_SIZE_MB MB${NC}"
fi

# Shoplazza compatibility check
if [ "$FINAL_SIZE" != "unknown" ]; then
    if [ "$FINAL_SIZE" -lt 1048576 ]; then
        echo -e "🎯 ${GREEN}EXCELLENT: Under 1MB - Shoplazza will love this!${NC}"
    elif [ "$FINAL_SIZE" -lt 2097152 ]; then
        echo -e "✅ ${GREEN}GOOD: Under 2MB - Shoplazza compatible${NC}"
    else
        echo -e "⚠️  ${YELLOW}WARNING: Over 2MB - may exceed Shoplazza limits${NC}"
    fi
fi

echo ""
echo -e "${BLUE}🚀 Next steps:${NC}"
echo -e "1. Copy ${GREEN}$WASM_OUTPUT${NC} to ${BLUE}../wwwroot/wasm/${NC}"
echo -e "2. Test with Shoplazza function creation API"
echo -e "3. Monitor for 'get module fail' errors"
echo -e "4. If successful, this WASM follows Shoplazza's actual protocol!"

echo ""
print_status "Shoplazza 2024.07 WASM build pipeline completed successfully!"
