using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Effects.Channel;
using ClubDoorman.Effects;

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
            // Пытаемся получить эффекты (могут отсутствовать если не зарегистрированы на раннем этапе)
            var channelEffectsBuilder = provider.GetService<IChannelModerationEffectsBuilder>();
            var effectBus = provider.GetService<IEffectBus>();
            var flags = provider.GetRequiredService<IChannelEffectsFlags>();
            return new ChannelModerationService(
                provider.GetRequiredService<ITelegramBotClientWrapper>(),
                provider.GetRequiredService<IModerationService>(),
                provider.GetRequiredService<IUserBanService>(),
                provider.GetRequiredService<ILogger<ChannelModerationService>>(),
                flags,
                channelEffectsBuilder,
                effectBus);
        });
        services.AddSingleton<IChannelEffectsFlags, ChannelEffectsFlags>();

        return services;
    }
}
