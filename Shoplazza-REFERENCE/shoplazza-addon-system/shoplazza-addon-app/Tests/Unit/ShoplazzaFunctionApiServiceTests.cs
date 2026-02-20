using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Services;
using ShoplazzaAddonApp.Tests.Utilities;
using System.Net;
using Xunit;


namespace ShoplazzaAddonApp.Tests.Unit;

/// <summary>
/// Unit tests for ShoplazzaFunctionApiService
/// </summary>
public class ShoplazzaFunctionApiServiceTests
{
    private readonly Mock<ILogger<ShoplazzaFunctionApiService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IShoplazzaAuthService> _mockAuthService;
    private readonly HttpClient _httpClient;
    private readonly ShoplazzaFunctionApiService _service;

    public ShoplazzaFunctionApiServiceTests()
    {
        _mockLogger = new Mock<ILogger<ShoplazzaFunctionApiService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockAuthService = new Mock<IShoplazzaAuthService>();
        
        // Setup default configuration values
        _mockConfiguration.Setup(c => c["ShoplazzaFunctionApi:BaseUrl"]).Returns("https://partners.shoplazza.com/openapi/2024-07");
        
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _service = new ShoplazzaFunctionApiService(_httpClient, _mockLogger.Object, _mockConfiguration.Object, _mockAuthService.Object);
    }

    [Fact]
    public async Task CreateFunctionAsync_WithValidRequest_ReturnsFunctionId()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var request = TestDataFactory.CreateTestFunctionRegistrationRequest();
        
        var expectedResponse = new
        {
            function_id = "test-function-123",
            name = request.Name,
            status = "active"
        };

        SetupMockHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var (functionId, errorDetails) = await _service.CreateFunctionAsync(merchant, request);

        // Assert
        Assert.NotNull(functionId);
        Assert.Null(errorDetails);
        Assert.Equal("test-function-123", functionId);
    }

    [Fact]
    public async Task CreateFunctionAsync_WithApiError_ReturnsNull()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var request = TestDataFactory.CreateTestFunctionRegistrationRequest();
        
        SetupMockHttpResponse(HttpStatusCode.BadRequest, new { error = "Invalid request" });

        // Act
        var (functionId, errorDetails) = await _service.CreateFunctionAsync(merchant, request);

        // Assert
        Assert.Null(functionId);
        Assert.NotNull(errorDetails);
        Assert.Contains("Invalid request", errorDetails);
    }

    [Fact]
    public async Task ActivateFunctionAsync_WithValidFunctionId_ReturnsTrue()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionId = "test-function-123";
        
        SetupMockHttpResponse(HttpStatusCode.OK, new { success = true });

        // Act
        var result = await _service.ActivateFunctionAsync(merchant, functionId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ActivateFunctionAsync_WithApiError_ReturnsFalse()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionId = "test-function-123";
        
        SetupMockHttpResponse(HttpStatusCode.NotFound, new { error = "Function not found" });

        // Act
        var result = await _service.ActivateFunctionAsync(merchant, functionId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFunctionAsync_WithValidFunctionId_ReturnsTrue()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionId = "test-function-123";
        
        SetupMockHttpResponse(HttpStatusCode.OK, new { success = true });

        // Act
        var result = await _service.DeleteFunctionAsync(merchant, functionId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteFunctionAsync_WithApiError_ReturnsFalse()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionId = "test-function-123";
        
        SetupMockHttpResponse(HttpStatusCode.NotFound, new { error = "Function not found" });

        // Act
        var result = await _service.DeleteFunctionAsync(merchant, functionId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetFunctionStatusAsync_WithValidFunctionId_ReturnsStatus()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionId = "test-function-123";
        
        var expectedResponse = new
        {
            function_id = functionId,
            name = "test-function",
            status = "active",
            type = "cart-transform"
        };

        SetupMockHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _service.GetFunctionStatusAsync(merchant, functionId);

        // Assert
        Assert.Equal(Models.Configuration.FunctionStatus.Active, result);
    }

    [Fact]
    public async Task GetFunctionStatusAsync_WithApiError_ReturnsUnknown()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionId = "test-function-123";
        
        SetupMockHttpResponse(HttpStatusCode.NotFound, new { error = "Function not found" });

        // Act
        var result = await _service.GetFunctionStatusAsync(merchant, functionId);

        // Assert
        Assert.Equal(Models.Configuration.FunctionStatus.Unknown, result);
    }

    [Fact]
    public async Task GetFunctionStatusAsync_WithUnknownStatus_ReturnsUnknown()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var functionId = "test-function-123";
        
        var expectedResponse = new
        {
            function_id = functionId,
            name = "test-function",
            function_status = "unknown-status",
            type = "cart-transform"
        };

        SetupMockHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _service.GetFunctionStatusAsync(merchant, functionId);

        // Assert
        Assert.Equal(Models.Configuration.FunctionStatus.Unknown, result);
    }

    [Fact]
    public async Task CreateFunctionAsync_WithExistingFunction_ShouldUpdateInsteadOfCreate()
    {
        // Arrange
        var merchant = TestDataFactory.CreateTestMerchant();
        var request = TestDataFactory.CreateTestFunctionRegistrationRequest();
        
        // Mock the auth service to return a partner token
        _mockAuthService.Setup(x => x.GetPartnerTokenAsync()).ReturnsAsync("test-partner-token");
        
        // Mock the HTTP handler to simulate finding an existing function first, then successful update
        var existingFunctionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""code"": ""SUCCESS"",
                ""message"": ""SUCCESS"",
                ""data"": {
                    ""functions"": [
                        {
                            ""function_id"": ""existing-function-123"",
                            ""name"": """ + request.Name + @""",
                            ""namespace"": ""cart_transform"",
                            ""status"": ""active"",
                            ""created_at"": ""2023-06-29 21:16:48"",
                            ""updated_at"": ""2023-06-30 10:27:23"",
                            ""version"": ""v1.0.0""
                        }
                    ]
                }
            }", System.Text.Encoding.UTF8, "application/json")
        };
        
        var updateResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""code"": ""SUCCESS"",
                ""message"": ""SUCCESS"",
                ""data"": {
                    ""function_id"": ""existing-function-123"",
                    ""version"": ""v1.0.1""
                }
            }", System.Text.Encoding.UTF8, "application/json")
        };
        
        // Setup the mock handler to return different responses for different requests
        _mockHttpHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(existingFunctionResponse)  // First call: GET /functions (find existing)
            .ReturnsAsync(updateResponse);           // Second call: PATCH /functions/{id} (update)
        
        // Act
        var (functionId, errorDetails) = await _service.CreateFunctionAsync(merchant, request);
        
        // Assert
        Assert.Equal("existing-function-123", functionId);
        Assert.Null(errorDetails);
        
        // Verify that the HTTP handler was called twice:
        // 1. GET /functions to find existing function
        // 2. PATCH /functions/{id} to update existing function
        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private void SetupMockHttpResponse(HttpStatusCode statusCode, object responseContent)
    {
        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(responseContent);
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
    }
}
