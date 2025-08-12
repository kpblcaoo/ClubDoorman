using System.Text;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.LinkFormatting;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Обработчик команды /stats для отображения статистики по группам (алиас для /stat)
/// </summary>
public class StatsAliasCommandHandler : ICommandHandler
{
    private readonly StatsCommandHandler _statsCommandHandler;

    public string CommandName => "stats";

    public StatsAliasCommandHandler(StatsCommandHandler statsCommandHandler)
    {
        _statsCommandHandler = statsCommandHandler ?? throw new ArgumentNullException(nameof(statsCommandHandler));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _statsCommandHandler.HandleAsync(message, cancellationToken);
    }
}