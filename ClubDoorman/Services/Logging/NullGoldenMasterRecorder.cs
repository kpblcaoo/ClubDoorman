using Telegram.Bot.Types;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// No-op реализация IGoldenMasterRecorder (используется в тестах / старых конструкторах).
/// </summary>
internal sealed class NullGoldenMasterRecorder : IGoldenMasterRecorder
{
    public static readonly NullGoldenMasterRecorder Instance = new();
    private NullGoldenMasterRecorder() { }
    public string? TryRecordInput(Update update, string handlerName, long? chatId, long? userId) => null;
    public void TryRecordOutput(string? correlationId, object? resultPayload) { }
}
