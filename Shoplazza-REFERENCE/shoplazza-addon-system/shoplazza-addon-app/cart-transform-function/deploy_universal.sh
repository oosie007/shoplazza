#!/bin/bash

# Shoplazza Cart-Transform Function Deployment Script
# This script builds the function and prepares it for deployment to Shoplazza

set -e

echo "🚀 Shoplazza Cart-Transform Function Deployment"
echo "================================================"

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js 18+ first."
    exit 1
fi

# Check Node.js version
NODE_VERSION=$(node -v | cut -d'v' -f2 | cut -d'.' -f1)
if [ "$NODE_VERSION" -lt 18 ]; then
    echo "❌ Node.js version 18+ is required. Current version: $(node -v)"
    exit 1
fi

echo "✅ Node.js version: $(node -v)"

# Function to detect platform and architecture
detect_platform() {
    local platform=""
    local arch=""
    
    case "$(uname -s)" in
        Darwin*)    platform="macos" ;;
        Linux*)     platform="linux" ;;
        CYGWIN*|MINGW32*|MSYS*|MINGW*) platform="windows" ;;
        *)          platform="unknown" ;;
    esac
    
    case "$(uname -m)" in
        x86_64)     arch="x86_64" ;;
        arm64|aarch64) arch="arm" ;;
        *)          arch="unknown" ;;
    esac
    
    echo "${arch}-${platform}"
}

# Function to download Javy CLI for a specific platform
download_javy() {
    local platform_arch=$1
    local javy_version="v5.0.4"
    local javy_dir="javy-cli"
    local javy_binary="javy-${platform_arch}"
    
    echo "📦 Downloading Javy CLI for ${platform_arch}..."
    
    # Create javy-cli directory if it doesn't exist
    mkdir -p "$javy_dir"
    
    # Download URL
    local download_url="https://github.com/bytecodealliance/javy/releases/download/${javy_version}/javy-${platform_arch}-${javy_version}.gz"
    
    echo "🔗 Downloading from: ${download_url}"
    
    # Download and extract
    if curl -L -o "${javy_dir}/${javy_binary}.gz" "$download_url"; then
        echo "✅ Download successful"
        gunzip -f "${javy_dir}/${javy_binary}.gz"
        chmod +x "${javy_dir}/${javy_binary}"
        echo "✅ Javy CLI extracted and made executable"
    else
        echo "❌ Failed to download Javy CLI for ${platform_arch}"
        exit 1
    fi
}

# Function to get Javy CLI path for a specific platform
get_javy_path() {
    local platform_arch=$1
    local javy_dir="javy-cli"
    local javy_binary="javy-${platform_arch}"
    local javy_path="${javy_dir}/${javy_binary}"
    
    if [ ! -f "$javy_path" ] || [ ! -x "$javy_path" ]; then
        download_javy "$platform_arch"
    fi
    
    echo "$javy_path"
}

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -f cart-transform-universal.wasm

# Build universal WASM file (works on all platforms)
echo "🔨 Building universal WASM file..."
CURRENT_PLATFORM=$(detect_platform)
JAVY_PATH=$(get_javy_path "$CURRENT_PLATFORM")
echo "✅ Using Javy CLI at: $JAVY_PATH"
"$JAVY_PATH" build cart-transform.js -o cart-transform-universal.wasm -C source-compression=y

echo "ℹ️  Universal WASM built - works on all platforms (macOS, Linux, Windows)"
echo "ℹ️  Ready for deployment to Azure App Service and other platforms"

# Check if universal build was successful
if [ ! -f "cart-transform-universal.wasm" ]; then
    echo "❌ Universal build failed - cart-transform-universal.wasm not found"
    exit 1
fi

# Get file size
UNIVERSAL_WASM_SIZE=$(du -h cart-transform-universal.wasm | cut -f1)
echo "✅ Universal build successful!"
echo "   Universal WASM: cart-transform-universal.wasm (${UNIVERSAL_WASM_SIZE})"
echo "   Platform: ${CURRENT_PLATFORM} (but WASM works everywhere)"

# Test the function
echo "🧪 Testing the function..."
node test-cart-transform.js

echo ""
echo "🎉 Deployment preparation complete!"
echo ""
echo "Next steps:"
echo "1. Upload cart-transform.wasm to Shoplazza via their Create Function API"
echo "2. Configure the function to trigger on cart operations"
echo "3. Test the function with real cart data"
echo ""
echo "Function details:"
echo "- Input: Cart data from stdin"
echo "- Output: Modified cart data to stdout"
echo "- Triggers: Cart operations (add to cart, update cart, checkout)"
echo "- Purpose: Automatic add-on pricing and cart adjustment"
echo ""
echo "Files created:"
echo "- cart-transform-universal.wasm (universal - works on all platforms)"
echo "- cart-transform.js (source code)"
echo "- cart-transform.js (source code)"
echo "- README.md (documentation)"
echo "- test-cart-transform.js (testing)"
echo "- deploy_universal.sh (this script)"
echo ""
echo "Universal deployment:"
echo "- WASM file: cart-transform-universal.wasm (ready for all platforms)"
echo "- Azure App Service: Deploy this file directly"
echo "- No additional building needed on Azure"
echo "- Works on macOS, Linux, Windows, ARM, x86_64"
