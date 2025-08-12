using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using ClubDoorman.Services.Messaging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeMessageService : IMessageService
{
    public Task SendAdminNotificationAsync(AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task SendLogNotificationAsync(LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task SendUserNotificationAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<Message> SendUserNotificationWithReplyAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Message());
    }

    public Task<Message> SendUserNotificationWithReplyAsync(User user, Chat chat, UserNotificationType type, object data, ReplyParameters replyParameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Message());
    }

    public Task<Message?> SendWelcomeMessageAsync(SendWelcomeMessageRequest request)
    {
        return Task.FromResult<Message?>(new Message());
    }

    public Task<Message?> SendWelcomeMessageAsync(User user, Chat chat, string reason = "приветствие", CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Message?>(new Message());
    }

    public Task<Message> SendCaptchaMessageAsync(SendCaptchaMessageRequest request)
    {
        return Task.FromResult(new Message());
    }
    
    public Task<Message?> ForwardToAdminWithNotificationAsync(Message originalMessage, AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Message?>(new Message());
    }
    
    public Task<Message?> ForwardToLogWithNotificationAsync(Message originalMessage, LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Message?>(new Message());
    }
    
    public Task SendErrorNotificationAsync(SendErrorNotificationRequest request)
    {
        return Task.CompletedTask;
    }
    
    public Task SendAiProfileAnalysisAsync(AiProfileAnalysisData data, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public MessageTemplates GetTemplates()
    {
        return new MessageTemplates();
    }
}
