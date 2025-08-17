using ClubDoorman.Services.AI;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Effects.AiAnalysis;

/// <summary>
/// Эффект для AI анализа сообщений
/// <tags>effects, ai-analysis, moderation</tags>
/// </summary>
public class RequireAiAnalysisEffect : IEffect
{
    private readonly IAiCascadeService _aiCascadeService;
    private readonly ILogger<RequireAiAnalysisEffect> _logger;
    private readonly Message _message;
    private readonly User _user;
    private readonly double _mlScore;
    private readonly bool _isSilentMode;

    public RequireAiAnalysisEffect(
        IAiCascadeService aiCascadeService,
        ILogger<RequireAiAnalysisEffect> logger,
        Message message,
        User user,
        double mlScore,
        bool isSilentMode)
    {
        _aiCascadeService = aiCascadeService;
        _logger = logger;
        _message = message;
        _user = user;
        _mlScore = mlScore;
        _isSilentMode = isSilentMode;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ML не уверен, запускаем AI анализ: {Reason}", "RequireAiAnalysis");
        await _aiCascadeService.HandleAiCascadeAnalysisAsync(_message, _user, _mlScore, _isSilentMode, ct);
    }
}
