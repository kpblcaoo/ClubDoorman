using ClubDoorman.Models.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Приватный хелпер для записи Golden Master данных
/// </summary>
internal static class GoldenMasterRecorder
{
    private static readonly string BaseGoldenPath = "golden";

    /// <summary>
    /// Записывает входные и выходные данные в Golden Master файл
    /// </summary>
    public static async Task RecordAsync(
        object input, 
        object output, 
        string handlerName, 
        long messageId, 
        LoggingFlags flags, 
        ILogger logger)
    {
        if (!flags.GoldenMasterEnabled)
            return;

        try
        {
            // Проверяем сэмплирование
            if (!ShouldRecord(messageId, flags.GoldenSampleRate))
                return;

            var data = new
            {
                input = input,
                output = output,
                timestamp = DateTime.UtcNow,
                handlerName = handlerName,
                messageId = messageId
            };

            var canonicalJson = JsonCanonicalizer.Canonicalize(data);
            var filePath = GenerateFilePath(handlerName, messageId);

            // Создаем директории если не существуют
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, canonicalJson);
            
            logger.LogDebug("Golden Master записан: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка записи Golden Master для handler {HandlerName}, messageId {MessageId}", 
                handlerName, messageId);
        }
    }

    /// <summary>
    /// Определяет, нужно ли записывать данное сообщение на основе сэмплирования
    /// </summary>
    private static bool ShouldRecord(long messageId, double sampleRate)
    {
        if (sampleRate <= 0) return false;
        if (sampleRate >= 1.0) return true;

        // Используем messageId для детерминированного сэмплирования
        var hash = Math.Abs(messageId.GetHashCode()) % 100;
        return hash < (sampleRate * 100);
    }

    /// <summary>
    /// Генерирует путь к файлу Golden Master
    /// </summary>
    private static string GenerateFilePath(string handlerName, long messageId)
    {
        var dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fileName = $"{messageId}.json";
        
        return Path.Combine(BaseGoldenPath, dateFolder, handlerName, fileName);
    }

    /// <summary>
    /// Читает Golden Master файл для тестирования
    /// </summary>
    public static async Task<string?> ReadAsync(string handlerName, long messageId, DateTime? date = null)
    {
        try
        {
            var dateFolder = (date ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
            var filePath = Path.Combine(BaseGoldenPath, dateFolder, handlerName, $"{messageId}.json");
            
            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllTextAsync(filePath);
        }
        catch
        {
            return null;
        }
    }
}