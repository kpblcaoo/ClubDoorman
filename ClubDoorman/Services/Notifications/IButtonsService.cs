using Telegram.Bot.Types;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Models.Notifications; // added for SuspiciousMessageNotificationData

namespace ClubDoorman.Services.Notifications;

public interface IButtonsService
{
    Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken);
}
