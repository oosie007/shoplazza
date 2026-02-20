# WASM Building and Deployment Guide

## Overview
This guide explains how the cart-transform WASM function is built and deployed to Shoplazza's platform during merchant app installation.

## WASM Building Process

### Prerequisites
- **Node.js 18+** installed on the build server
- **Javy compiler** installed globally: `npm install -g @bytecodealliance/javy`
- **cart-transform-function** directory with source code

### Build Process
```bash
# Navigate to cart-transform-function directory
cd cart-transform-function

# Install dependencies
npm install

# Build WASM file
npm run build

# Verify build output
ls -la cart-transform.wasm
```

### Build Script Details
The `package.json` contains the build script:
```json
{
  "scripts": {
    "build": "javy cart-transform.js -o cart-transform.wasm"
  }
}
```

**Javy converts JavaScript to WebAssembly (WASM)**, making it:
- **Faster execution** than interpreted JavaScript
- **Smaller file size** for network transfer
- **Native performance** on Shoplazza's platform

## WASM File Format

### Output
- **File**: `cart-transform.wasm`
- **Size**: Typically 50-200KB (optimized)
- **Format**: WebAssembly binary format
- **Compatibility**: Shoplazza's WASM runtime

### Content
The WASM file contains:
- **Compiled JavaScript logic** for cart processing
- **Add-on pricing calculations**
- **Cart manipulation functions**
- **Error handling and validation**

## Deployment to Shoplazza

### Function API Integration
Shoplazza's Function API accepts WASM files in **base64 encoded format**:

```csharp
// Convert WASM to base64 for API upload
var wasmBytes = await File.ReadAllBytesAsync("cart-transform.wasm");
var wasmBase64 = Convert.ToBase64String(wasmBytes);

// Send to Shoplazza Function API
var request = new FunctionRegistrationRequest
{
    Name = "cart-transform-addon-pricing",
    Type = "cart-transform",
    WasmBase64 = wasmBase64,
    Triggers = ["cart.add", "cart.update", "checkout.begin"]
};
```

### API Endpoints
```bash
# Create function
POST /api/v1/functions

# Activate function
POST /api/v1/functions/{functionId}/activate

# Get function status
GET /api/v1/functions/{functionId}

# Delete function
DELETE /api/v1/functions/{functionId}
```

## Build Server Requirements

### Environment Setup
```bash
# Install Node.js 18+
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Install Javy compiler
npm install -g @bytecodealliance/javy

# Verify installations
node --version  # Should be 18.x or higher
javy --version  # Should show Javy version
```

### Build Directory Structure
```
cart-transform-function/
├── package.json
├── cart-transform.js
├── test-cart-transform.js
├── test-scenarios.js
├── README.md
├── DEPLOYMENT.md
└── node_modules/ (after npm install)
```

## Automated Build Process

### Build Service Implementation
```csharp
public class CartTransformFunctionService : ICartTransformFunctionService
{
    public async Task<byte[]> BuildWasmAsync()
    {
        var wasmSourcePath = Path.Combine(Directory.GetCurrentDirectory(), "cart-transform-function");
        
        // Run npm install
        await RunNpmCommandAsync(wasmSourcePath, "install");
        
        // Run npm run build
        await RunNpmCommandAsync(wasmSourcePath, "run", "build");
        
        // Read generated WASM file
        var wasmPath = Path.Combine(wasmSourcePath, "cart-transform.wasm");
        if (!File.Exists(wasmPath))
        {
            throw new InvalidOperationException("WASM file not generated");
        }
        
        return await File.ReadAllBytesAsync(wasmPath);
    }
    
    private async Task RunNpmCommandAsync(string workingDirectory, params string[] args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = string.Join(" ", args),
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"npm command failed: {error}");
        }
    }
}
```

## Build Optimization

### WASM Size Optimization
```bash
# Javy optimization flags
javy cart-transform.js -o cart-transform.wasm --optimize

# Additional optimization options
javy cart-transform.js -o cart-transform.wasm \
  --optimize \
  --strip-debug \
  --compress
```

### Code Optimization
- **Remove unused functions** from JavaScript
- **Minimize dependencies** in package.json
- **Use efficient algorithms** for cart processing
- **Implement lazy loading** where possible

## Error Handling

### Build Failures
```csharp
try
{
    var wasmBytes = await _cartTransformFunctionService.BuildWasmAsync();
    // Continue with deployment
}
catch (Exception ex)
{
    _logger.LogError(ex, "WASM build failed");
    
    // Fallback to pre-built WASM
    var fallbackWasm = await GetPreBuiltWasmAsync();
    if (fallbackWasm != null)
    {
        _logger.LogInformation("Using pre-built WASM as fallback");
        wasmBytes = fallbackWasm;
    }
    else
    {
        throw new InvalidOperationException("WASM build failed and no fallback available");
    }
}
```

### Deployment Failures
```csharp
try
{
    var functionId = await _shoplazzaFunctionApiService.CreateFunctionAsync(merchant, request);
    // Store function ID and continue
}
catch (Exception ex)
{
    _logger.LogError(ex, "Function deployment failed");
    
    // Mark function as failed in database
    await _merchantService.UpdateFunctionStatusAsync(merchant.Id, FunctionStatus.Failed, ex.Message);
    
    // Continue with app installation but log the issue
    // The app can still work without the cart-transform function
}
```

## Monitoring and Validation

### Build Success Validation
```csharp
public async Task<bool> ValidateWasmAsync(byte[] wasmBytes)
{
    try
    {
        // Check file size (should be reasonable)
        if (wasmBytes.Length < 1000 || wasmBytes.Length > 1000000)
        {
            _logger.LogWarning("WASM file size seems unusual: {Size} bytes", wasmBytes.Length);
        }
        
        // Check file header (WASM files start with specific bytes)
        if (wasmBytes.Length < 4 || 
            wasmBytes[0] != 0x00 || wasmBytes[1] != 0x61 || 
            wasmBytes[2] != 0x73 || wasmBytes[3] != 0x6D)
        {
            _logger.LogError("Invalid WASM file header");
            return false;
        }
        
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating WASM file");
        return false;
    }
}
```

### Deployment Status Monitoring
```csharp
public async Task<FunctionStatus> MonitorFunctionStatusAsync(Merchant merchant, string functionId)
{
    try
    {
        var status = await _shoplazzaFunctionApiService.GetFunctionStatusAsync(merchant, functionId);
        
        // Update local database
        await _merchantService.UpdateFunctionStatusAsync(merchant.Id, status);
        
        return status;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error monitoring function status");
        return FunctionStatus.Unknown;
    }
}
```

## Security Considerations

### WASM File Security
- **Validate WASM content** before deployment
- **Check file integrity** using checksums
- **Limit file size** to prevent abuse
- **Scan for malicious code** patterns

### API Security
- **Use HTTPS** for all API communications
- **Implement rate limiting** for build requests
- **Validate merchant permissions** before deployment
- **Log all deployment activities** for audit

## Performance Considerations

### Build Performance
- **Parallel builds** for multiple merchants (if needed)
- **Build caching** for identical source code
- **Incremental builds** when possible
- **Background processing** for non-critical builds

### Deployment Performance
- **Async deployment** to avoid blocking app installation
- **Deployment queuing** for high-volume scenarios
- **Status polling** instead of blocking calls
- **Fallback mechanisms** for failed deployments

## Troubleshooting

### Common Build Issues
```bash
# Javy not found
npm install -g @bytecodealliance/javy

# Node.js version too old
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -

# Permission issues
sudo chown -R $USER:$USER cart-transform-function/
```

### Common Deployment Issues
```bash
# API authentication failed
# Check Shoplazza API credentials and permissions

# WASM file too large
# Optimize JavaScript code and remove unused functions

# Function creation timeout
# Increase API timeout settings and implement retry logic
```

## Success Metrics

### Build Metrics
- **Build success rate**: >95%
- **Build time**: <30 seconds
- **WASM file size**: <200KB
- **Build error rate**: <5%

### Deployment Metrics
- **Deployment success rate**: >90%
- **Deployment time**: <60 seconds
- **Function activation rate**: >95%
- **Error recovery rate**: >80%

## Conclusion

The WASM building and deployment process is designed to be:
- **Automated** - No manual intervention required
- **Reliable** - Fallback mechanisms for failures
- **Fast** - Optimized builds and deployments
- **Secure** - Validation and security checks
- **Monitorable** - Comprehensive logging and status tracking

This ensures that every merchant gets a working cart-transform function automatically when they install the app, providing seamless add-on pricing functionality from day one.
