using Microsoft.Extensions.DependencyInjection;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Services;

namespace NotificationHub.Infrastructure.DependencyInjection;

/// <summary>
/// Add this call inside your existing Infrastructure AddInfrastructure() extension method.
///
///   services.AddAnalytics();
///
/// </summary>
public static class AnalyticsRegistration
{
    public static IServiceCollection AddAnalytics(this IServiceCollection services)
    {
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        return services;
    }
}