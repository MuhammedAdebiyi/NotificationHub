using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NotificationHub.Application;
using NotificationHub.Infrastructure.Persistence;
using MediatR;
using NotificationHub.Application.Behaviors;
using NotificationHub.Infrastructure.DependencyInjection;
using NotificationHub.Api.Middlewares;
using NotificationHub.Infrastructure.Auth;
using NotificationHub.Application.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://notificationhub.space",
                "https://www.notificationhub.space",
                "https://notification-hub-chi.vercel.app",
                "https://docs.notificationhub.space")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpAuth(builder.Configuration);

builder.Services.Configure<VerificationSettings>(
    builder.Configuration.GetSection(VerificationSettings.SectionName));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IApplicationMarker).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(IApplicationMarker).Assembly);

const string swaggerUiHtml = """
<!DOCTYPE html>
<html>
<head>
    <title>NotificationHub API Documentation</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
    <style>
        body { margin: 0; padding: 0; }
        .swagger-ui .topbar { display: none; }
    </style>
</head>
<body>
    <div id="swagger-ui"></div>
    <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
    <script>
        SwaggerUIBundle({
            url: '/openapi/v1.json',
            dom_id: '#swagger-ui',
            presets: [
                SwaggerUIBundle.presets.apis,
                SwaggerUIBundle.SwaggerUIStandalonePreset
            ],
            layout: 'BaseLayout',
            deepLinking: true,
            defaultModelsExpandDepth: -1,
            docExpansion: 'list',
            filter: true
        });
    </script>
</body>
</html>
""";

var app = builder.Build();

// Auto-apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Suppress PendingModelChangesWarning — we apply SQL migrations manually
    // and the EF snapshot may be ahead of __EFMigrationsHistory.
    var pendingModelChanges = db.Database.GetPendingMigrations();
    if (pendingModelChanges.Any())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Pending EF migrations detected ({Count}), skipping auto-migrate. Apply SQL migrations manually.",
            pendingModelChanges.Count());
    }
}

// OpenAPI + Swagger UI (always available for developers)
app.MapOpenApi();
app.MapGet("/docs", () => Results.Content(swaggerUiHtml, "text/html"));
app.MapGet("/health", async (AppDbContext db) =>
{
    await db.Database.ExecuteSqlRawAsync("SELECT 1");
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<OrgMembershipMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();