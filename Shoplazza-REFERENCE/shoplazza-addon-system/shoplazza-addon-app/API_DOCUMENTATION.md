# Shoplazza Add-On API Documentation

## Overview

The Shoplazza Add-On API enables merchants to configure optional product add-ons that customers can select when adding products to their cart. This API provides endpoints for OAuth authentication, product configuration, and webhook management.

**Base URL**: `https://your-app.azurewebsites.net`  
**API Version**: v1  
**Authentication**: OAuth 2.0 + HMAC Signature Verification

---

## 🔐 Authentication

### OAuth 2.0 Flow

The app uses Shoplazza's OAuth 2.0 implementation for merchant authentication.

#### 1. Authorization Request
```
GET /api/auth
```

**Description**: Initiates the OAuth flow by redirecting the merchant to Shoplazza's authorization server.

**Query Parameters**:
- `shop` (required): The shop domain (e.g., `example-store.myshoplazza.com`)

**Response**: Redirects to Shoplazza OAuth authorization page.

#### 2. Authorization Callback
```
POST /api/auth/callback
```

**Description**: Handles the OAuth callback from Shoplazza and exchanges the authorization code for an access token.

**Request Body**:
```json
{
  "code": "authorization_code_from_shoplazza",
  "shop": "example-store.myshoplazza.com",
  "state": "csrf_protection_token",
  "timestamp": "1234567890",
  "signature": "hmac_signature"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Installation completed successfully",
  "shop": "example-store.myshoplazza.com",
  "redirectUrl": "/dashboard"
}
```

#### 3. Check Installation Status
```
GET /api/auth/status?shop={shop_domain}
```

**Description**: Verifies if the app is properly installed and configured for a shop.

**Response**:
```json
{
  "isInstalled": true,
  "isActive": true,
  "hasValidToken": true,
  "shop": "example-store.myshoplazza.com",
  "installedAt": "2024-08-04T12:00:00Z"
}
```

---

## 📦 Product Management API

### List Product Add-Ons

```
GET /api/products
```

**Description**: Retrieves all product add-on configurations for the authenticated merchant.

**Headers**:
- `X-Shoplazza-Shop-Domain`: The shop domain
- `X-Shoplazza-Hmac-Sha256`: HMAC signature for verification

**Query Parameters**:
- `page` (optional): Page number (default: 1)
- `limit` (optional): Items per page (default: 50, max: 250)
- `isEnabled` (optional): Filter by enabled status (true/false)

**Response**:
```json
{
  "data": [
    {
      "id": 1,
      "productId": 123456789,
      "productTitle": "Premium T-Shirt",
      "productHandle": "premium-t-shirt",
      "isEnabled": true,
      "isActive": true,
      "addOn": {
        "title": "Insurance Protection",
        "description": "Protect your purchase with comprehensive coverage",
        "price": "USD 2.50",
        "priceCents": 250,
        "currency": "USD",
        "displayText": "Add Insurance Protection (+$2.50)",
        "sku": "INS-PROTECTION-001",
        "requiresShipping": false,
        "weightGrams": 0,
        "isTaxable": true,
        "imageUrl": null,
        "position": 1,
        "addOnVariantId": 987654321
      },
      "createdAt": "2024-08-04T12:00:00Z",
      "updatedAt": "2024-08-04T12:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 1,
    "pages": 1
  }
}
```

### Get Product Add-On

```
GET /api/products/{productId}
```

**Description**: Retrieves a specific product add-on configuration.

**Parameters**:
- `productId`: The Shoplazza product ID

**Query Parameters**:
- `shop`: The shop domain

**Response**:
```json
{
  "id": 1,
  "productId": 123456789,
  "productTitle": "Premium T-Shirt",
  "productHandle": "premium-t-shirt",
  "isEnabled": true,
  "isActive": true,
  "addOn": {
    "title": "Insurance Protection",
    "description": "Protect your purchase with comprehensive coverage",
    "price": "USD 2.50",
    "priceCents": 250,
    "currency": "USD",
    "displayText": "Add Insurance Protection (+$2.50)",
    "sku": "INS-PROTECTION-001",
    "requiresShipping": false,
    "weightGrams": 0,
    "isTaxable": true,
    "imageUrl": null,
    "position": 1,
    "addOnVariantId": 987654321
  },
  "createdAt": "2024-08-04T12:00:00Z",
  "updatedAt": "2024-08-04T12:30:00Z"
}
```

**Error Response** (404):
```json
{
  "error": "Product add-on not found",
  "productId": 123456789,
  "message": "No add-on configuration exists for this product"
}
```

### Create Product Add-On

```
POST /api/products
```

**Description**: Creates a new product add-on configuration.

**Request Body**:
```json
{
  "productId": 123456789,
  "addOn": {
    "title": "Insurance Protection",
    "description": "Protect your purchase with comprehensive coverage",
    "priceCents": 250,
    "currency": "USD",
    "displayText": "Add Insurance Protection (+$2.50)",
    "sku": "INS-PROTECTION-001",
    "requiresShipping": false,
    "weightGrams": 0,
    "isTaxable": true,
    "imageUrl": "https://example.com/insurance-icon.png",
    "position": 1
  }
}
```

**Response** (201):
```json
{
  "id": 1,
  "productId": 123456789,
  "productTitle": "Premium T-Shirt",
  "productHandle": "premium-t-shirt",
  "isEnabled": true,
  "isActive": true,
  "addOn": {
    "title": "Insurance Protection",
    "description": "Protect your purchase with comprehensive coverage",
    "price": "USD 2.50",
    "priceCents": 250,
    "currency": "USD",
    "displayText": "Add Insurance Protection (+$2.50)",
    "sku": "INS-PROTECTION-001",
    "requiresShipping": false,
    "weightGrams": 0,
    "isTaxable": true,
    "imageUrl": "https://example.com/insurance-icon.png",
    "position": 1,
    "addOnVariantId": 987654321
  },
  "createdAt": "2024-08-04T12:00:00Z",
  "updatedAt": "2024-08-04T12:00:00Z"
}
```

### Update Product Add-On

```
PUT /api/products/{productId}
```

**Description**: Updates an existing product add-on configuration.

**Request Body**: Same as Create Product Add-On

**Response**: Same as Get Product Add-On

### Delete Product Add-On

```
DELETE /api/products/{productId}
```

**Description**: Deletes a product add-on configuration.

**Response** (204): No content

---

## 🔔 Webhook API

### App Uninstall

```
POST /api/webhooks/app/uninstalled
```

**Description**: Handles app uninstall notifications from Shoplazza.

**Headers**:
- `X-Shoplazza-Hmac-Sha256`: HMAC signature for verification
- `X-Shoplazza-Shop-Domain`: Shop domain

**Request Body**:
```json
{
  "id": 12345,
  "app_id": 67890,
  "shop_id": 54321,
  "uninstalled_at": "2024-08-04T12:00:00Z"
}
```

### Product Create/Update

```
POST /api/webhooks/products/create
POST /api/webhooks/products/update
```

**Description**: Handles product creation and update notifications from Shoplazza.

**Request Body**:
```json
{
  "id": 123456789,
  "title": "Premium T-Shirt",
  "handle": "premium-t-shirt",
  "vendor": "Your Brand",
  "product_type": "Clothing",
  "created_at": "2024-08-04T12:00:00Z",
  "updated_at": "2024-08-04T12:30:00Z",
  "published_at": "2024-08-04T12:00:00Z",
  "variants": [
    {
      "id": 987654321,
      "title": "Default Title",
      "price": "29.99",
      "sku": "PREMIUM-TEE-001",
      "inventory_quantity": 100
    }
  ]
}
```

### Product Delete

```
POST /api/webhooks/products/delete
```

**Description**: Handles product deletion notifications from Shoplazza.

**Request Body**:
```json
{
  "id": 123456789,
  "deleted_at": "2024-08-04T12:00:00Z"
}
```

### Order Create

```
POST /api/webhooks/orders/create
```

**Description**: Handles new order notifications for analytics and tracking.

**Request Body**:
```json
{
  "id": 456789123,
  "order_number": "#1001",
  "email": "customer@example.com",
  "created_at": "2024-08-04T12:00:00Z",
  "currency": "USD",
  "total_price": "32.49",
  "line_items": [
    {
      "id": 789123456,
      "variant_id": 987654321,
      "title": "Premium T-Shirt",
      "quantity": 1,
      "price": "29.99",
      "properties": [
        {
          "name": "_addon",
          "value": "true"
        },
        {
          "name": "_addon_for",
          "value": "987654321"
        },
        {
          "name": "_addon_title",
          "value": "Insurance Protection"
        }
      ]
    }
  ]
}
```

### Cart Update

```
POST /api/webhooks/cart/update
```

**Description**: Handles cart update notifications for real-time analytics.

---

## 📊 Data Models

### ProductAddOn Model

```typescript
interface ProductAddOn {
  id: number;
  productId: number;
  productTitle: string;
  productHandle: string;
  isEnabled: boolean;
  isActive: boolean;
  addOn: AddOnConfiguration;
  createdAt: string;
  updatedAt: string;
}
```

### AddOnConfiguration Model

```typescript
interface AddOnConfiguration {
  title: string;
  description: string;
  price: string;           // Formatted price (e.g., "USD 2.50")
  priceCents: number;      // Price in cents
  currency: string;        // ISO currency code
  displayText: string;     // Text shown to customers
  sku: string;            // SKU for the add-on
  requiresShipping: boolean;
  weightGrams: number;
  isTaxable: boolean;
  imageUrl?: string;
  position: number;
  addOnVariantId?: number; // Shoplazza variant ID
}
```

### MerchantSettings Model

```typescript
interface MerchantSettings {
  id: number;
  shop: string;
  storeName?: string;
  storeEmail?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
}
```

---

## 🔒 Security & Validation

### HMAC Signature Verification

All webhook requests and sensitive API calls must include a valid HMAC signature in the `X-Shoplazza-Hmac-Sha256` header.

**Signature Calculation**:
```javascript
const crypto = require('crypto');
const signature = crypto
  .createHmac('sha256', webhookSecret)
  .update(requestBody, 'utf8')
  .digest('base64');
```

### Required Headers

For all authenticated requests:
- `X-Shoplazza-Shop-Domain`: The shop domain
- `X-Shoplazza-Hmac-Sha256`: HMAC signature
- `Content-Type: application/json`

---

## ⚠️ Error Handling

### Error Response Format

```json
{
  "error": "Error type",
  "message": "Human readable error message",
  "details": {
    "field": "validation_error_details"
  },
  "timestamp": "2024-08-04T12:00:00Z"
}
```

### HTTP Status Codes

- `200` - OK
- `201` - Created
- `204` - No Content
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `409` - Conflict
- `422` - Unprocessable Entity
- `500` - Internal Server Error

---

## 🚀 Rate Limiting

- **API Endpoints**: 1000 requests per hour per shop
- **Webhook Endpoints**: No rate limiting (but HMAC validation required)
- **OAuth Endpoints**: 100 requests per hour per IP

---

## 🔗 Useful Links

- **Swagger UI**: `/swagger` (when running in development)
- **Health Check**: `/health`
- **Database Setup**: See `DATABASE_SETUP.md`
- **Widget Integration**: See widget repository documentation

---

## 📞 Support

For API support and questions:
- **Email**: support@your-domain.com
- **Documentation**: This file
- **Issues**: GitHub repository issues