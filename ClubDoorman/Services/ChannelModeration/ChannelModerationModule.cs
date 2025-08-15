using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;

namespace ClubDoorman.Services.ChannelModeration;

public static class ChannelModerationModule
{
    public static IServiceCollection AddChannelModerationServices(this IServiceCollection services)
    {
        // Регистрация IChannelModerationService с фабрикой как в Program.cs
        services.AddSingleton<IChannelModerationService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ChannelModerationService>>();
            logger.LogDebug("[DI] IChannelModerationService factory called");
            return new ChannelModerationService(
                provider.GetRequiredService<ITelegramBotClientWrapper>(),
                provider.GetRequiredService<IModerationService>(),
                provider.GetRequiredService<IUserBanService>(),
                provider.GetRequiredService<ILogger<ChannelModerationService>>());
        });

        return services;
    }
}
