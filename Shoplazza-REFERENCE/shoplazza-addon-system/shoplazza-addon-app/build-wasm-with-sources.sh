#!/bin/bash

# Build WASM and copy source files to wwwroot/wasm/src
# This ensures source code is always bundled with WASM files

set -e

echo "🔨 Building WASM and bundling source code..."

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Create the source directories
echo "📁 Creating source directories..."
mkdir -p wwwroot/wasm/src/cart-transform-universal
mkdir -p wwwroot/wasm/src/cart-transform

# Build Rust WASM
echo "🦀 Building Rust WASM..."
cd cart-transform-rust
if command -v wasm-pack &> /dev/null; then
    echo "Using wasm-pack..."
    wasm-pack build --target web --out-dir pkg
    cp pkg/*.wasm ../wwwroot/wasm/cart-transform-universal.wasm
else
    echo "Using cargo build..."
    cargo build --target wasm32-unknown-unknown --release
    cp target/wasm32-unknown-unknown/release/cart_transform_rust.wasm ../wwwroot/wasm/cart-transform-universal.wasm
fi
cd ..

# Copy Rust source files
echo "📋 Copying Rust source files..."
cp cart-transform-rust/src/*.rs wwwroot/wasm/src/cart-transform-universal/

# Copy JavaScript source files
echo "📋 Copying JavaScript source files..."
cp cart-transform-function/*.js wwwroot/wasm/src/cart-transform/

# Show the final structure
echo "✅ Build complete! Final structure:"
echo "wwwroot/wasm/"
ls -la wwwroot/wasm/
echo ""
echo "wwwroot/wasm/src/"
ls -la wwwroot/wasm/src/*/

echo ""
echo "🎉 WASM built and source code bundled successfully!"
