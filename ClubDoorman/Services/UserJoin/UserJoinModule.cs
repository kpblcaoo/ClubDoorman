using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.UserJoin;

public static class UserJoinModule
{
    public static IServiceCollection AddUserJoinServices(this IServiceCollection services)
    {
        services.AddSingleton<IFolderInviteService, FolderInviteService>();
        return services;
    }
}
