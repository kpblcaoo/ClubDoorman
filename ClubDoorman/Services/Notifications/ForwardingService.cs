using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums; // added for ChatType
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Notifications;

public class ForwardingService : IForwardingService
{
    private readonly ILogger<ForwardingService> _logger;

    public ForwardingService(ILogger<ForwardingService> logger)
    {
        _logger = logger;
    }

    // WRAP of MessageHandler.IsChannelDiscussion without logic changes
    public Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        try
        {
            if (chat.Type != ChatType.Supergroup)
                return Task.FromResult(false);

            // Проверяем, является ли это автоматическим пересыланием из канала
            var isAutoForward = message.IsAutomaticForward;
            
            if (isAutoForward)
            {
                _logger.LogDebug("Обнаружено обсуждение канала: chat={ChatId}, autoForward={AutoForward}", 
                    chat.Id, message.IsAutomaticForward);
            }
            
            return Task.FromResult(isAutoForward);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось определить тип чата {ChatId}", chat.Id);
            return Task.FromResult(false);
        }
    }
}
