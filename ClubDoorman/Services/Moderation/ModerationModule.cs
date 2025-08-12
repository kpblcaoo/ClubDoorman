using Microsoft.Extensions.DependencyInjection;
using ClubDoorman.Features.Moderation;

namespace ClubDoorman.Services.Moderation;

public static class ModerationModule
{
    public static IServiceCollection AddModerationServices(this IServiceCollection services)
    {
        services.AddSingleton<IModerationFacade, ModerationFacade>();
        return services;
    }
}
