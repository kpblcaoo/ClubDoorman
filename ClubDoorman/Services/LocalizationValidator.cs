using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация валидатора ресурсов локализации
/// </summary>
public class LocalizationValidator : ILocalizationValidator
{
    private readonly ILogger<LocalizationValidator> _logger;
    private readonly Dictionary<string, System.Resources.ResourceManager> _resourceManagers;
    private readonly string[] _supportedCultures = { "", "ru" }; // "" = default (en), "ru" = Russian
    
    public LocalizationValidator(ILogger<LocalizationValidator> logger)
    {
        _logger = logger;
        _resourceManagers = new Dictionary<string, System.Resources.ResourceManager>
        {
            ["UserMessages"] = new System.Resources.ResourceManager("ClubDoorman.Resources.UserMessages", Assembly.GetExecutingAssembly()),
            ["AdminMessages"] = new System.Resources.ResourceManager("ClubDoorman.Resources.AdminMessages", Assembly.GetExecutingAssembly()),
            ["SystemMessages"] = new System.Resources.ResourceManager("ClubDoorman.Resources.SystemMessages", Assembly.GetExecutingAssembly())
        };
    }
    
    /// <summary>
    /// Валидировать все ресурсы локализации
    /// </summary>
    public LocalizationValidationResult ValidateAllResources()
    {
        var result = new LocalizationValidationResult { IsValid = true };
        
        _logger.LogInformation("Starting validation of all localization resources");
        
        foreach (var resourceName in _resourceManagers.Keys)
        {
            var resourceResult = ValidateResource(resourceName);
            if (!resourceResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(resourceResult.Errors);
            }
            result.Warnings.AddRange(resourceResult.Warnings);
        }
        
        // Дополнительные проверки
        var keysResult = ValidateKeys();
        if (!keysResult.IsValid)
        {
            result.IsValid = false;
            result.Errors.AddRange(keysResult.Errors);
        }
        
        var formattingResult = ValidateFormatting();
        if (!formattingResult.IsValid)
        {
            result.IsValid = false;
            result.Errors.AddRange(formattingResult.Errors);
        }
        
        result.Warnings.AddRange(keysResult.Warnings);
        result.Warnings.AddRange(formattingResult.Warnings);
        
        // Статистика
        result.Statistics["TotalResources"] = _resourceManagers.Count;
        result.Statistics["SupportedCultures"] = _supportedCultures.Length;
        result.Statistics["TotalErrors"] = result.Errors.Count;
        result.Statistics["TotalWarnings"] = result.Warnings.Count;
        
        _logger.LogInformation("Localization validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}", 
            result.IsValid, result.Errors.Count, result.Warnings.Count);
        
        return result;
    }
    
    /// <summary>
    /// Валидировать конкретный ресурсный файл
    /// </summary>
    public LocalizationValidationResult ValidateResource(string resourceName)
    {
        var result = new LocalizationValidationResult { IsValid = true };
        
        if (!_resourceManagers.TryGetValue(resourceName, out var resourceManager))
        {
            result.AddError($"Resource manager not found for '{resourceName}'");
            return result;
        }
        
        _logger.LogDebug("Validating resource: {ResourceName}", resourceName);
        
        // Получаем все ключи из основного ресурса (en)
        var defaultCulture = new CultureInfo("");
        var resourceSet = resourceManager.GetResourceSet(defaultCulture, true, true);
        if (resourceSet == null)
        {
            result.AddError($"Cannot load resource set for '{resourceName}'");
            return result;
        }
        
        var keys = new List<string>();
        foreach (System.Collections.DictionaryEntry entry in resourceSet)
        {
            keys.Add(entry.Key.ToString()!);
        }
        
        result.Statistics["TotalKeys"] = keys.Count;
        
        // Проверяем каждый ключ во всех культурах
        foreach (var key in keys)
        {
            foreach (var cultureCode in _supportedCultures)
            {
                var culture = string.IsNullOrEmpty(cultureCode) ? defaultCulture : new CultureInfo(cultureCode);
                var value = resourceManager.GetString(key, culture);
                
                if (string.IsNullOrEmpty(value))
                {
                    result.AddError($"Missing key '{key}' in resource '{resourceName}' for culture '{culture.Name}'");
                }
                else
                {
                    // Проверяем на пустые строки
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        result.AddWarning($"Empty value for key '{key}' in resource '{resourceName}' for culture '{culture.Name}'");
                    }
                    
                    // Проверяем на дубликаты
                    var duplicateCount = keys.Count(k => k == key);
                    if (duplicateCount > 1)
                    {
                        result.AddWarning($"Duplicate key '{key}' found {duplicateCount} times in resource '{resourceName}'");
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Проверить наличие всех ключей в ресурсах
    /// </summary>
    public LocalizationValidationResult ValidateKeys()
    {
        var result = new LocalizationValidationResult { IsValid = true };
        
        // Получаем все ключи из всех ресурсов
        var allKeys = new Dictionary<string, HashSet<string>>();
        
        foreach (var (resourceName, resourceManager) in _resourceManagers)
        {
            var defaultCulture = new CultureInfo("");
            var resourceSet = resourceManager.GetResourceSet(defaultCulture, true, true);
            if (resourceSet == null) continue;
            
            var keys = new HashSet<string>();
            foreach (System.Collections.DictionaryEntry entry in resourceSet)
            {
                keys.Add(entry.Key.ToString()!);
            }
            allKeys[resourceName] = keys;
        }
        
        // Проверяем, что все ресурсы имеют одинаковые ключи
        if (allKeys.Count > 1)
        {
            var firstResource = allKeys.First();
            var firstKeys = firstResource.Value;
            
            foreach (var (resourceName, keys) in allKeys.Skip(1))
            {
                var missingKeys = firstKeys.Except(keys).ToList();
                var extraKeys = keys.Except(firstKeys).ToList();
                
                if (missingKeys.Any())
                {
                    result.AddError($"Resource '{resourceName}' is missing keys: {string.Join(", ", missingKeys)}");
                }
                
                if (extraKeys.Any())
                {
                    result.AddWarning($"Resource '{resourceName}' has extra keys: {string.Join(", ", extraKeys)}");
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Проверить форматирование строк (наличие плейсхолдеров)
    /// </summary>
    public LocalizationValidationResult ValidateFormatting()
    {
        var result = new LocalizationValidationResult { IsValid = true };
        
        // Регулярное выражение для поиска плейсхолдеров {0}, {1}, {2} и т.д.
        var placeholderRegex = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);
        
        foreach (var (resourceName, resourceManager) in _resourceManagers)
        {
            var defaultCulture = new CultureInfo("");
            var resourceSet = resourceManager.GetResourceSet(defaultCulture, true, true);
            if (resourceSet == null) continue;
            
            foreach (System.Collections.DictionaryEntry entry in resourceSet)
            {
                var key = entry.Key.ToString()!;
                var value = entry.Value?.ToString() ?? "";
                
                var matches = placeholderRegex.Matches(value);
                var placeholders = matches.Select(m => int.Parse(m.Groups[1].Value)).OrderBy(p => p).ToList();
                
                // Проверяем последовательность плейсхолдеров
                for (int i = 0; i < placeholders.Count; i++)
                {
                    if (placeholders[i] != i)
                    {
                        result.AddWarning($"Non-sequential placeholder in '{resourceName}.{key}': expected {i}, found {placeholders[i]}");
                    }
                }
                
                // Проверяем, что нет пропущенных плейсхолдеров
                if (placeholders.Count > 0)
                {
                    var expectedPlaceholders = Enumerable.Range(0, placeholders.Count).ToList();
                    var missingPlaceholders = expectedPlaceholders.Except(placeholders).ToList();
                    
                    if (missingPlaceholders.Any())
                    {
                        result.AddWarning($"Missing placeholders in '{resourceName}.{key}': {string.Join(", ", missingPlaceholders)}");
                    }
                }
            }
        }
        
        return result;
    }
} 