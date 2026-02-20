using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ShoplazzaAddonApp.Services;
using Xunit;

namespace ShoplazzaAddonApp.Tests.Unit
{
    public class TemplateServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly Mock<ILogger<TemplateService>> _mockLogger;
        private readonly TemplateService _templateService;

        public TemplateServiceTests()
        {
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockLogger = new Mock<ILogger<TemplateService>>();
            
            // Set up the web root path to point to our test directory
            _mockWebHostEnvironment.Setup(x => x.WebRootPath).Returns("wwwroot");
            
            _templateService = new TemplateService(_mockWebHostEnvironment.Object, _mockLogger.Object);
        }

        [Fact]
        public void ProcessTemplate_ShouldReplacePlaceholders()
        {
            // Arrange
            var template = "Hello {{NAME}}, welcome to {{SHOP}}!";
            var variables = new Dictionary<string, string>
            {
                { "NAME", "John" },
                { "SHOP", "MyStore" }
            };

            // Act
            var result = _templateService.ProcessTemplate(template, variables);

            // Assert
            Assert.Equal("Hello John, welcome to MyStore!", result);
        }

        [Fact]
        public void ProcessTemplate_ShouldHandleEmptyVariables()
        {
            // Arrange
            var template = "Hello {{NAME}}, welcome to {{SHOP}}!";
            var variables = new Dictionary<string, string>
            {
                { "NAME", "" },
                { "SHOP", null }
            };

            // Act
            var result = _templateService.ProcessTemplate(template, variables);

            // Assert
            Assert.Equal("Hello , welcome to !", result);
        }

        [Fact]
        public void ProcessTemplate_ShouldHandleNoVariables()
        {
            // Arrange
            var template = "Hello World!";
            var variables = new Dictionary<string, string>();

            // Act
            var result = _templateService.ProcessTemplate(template, variables);

            // Assert
            Assert.Equal("Hello World!", result);
        }

        [Fact]
        public void ProcessTemplate_ShouldHandleUnusedPlaceholders()
        {
            // Arrange
            var template = "Hello {{NAME}}, welcome to {{SHOP}}!";
            var variables = new Dictionary<string, string>
            {
                { "NAME", "John" }
                // SHOP placeholder not provided
            };

            // Act
            var result = _templateService.ProcessTemplate(template, variables);

            // Assert
            Assert.Equal("Hello John, welcome to {{SHOP}}!", result);
        }

        [Fact]
        public void ProcessTemplate_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var template = "Price: {{PRICE}}, Description: {{DESC}}";
            var variables = new Dictionary<string, string>
            {
                { "PRICE", "$19.99" },
                { "DESC", "Product with \"quotes\" and <tags>" }
            };

            // Act
            var result = _templateService.ProcessTemplate(template, variables);

            // Assert
            Assert.Equal("Price: $19.99, Description: Product with \"quotes\" and <tags>", result);
        }
    }
}
