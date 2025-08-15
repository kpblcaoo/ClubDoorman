using Telegram.Bot.Types;
using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Services.Notifications;

public interface IForwardingService
{
    Task<bool> IsChannelDiscussion(Chat chat, Message message);
}
