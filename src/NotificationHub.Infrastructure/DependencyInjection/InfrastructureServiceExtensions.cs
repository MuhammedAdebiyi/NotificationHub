using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Repositories;

namespace NotificationHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        return services;
    }
}