using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.UserJoin;

public static class UserJoinModule
{
    public static IServiceCollection AddUserJoinServices(this IServiceCollection services)
    {
        services.AddScoped<IFolderInviteService, FolderInviteService>();
        return services;
    }
}
