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
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpAuth(builder.Configuration);
// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<VerificationSettings>(
    builder.Configuration.GetSection(VerificationSettings.SectionName));

// MediatR — scans Application assembly for IRequestHandler<> implementations
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IApplicationMarker).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation — scans Application assembly for AbstractValidator<> implementations
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
app.UseAuthentication();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.UseAuthorization();
app.MapControllers();

app.Run();