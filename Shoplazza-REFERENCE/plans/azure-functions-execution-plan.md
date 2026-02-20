# Azure Functions Execution Plan

## Executive Summary

This document outlines the detailed execution plan for creating a **separate Azure Functions app** for order synchronization, using **.NET 8 isolation mode**. This will be a completely new application that works alongside our existing web app.

## Azure Functions Research & Architecture

### 🎯 **Azure Functions .NET 8 Isolation Mode**

#### **Why .NET 8 Isolation Mode?**
- **Performance**: Better cold start performance
- **Memory Efficiency**: Lower memory footprint
- **Security**: Process isolation from the runtime
- **Flexibility**: Full control over dependencies
- **Future-Proof**: Microsoft's recommended approach

#### **Key Benefits:**
```csharp
// Isolation mode provides:
// - Process-level isolation
// - Custom dependency injection
// - Full .NET 8 features
// - Better performance
// - Smaller deployment size
```

## Project Structure & Architecture

### 🏗️ **New Azure Functions App Structure**

```
shoplazza-addon-functions/
├── src/
│   ├── ShoplazzaAddonFunctions/
│   │   ├── Program.cs                    # Host configuration
│   │   ├── Functions/
│   │   │   ├── OrderSyncFunction.cs      # Timer-triggered sync
│   │   │   ├── AnalyticsFunction.cs      # Analytics processing
│   │   │   └── CleanupFunction.cs        # Data cleanup
│   │   ├── Services/
│   │   │   ├── IOrderSyncService.cs      # Shared interface
│   │   │   ├── OrderSyncService.cs       # Core sync logic
│   │   │   ├── ICacheService.cs          # Cache interface
│   │   │   ├── InMemoryCacheService.cs   # In-memory cache
│   │   │   ├── FileSystemCacheService.cs # File system cache
│   │   │   └── IRateLimiterService.cs    # Rate limiting
│   │   ├── Models/
│   │   │   ├── Order.cs                  # Order entity
│   │   │   ├── OrderLineItem.cs          # Line item entity
│   │   │   ├── SyncState.cs              # Sync state entity
│   │   │   └── DTOs/                     # Data transfer objects
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs   # EF Core context
│   │   │   └── Repositories/             # Data access layer
│   │   └── Shared/                       # Shared libraries
│   │       ├── DatabaseConfiguration.cs  # DB provider logic
│   │       └── Extensions/               # Extension methods
│   └── ShoplazzaAddonFunctions.Tests/    # Unit tests
├── host.json                             # Functions host config
├── local.settings.json                   # Local development
├── ShoplazzaAddonFunctions.csproj        # Project file
└── README.md                             # Documentation
```

### 🔗 **Shared Library Strategy**

#### **Option 1: Shared Class Library (Recommended)**
```
shoplazza-addon-system/
├── shoplazza-addon-app/                  # Existing web app
├── shoplazza-addon-functions/            # New functions app
└── shared/
    ├── ShoplazzaAddon.Shared/
    │   ├── Services/
    │   │   ├── IOrderSyncService.cs
    │   │   ├── ICacheService.cs
    │   │   └── IRateLimiterService.cs
    │   ├── Models/
    │   │   ├── Order.cs
    │   │   ├── OrderLineItem.cs
    │   │   └── SyncState.cs
    │   ├── Data/
    │   │   └── ApplicationDbContext.cs
    │   └── Shared.csproj
    └── README.md
```

## Detailed Implementation Plan

### **Phase 1: Project Setup & Foundation**

#### **1.1 Create New Git Repository**
```bash
# Create new repository for functions app
mkdir shoplazza-addon-functions
cd shoplazza-addon-functions
git init
git checkout -b feature/azure-functions-setup
```

#### **1.2 Scaffold Azure Functions Project**
```bash
# Create .NET 8 Functions project with isolation mode
dotnet new azurefunctions --name ShoplazzaAddonFunctions --framework net8.0 --worker-runtime dotnet-isolated

# Add required packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.Extensions.Http
dotnet add package Newtonsoft.Json
dotnet add package Microsoft.ApplicationInsights.WorkerService
```

#### **1.3 Configure Isolation Mode**
```csharp
// Program.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Add dependency injection
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Add database context
        services.AddDbContext<ApplicationDbContext>();
        
        // Add services
        services.AddScoped<IOrderSyncService, OrderSyncService>();
        services.AddScoped<ICacheService, InMemoryCacheService>();
        services.AddScoped<IRateLimiterService, RateLimiterService>();
        
        // Add HTTP client
        services.AddHttpClient();
    })
    .Build();

host.Run();
```

### **Phase 2: Data Models & Database**

#### **2.1 Create Order Entities**
```csharp
// Models/Order.cs
public class Order
{
    public long Id { get; set; }
    public int MerchantId { get; set; }
    public long ShoplazzaOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal AddOnRevenue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FinancialStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? Source { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public bool HasAddOns { get; set; }
    
    public virtual Merchant Merchant { get; set; } = null!;
    public virtual ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();
}
```

### **Phase 3: Core Services Implementation**

#### **3.1 Cache Service Interface & Implementations**
```csharp
// Services/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<long> IncrementAsync(string key, long value = 1);
}

// Services/InMemoryCacheService.cs
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryCacheService> _logger;

    public InMemoryCacheService(IMemoryCache memoryCache, ILogger<InMemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        return _memoryCache.Get<T>(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration;
        
        _memoryCache.Set(key, value, options);
        _logger.LogDebug("Cached value for key: {Key}", key);
    }
}
```

### **Phase 4: Azure Functions Implementation**

#### **4.1 Timer-Triggered Order Sync Function**
```csharp
// Functions/OrderSyncFunction.cs
public class OrderSyncFunction
{
    private readonly IOrderSyncService _orderSyncService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderSyncFunction> _logger;

    public OrderSyncFunction(
        IOrderSyncService orderSyncService,
        ApplicationDbContext context,
        ILogger<OrderSyncFunction> logger)
    {
        _orderSyncService = orderSyncService;
        _context = context;
        _logger = logger;
    }

    [Function("ScheduledOrderSync")]
    public async Task Run(
        [TimerTrigger("0 */30 * * * *")] TimerInfo myTimer) // Every 30 minutes
    {
        _logger.LogInformation("Scheduled order sync started at: {Time}", DateTime.Now);
        
        try
        {
            // Get all active merchants
            var activeMerchants = await _context.Merchants
                .Where(m => m.IsActive && m.AccessToken != null)
                .ToListAsync();

            _logger.LogInformation("Found {MerchantCount} active merchants to sync", activeMerchants.Count);

            foreach (var merchant in activeMerchants)
            {
                try
                {
                    var options = new SyncOptions
                    {
                        Since = DateTime.UtcNow.AddHours(-2), // Last 2 hours
                        Limit = 100,
                        OnlyPaidOrders = true,
                        MaxRetries = 3,
                        RetryDelay = TimeSpan.FromSeconds(5)
                    };

                    var result = await _orderSyncService.SyncOrdersAsync(merchant.Id, options);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("Successfully synced {OrderCount} orders for merchant {Shop}", 
                            result.TotalOrders, merchant.Shop);
                    }
                    else
                    {
                        _logger.LogWarning("Sync failed for merchant {Shop}. Errors: {Errors}", 
                            merchant.Shop, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing orders for merchant {Shop}", merchant.Shop);
                }
            }
            
            _logger.LogInformation("Scheduled order sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled order sync");
        }
    }
}
```

### **Phase 5: Configuration & Deployment**

#### **5.1 Configuration Files**
```json
// host.json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      }
    }
  },
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[4.*, 5.0.0)"
  }
}

// local.settings.json
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

#### **5.2 Azure Deployment**
```bash
# Deploy to Azure Functions
az functionapp create \
  --name "shoplazza-addon-functions" \
  --resource-group "shoplazza-addon-rg" \
  --consumption-plan-location "East US" \
  --runtime "dotnet-isolated" \
  --runtime-version "8.0" \
  --functions-version "4" \
  --storage-account "shoplazzaaddonfunc" \
  --tags "Environment=Production" "Project=ShoplazzaAddon"

# Deploy the function app
func azure functionapp publish shoplazza-addon-functions
```

## Implementation Timeline

### **Week 1: Foundation**
- [ ] Create new Git repository
- [ ] Scaffold Azure Functions project (.NET 8 isolation)
- [ ] Set up shared library structure
- [ ] Configure dependency injection
- [ ] Create basic data models

### **Week 2: Core Services**
- [ ] Implement cache service interface and implementations
- [ ] Create order sync service
- [ ] Add rate limiting service
- [ ] Set up database context and migrations
- [ ] Create basic unit tests

### **Week 3: Functions Implementation**
- [ ] Implement timer-triggered order sync function
- [ ] Create analytics processing function
- [ ] Add error handling and retry logic
- [ ] Implement monitoring and logging
- [ ] Integration testing

### **Week 4: Production Ready**
- [ ] Azure deployment and configuration
- [ ] Performance optimization
- [ ] Comprehensive testing
- [ ] Documentation
- [ ] Monitoring setup

## Cost Analysis

### **Azure Functions (Consumption Plan)**
- **Free Tier**: 1M requests/month, 400K GB-seconds/month
- **Pay-per-use**: $0.20 per million requests, $0.000016/GB-second
- **Estimated Cost**: $5-15/month for typical usage

### **Storage Account**
- **Cost**: $0.0184 per GB per month
- **Estimated Cost**: $1-2/month

### **Total Estimated Cost**: $6-17/month

## Success Criteria

### **Functional Requirements**
- ✅ **Scheduled Sync**: Automatic background sync every 30 minutes
- ✅ **Data Accuracy**: 100% order data consistency
- ✅ **Error Recovery**: Automatic retry and failure handling
- ✅ **Performance**: <5 minutes per sync operation
- ✅ **Monitoring**: Comprehensive logging and metrics

### **Non-Functional Requirements**
- ✅ **Reliability**: 99.9% sync success rate
- ✅ **Scalability**: Handle 1000+ merchants
- ✅ **Cost Efficiency**: <$20/month total cost
- ✅ **Maintainability**: Clean architecture and documentation

## Conclusion

This execution plan provides a comprehensive roadmap for creating a **separate Azure Functions app** using **.NET 8 isolation mode**. The architecture ensures:

1. **Clean Separation**: Functions app independent of main web app
2. **Performance**: .NET 8 isolation mode for better performance
3. **Scalability**: Serverless architecture with automatic scaling
4. **Cost Efficiency**: Pay-per-use model with minimal costs
5. **Reliability**: Multiple sync strategies and error handling

**Ready to proceed with implementation!**

---

*Document Version: 1.0*  
*Last Updated: 2024-08-04*  
*Status: Ready for Implementation* 