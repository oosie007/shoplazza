using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using ShoplazzaAddonApp.Services;
using Xunit;

namespace ShoplazzaAddonApp.Tests.Unit
{
    public class TemplateServiceIntegrationTests
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<TemplateService> _logger;
        private readonly TemplateService _templateService;

        public TemplateServiceIntegrationTests()
        {
            // Use the actual web host environment from the test project
            _webHostEnvironment = new TestWebHostEnvironment();
            _logger = new TestLogger<TemplateService>();
            _templateService = new TemplateService(_webHostEnvironment, _logger);
        }

        [Fact]
        public async Task LoadAndProcessConfigTemplate_ShouldReplacePlaceholders()
        {
            // Arrange
            var variables = new Dictionary<string, string>
            {
                { "SHOP_DOMAIN", "test-shop.myshoplaza.com" },
                { "ADDON_LIST_HTML", "<div>Test add-on list</div>" }
            };

            // Act
            var result = await _templateService.LoadAndProcessTemplateAsync("merchant/config.html", variables);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("test-shop.myshoplaza.com", result);
            Assert.Contains("<div>Test add-on list</div>", result);
            Assert.DoesNotContain("{{SHOP_DOMAIN}}", result);
            Assert.DoesNotContain("{{ADDON_LIST_HTML}}", result);
            
            // Check that key elements exist in the processed HTML
            Assert.Contains("id=\"productSelect\"", result);
            Assert.Contains("id=\"productIdHidden\"", result);
            Assert.Contains("id=\"variantSelect\"", result);
        }

        [Fact]
        public async Task LoadAndProcessConfigTemplate_ShouldContainRequiredElements()
        {
            // Arrange
            var variables = new Dictionary<string, string>
            {
                { "SHOP_DOMAIN", "test-shop.myshoplaza.com" },
                { "ADDON_LIST_HTML", "" }
            };

            // Act
            var result = await _templateService.LoadAndProcessTemplateAsync("merchant/config.html", variables);

            // Assert
            Assert.NotNull(result);
            
            // Check that all required form elements are present
            Assert.Contains("id=\"addonForm\"", result);
            Assert.Contains("id=\"productSelect\"", result);
            Assert.Contains("id=\"productIdHidden\"", result);
            Assert.Contains("id=\"variantSelect\"", result);
            Assert.Contains("id=\"addOnTitle\"", result);
            Assert.Contains("id=\"addOnDescription\"", result);
            Assert.Contains("id=\"addOnPrice\"", result);
            Assert.Contains("id=\"addOnSku\"", result);
            Assert.Contains("id=\"isActive\"", result);
            
            // Check that JavaScript functions are present
            Assert.Contains("function deleteAddOn", result);
            Assert.Contains("function loadAddOns", result);
            Assert.Contains("function loadProducts", result);
        }

        // Test web host environment for testing
        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; }
            public string ApplicationName { get; set; } = "TestApp";
            public string EnvironmentName { get; set; } = "Test";
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }

            public TestWebHostEnvironment()
            {
                // Get the project root directory (where the .csproj file is)
                var currentDir = Directory.GetCurrentDirectory();
                var projectRoot = currentDir;
                
                // Navigate up from test execution directory to find the main project
                // Tests run from: Tests/bin/Debug/net8.0/
                // We need to go up 4 levels to reach the project root
                if (currentDir.Contains("bin/Debug") || currentDir.Contains("bin/Release"))
                {
                    // Go up 4 levels: bin/Debug/net8.0/ -> Tests/ -> project root
                    projectRoot = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
                }
                else if (currentDir.EndsWith("Tests"))
                {
                    // If we're in the Tests directory, go up one level
                    projectRoot = Path.GetFullPath(Path.Combine(currentDir, ".."));
                }
                
                // Set the wwwroot path
                var wwwrootPath = Path.Combine(projectRoot, "wwwroot");
                WebRootPath = wwwrootPath;
                ContentRootPath = projectRoot;
                
                // Create file providers with error handling
                try
                {
                    if (Directory.Exists(projectRoot))
                    {
                        ContentRootFileProvider = new PhysicalFileProvider(projectRoot);
                    }
                    else
                    {
                        // Fallback: use current directory
                        ContentRootFileProvider = new PhysicalFileProvider(currentDir);
                    }
                    
                    if (Directory.Exists(wwwrootPath))
                    {
                        WebRootFileProvider = new PhysicalFileProvider(wwwrootPath);
                    }
                    else
                    {
                        // Fallback: use content root
                        WebRootFileProvider = ContentRootFileProvider;
                    }
                }
                catch
                {
                    // If file provider creation fails, use null providers
                    ContentRootFileProvider = new PhysicalFileProvider(currentDir);
                    WebRootFileProvider = ContentRootFileProvider;
                }
            }
        }

        // Test logger for testing
        private class TestLogger<T> : ILogger<T>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
