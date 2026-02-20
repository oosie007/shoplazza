using Microsoft.AspNetCore.Hosting;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Implementation of template service for loading and processing HTML templates
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(IWebHostEnvironment webHostEnvironment, ILogger<TemplateService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task<string> LoadTemplateAsync(string templatePath)
    {
        try
        {
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "views", templatePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogError("Template file not found: {TemplatePath}", fullPath);
                throw new FileNotFoundException($"Template file not found: {templatePath}");
            }

            return await File.ReadAllTextAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading template: {TemplatePath}", templatePath);
            throw;
        }
    }

    public string ProcessTemplate(string template, Dictionary<string, string> variables)
    {
        try
        {
            var processedTemplate = template;

            foreach (var variable in variables)
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                processedTemplate = processedTemplate.Replace(placeholder, variable.Value ?? string.Empty);
            }

            return processedTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template with variables");
            throw;
        }
    }

    public async Task<string> LoadAndProcessTemplateAsync(string templatePath, Dictionary<string, string> variables)
    {
        var template = await LoadTemplateAsync(templatePath);
        return ProcessTemplate(template, variables);
    }
} 