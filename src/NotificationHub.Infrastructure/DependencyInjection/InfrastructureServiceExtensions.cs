using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Auth;
using NotificationHub.Infrastructure.Common;
using NotificationHub.Infrastructure.Email.Providers;
using NotificationHub.Infrastructure.Messaging.Providers;
using NotificationHub.Infrastructure.Messaging.Redis;
using NotificationHub.Infrastructure.Repositories;
using NotificationHub.Shared.Abstractions;
using StackExchange.Redis;
using System.Text;

namespace NotificationHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Tokens
        services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();

        // Auth services
        services.AddScoped<IPasswordHasher, Infrastructure.Auth.PasswordHasherService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IClock, SystemClock>();

        // Email provider
        services.AddHttpClient<IEmailProvider, SendByteEmailProvider>();

        // Notification provider
        services.AddScoped<INotificationProvider, StubNotificationProvider>();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<INotificationQueue, RedisNotificationQueue>();

        // JWT auth
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "NotificationHub",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "NotificationHub",
                    ValidateLifetime = true,
                };
            });

        services.AddAuthorization();

        return services;
    }
}