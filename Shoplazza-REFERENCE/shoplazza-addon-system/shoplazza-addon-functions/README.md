# Shoplazza Add-On Azure Functions

## Overview

This Azure Functions app provides background order synchronization for the Shoplazza Add-On application. It runs scheduled tasks to sync orders from Shoplazza stores and process analytics data.

## Architecture

### .NET 8 Isolation Mode
- **Runtime**: .NET 8.0 with Azure Functions v4
- **Mode**: Isolated process mode for better performance
- **Hosting**: Azure Functions Consumption Plan (serverless)

### Functions

#### 1. ScheduledOrderSync
- **Trigger**: Timer (every 30 minutes)
- **Purpose**: Sync orders from all active merchants
- **Features**: Rate limiting, error handling, retry logic

#### 2. ProcessAnalytics
- **Trigger**: Timer (every 6 hours)
- **Purpose**: Calculate and cache analytics data
- **Features**: Daily and overall statistics

## Project Structure

```
src/ShoplazzaAddonFunctions/
├── Functions/                 # Azure Functions
│   ├── OrderSyncFunction.cs   # Timer-triggered order sync
│   └── AnalyticsFunction.cs   # Analytics processing
├── Services/                  # Business logic services
│   ├── ICacheService.cs       # Cache interface
│   ├── InMemoryCacheService.cs # In-memory cache implementation
│   ├── IRateLimiterService.cs # Rate limiting interface
│   ├── RateLimiterService.cs  # Rate limiting implementation
│   ├── IShoplazzaApiService.cs # Shoplazza API interface
│   ├── ShoplazzaApiService.cs # Shoplazza API implementation
│   ├── IOrderSyncService.cs   # Order sync interface
│   └── OrderSyncService.cs    # Order sync implementation
├── Models/                    # Data models
│   ├── Order.cs              # Order entity
│   ├── OrderLineItem.cs      # Order line item entity
│   ├── SyncState.cs          # Sync state entity
│   ├── Merchant.cs           # Merchant entity
│   ├── SyncOptions.cs        # Sync configuration
│   └── DTOs/                 # Data transfer objects
│       └── ShoplazzaOrderDto.cs
├── Data/                     # Data access
│   └── ApplicationDbContext.cs # Entity Framework context
├── Program.cs                # Application entry point
├── host.json                 # Functions host configuration
└── local.settings.json       # Local development settings

tests/ShoplazzaAddonFunctions.Tests/
├── CacheServiceTests.cs      # Unit tests for cache service
└── ...                       # Additional test files
```

## Features

### Order Synchronization
- **Automatic Sync**: Every 30 minutes for all active merchants
- **Rate Limiting**: Respects Shoplazza API limits (2 calls/second)
- **Error Handling**: Comprehensive error handling and retry logic
- **Data Validation**: Validates orders before processing
- **Add-on Detection**: Identifies orders with add-ons

### Caching
- **Flexible Cache**: Interface-based design supporting multiple providers
- **In-Memory Cache**: Default implementation for development
- **Expiration**: Configurable cache expiration times
- **Rate Limiting**: Cache-based rate limiting for API calls

### Analytics
- **Daily Statistics**: Calculated every 6 hours
- **Overall Statistics**: Merchant-level analytics
- **Caching**: Results cached for quick access
- **Metrics**: Orders, revenue, add-on revenue tracking

## Configuration

### Local Development
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings:DefaultConnection": "Data Source=shoplazza_addon_functions.db",
    "Cache:Provider": "InMemory",
    "Cache:DefaultExpirationMinutes": "60",
    "OrderSync:DefaultSyncDays": "30",
    "OrderSync:MaxOrdersPerSync": "1000",
    "OrderSync:SyncIntervalMinutes": "30"
  }
}
```

### Production Settings
- **Database**: Azure SQL Database or SQLite
- **Cache**: Redis (recommended) or In-Memory
- **Monitoring**: Application Insights
- **Scaling**: Automatic (Consumption Plan)

## Development

### Prerequisites
- .NET 8.0 SDK
- Azure Functions Core Tools
- Azure Storage Emulator (for local development)

### Local Development
```bash
# Build the project
dotnet build

# Run locally
func start

# Run tests
dotnet test
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~CacheServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Deployment

### Azure Functions Deployment
```bash
# Deploy to Azure
func azure functionapp publish shoplazza-addon-functions

# Deploy with specific settings
func azure functionapp publish shoplazza-addon-functions --publish-local-settings
```

### Environment Variables
- `ConnectionStrings:DefaultConnection`: Database connection string
- `Cache:Provider`: Cache provider (InMemory, Redis)
- `OrderSync:MaxOrdersPerSync`: Maximum orders per sync operation
- `RateLimit:ApiCallsPerSecond`: API rate limit per second

## Monitoring

### Application Insights
- **Performance Monitoring**: Function execution times
- **Error Tracking**: Exception logging and alerting
- **Custom Metrics**: Sync success rates, order counts
- **Dependencies**: Database and API call monitoring

### Logging
- **Structured Logging**: JSON format for easy parsing
- **Log Levels**: Configurable per component
- **Correlation IDs**: Request tracing across functions

## Security

### Authentication
- **OAuth Tokens**: Encrypted storage of Shoplazza access tokens
- **API Security**: Bearer token authentication for Shoplazza API
- **Rate Limiting**: Protection against API abuse

### Data Protection
- **Encryption**: Sensitive data encrypted at rest
- **HTTPS**: All external communications use HTTPS
- **Input Validation**: Comprehensive input validation

## Performance

### Optimization
- **Connection Pooling**: Database connection optimization
- **Caching**: Strategic caching of frequently accessed data
- **Async Operations**: Non-blocking I/O operations
- **Batch Processing**: Efficient bulk operations

### Scaling
- **Automatic Scaling**: Consumption plan auto-scaling
- **Cold Start Optimization**: .NET 8 isolation mode
- **Resource Management**: Efficient memory and CPU usage

## Troubleshooting

### Common Issues

#### Function Not Triggering
- Check timer trigger configuration
- Verify Azure Functions runtime
- Check application logs

#### Database Connection Issues
- Verify connection string
- Check database availability
- Review connection pooling settings

#### API Rate Limiting
- Monitor rate limit logs
- Adjust sync intervals
- Implement exponential backoff

### Debugging
```bash
# Enable verbose logging
func start --verbose

# View function logs
func azure functionapp logstream shoplazza-addon-functions

# Check function status
func azure functionapp list-functions shoplazza-addon-functions
```

## Contributing

### Development Guidelines
- Follow Microsoft C# coding conventions
- Include XML documentation for public APIs
- Write unit tests for new functionality
- Use conventional commit messages

### Testing Requirements
- Minimum 80% code coverage
- Unit tests for all services
- Integration tests for functions
- Performance benchmarks

## License

This project is part of the Shoplazza Add-On application and follows the same licensing terms. 