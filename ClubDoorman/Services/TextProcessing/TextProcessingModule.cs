using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.TextProcessing;

public static class TextProcessingModule
{
    public static IServiceCollection AddTextProcessingServices(this IServiceCollection services)
    {
        // TextProcessor is a static class, so no DI registration needed
        // This module is created for consistency with the modular architecture
        return services;
    }
} 