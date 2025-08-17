using ClubDoorman.Effects;
using ClubDoorman.Effects.Delete;
using ClubDoorman.Effects.Report;
using ClubDoorman.Effects.Ban;
using ClubDoorman.Effects.Allow;
using ClubDoorman.Effects.ManualReview;
using ClubDoorman.Effects.AiAnalysis;
using ClubDoorman.Models;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.AI;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using System.Collections.Generic;

namespace ClubDoorman.Features.Moderation;

/// <summary>
/// Интерфейс для построения эффектов модерации
/// <tags>effects, moderation, builder, interface</tags>
/// </summary>
public interface IModerationEffectsBuilder
{
    /// <summary>
    /// Строит массив эффектов для обработки сообщения
    /// </summary>
    /// <param name="message">Сообщение для обработки</param>
    /// <param name="result">Результат модерации</param>
    /// <param name="isSilentMode">Тихий режим</param>
    /// <returns>Массив эффектов для выполнения</returns>
    IEffect[] BuildEffects(Message message, ModerationResult result, bool isSilentMode);
}

/// <summary>
/// Реальный билдер эффектов модерации
/// Будет заполняться по мере миграции действий
/// <tags>effects, moderation, builder, real</tags>
/// </summary>
public class ModerationEffectsBuilder : IModerationEffectsBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModerationEffectsBuilder> _logger;

    public ModerationEffectsBuilder(
        IServiceProvider serviceProvider,
        ILogger<ModerationEffectsBuilder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IEffect[] BuildEffects(Message message, ModerationResult result, bool isSilentMode)
    {
        var effects = new List<IEffect>();

        switch (result.Action)
        {
            case ModerationAction.Delete:
                _logger.LogInformation("Удаление сообщения: {Reason}", result.Reason);

                if (result.Reason.Contains("Ссылки запрещены") || result.Reason.Contains("Банальное приветствие"))
                {
                    effects.Add(new DeleteToLogEffect(
                        _serviceProvider.GetRequiredService<INotificationService>(),
                        _serviceProvider.GetRequiredService<ILogger<DeleteToLogEffect>>(),
                        message,
                        result.Reason));
                }
                else
                {
                    effects.Add(new DeleteWithReportEffect(
                        _serviceProvider.GetRequiredService<INotificationService>(),
                        _serviceProvider.GetRequiredService<ILogger<DeleteWithReportEffect>>(),
                        message,
                        result.Reason,
                        isSilentMode));
                }

                                            effects.Add(new TrackViolationEffect(
                                _serviceProvider.GetRequiredService<IUserBanService>(),
                                _serviceProvider.GetRequiredService<ILogger<TrackViolationEffect>>(),
                                message,
                                message.From!,
                                result.Reason));
                            break;

                        case ModerationAction.Report:
                            _logger.LogInformation("Отправка в админ-чат: {Reason}", result.Reason);
                            effects.Add(new ReportMessageEffect(
                                _serviceProvider.GetRequiredService<INotificationService>(),
                                _serviceProvider.GetRequiredService<ILogger<ReportMessageEffect>>(),
                                message,
                                message.From!,
                                isSilentMode));
                            break;

                        case ModerationAction.Ban:
                            _logger.LogInformation("Бан пользователя: {Reason}", result.Reason);
                            effects.Add(new BanUserEffect(
                                _serviceProvider.GetRequiredService<IUserBanService>(),
                                _serviceProvider.GetRequiredService<IUserFlowLogger>(),
                                _serviceProvider.GetRequiredService<ILogger<BanUserEffect>>(),
                                message,
                                message.From!,
                                message.Chat,
                                result.Reason));
                            break;

                        case ModerationAction.Allow:
                            _logger.LogDebug("Разрешение сообщения: {Reason}", result.Reason);
                            effects.Add(new AllowMessageEffect(
                                _serviceProvider.GetRequiredService<IModerationPolicy>(),
                                _serviceProvider.GetRequiredService<ILogger<AllowMessageEffect>>(),
                                message,
                                message.From!,
                                message.Chat,
                                result.Reason));
                            break;

                        case ModerationAction.RequireManualReview:
                            _logger.LogInformation("Требует ручной проверки: {Reason}", result.Reason);
                            effects.Add(new RequireManualReviewEffect(
                                _serviceProvider.GetRequiredService<INotificationService>(),
                                _serviceProvider.GetRequiredService<ILogger<RequireManualReviewEffect>>(),
                                message,
                                message.From!,
                                isSilentMode));
                            break;

                        case ModerationAction.RequireAiAnalysis:
                            _logger.LogInformation("ML не уверен, запускаем AI анализ: {Reason}", result.Reason);
                            effects.Add(new RequireAiAnalysisEffect(
                                _serviceProvider.GetRequiredService<IAiCascadeService>(),
                                _serviceProvider.GetRequiredService<ILogger<RequireAiAnalysisEffect>>(),
                                message,
                                message.From!,
                                result.Confidence ?? 0,
                                isSilentMode));
                            break;

                        default:
                            _logger.LogDebug("Real effects not implemented yet for action: {Action}", result.Action);
                            break;
        }

        return effects.ToArray();
    }
}
