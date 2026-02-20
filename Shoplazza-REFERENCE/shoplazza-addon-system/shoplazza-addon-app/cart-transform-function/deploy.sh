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
rm -f cart-transform.wasm

# Build WASM for current platform (development)
echo "🔨 Building WASM for current platform (development)..."
CURRENT_PLATFORM=$(detect_platform)
JAVY_PATH=$(get_javy_path "$CURRENT_PLATFORM")
echo "✅ Using Javy CLI at: $JAVY_PATH"
"$JAVY_PATH" build cart-transform.js -o cart-transform.wasm -C source-compression=y

# Download Linux Javy CLI for production deployment
echo "🔨 Preparing Linux WASM build tools..."
LINUX_PLATFORM="x86_64-linux"
LINUX_JAVY_PATH=$(get_javy_path "$LINUX_PLATFORM")
echo "✅ Linux Javy CLI downloaded at: $LINUX_JAVY_PATH"

# Note: Linux binary cannot run on macOS, but is ready for deployment
echo "ℹ️  Linux Javy CLI downloaded and ready for Azure deployment"
echo "ℹ️  On Linux, run: $LINUX_JAVY_PATH build cart-transform.js -o cart-transform-linux.wasm -C source-compression=y"

# Check if development build was successful
if [ ! -f "cart-transform.wasm" ]; then
    echo "❌ Development build failed - cart-transform.wasm not found"
    exit 1
fi

# Get file size
DEV_WASM_SIZE=$(du -h cart-transform.wasm | cut -f1)
echo "✅ Development build successful!"
echo "   Development (${CURRENT_PLATFORM}): cart-transform.wasm (${DEV_WASM_SIZE})"
echo "   Linux tools: Ready for Azure deployment"

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
echo "- cart-transform.wasm (development - ${CURRENT_PLATFORM})"
echo "- cart-transform.js (source code)"
echo "- cart-transform.js (source code)"
echo "- README.md (documentation)"
echo "- test-cart-transform.js (testing)"
echo "- deploy.sh (this script)"
echo ""
echo "Deployment preparation:"
echo "- Development WASM: cart-transform.wasm (ready for testing)"
echo "- Linux Javy CLI: javy-cli/javy-x86_64-linux (ready for Azure)"
echo "- On Azure Linux: run: javy-cli/javy-x86_64-linux build cart-transform.js -o cart-transform-linux.wasm -C source-compression=y"
