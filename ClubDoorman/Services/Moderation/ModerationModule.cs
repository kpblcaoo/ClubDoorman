using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Features.Moderation;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Moderation;

public static class ModerationModule
{
    public static IServiceCollection AddModerationServices(this IServiceCollection services)
    {
        // LEGACY NOTE:
        // Раньше здесь регистрировался IModerationFacade (дублировало Feature: AddModerationFeature).
        // Теперь единственный источник регистрации фасада — Feature слой.
        // Оставляем здесь только мост (адаптер) IModerationService -> IModerationPolicy для старых потребителей.

        // Регистрация IModerationService с фабрикой как в Program.cs
        services.AddSingleton<IModerationService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ModerationServiceAdapter>>();
            logger.LogDebug("[DI] IModerationService factory called");
            return new ModerationServiceAdapter(
                provider.GetRequiredService<IModerationPolicy>());
        });

        return services;
    }
}
