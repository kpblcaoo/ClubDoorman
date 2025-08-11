using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.Notification;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        // No-op: Notification services are registered in MessagingModule now
        return services;
    }
}
