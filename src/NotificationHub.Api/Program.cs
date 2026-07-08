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
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<OrgMembershipMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();