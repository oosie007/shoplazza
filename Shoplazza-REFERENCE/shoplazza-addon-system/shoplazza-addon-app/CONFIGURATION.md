# Configuration Reference

Quick reference for configuring the Shoplazza Add-On application.

## 🔧 Environment Variables

### Required Configuration

```bash
# Shoplazza App Credentials (Required)
SHOPLAZZA_CLIENT_ID=your_client_id_here
SHOPLAZZA_CLIENT_SECRET=your_client_secret_here
SHOPLAZZA_WEBHOOK_SECRET=your_webhook_secret_here

# App URLs (Required)
SHOPLAZZA_REDIRECT_URI=https://your-app.azurewebsites.net/api/auth/callback
SHOPLAZZA_APP_URL=https://your-app.azurewebsites.net/api/auth

# Database (Choose one)
## SQLite (Development/POC)
CONNECTION_STRING=Data Source=shoplazza_addon.db

## SQL Server (Production)
CONNECTION_STRING=Server=your-server;Database=ShoplazzaAddonApp;User Id=user;Password=password;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true

# Encryption (Required)
ENCRYPTION_KEY=your_32_character_encryption_key_here_12345
```

### Optional Configuration

```bash
# Logging
ASPNETCORE_ENVIRONMENT=Development
LOGGING_LEVEL=Information

# CORS (if needed)
CORS_ORIGINS=https://your-frontend-domain.com

# Performance
SESSION_TIMEOUT_MINUTES=30
```

---

## 📁 Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=shoplazza_addon.db"
  },
  "Shoplazza": {
    "ClientId": "your_client_id_here",
    "ClientSecret": "your_client_secret_here",
    "WebhookSecret": "your_webhook_secret_here",
    "RedirectUri": "https://your-app.azurewebsites.net/api/auth/callback",
    "AppUrl": "https://your-app.azurewebsites.net/api/auth",
    "RequiredScopes": [
      "read_products",
      "write_products",
      "read_orders",
      "write_orders"
    ]
  },
  "Encryption": {
    "Key": "your_32_character_encryption_key_here_12345"
  }
}
```

### appsettings.Development.json (SQLite)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=shoplazza_addon_dev.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### appsettings.Production.json (SQL Server)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=production-server;Database=ShoplazzaAddonApp;User Id=user;Password=password;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## 🏗️ Shoplazza App Configuration

### App Settings in Shoplazza Partner Dashboard

```yaml
App Name: "Optional Product Add-Ons"
App URL: https://your-app.azurewebsites.net/api/auth
Redirect URL: https://your-app.azurewebsites.net/api/auth/callback

Required Scopes:
  - read_products (Read product information)
  - write_products (Create/update products and variants)
  - read_orders (Read order information for analytics)
  - write_orders (Update orders if needed)

Webhook Endpoints:
  - App Uninstall: https://your-app.azurewebsites.net/api/webhooks/app/uninstalled
  - Product Create: https://your-app.azurewebsites.net/api/webhooks/products/create
  - Product Update: https://your-app.azurewebsites.net/api/webhooks/products/update
  - Product Delete: https://your-app.azurewebsites.net/api/webhooks/products/delete
  - Order Create: https://your-app.azurewebsites.net/api/webhooks/orders/create
```

---

## 🚀 Deployment Configuration

### Azure App Service Settings

```bash
# App Settings (Azure Portal)
SHOPLAZZA_CLIENT_ID=your_client_id
SHOPLAZZA_CLIENT_SECRET=your_client_secret
SHOPLAZZA_WEBHOOK_SECRET=your_webhook_secret
SHOPLAZZA_REDIRECT_URI=https://your-app.azurewebsites.net/api/auth/callback
SHOPLAZZA_APP_URL=https://your-app.azurewebsites.net/api/auth
ENCRYPTION_KEY=your_32_character_key

# Connection String (Azure Portal)
DefaultConnection=Server=tcp:your-server.database.windows.net,1433;Database=ShoplazzaAddonApp;User ID=username;Password=password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Docker Configuration (Optional)

```yaml
# docker-compose.yml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "80:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/shoplazza.db
      - Shoplazza__ClientId=${SHOPLAZZA_CLIENT_ID}
      - Shoplazza__ClientSecret=${SHOPLAZZA_CLIENT_SECRET}
      - Shoplazza__WebhookSecret=${SHOPLAZZA_WEBHOOK_SECRET}
    volumes:
      - ./data:/app/data
```

---

## 🎛️ Widget Configuration

### HTML Meta Tags (Auto-initialization)
```html
<meta name="shoplazza-addon-shop" content="your-store.myshoplazza.com">
<meta name="shoplazza-addon-product-id" content="123456">
<meta name="shoplazza-addon-api-endpoint" content="https://your-app.azurewebsites.net">
<meta name="shoplazza-addon-theme" content="default">
<meta name="shoplazza-addon-debug" content="false">
```

### JavaScript Configuration
```javascript
window.ShoplazzaAddonConfig = {
  shop: 'your-store.myshoplazza.com',
  apiEndpoint: 'https://your-app.azurewebsites.net',
  theme: 'default',
  primaryColor: '#007bff',
  debug: false
};
```

---

## 🔐 Security Configuration

### Required Security Headers

```yaml
# Azure App Service
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
```

### CORS Configuration (if needed)

```json
{
  "AllowedOrigins": [
    "https://your-store.myshoplazza.com",
    "https://admin.myshoplazza.com"
  ],
  "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
  "AllowedHeaders": [
    "Content-Type",
    "X-Shoplazza-Shop-Domain",
    "X-Shoplazza-Hmac-Sha256"
  ]
}
```

---

## 📊 Database Configuration

See `DATABASE_SETUP.md` for comprehensive database configuration guide.

### Quick SQLite Setup (Development)
```bash
# Automatic - just run the app
dotnet run
# Database file created: shoplazza_addon_dev.db
```

### Quick SQL Server Setup (Production)
```bash
# 1. Update connection string in appsettings.Production.json
# 2. Apply migrations
dotnet ef database update
```

---

## 🔍 Health Checks

### Built-in Health Check
```
GET /health
Response: {"status":"healthy","timestamp":"2024-08-04T12:00:00Z"}
```

### Custom Health Checks (Future)
- Database connectivity
- External API availability
- Memory usage
- Disk space (for SQLite)

---

## 📝 Logging Configuration

### Log Levels
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "ShoplazzaAddonApp": "Information"
    }
  }
}
```

### Log Destinations
- **Development**: Console
- **Production**: Azure Application Insights (recommended)
- **File**: Optional file logging

---

## ⚡ Performance Configuration

### Entity Framework
```json
{
  "EntityFramework": {
    "CommandTimeout": 30,
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "MaxRetryDelay": "00:00:30"
  }
}
```

### Session Configuration
```json
{
  "Session": {
    "IdleTimeout": "00:30:00",
    "CookieHttpOnly": true,
    "CookieSecure": true
  }
}
```

---

This configuration reference covers the essential settings needed to run the Shoplazza Add-On application in both development and production environments.