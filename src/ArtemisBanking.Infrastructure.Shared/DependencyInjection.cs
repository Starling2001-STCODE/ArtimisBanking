using ArtemisBanking.Infrastructure.Shared.Interfaces;
using ArtemisBanking.Infrastructure.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBanking.Infrastructure.Shared.DependencyInjection;

public static class SharedServicesRegistration
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
