using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// Приватный хелпер для канонизации JSON данных для Golden Master
/// </summary>
internal static class JsonCanonicalizer
{
    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Канонизирует объект в стабильный JSON формат
    /// </summary>
    public static string Canonicalize(object obj)
    {
        if (obj == null) return "null";

        // Сериализуем в JSON
        var json = JsonSerializer.Serialize(obj, CanonicalOptions);

        // Применяем канонизацию
        json = MaskPiiData(json);
        json = NormalizeTimestamps(json);
        json = NormalizeGuids(json);
        json = RoundNumbers(json);
        json = SortCollections(json);

        return json;
    }

    /// <summary>
    /// Маскирует персональные данные
    /// </summary>
    private static string MaskPiiData(string json)
    {
        // Маскируем токены
        json = Regex.Replace(json, @"\b\d{10}:[A-Za-z0-9_-]{35}\b", "BOT_TOKEN_MASKED");
        
        // Маскируем телефоны
        json = Regex.Replace(json, @"\+?\d{10,15}", "PHONE_MASKED");
        
        // Маскируем username (оставляем только первую букву)
        json = Regex.Replace(json, @"""@(\w)\w+""", @"""@$1***""");
        
        return json;
    }

    /// <summary>
    /// Нормализует временные метки к фиксированному значению
    /// </summary>
    private static string NormalizeTimestamps(string json)
    {
        // ISO 8601 даты
        json = Regex.Replace(json, @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?", "2024-01-01T00:00:00Z");
        
        // Unix timestamps
        json = Regex.Replace(json, @"\b1[6-7]\d{8}\b", "1640995200");
        
        return json;
    }

    /// <summary>
    /// Нормализует GUID к фиксированному значению
    /// </summary>
    private static string NormalizeGuids(string json)
    {
        return Regex.Replace(json, @"\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\b", 
            "00000000-0000-0000-0000-000000000000");
    }

    /// <summary>
    /// Округляет числа до 3 знаков после запятой
    /// </summary>
    private static string RoundNumbers(string json)
    {
        return Regex.Replace(json, @"\b\d+\.\d{4,}\b", match =>
        {
            if (double.TryParse(match.Value, out var number))
            {
                return Math.Round(number, 3).ToString("F3");
            }
            return match.Value;
        });
    }

    /// <summary>
    /// Сортирует коллекции для стабильного порядка
    /// </summary>
    private static string SortCollections(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var sortedJson = SortJsonElement(document.RootElement);
            return JsonSerializer.Serialize(sortedJson, CanonicalOptions);
        }
        catch
        {
            // Если не удалось распарсить, возвращаем как есть
            return json;
        }
    }

    private static object SortJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .OrderBy(p => p.Name)
                .ToDictionary(p => p.Name, p => SortJsonElement(p.Value)),
            
            JsonValueKind.Array => element.EnumerateArray()
                .Select(SortJsonElement)
                .ToArray(),
            
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}