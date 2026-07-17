using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationHub.Infrastructure.DependencyInjection;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<CampaignWorker>();
builder.Services.AddHostedService<KeepAliveWorker>();
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddHostedService<ImportWorker>();

var host = builder.Build();
host.Run();