# Shoplazza Cart-Transform Function

This function automatically adjusts cart prices based on add-on selections. It runs server-side on Shoplazza's platform and processes cart data to apply add-on pricing.

## What It Does

- **Reads cart data** from Shoplazza's cart system
- **Scans for add-on metafields** (`cdh_shoplazza_addon`)
- **Calculates add-on costs** based on metafield configuration
- **Adds add-on line items** to the cart automatically
- **Updates cart totals** to include add-on pricing
- **Outputs modified cart** back to Shoplazza

## How It Works

1. **Input**: Cart data from stdin (Shoplazza's cart system)
2. **Processing**: Parse metafields, calculate costs, create add-on items
3. **Output**: Modified cart data to stdout (back to Shoplazza)

## Building the Function

### Prerequisites

- Node.js 18+ installed
- Javy compiler installed globally: `npm install -g @bytecodealliance/javy`

### Build Steps

```bash
# Install dependencies
npm install

# Build WASM file
npm run build

# Clean build artifacts
npm run clean
```

This creates `cart-transform.wasm` which can be uploaded to Shoplazza.

## Testing

```bash
# Test with sample cart data
npm test
```

## Deployment

1. Build the WASM file: `npm run build`
2. Upload `cart-transform.wasm` to Shoplazza via their Create Function API
3. Configure the function to trigger on cart operations

## Function Configuration

The function expects cart items with metafields in this format:

```json
{
  "properties": {
    "cdh_shoplazza_addon": "{\"isSelected\": true, \"config\": {\"title\": \"Premium Protection\", \"price\": 9.99, \"sku\": \"PROTECTION-001\"}}"
  }
}
```

## Add-on Configuration Fields

- `title`: Display name for the add-on
- `price`: Base price (will be multiplied by quantity)
- `sku`: Stock keeping unit
- `description`: Add-on description
- `weightGrams`: Weight for shipping calculations
- `isTaxable`: Whether the add-on is taxable
- `requiresShipping`: Whether the add-on requires shipping
- `imageUrl`: Image URL for the add-on

## Integration with Widget

This function works alongside the existing widget:
- **Widget**: Handles add-on selection UI and sets metafields
- **Function**: Handles automatic price calculation and cart adjustment

## Error Handling

- If the function encounters an error, it returns the original cart unchanged
- All errors are logged to console for debugging
- The function gracefully handles malformed metafield data
