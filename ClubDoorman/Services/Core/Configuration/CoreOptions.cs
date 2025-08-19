namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Базовые параметры ядра приложения / инфраструктуры.
/// Источник значений: переменные окружения DOORMAN_* (пока без appsettings).
/// </summary>
public class CoreOptions
{
    public string? BotApi { get; set; }
    public long AdminChatId { get; set; } = 123456789; // тестовое значение по умолчанию
    public long LogAdminChatId { get; set; } // если 0 -> использовать AdminChatId
    public string? ClubServiceToken { get; set; }
    public string ClubUrl { get; set; } = "https://vas3k.club/";
}
