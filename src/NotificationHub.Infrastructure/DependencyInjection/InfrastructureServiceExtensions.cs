using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Email.Providers;
using NotificationHub.Infrastructure.Messaging.Providers;
using NotificationHub.Infrastructure.Messaging.Redis;
using NotificationHub.Infrastructure.Repositories;
using StackExchange.Redis;

namespace NotificationHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Email provider — HttpClient managed by IHttpClientFactory
        services.AddHttpClient<IEmailProvider, SendByteEmailProvider>();

        // Notification provider — now receives IEmailProvider via DI
        services.AddScoped<INotificationProvider, StubNotificationProvider>();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<INotificationQueue, RedisNotificationQueue>();

        return services;
    }
}