using Microsoft.EntityFrameworkCore;
using NotificationHub.Infrastructure.DependencyInjection;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();
host.Run();