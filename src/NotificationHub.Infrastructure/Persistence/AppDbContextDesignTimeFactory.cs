using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotificationHub.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations ...` run without booting the API host — the
/// host eagerly connects to Redis, which is not available on a developer
/// machine or in CI. Runtime code paths are unaffected: the real options
/// still come from Program.cs / the Worker's service registration.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=notificationhub_design")
            .Options);
}
