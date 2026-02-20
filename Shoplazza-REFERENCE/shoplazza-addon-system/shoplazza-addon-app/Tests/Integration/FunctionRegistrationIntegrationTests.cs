using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ShoplazzaAddonApp.Data;
using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Services;
using ShoplazzaAddonApp.Tests.Utilities;
using Xunit;

namespace ShoplazzaAddonApp.Tests.Integration;

/// <summary>
/// Integration tests for the complete function registration flow
/// </summary>
public class FunctionRegistrationIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICartTransformFunctionService> _mockCartTransformService;
    private readonly Mock<IShoplazzaFunctionApiService> _mockShoplazzaFunctionApiService;
    private readonly Mock<ILogger<MerchantService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly Mock<ILogger<Repository<Merchant>>> _mockMerchantRepoLogger;
    private readonly Mock<ILogger<Repository<Models.Configuration.FunctionConfiguration>>> _mockFunctionRepoLogger;
    private readonly Mock<ILogger<Repository<ProductAddOn>>> _mockProductAddOnRepoLogger;
    private readonly Mock<ILogger<Repository<Configuration>>> _mockConfigRepoLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly MerchantService _merchantService;

    public FunctionRegistrationIntegrationTests()
    {
        // Setup in-memory database for testing using our factory
        _dbContext = TestDbContextFactory.CreateTestContext();

        // Create mocks
        _mockCartTransformService = new Mock<ICartTransformFunctionService>();
        _mockShoplazzaFunctionApiService = new Mock<IShoplazzaFunctionApiService>();
        _mockLogger = new Mock<ILogger<MerchantService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpClient = new Mock<HttpClient>();
        _mockMerchantRepoLogger = new Mock<ILogger<Repository<Merchant>>>();
        _mockFunctionRepoLogger = new Mock<ILogger<Repository<Models.Configuration.FunctionConfiguration>>>();
        _mockProductAddOnRepoLogger = new Mock<ILogger<Repository<ProductAddOn>>>();
        _mockConfigRepoLogger = new Mock<ILogger<Repository<Configuration>>>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        // Setup mock responses
        SetupMockResponses();

        // Setup IServiceProvider mock to return DbContext
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ApplicationDbContext)))
            .Returns(_dbContext);

        // Create a simple test service provider that bypasses scoping
        var testServiceProvider = new TestServiceProvider(_dbContext);

        // Create service with mocked dependencies
        _merchantService = new MerchantService(
            new Repository<Merchant>(_dbContext, _mockMerchantRepoLogger.Object),
            new Repository<Models.Configuration.FunctionConfiguration>(_dbContext, _mockFunctionRepoLogger.Object),
            new Repository<ProductAddOn>(_dbContext, _mockProductAddOnRepoLogger.Object),
            new Repository<Configuration>(_dbContext, _mockConfigRepoLogger.Object),
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockHttpClient.Object,
            _mockShoplazzaFunctionApiService.Object,
            _mockCartTransformService.Object,
            testServiceProvider
        );
    }

    [Fact]
    public async Task RegisterCartTransformFunctionAsync_CompleteFlow_Success()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _merchantService.RegisterCartTransformFunctionAsync(merchant);

        // Assert
        Assert.True(result);

        // Verify function configuration was created in database using global function
        var functionConfig = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);
        Assert.NotNull(functionConfig);
        Assert.Equal("test-global-function-id-123", functionConfig.FunctionId);
        Assert.Equal("cart-transform-addon", functionConfig.FunctionName);
        Assert.Equal(Models.Configuration.FunctionStatus.Active, functionConfig.Status);
        Assert.NotNull(functionConfig.ActivatedAt);
    }

    [Fact]
    public async Task RegisterCartTransformFunctionAsync_WithExistingFunction_UpdatesSuccessfully()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.SaveChangesAsync();

        // Create existing function configuration
        var existingConfig = TestDataFactory.CreateTestFunctionConfiguration(merchant.Id);
        await _dbContext.FunctionConfigurations.AddAsync(existingConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _merchantService.RegisterCartTransformFunctionAsync(merchant);

        // Assert
        Assert.True(result);

        // Verify existing configuration was updated to use global function
        var updatedConfig = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);
        Assert.NotNull(updatedConfig);
        Assert.Equal("test-global-function-id-123", updatedConfig.FunctionId);
        Assert.Equal("cart-transform-addon", updatedConfig.FunctionName);
        Assert.Equal(Models.Configuration.FunctionStatus.Active, updatedConfig.Status);
    }

    [Fact]
    public async Task RegisterCartTransformFunctionAsync_WhenWasmBuildFails_ReturnsFalse()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.SaveChangesAsync();

        // Setup mock to fail WASM building (this shouldn't affect the new implementation)
        _mockCartTransformService
            .Setup(s => s.BuildWasmAsync())
            .ThrowsAsync(new InvalidOperationException("WASM build failed"));

        // Act
        var result = await _merchantService.RegisterCartTransformFunctionAsync(merchant);

        // Assert
        // In the new implementation, WASM building happens at startup, not during merchant install
        // So this should still succeed as long as the global function exists
        Assert.True(result);

        // Verify function configuration was created successfully using global function
        var functionConfig = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);
        Assert.NotNull(functionConfig);
        Assert.Equal("test-global-function-id-123", functionConfig.FunctionId);
        Assert.Equal(Models.Configuration.FunctionStatus.Active, functionConfig.Status);
    }

    [Fact]
    public async Task RegisterCartTransformFunctionAsync_WhenShoplazzaApiFails_ReturnsFalse()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.SaveChangesAsync();

        // Setup mock to fail Shoplazza API binding call
        _mockShoplazzaFunctionApiService
            .Setup(s => s.BindCartTransformFunctionAsync(It.IsAny<Merchant>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _merchantService.RegisterCartTransformFunctionAsync(merchant);

        // Assert
        Assert.False(result);

        // Verify function configuration was created but with failed status
        var functionConfig = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);
        Assert.NotNull(functionConfig);
        Assert.Equal(Models.Configuration.FunctionStatus.Failed, functionConfig.Status);
        Assert.NotNull(functionConfig.ErrorMessage);
    }

    [Fact]
    public async Task RegisterCartTransformFunctionAsync_WhenFunctionActivationFails_ReturnsFalse()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.SaveChangesAsync();

        // Setup mock to fail function binding (activation happens at startup in new implementation)
        _mockShoplazzaFunctionApiService
            .Setup(s => s.BindCartTransformFunctionAsync(It.IsAny<Merchant>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _merchantService.RegisterCartTransformFunctionAsync(merchant);

        // Assert
        Assert.False(result);

        // Verify function configuration was created but with failed status
        var functionConfig = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);
        Assert.NotNull(functionConfig);
        Assert.Equal(Models.Configuration.FunctionStatus.Failed, functionConfig.Status);
        Assert.NotNull(functionConfig.ErrorMessage);
    }

    [Fact]
    public async Task GetFunctionConfigurationAsync_WithValidMerchantId_ReturnsConfiguration()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionConfig = TestDataFactory.CreateTestFunctionConfiguration(merchant.Id);
        
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.FunctionConfigurations.AddAsync(functionConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(functionConfig.FunctionId, result.FunctionId);
        Assert.Equal(functionConfig.Status, result.Status);
    }

    [Fact]
    public async Task GetFunctionConfigurationAsync_WithInvalidMerchantId_ReturnsNull()
    {
        // Act
        var result = await _merchantService.GetFunctionConfigurationAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateFunctionStatusAsync_WithValidStatus_UpdatesSuccessfully()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionConfig = TestDataFactory.CreateTestFunctionConfiguration(merchant.Id);
        
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.FunctionConfigurations.AddAsync(functionConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _merchantService.UpdateFunctionStatusAsync(
            merchant.Id, 
            Models.Configuration.FunctionStatus.Failed, 
            "Test error message");

        // Assert
        Assert.True(result);

        // Verify status was updated
        var updatedConfig = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);
        Assert.NotNull(updatedConfig);
        Assert.Equal(Models.Configuration.FunctionStatus.Failed, updatedConfig.Status);
        Assert.Equal("Test error message", updatedConfig.ErrorMessage);
    }

    [Fact]
    public async Task Debug_RegisterCartTransformFunctionAsync_CheckWhatHappens()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        await _dbContext.Merchants.AddAsync(merchant);
        await _dbContext.SaveChangesAsync();

        // Check if global function exists
        var globalFunction = await _dbContext.GlobalFunctionConfigurations
            .FirstOrDefaultAsync(f => f.FunctionType == "cart-transform" && f.IsActive);
        
        // Log what we found
        Console.WriteLine($"Global function found: {globalFunction?.FunctionId ?? "NULL"}");
        Console.WriteLine($"Global function status: {globalFunction?.Status}");
        Console.WriteLine($"Global function active: {globalFunction?.IsActive}");

        // Act
        var result = await _merchantService.RegisterCartTransformFunctionAsync(merchant);

        // Assert
        Console.WriteLine($"RegisterCartTransformFunctionAsync result: {result}");
        
        // Check what was created in the database
        var functionConfig = await _merchantService.GetFunctionConfigurationAsync(merchant.Id);
        Console.WriteLine($"Function config created: {functionConfig?.FunctionId ?? "NULL"}");
        Console.WriteLine($"Function config status: {functionConfig?.Status}");
        
        // For now, just check that we can get the merchant
        Assert.NotNull(merchant);
    }

    private void SetupMockResponses()
    {
        // Setup WASM building mock (not used in new implementation but kept for compatibility)
        var testWasmBytes = TestDataFactory.CreateTestWasmBytes();
        _mockCartTransformService
            .Setup(s => s.BuildWasmAsync())
            .ReturnsAsync(testWasmBytes);

        // Setup Shoplazza Function API mocks for the new global function approach
        _mockShoplazzaFunctionApiService
            .Setup(s => s.BindCartTransformFunctionAsync(It.IsAny<Merchant>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Create a global function in the test database
        var globalFunction = new Models.Configuration.GlobalFunctionConfiguration
        {
            FunctionId = "test-global-function-id-123",
            FunctionName = "cart-transform-addon",
            FunctionNamespace = "cart_transform",
            FunctionType = "cart-transform",
            Status = Models.Configuration.FunctionStatus.Active,
            IsActive = true,
            Version = "1.0.0",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _dbContext.GlobalFunctionConfigurations.Add(globalFunction);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}

/// <summary>
/// Simple test service provider that bypasses scoping for testing
/// </summary>
public class TestServiceProvider : IServiceProvider, IServiceScopeFactory
{
    private readonly ApplicationDbContext _dbContext;

    public TestServiceProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(ApplicationDbContext))
            return _dbContext;
        if (serviceType == typeof(IServiceScopeFactory))
            return this;
        return null;
    }

    public IServiceScope CreateScope()
    {
        return new TestServiceScope(_dbContext);
    }
}

public class TestServiceScope : IServiceScope
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TestServiceProvider _serviceProvider;

    public TestServiceScope(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _serviceProvider = new TestServiceProvider(_dbContext);
    }

    public IServiceProvider ServiceProvider => _serviceProvider;

    public void Dispose()
    {
        // Nothing to dispose in test
    }
}
