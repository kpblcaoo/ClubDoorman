using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Features.Moderation;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Services.Moderation;

public static class ModerationModule
{
    public static IServiceCollection AddModerationServices(this IServiceCollection services)
    {
        services.AddSingleton<IModerationFacade, ModerationFacade>();
        
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
