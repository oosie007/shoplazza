# 🧪 Testing Guide for Cart-Transform Function Integration

## Overview
This document provides comprehensive guidance for testing the cart-transform function integration in the Shoplazza Add-On App.

## Test Structure

### 📁 Test Organization
```
Tests/
├── Unit/                           # Individual component tests
│   ├── CartTransformFunctionServiceTests.cs
│   └── ShoplazzaFunctionApiServiceTests.cs
├── Integration/                    # Service interaction tests
│   └── FunctionRegistrationIntegrationTests.cs
├── Utilities/                      # Test data and helper functions
│   └── TestDataFactory.cs
├── run-tests.sh                    # Test execution script
└── TESTING_GUIDE.md               # This guide
```

## 🚀 Running Tests

### Quick Start
```bash
# From the shoplazza-addon-app directory
./Tests/run-tests.sh
```

### Manual Test Execution
```bash
# Run all tests
dotnet test Tests/ShoplazzaAddonApp.Tests.csproj

# Run only unit tests
dotnet test Tests/ShoplazzaAddonApp.Tests.csproj --filter "Category=Unit"

# Run only integration tests
dotnet test Tests/ShoplazzaAddonApp.Tests.csproj --filter "Category=Integration"

# Run with detailed output
dotnet test Tests/ShoplazzaAddonApp.Tests.csproj --verbosity detailed
```

## 🧪 Test Categories

### 1. Unit Tests
**Purpose**: Test individual components in isolation
**Coverage**: 
- `CartTransformFunctionService` - WASM validation and building
- `ShoplazzaFunctionApiService` - API interaction methods

**Key Test Scenarios**:
- Valid WASM file validation
- Invalid WASM file rejection
- API success responses
- API error handling
- Edge cases (null values, empty data, etc.)

### 2. Integration Tests
**Purpose**: Test complete workflows and service interactions
**Coverage**:
- Complete function registration flow
- Database operations
- Service coordination
- Error handling across services

**Key Test Scenarios**:
- Successful function registration
- WASM build failures
- Shoplazza API failures
- Function activation failures
- Database state management

## 🔍 Test Data

### TestDataFactory
The `TestDataFactory` class provides consistent test data for all tests:

- **Merchants**: Valid merchant entities with realistic data
- **Function Configurations**: Function metadata for testing
- **WASM Files**: Valid and invalid WASM byte arrays
- **API Requests**: Properly formatted function registration requests

### Test Data Characteristics
- **Realistic**: Mimics actual production data structures
- **Consistent**: Same data across test runs
- **Isolated**: Each test gets fresh data instances
- **Valid**: Follows all business rules and constraints

## 🎯 Testing Strategy

### 1. Happy Path Testing
- **Goal**: Verify normal operation works correctly
- **Coverage**: All success scenarios
- **Validation**: Database state, API calls, service responses

### 2. Error Path Testing
- **Goal**: Ensure graceful failure handling
- **Coverage**: All error conditions
- **Validation**: Error messages, status updates, rollback behavior

### 3. Edge Case Testing
- **Goal**: Handle boundary conditions
- **Coverage**: Null values, empty data, size limits
- **Validation**: Input validation, error handling

### 4. Integration Testing
- **Goal**: Verify component interactions
- **Coverage**: Service dependencies, data flow
- **Validation**: End-to-end workflows

## 🛠️ Test Environment

### In-Memory Database
- **Technology**: Entity Framework Core In-Memory Provider
- **Benefits**: Fast execution, isolated tests, no external dependencies
- **Setup**: Automatically configured in test constructors

### Mocked Dependencies
- **External APIs**: Shoplazza Function API calls
- **File System**: WASM building operations
- **HTTP Client**: Network requests
- **Logging**: Log output capture

### Test Isolation
- **Database**: Fresh instance per test
- **Mocks**: Reset between tests
- **Data**: No shared state between tests

## 📊 Test Metrics

### Coverage Goals
- **Unit Tests**: 90%+ code coverage
- **Integration Tests**: All major workflows
- **Error Handling**: All error paths covered
- **Edge Cases**: Boundary conditions tested

### Success Criteria
- All tests pass consistently
- No flaky tests
- Clear error messages for failures
- Fast execution (< 30 seconds total)

## 🚨 Common Test Issues

### 1. Database Context Issues
**Problem**: Tests affecting each other's data
**Solution**: Use `IDisposable` pattern, fresh context per test

### 2. Mock Setup Problems
**Problem**: Incorrect mock configurations
**Solution**: Verify mock setup in test arrange section

### 3. Async/Await Issues
**Problem**: Tests not waiting for async operations
**Solution**: Always use `await` with async test methods

### 4. Configuration Dependencies
**Problem**: Tests failing due to missing config
**Solution**: Mock configuration values in test setup

## 🔧 Debugging Tests

### Enable Detailed Logging
```bash
dotnet test --verbosity detailed --logger "console;verbosity=detailed"
```

### Debug Specific Test
```bash
dotnet test --filter "FullyQualifiedName~TestName"
```

### Run Single Test File
```bash
dotnet test Tests/Unit/CartTransformFunctionServiceTests.cs
```

## 📝 Adding New Tests

### Test Naming Convention
- **Format**: `MethodName_Scenario_ExpectedResult`
- **Example**: `ValidateWasmAsync_WithValidWasmBytes_ReturnsTrue`

### Test Structure (AAA Pattern)
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange - Setup test data and mocks
    
    // Act - Execute the method being tested
    
    // Assert - Verify the results
}
```

### Test Categories
```csharp
[Fact]
[Trait("Category", "Unit")]  // or "Integration"
public async Task TestMethod()
{
    // Test implementation
}
```

## 🎯 What We're Testing

### Cart-Transform Function Integration
1. **WASM Building**: JavaScript to WebAssembly compilation
2. **Function Registration**: Upload to Shoplazza Function API
3. **Function Activation**: Enable the function for cart processing
4. **Database Management**: Store function configuration and status
5. **Error Handling**: Graceful failure and recovery
6. **Service Coordination**: Multiple services working together

### Success Criteria
- ✅ WASM files build and validate correctly
- ✅ Functions register successfully with Shoplazza
- ✅ Functions activate and become available
- ✅ Database tracks function status accurately
- ✅ Errors are handled gracefully
- ✅ All services coordinate properly

## 🚀 Next Steps

After running tests successfully:
1. **Deploy to Development Environment**
2. **Test with Real Shoplazza Store**
3. **Validate Cart-Transform Functionality**
4. **Monitor Performance and Errors**
5. **Deploy to Production**

---

**Remember**: Tests are your safety net. Run them frequently and fix any failures immediately!
