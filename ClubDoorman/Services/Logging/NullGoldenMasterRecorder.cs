using Telegram.Bot.Types;
using System;

namespace ClubDoorman.Services.Logging;

/// <summary>
/// No-op реализация IGoldenMasterRecorder (используется в тестах / старых конструкторах).
/// </summary>
internal sealed class NullGoldenMasterRecorder : IGoldenMasterRecorder
{
    public static readonly NullGoldenMasterRecorder Instance = new();
    private static bool _warned;
    public static int UsageCount; // диагностика: сколько раз вызывался TryRecordInput
    private NullGoldenMasterRecorder() { }
    public string? TryRecordInput(Update update, string handlerName, long? chatId, long? userId)
    {
        UsageCount++;
        if (!_warned)
        {
            _warned = true;
            // Одноразовое предупреждение в stdout, чтобы не засорять логи
            Console.WriteLine("[GM-WARN] NullGoldenMasterRecorder active (handler=" + handlerName + ") — снапшоты не пишутся. Мигрируй на DI-конструктор.");
        }
        return null;
    }
    public void TryRecordOutput(string? correlationId, object? resultPayload) { }
}
