namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service for loading and processing HTML templates
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Loads a template from the wwwroot/views directory
    /// </summary>
    /// <param name="templatePath">Path to template relative to wwwroot/views</param>
    /// <returns>Template content as string</returns>
    Task<string> LoadTemplateAsync(string templatePath);

    /// <summary>
    /// Processes a template with variable replacements
    /// </summary>
    /// <param name="template">Template content</param>
    /// <param name="variables">Dictionary of variable names and values</param>
    /// <returns>Processed template</returns>
    string ProcessTemplate(string template, Dictionary<string, string> variables);

    /// <summary>
    /// Loads and processes a template in one step
    /// </summary>
    /// <param name="templatePath">Path to template relative to wwwroot/views</param>
    /// <param name="variables">Dictionary of variable names and values</param>
    /// <returns>Processed template</returns>
    Task<string> LoadAndProcessTemplateAsync(string templatePath, Dictionary<string, string> variables);
} 