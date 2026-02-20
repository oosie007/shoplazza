# Technical Architecture Document

## System Overview

The Shoplazza Optional Add-On system consists of two main components:
1. **Public App** (.NET 8.0) - Backend API and merchant dashboard
2. **Frontend Widget** (JavaScript) - Customer-facing toggle interface

## Architecture Diagram

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Merchant      │    │  Shoplazza      │    │   Customer      │
│   Dashboard     │    │   Store API     │    │   Storefront    │
└─────┬───────────┘    └─────┬───────────┘    └─────┬───────────┘
      │                      │                      │
      │ Configure            │ Webhooks             │ Interacts
      │ Add-ons             │ Product Data         │ with Widget
      │                      │                      │
      v                      v                      v
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   .NET 8.0      │◄──►│   OAuth 2.0     │    │   JavaScript    │
│   Public App    │    │   HMAC Auth     │    │   Widget        │
│   (Backend)     │    │   API Client    │    │   (Frontend)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
      │                                             │
      │ Serves Widget Script                        │
      │◄────────────────────────────────────────────┘
      │
      v
┌─────────────────┐
│   Azure App     │
│   Service       │
│   (Hosting)     │
└─────────────────┘
```

## Component Details

### 1. Public App (.NET 8.0)

#### Project Structure
```
ShoplazzaAddonApp/
├── Controllers/
│   ├── AuthController.cs           # OAuth flow, HMAC verification
│   ├── DashboardController.cs      # Merchant UI endpoints
│   ├── ConfigurationController.cs  # Add-on setup API
│   ├── WebhookController.cs        # Shoplazza event handlers
│   ├── WidgetController.cs         # Dynamic script delivery
│   └── HealthController.cs         # Health checks
├── Services/
│   ├── IShoplazzaAuthService.cs    # Authentication interface
│   ├── ShoplazzaAuthService.cs     # OAuth & HMAC implementation
│   ├── IShoplazzaApiClient.cs      # API client interface
│   ├── ShoplazzaApiClient.cs       # Shoplazza API wrapper
│   ├── IProductService.cs          # Product operations interface
│   ├── ProductService.cs           # Product management logic
│   ├── IConfigurationService.cs    # Configuration interface
│   ├── ConfigurationService.cs     # Add-on configuration logic
│   ├── IWidgetService.cs           # Widget generation interface
│   ├── WidgetService.cs            # Dynamic script generation
│   └── IWebhookService.cs          # Webhook processing interface
├── Models/
│   ├── Auth/
│   │   ├── ShoplazzaAuthRequest.cs
│   │   ├── ShoplazzaAuthResponse.cs
│   │   └── HmacValidationModel.cs
│   ├── Configuration/
│   │   ├── ProductConfiguration.cs
│   │   ├── AddOnConfiguration.cs
│   │   ├── MerchantSettings.cs
│   │   └── WidgetSettings.cs
│   ├── Api/
│   │   ├── ShoplazzaProduct.cs
│   │   ├── ShoplazzaVariant.cs
│   │   └── WebhookPayload.cs
│   └── Dto/
│       ├── ConfigurationDto.cs
│       ├── ProductDto.cs
│       └── AddOnDto.cs
├── Data/
│   ├── IRepository.cs              # Generic repository interface
│   ├── Repository.cs               # Generic repository implementation
│   ├── ApplicationDbContext.cs     # Entity Framework context
│   └── Entities/
│       ├── Merchant.cs
│       ├── ProductAddOn.cs
│       └── Configuration.cs
├── Middleware/
│   ├── HmacValidationMiddleware.cs # HMAC signature validation
│   ├── ErrorHandlingMiddleware.cs  # Global error handling
│   └── RateLimitingMiddleware.cs   # API rate limiting
├── wwwroot/
│   ├── js/
│   │   └── dashboard.js            # Dashboard frontend
│   ├── css/
│   │   └── dashboard.css           # Dashboard styling
│   └── views/
│       ├── dashboard.html          # Main dashboard
│       ├── configuration.html      # Configuration page
│       └── onboarding.html         # Welcome page
└── Configuration/
    ├── appsettings.json
    ├── appsettings.Development.json
    └── appsettings.Production.json
```

#### Key Services

##### ShoplazzaAuthService
- OAuth 2.0 flow implementation
- HMAC signature verification
- Session token management
- Store authentication state

##### ShoplazzaApiClient
- HTTP client wrapper for Shoplazza APIs
- Authentication header injection
- Rate limiting and retry logic
- Error handling and logging

##### ProductService
- Fetch merchant products from Shoplazza
- Manage add-on configurations
- SKU creation and management
- Product data synchronization

##### ConfigurationService
- Store merchant preferences
- Manage add-on settings per product
- Handle enable/disable states
- Configuration validation

##### WidgetService
- Generate dynamic JavaScript for each merchant
- Inject configuration data
- Version management
- Cache optimization

### 2. Frontend Widget (JavaScript)

#### Project Structure
```
ShoplazzaAddonWidget/
├── src/
│   ├── core/
│   │   ├── Widget.js              # Main widget class
│   │   ├── ConfigLoader.js        # Configuration loading
│   │   ├── ProductDetector.js     # Product page detection
│   │   └── EventManager.js        # Event handling
│   ├── ui/
│   │   ├── ToggleComponent.js     # Add-on toggle UI
│   │   ├── PriceUpdater.js        # Price display updates
│   │   └── StyleManager.js        # CSS injection
│   ├── cart/
│   │   ├── CartManager.js         # Cart operations
│   │   ├── AddOnManager.js        # Add-on specific logic
│   │   └── StateManager.js        # State persistence
│   ├── utils/
│   │   ├── DomUtils.js            # DOM manipulation helpers
│   │   ├── ApiClient.js           # Communication with app
│   │   └── Logger.js              # Error logging
│   └── styles/
│       ├── widget.css             # Base widget styles
│       └── themes/
│           ├── default.css
│           └── minimal.css
├── dist/                          # Built files
├── tests/
│   ├── unit/
│   └── integration/
├── package.json
├── webpack.config.js
└── README.md
```

#### Widget Architecture

##### Core Components

1. **Widget.js** - Main entry point
   - Initialize widget on page load
   - Detect product pages
   - Load configuration
   - Render appropriate UI

2. **ToggleComponent.js** - Interactive toggle
   - Render add-on selection UI
   - Handle user interactions
   - Update visual state
   - Communicate state changes

3. **CartManager.js** - Cart operations
   - Add/remove add-ons from cart
   - Sync with Shoplazza cart API
   - Handle cart state changes
   - Manage cart persistence

4. **PriceUpdater.js** - Price calculations
   - Update displayed prices
   - Show add-on costs
   - Handle currency formatting
   - Animate price changes

## Data Flow

### 1. Merchant Configuration Flow
```
Merchant → Dashboard → Configuration API → Database
    ↓
Widget Configuration Update → CDN/Cache Invalidation
```

### 2. Customer Interaction Flow
```
Customer → Product Page → Widget Load → Configuration Fetch
    ↓
Toggle Interaction → Cart API → Price Update → State Persist
    ↓
Add to Cart → Cart API → Add-on Addition → Checkout Flow
```

### 3. Webhook Processing Flow
```
Shoplazza → Webhook Endpoint → Validation → Processing
    ↓
Database Update → Cache Invalidation → Widget Update
```

## Security Considerations

### Authentication & Authorization
- OAuth 2.0 for merchant authentication
- HMAC-SHA256 for webhook verification
- API key management for internal calls
- Session management with secure tokens

### Data Protection
- Encrypt sensitive configuration data
- Validate all input parameters
- Sanitize user-generated content
- Implement rate limiting

### Communication Security
- HTTPS for all communications
- CORS policies for widget loading
- CSP headers for XSS protection
- Input validation and sanitization

## Performance Optimization

### Backend Optimizations
- Async/await for all I/O operations
- Caching for frequently accessed data
- Connection pooling for database
- Compression for API responses

### Frontend Optimizations
- Lazy loading of widget components
- Minimal DOM manipulation
- Event delegation for performance
- CSS/JS minification and compression

### Caching Strategy
- CDN for widget distribution
- Browser caching for static assets
- API response caching
- Configuration caching

## Scalability Considerations

### Horizontal Scaling
- Stateless application design
- Database connection pooling
- Load balancer configuration
- Session state externalization

### Performance Monitoring
- Application Performance Monitoring (APM)
- Database query optimization
- API response time monitoring
- Error rate tracking

## Deployment Architecture

### Azure App Service Configuration
```
Production Environment:
├── App Service Plan (Standard S1+)
├── Application Insights (Monitoring)
├── Azure SQL Database (Configuration data)
├── Azure Cache for Redis (Session/Cache)
├── Azure CDN (Widget distribution)
└── Azure Key Vault (Secrets management)
```

### Environment Configuration
- Development: Local SQL Server + In-memory cache
- Staging: Azure SQL + Redis (shared)
- Production: Azure SQL + Redis (dedicated)

## API Design

### RESTful Endpoints
```
Authentication:
POST /api/auth                    # Initial OAuth
GET  /api/auth/callback          # OAuth callback

Configuration:
GET    /api/products             # List merchant products
POST   /api/products/{id}/addon  # Configure add-on
PUT    /api/products/{id}/addon  # Update add-on
DELETE /api/products/{id}/addon  # Remove add-on

Widget:
GET /api/widget.js               # Dynamic widget script
GET /api/config/{storeId}        # Widget configuration

Webhooks:
POST /api/webhooks/products      # Product update webhook
POST /api/webhooks/orders        # Order webhook
```

### Response Formats
- JSON for all API responses
- Consistent error response structure
- Proper HTTP status codes
- CORS headers for cross-origin requests

This architecture provides a solid foundation for building a scalable, secure, and maintainable Shoplazza add-on system.