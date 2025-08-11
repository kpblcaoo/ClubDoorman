using Telegram.Bot.Types;
using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Services.Notifications;

public interface IButtonsService
{
    Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken);
}
