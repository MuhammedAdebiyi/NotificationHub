using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Messaging.Redis;
using NotificationHub.Infrastructure.Repositories;
using StackExchange.Redis;
using NotificationHub.Infrastructure.Messaging.Providers;

namespace NotificationHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationProvider, StubNotificationProvider>();

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<INotificationQueue, RedisNotificationQueue>();

        return services;
    }
}