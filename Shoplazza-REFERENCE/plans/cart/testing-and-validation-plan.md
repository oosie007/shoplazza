# Testing and Validation Plan: New Cart-Transform System

## **CONTEXT: What We're Testing**

### **System Transformation:**
We're converting from a **complex JavaScript cart manipulation system** to a **native Shoplazza cart-transform function system**:

**Before (Old System):**
- ❌ Client-side JavaScript cart manipulation
- ❌ Virtual line item creation
- ❌ Complex cart state management
- ❌ Form interception with heavy JavaScript

**After (New System):**
- ✅ Native Shoplazza cart-transform functions
- ✅ Server-side WASM execution
- ✅ Metafield-based configuration
- ✅ Clean, simple widget with HTML templates

### **Testing Goals:**
1. **Ensure functionality is preserved** during the transition
2. **Validate new native approach** works correctly
3. **Verify performance improvements** are achieved
4. **Confirm error handling** works in all scenarios
5. **Test integration** with Shoplazza's cart system

## **TESTING STRATEGY OVERVIEW**

### **Testing Phases:**
1. **Unit Testing** - Individual components and functions
2. **Integration Testing** - Component interactions
3. **End-to-End Testing** - Complete user workflows
4. **Performance Testing** - Response times and throughput
5. **Error Handling Testing** - Failure scenarios and recovery

### **Testing Environments:**
- **Local Development** - Individual component testing
- **Staging Environment** - Integration testing
- **Production Simulation** - Real-world scenario testing

## **DETAILED TESTING PLAN**

### **Phase 1: Unit Testing**

#### **1.1 Widget Controller Testing**
**File:** `shoplazza-addon-system/shoplazza-addon-app/Controllers/WidgetController.cs`

**Test Cases:**
```csharp
[Test]
public void GenerateWidgetAsync_WithValidMerchant_ReturnsValidHtml()
{
    // Arrange
    var merchant = CreateTestMerchant();
    var controller = CreateTestController();
    
    // Act
    var result = await controller.GenerateWidgetAsync(merchant);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result, Contains.Substring("shoplazza-addon-widget"));
    Assert.That(result, Contains.Substring("{{ADDON_TITLE}}"));
}

[Test]
public void LoadTemplateAsync_WithValidTemplate_ReturnsTemplateContent()
{
    // Arrange
    var controller = CreateTestController();
    var templateName = "widget-base.html";
    
    // Act
    var result = await controller.LoadTemplateAsync(templateName);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result, Is.Not.Empty);
}

[Test]
public void LoadTemplateAsync_WithInvalidTemplate_ReturnsEmptyString()
{
    // Arrange
    var controller = CreateTestController();
    var templateName = "nonexistent-template.html";
    
    // Act
    var result = await controller.LoadTemplateAsync(templateName);
    
    // Assert
    Assert.That(result, Is.Empty);
}
```

#### **1.2 HTML Template Testing**
**Files:** `wwwroot/widget-templates/`

**Test Cases:**
- ✅ **Template Structure** - Valid HTML syntax
- ✅ **Placeholder Replacement** - All placeholders get replaced
- ✅ **Dynamic Data Injection** - Product and add-on data correctly injected
- ✅ **CSS Integration** - Styles are properly included
- ✅ **JavaScript Integration** - Scripts are properly included

**Template Validation Tests:**
```javascript
// Test template structure
describe('HTML Templates', () => {
  test('widget-base.html has required structure', () => {
    const template = loadTemplate('widget-base.html');
    expect(template).toContain('<!DOCTYPE html>');
    expect(template).toContain('{{PRODUCT_ID}}');
    expect(template).toContain('{{ADDON_TITLE}}');
    expect(template).toContain('{{WIDGET_SCRIPT}}');
  });
  
  test('addon-selection.html has required elements', () => {
    const template = loadTemplate('addon-selection.html');
    expect(template).toContain('addon-checkbox');
    expect(template).toContain('addon-label');
    expect(template).toContain('addon-description');
  });
});
```

#### **1.3 WASM Function Testing**
**Files:** `shoplazza-addon-system/shoplazza-addon-functions/`

**Test Cases:**
```csharp
[Test]
public void CartTransformFunction_WithValidCartData_ProcessesAddOns()
{
    // Arrange
    var function = CreateTestFunction();
    var request = CreateTestCartRequest();
    
    // Act
    var result = await function.Run(request);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.TotalAddOnPrice, Is.GreaterThan(0));
    Assert.That(result.UpdatedItems, Is.Not.Empty);
}

[Test]
public void AddOnPriceCalculator_WithProtectionPlan_CalculatesCorrectPrice()
{
    // Arrange
    var calculator = new AddOnPriceCalculator();
    var config = new AddOnConfig 
    { 
        Type = "protection-plan", 
        BasePrice = 10.00m, 
        PercentageRate = 5.0m,
        ProductPrice = 100.00m 
    };
    
    // Act
    var result = calculator.CalculateAddOnPrice(config, 1);
    
    // Assert
    var expectedPrice = 10.00m + (100.00m * 0.05m); // Base + 5%
    Assert.That(result, Is.EqualTo(expectedPrice));
}

[Test]
public void MetafieldReader_WithValidMetafields_ParsesAddOnConfig()
{
    // Arrange
    var reader = new MetafieldReader();
    var properties = new Dictionary<string, string>
    {
        ["_addon_selected"] = "true",
        ["_addon_config"] = "{\"title\":\"Protection Plan\",\"price\":9.99}"
    };
    
    // Act
    var result = reader.GetAddOnMetafield(properties);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.IsSelected, Is.True);
    Assert.That(result.Config.Title, Is.EqualTo("Protection Plan"));
}
```

### **Phase 2: Integration Testing**

#### **2.1 Widget to Function Integration**
**Test Flow:**
```
Widget Selection → Metafield Setting → Form Submission → Cart-Transform Function → Price Adjustment
```

**Test Cases:**
```javascript
describe('Widget to Function Integration', () => {
  test('add-on selection sets correct metafields', async () => {
    // Arrange
    const widget = createTestWidget();
    const form = createTestForm();
    
    // Act
    widget.selectAddOn(true);
    const formData = form.serialize();
    
    // Assert
    expect(formData).toContain('_addon_selected=true');
    expect(formData).toContain('_addon_config');
  });
  
  test('form submission includes add-on data', async () => {
    // Arrange
    const widget = createTestWidget();
    const form = createTestForm();
    
    // Act
    widget.selectAddOn(true);
    const submission = await submitForm(form);
    
    // Assert
    expect(submission.properties).toHaveProperty('_addon_selected');
    expect(submission.properties).toHaveProperty('_addon_config');
  });
});
```

#### **2.2 Cart-Transform Function Integration**
**Test Flow:**
```
Cart Data → Function Input → Metafield Processing → Price Calculation → Response
```

**Test Cases:**
```csharp
[Test]
public async Task CartTransformFunction_Integration_ProcessesCompleteCart()
{
    // Arrange
    var function = CreateTestFunction();
    var cartData = CreateTestCartWithAddOns();
    var request = new CartTransformRequest 
    { 
        CartId = "test-cart-123",
        CartData = JsonSerializer.Serialize(cartData)
    };
    
    // Act
    var result = await function.Run(request);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.CartId, Is.EqualTo("test-cart-123"));
    Assert.That(result.TotalAddOnPrice, Is.GreaterThan(0));
    
    // Verify line items were processed
    var processedItems = result.UpdatedItems.Where(i => i.AddOnPrice > 0);
    Assert.That(processedItems, Is.Not.Empty);
    
    // Verify cart totals were updated
    Assert.That(result.CartSubtotal, Is.GreaterThan(cartData.Subtotal));
    Assert.That(result.CartTotal, Is.GreaterThan(cartData.Total));
}
```

### **Phase 3: End-to-End Testing**

#### **3.1 Complete Add-On Workflow**
**Test Scenario:** Customer selects add-on and completes purchase

**Test Steps:**
1. **Load Product Page** - Widget renders correctly
2. **Select Add-On** - Checkbox selection works
3. **Add to Cart** - Form submission includes metafields
4. **Cart Transformation** - Function processes add-on
5. **Price Adjustment** - Cart total reflects add-on price
6. **Checkout Process** - Add-on price maintained through checkout

**Test Data:**
```json
{
  "product": {
    "id": "test-product-123",
    "title": "Test Product",
    "price": 99.99
  },
  "addon": {
    "title": "Premium Protection",
    "price": 9.99,
    "type": "protection-plan"
  },
  "expectedCartTotal": 109.98
}
```

#### **3.2 Multiple Add-Ons Scenario**
**Test Scenario:** Customer selects multiple add-ons for different products

**Test Steps:**
1. **Product 1** - Select protection plan
2. **Product 2** - Select extended warranty
3. **Cart Submission** - Both add-ons processed
4. **Price Calculation** - Combined add-on pricing
5. **Cart Totals** - Accurate total calculation

#### **3.3 Error Handling Scenarios**
**Test Scenarios:**
- ❌ **Invalid Metafield Data** - Malformed JSON in add-on config
- ❌ **Missing Add-On Configuration** - Add-on selected but no config
- ❌ **Function Execution Failure** - Network or processing errors
- ❌ **Invalid Pricing Rules** - Unsupported add-on types

**Expected Behavior:**
- ✅ **Graceful Degradation** - Cart still works without add-ons
- ✅ **Error Logging** - All errors properly logged
- ✅ **User Feedback** - Clear error messages to users
- ✅ **Fallback Handling** - System continues to function

### **Phase 4: Performance Testing**

#### **4.1 Response Time Testing**
**Metrics to Measure:**
- **Widget Rendering Time** - Time to render add-on selection UI
- **Template Loading Time** - Time to load HTML templates
- **Function Execution Time** - Time for cart-transform function
- **End-to-End Response Time** - Complete add-on selection flow

**Performance Benchmarks:**
```json
{
  "widgetRendering": "< 100ms",
  "templateLoading": "< 50ms", 
  "functionExecution": "< 200ms",
  "endToEndFlow": "< 500ms"
}
```

#### **4.2 Throughput Testing**
**Test Scenarios:**
- **Single User** - One customer using add-on system
- **Multiple Users** - Concurrent customers selecting add-ons
- **High Volume** - Many add-on selections in short time
- **Peak Load** - Maximum expected concurrent usage

**Load Testing Tools:**
- **JMeter** - For HTTP load testing
- **Artillery** - For JavaScript-based testing
- **Azure Load Testing** - For cloud-based testing

### **Phase 5: Error Handling Testing**

#### **5.1 Template Loading Failures**
**Test Scenarios:**
```csharp
[Test]
public async Task WidgetController_TemplateNotFound_ReturnsFallbackWidget()
{
    // Arrange
    var controller = CreateTestController();
    var merchant = CreateTestMerchant();
    
    // Delete template files to simulate failure
    DeleteTemplateFiles();
    
    // Act
    var result = await controller.GenerateWidgetAsync(merchant);
    
    // Assert
    Assert.That(result, Contains.Substring("Add-on widget loading..."));
    Assert.That(result, Contains.Substring("shoplazza-addon-widget"));
}
```

#### **5.2 Function Execution Failures**
**Test Scenarios:**
```csharp
[Test]
public async Task CartTransformFunction_InvalidCartData_ReturnsErrorResponse()
{
    // Arrange
    var function = CreateTestFunction();
    var request = new CartTransformRequest 
    { 
        CartData = "invalid-json-data" 
    };
    
    // Act
    var result = await function.Run(request);
    
    // Assert
    Assert.That(result.Success, Is.False);
    Assert.That(result.Error, Is.EqualTo("Invalid cart data"));
}
```

#### **5.3 Network and Infrastructure Failures**
**Test Scenarios:**
- **Function Service Unavailable** - Azure Functions down
- **Database Connection Issues** - Connection timeouts
- **Template File System Issues** - File access problems
- **Memory and Resource Issues** - High resource usage

## **TESTING TOOLS AND FRAMEWORKS**

### **Backend Testing:**
- **NUnit** - C# unit testing framework
- **Moq** - Mocking framework for dependencies
- **FluentAssertions** - More readable assertions
- **TestContainers** - Database testing

### **Frontend Testing:**
- **Jest** - JavaScript testing framework
- **Testing Library** - React component testing
- **Cypress** - End-to-end testing
- **Playwright** - Cross-browser testing

### **Performance Testing:**
- **JMeter** - Load and performance testing
- **Artillery** - Modern load testing
- **Azure Load Testing** - Cloud-based testing
- **Application Insights** - Performance monitoring

### **Integration Testing:**
- **TestServer** - In-memory ASP.NET Core testing
- **WebApplicationFactory** - Integration test factory
- **Respawn** - Database state management
- **WireMock** - HTTP service mocking

## **TEST DATA MANAGEMENT**

### **Test Data Strategy:**
```csharp
public static class TestDataFactory
{
    public static Merchant CreateTestMerchant()
    {
        return new Merchant
        {
            Id = Guid.NewGuid(),
            Shop = "test-shop.myshoplazza.com",
            AccessToken = "test-access-token",
            ScriptTagId = "test-script-tag-id"
        };
    }
    
    public static ProductAddOn CreateTestAddOn()
    {
        return new ProductAddOn
        {
            Id = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            AddOnTitle = "Premium Protection",
            AddOnDescription = "Protect your purchase",
            AddOnPriceCents = 999, // $9.99
            AddOnSku = "PROTECTION-001",
            IsActive = true
        };
    }
    
    public static CartData CreateTestCartWithAddOns()
    {
        return new CartData
        {
            Id = "test-cart-123",
            LineItems = new List<CartLineItem>
            {
                new CartLineItem
                {
                    Id = "item-1",
                    ProductId = "product-123",
                    Title = "Test Product",
                    Price = 99.99m,
                    Quantity = 1,
                    Properties = new Dictionary<string, string>
                    {
                        ["_addon_selected"] = "true",
                        ["_addon_config"] = "{\"title\":\"Protection Plan\",\"price\":9.99}"
                    }
                }
            },
            Subtotal = 99.99m,
            Total = 99.99m,
            Currency = "USD"
        };
    }
}
```

## **TESTING CHECKLIST**

### **Pre-Testing Setup:**
- [ ] **Test Environment** - Local, staging, and production-like environments
- [ ] **Test Data** - Comprehensive test data sets
- [ ] **Test Tools** - All testing frameworks and tools installed
- [ ] **Monitoring** - Performance and error monitoring setup
- [ ] **Documentation** - Test cases and expected results documented

### **Testing Execution:**
- [ ] **Unit Tests** - All individual components tested
- [ ] **Integration Tests** - Component interactions verified
- [ ] **End-to-End Tests** - Complete workflows validated
- [ ] **Performance Tests** - Response times and throughput measured
- [ ] **Error Handling Tests** - Failure scenarios tested

### **Post-Testing Validation:**
- [ ] **Test Results** - All tests passing
- [ ] **Performance Metrics** - Within acceptable benchmarks
- [ ] **Error Handling** - Graceful degradation verified
- [ ] **Documentation** - Test results documented
- [ ] **Deployment Readiness** - System ready for production

## **GIT COMMIT INSTRUCTIONS**

### **Commit 1: Add Testing Infrastructure**
```bash
# Add testing frameworks and test projects
git add shoplazza-addon-system/shoplazza-addon-app.Tests/
git add shoplazza-addon-system/shoplazza-addon-functions.Tests/
git commit -m "test: add comprehensive testing infrastructure

- Add NUnit testing framework for backend tests
- Add Jest testing framework for frontend tests
- Add test data factories and utilities
- Add performance testing tools and configurations
- Prepare for comprehensive system validation"
```

### **Commit 2: Add Unit Tests**
```bash
# Add unit tests for all components
git add shoplazza-addon-system/shoplazza-addon-app.Tests/Controllers/
git add shoplazza-addon-system/shoplazza-addon-functions.Tests/
git commit -m "test: add comprehensive unit tests for all components

- Add WidgetController unit tests
- Add HTML template validation tests
- Add WASM function unit tests
- Add price calculator and metafield reader tests
- Ensure 100% code coverage for critical paths"
```

### **Commit 3: Add Integration and E2E Tests**
```bash
# Add integration and end-to-end tests
git add shoplazza-addon-system/shoplazza-addon-app.Tests/Integration/
git add shoplazza-addon-system/shoplazza-addon-app.Tests/E2E/
git commit -m "test: add integration and end-to-end tests

- Add widget to function integration tests
- Add complete add-on workflow tests
- Add error handling and failure scenario tests
- Add performance and load testing
- Complete testing coverage for production readiness"
```

## **ROLLBACK PROCEDURE**

### **If Testing Reveals Critical Issues:**
```bash
# Find the last working commit before testing changes
git log --oneline -10

# Rollback to last working state
git reset --hard <commit-hash>

# Force push if needed (be careful!)
git push --force-with-lease origin main
```

### **Rollback Checkpoints:**
- **Before Testing** - Current working state
- **After Unit Tests** - Basic component testing complete
- **After Integration Tests** - Component interaction testing complete
- **After E2E Tests** - Complete workflow testing complete

## **SUCCESS CRITERIA**

### **Testing Completion:**
- ✅ **100% Unit Test Coverage** - All critical code paths tested
- ✅ **Integration Tests Passing** - Component interactions verified
- ✅ **End-to-End Tests Passing** - Complete workflows validated
- ✅ **Performance Benchmarks Met** - Response times within limits
- ✅ **Error Handling Verified** - All failure scenarios handled

### **Quality Metrics:**
- ✅ **Zero Critical Bugs** - No blocking issues found
- ✅ **Performance Targets Met** - All response time targets achieved
- ✅ **Error Recovery Verified** - System handles failures gracefully
- ✅ **Documentation Complete** - All test results documented
- ✅ **Production Ready** - System validated for deployment

## **NEXT STEPS AFTER TESTING**

1. **Production Deployment** - Deploy validated system
2. **Monitoring Setup** - Production monitoring and alerting
3. **User Acceptance Testing** - Real user validation
4. **Performance Monitoring** - Ongoing performance tracking

---

**This comprehensive testing plan ensures our new cart-transform system is thoroughly validated before production deployment, providing confidence in the system's reliability, performance, and error handling capabilities.**
