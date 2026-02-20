# Cart-Transform Function Deployment Guide

This guide explains how to deploy the cart-transform function to Shoplazza's platform.

## Prerequisites

- Node.js 18+ installed
- Access to Shoplazza's Function API
- Merchant account with appropriate permissions

## Step 1: Build the Function

```bash
# Navigate to the function directory
cd cart-transform-function

# Install dependencies
npm install

# Build the WASM file
npm run build

# Verify the build
ls -la cart-transform.wasm
```

## Step 2: Test the Function

```bash
# Run basic tests
npm test

# Run comprehensive tests
npm run test:comprehensive

# Run all tests
npm run test:all
```

All tests should pass before proceeding to deployment.

## Step 3: Upload to Shoplazza

### Using Shoplazza's Create Function API

```bash
# The function will be uploaded via Shoplazza's API
# You'll need to use their Create Function endpoint with:
# - Function name: "cart-transform-addon-pricing"
# - Function type: "cart-transform"
# - WASM file: cart-transform.wasm
# - Trigger: Cart operations (add to cart, update cart, checkout)
```

### API Request Example

```json
POST /api/v1/functions
{
  "name": "cart-transform-addon-pricing",
  "type": "cart-transform",
  "description": "Automatically applies add-on pricing to cart items",
  "wasm_file": "base64_encoded_wasm_content",
  "triggers": ["cart.add", "cart.update", "checkout.begin"],
  "settings": {
    "timeout": 5000,
    "memory_limit": "128MB"
  }
}
```

## Step 4: Configure Function Triggers

The function should be configured to trigger on:

- **Cart Add**: When items are added to cart
- **Cart Update**: When cart quantities are modified
- **Checkout Begin**: Before checkout process starts

## Step 5: Test in Production

1. **Add a product with add-on to cart**
2. **Verify add-on line item appears**
3. **Check cart totals are correct**
4. **Verify add-on appears in checkout**

## Function Behavior

### What It Does

- **Reads cart data** from Shoplazza's cart system
- **Scans for add-on metafields** (`cdh_shoplazza_addon`)
- **Calculates add-on costs** based on metafield configuration
- **Adds add-on line items** to the cart automatically
- **Updates cart totals** to include add-on pricing
- **Outputs modified cart** back to Shoplazza

### Expected Input Format

Cart items should have metafields in this format:

```json
{
  "properties": {
    "cdh_shoplazza_addon": "{\"isSelected\": true, \"config\": {\"title\": \"Premium Protection\", \"price\": 9.99, \"sku\": \"PROTECTION-001\"}}"
  }
}
```

### Output Format

The function returns the modified cart with:

- Original items (marked with `_has_addon: "true"`)
- Add-on line items (with `_addon_type: "cdh_shoplazza_addon"`)
- Updated cart totals
- Correct item counts

## Monitoring and Debugging

### Function Logs

Check Shoplazza's function logs for:
- Execution success/failure
- Processing time
- Error messages
- Cart modification details

### Common Issues

1. **Function not triggering**: Check trigger configuration
2. **Add-ons not appearing**: Verify metafield format
3. **Price calculations wrong**: Check add-on configuration data
4. **Function timeout**: Increase memory/timeout limits

## Rollback Plan

If issues arise:

1. **Disable the function** via Shoplazza's API
2. **Verify cart behavior** returns to normal
3. **Debug the issue** using function logs
4. **Redeploy** with fixes

## Performance Considerations

- **Function timeout**: Set to 5 seconds minimum
- **Memory limit**: 128MB should be sufficient
- **Execution frequency**: Only on cart operations
- **WASM size**: Optimized for fast loading

## Security Notes

- Function runs in isolated WASM environment
- No external network access
- Input validation prevents malformed data
- Error handling returns original cart on failure

## Support

For issues with:
- **Function deployment**: Check Shoplazza's API documentation
- **Function execution**: Review function logs
- **Cart behavior**: Verify metafield data format
- **Performance**: Monitor execution times and resource usage
