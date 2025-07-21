namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для валидации ресурсов локализации
/// </summary>
public interface ILocalizationValidator
{
    /// <summary>
    /// Валидировать все ресурсы локализации
    /// </summary>
    /// <returns>Результат валидации</returns>
    LocalizationValidationResult ValidateAllResources();
    
    /// <summary>
    /// Валидировать конкретный ресурсный файл
    /// </summary>
    /// <param name="resourceName">Имя ресурса (UserMessages, AdminMessages, SystemMessages)</param>
    /// <returns>Результат валидации</returns>
    LocalizationValidationResult ValidateResource(string resourceName);
    
    /// <summary>
    /// Проверить наличие всех ключей в ресурсах
    /// </summary>
    /// <returns>Результат проверки ключей</returns>
    LocalizationValidationResult ValidateKeys();
    
    /// <summary>
    /// Проверить форматирование строк (наличие плейсхолдеров)
    /// </summary>
    /// <returns>Результат проверки форматирования</returns>
    LocalizationValidationResult ValidateFormatting();
}

/// <summary>
/// Результат валидации локализации
/// </summary>
public class LocalizationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
    
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
} 