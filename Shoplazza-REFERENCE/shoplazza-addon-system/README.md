# Shoplazza Add-On System

A comprehensive solution that enables Shoplazza merchants to offer optional product add-ons (like protection plans) with customer-facing toggles.

## System Overview

The Shoplazza Add-On System consists of two main components:

1. **[Public App](./shoplazza-addon-app/)** (.NET 8.0) - Backend API and merchant dashboard
2. **[Frontend Widget](./shoplazza-addon-widget/)** (JavaScript) - Customer-facing toggle interface

## Architecture

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

## Features

### For Merchants
- **Easy Installation** - One-click app installation from Shoplazza App Store
- **Product Configuration** - Select which products can have add-ons
- **Flexible Pricing** - Set custom prices for each add-on
- **Real-time Analytics** - Track add-on performance and revenue
- **Theme Compatibility** - Works with any Shoplazza theme

### For Customers
- **Intuitive Toggles** - Clear add-on selection interface
- **Real-time Pricing** - See updated prices immediately
- **Cart Integration** - Add-ons seamlessly added to cart
- **Mobile Optimized** - Responsive design for all devices
- **Accessibility** - WCAG compliant interface

## Project Structure

```
shoplazza-addon-system/
├── shoplazza-addon-app/          # .NET 8.0 Public App
│   ├── Controllers/              # API endpoints
│   ├── Services/                 # Business logic
│   ├── Models/                   # Data models
│   ├── Data/                     # Entity Framework
│   ├── Middleware/               # Custom middleware
│   └── wwwroot/                  # Static assets
├── shoplazza-addon-widget/       # JavaScript Widget
│   ├── src/                      # Source code
│   │   ├── core/                 # Core widget logic
│   │   ├── ui/                   # UI components
│   │   ├── cart/                 # Cart integration
│   │   └── utils/                # Utilities
│   ├── tests/                    # Test files
│   └── dist/                     # Built files
└── docs/                         # Documentation
    ├── execution-plan.md
    ├── technical-architecture.md
    └── development-guidelines.md
```

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ and npm
- Shoplazza Partner Account
- Azure Account (for deployment)

### Setup

1. **Clone the repositories**
   ```bash
   cd shoplazza-addon-app
   git clone <app-repo-url> .
   
   cd ../shoplazza-addon-widget
   git clone <widget-repo-url> .
   ```

2. **Setup the .NET App**
   ```bash
   cd shoplazza-addon-app
   dotnet restore
   dotnet run
   ```

3. **Setup the Widget**
   ```bash
   cd shoplazza-addon-widget
   npm install
   npm run build
   ```

## Development Workflow

### Git Strategy
- Two separate repositories for independent versioning
- Feature branches for all development
- Code review required for all merges
- Conventional commit messages

### Testing
- Unit tests for all business logic
- Integration tests for API endpoints
- End-to-end tests for widget functionality

### Deployment
- Azure App Service for the .NET app
- CDN for widget distribution
- CI/CD pipeline with automated testing

## Business Use Cases

### Primary Use Case: Protection Plans
- **Example**: "Add Cover It Protection (+$1.00)"
- **Benefit**: Additional revenue stream for merchants
- **Customer Value**: Peace of mind for purchases

### Additional Use Cases
- **Extended Warranties** - Additional coverage periods
- **Gift Wrapping** - Premium packaging options
- **Express Shipping** - Upgrade delivery options
- **Installation Services** - Professional setup
- **Accessories** - Related product suggestions

## Technical Highlights

### Security
- OAuth 2.0 authentication with Shoplazza
- HMAC verification for webhooks
- Input validation and sanitization
- HTTPS enforcement

### Performance
- Lightweight widget (<50KB gzipped)
- Lazy loading of components
- Efficient cart API integration
- Optimized for mobile devices

### Scalability
- Stateless architecture
- Database connection pooling
- CDN for global distribution
- Horizontal scaling support

## Documentation

- **[Execution Plan](../execution-plan.md)** - Development roadmap and phases
- **[Technical Architecture](../technical-architecture.md)** - Detailed system design
- **[Development Guidelines](../development-guidelines.md)** - Coding standards and practices

## Contributing

1. Follow the development guidelines
2. Create feature branches for all changes
3. Write tests for new functionality
4. Submit pull requests for code review
5. Update documentation as needed

## Support

For technical support and questions:
- Check the documentation first
- Review troubleshooting guides
- Submit issues with detailed reproduction steps
- Contact the development team

## License

Copyright © 2025 - Shoplazza Add-On System

---

**Current Status**: Phase 1 Complete - Project Setup & Infrastructure ✅