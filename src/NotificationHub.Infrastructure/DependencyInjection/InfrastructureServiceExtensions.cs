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
    // Used by BOTH Api and Worker
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();

        // Auth services
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IClock, SystemClock>();

        // Tokens
        services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();

        // Template Repository
        services.AddScoped<ITemplateRepository, TemplateRepository>();


        // Organisation Repository
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrgInviteRepository, OrgInviteRepository>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentOrganization, CurrentOrganization>();

        // Email provider
        services.AddHttpClient<IEmailProvider, SendByteEmailProvider>();

        // Notification provider
        services.AddScoped<INotificationProvider, StubNotificationProvider>();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<INotificationQueue, RedisNotificationQueue>();

        return services;
    }

    // Used by Api ONLY — needs ASP.NET Core routing infrastructure
    public static IServiceCollection AddHttpAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentOrganization, CurrentOrganization>();

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