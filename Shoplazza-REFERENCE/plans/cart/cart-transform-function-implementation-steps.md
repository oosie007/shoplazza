# Cart-Transform Function Implementation Steps

## Phase 1: Core Infrastructure

### Step 1.1: Create New Service Interfaces

#### Create `IShoplazzaFunctionApiService.cs`
```csharp
// Services/IShoplazzaFunctionApiService.cs
public interface IShoplazzaFunctionApiService
{
    Task<string> CreateFunctionAsync(Merchant merchant, FunctionRegistrationRequest request);
    Task<bool> ActivateFunctionAsync(Merchant merchant, string functionId);
    Task<bool> DeleteFunctionAsync(Merchant merchant, string functionId);
    Task<FunctionStatus> GetFunctionStatusAsync(Merchant merchant, string functionId);
}
```

#### Create `ICartTransformFunctionService.cs`
```csharp
// Services/ICartTransformFunctionService.cs
public interface ICartTransformFunctionService
{
    Task<byte[]> BuildWasmAsync();
    Task<string> BuildWasmBase64Async();
    Task<bool> ValidateWasmAsync(byte[] wasmBytes);
}
```

### Step 1.2: Create New Models

#### Create `FunctionConfiguration.cs`
```csharp
// Models/Configuration/FunctionConfiguration.cs
public class FunctionConfiguration
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string FunctionId { get; set; }
    public string FunctionName { get; set; }
    public string FunctionType { get; set; }
    public FunctionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string ErrorMessage { get; set; }
    
    // Navigation properties
    public Merchant Merchant { get; set; }
}

public enum FunctionStatus
{
    Pending,
    Active,
    Failed,
    Deleted
}
```

#### Create `FunctionRegistrationRequest.cs`
```csharp
// Models/Api/FunctionRegistrationRequest.cs
public class FunctionRegistrationRequest
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string WasmBase64 { get; set; }
    public List<string> Triggers { get; set; }
    public FunctionSettings Settings { get; set; }
}

public class FunctionSettings
{
    public int Timeout { get; set; } = 5000;
    public string MemoryLimit { get; set; } = "128MB";
}
```

### Step 1.3: Update Configuration

#### Update `appsettings.json`
```json
{
  "ShoplazzaFunctionApi": {
    "BaseUrl": "https://api.shoplazza.com/v1",
    "Timeout": 30000,
    "RetryAttempts": 3,
    "RetryDelayMs": 1000
  }
}
```

## Phase 2: Service Implementations

### Step 2.1: Implement ShoplazzaFunctionApiService

#### Create `ShoplazzaFunctionApiService.cs`
```csharp
// Services/ShoplazzaFunctionApiService.cs
public class ShoplazzaFunctionApiService : IShoplazzaFunctionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShoplazzaFunctionApiService> _logger;
    private readonly IConfiguration _configuration;
    
    public ShoplazzaFunctionApiService(
        HttpClient httpClient,
        ILogger<ShoplazzaFunctionApiService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task<string> CreateFunctionAsync(Merchant merchant, FunctionRegistrationRequest request)
    {
        try
        {
            var endpoint = $"{GetApiBase()}/functions";
            var response = await MakeApiCallAsync<FunctionCreateResponse>(merchant, endpoint, HttpMethod.Post, request);
            return response?.FunctionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating function for merchant {Shop}", merchant.Shop);
            throw;
        }
    }
    
    // Implement other methods...
}
```

### Step 2.2: Implement CartTransformFunctionService

#### Create `CartTransformFunctionService.cs`
```csharp
// Services/CartTransformFunctionService.cs
public class CartTransformFunctionService : ICartTransformFunctionService
{
    private readonly ILogger<CartTransformFunctionService> _logger;
    private readonly string _wasmSourcePath;
    
    public CartTransformFunctionService(ILogger<CartTransformFunctionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _wasmSourcePath = Path.Combine(Directory.GetCurrentDirectory(), "cart-transform-function");
    }
    
    public async Task<byte[]> BuildWasmAsync()
    {
        try
        {
            // Build WASM using Node.js and Javy
            var wasmPath = await BuildWasmFileAsync();
            return await File.ReadAllBytesAsync(wasmPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building WASM file");
            throw;
        }
    }
    
    private async Task<string> BuildWasmFileAsync()
    {
        // Implementation for building WASM file
        // This will run npm run build in the cart-transform-function directory
    }
}
```

## Phase 3: Merchant Service Integration

### Step 3.1: Extend IMerchantService

#### Update `IMerchantService.cs`
```csharp
// Add to existing IMerchantService interface:
Task<bool> RegisterCartTransformFunctionAsync(Merchant merchant);
Task<FunctionConfiguration> GetFunctionConfigurationAsync(int merchantId);
Task<bool> UpdateFunctionStatusAsync(int merchantId, FunctionStatus status, string errorMessage = null);
```

### Step 3.2: Extend MerchantService

#### Update `MerchantService.cs`
```csharp
// Add to existing MerchantService class:
public async Task<bool> RegisterCartTransformFunctionAsync(Merchant merchant)
{
    try
    {
        // 1. Build WASM file
        var wasmBytes = await _cartTransformFunctionService.BuildWasmAsync();
        var wasmBase64 = Convert.ToBase64String(wasmBytes);
        
        // 2. Create function registration request
        var request = new FunctionRegistrationRequest
        {
            Name = $"cart-transform-addon-{merchant.Shop}",
            Type = "cart-transform",
            Description = "Automatically applies add-on pricing to cart items",
            WasmBase64 = wasmBase64,
            Triggers = new List<string> { "cart.add", "cart.update", "checkout.begin" },
            Settings = new FunctionSettings()
        };
        
        // 3. Upload to Shoplazza
        var functionId = await _shoplazzaFunctionApiService.CreateFunctionAsync(merchant, request);
        
        // 4. Store configuration
        var functionConfig = new FunctionConfiguration
        {
            MerchantId = merchant.Id,
            FunctionId = functionId,
            FunctionName = request.Name,
            FunctionType = request.Type,
            Status = FunctionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        await _functionConfigurationRepository.AddAsync(functionConfig);
        
        // 5. Activate function
        var activated = await _shoplazzaFunctionApiService.ActivateFunctionAsync(merchant, functionId);
        if (activated)
        {
            functionConfig.Status = FunctionStatus.Active;
            functionConfig.ActivatedAt = DateTime.UtcNow;
            await _functionConfigurationRepository.UpdateAsync(functionConfig);
        }
        
        return activated;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error registering cart-transform function for merchant {Shop}", merchant.Shop);
        return false;
    }
}
```

## Phase 4: Database Schema Updates

### Step 4.1: Create Migration

#### Create new migration for FunctionConfiguration table
```csharp
// Migrations/AddFunctionConfigurationTable.cs
public partial class AddFunctionConfigurationTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FunctionConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                MerchantId = table.Column<int>(type: "INTEGER", nullable: false),
                FunctionId = table.Column<string>(type: "TEXT", nullable: false),
                FunctionName = table.Column<string>(type: "TEXT", nullable: false),
                FunctionType = table.Column<string>(type: "TEXT", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FunctionConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_FunctionConfigurations_Merchants_MerchantId",
                    column: x => x.MerchantId,
                    principalTable: "Merchants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }
}
```

## Phase 5: Integration Points

### Step 5.1: Update Merchant Installation Flow

#### Update `MerchantController.cs`
```csharp
// In the existing installation method, add:
public async Task<IActionResult> InstallAppAsync(InstallAppRequest request)
{
    try
    {
        // ... existing merchant creation logic ...
        
        // Register cart-transform function
        var functionRegistered = await _merchantService.RegisterCartTransformFunctionAsync(merchant);
        if (!functionRegistered)
        {
            _logger.LogWarning("Cart-transform function registration failed for merchant {Shop}", merchant.Shop);
            // Continue with installation but log the issue
        }
        
        // ... complete installation ...
    }
    catch (Exception ex)
    {
        // ... error handling ...
    }
}
```

### Step 5.2: Update Program.cs

#### Update `Program.cs`
```csharp
// Add new services to DI container:
builder.Services.AddScoped<IShoplazzaFunctionApiService, ShoplazzaFunctionApiService>();
builder.Services.AddScoped<ICartTransformFunctionService, CartTransformFunctionService>();

// Add HttpClient for Shoplazza Function API:
builder.Services.AddHttpClient<IShoplazzaFunctionApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ShoplazzaFunctionApi:BaseUrl"]);
    client.Timeout = TimeSpan.FromMilliseconds(builder.Configuration.GetValue<int>("ShoplazzaFunctionApi:Timeout"));
});
```

## Phase 6: Testing & Validation

### Step 6.1: Unit Tests
- Test WASM building process
- Test function registration flow
- Test error handling and fallbacks

### Step 6.2: Integration Tests
- Test complete merchant installation flow
- Test function activation and status checking
- Test add-on pricing functionality

### Step 6.3: End-to-End Tests
- Test complete flow from app installation to add-on pricing
- Validate function works in merchant's shop
- Test error scenarios and recovery

## Success Criteria Checklist

- [ ] WASM function builds successfully during installation
- [ ] Function uploads to Shoplazza without errors
- [ ] Function ID is stored in merchant configuration
- [ ] Function activates immediately after installation
- [ ] Add-on pricing works correctly in merchant's shop
- [ ] No manual configuration required from merchant
- [ ] Error handling and fallbacks work correctly
- [ ] Function status monitoring is implemented
- [ ] Database schema updates are applied
- [ ] All tests pass

## Deployment Notes

### Pre-deployment Requirements
- Shoplazza Function API access and credentials
- Node.js environment available for WASM building
- Javy compiler installed globally
- Database migration applied

### Post-deployment Validation
- Test merchant installation flow
- Verify function registration and activation
- Test add-on pricing functionality
- Monitor function execution logs
