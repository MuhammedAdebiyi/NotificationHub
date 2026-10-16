using Microsoft.AspNetCore.DataProtection;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Services;

public class UnsubscribeTokenService : IUnsubscribeTokenService
{
    private readonly IDataProtector _protector;

    public UnsubscribeTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("NotificationHub.Unsubscribe.v1");
    }

    public string CreateToken(Guid organizationId, string email)
    {
        var payload = $"{organizationId:N}|{email.Trim().ToLowerInvariant()}";
        return ToUrlSafe(_protector.Protect(payload));
    }

    public UnsubscribeTokenPayload? ReadToken(string token)
    {
        try
        {
            var payload = _protector.Unprotect(FromUrlSafe(token));
            var parts = payload.Split('|', 2);
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var orgId))
                return null;

            return new UnsubscribeTokenPayload(orgId, parts[1]);
        }
        catch
        {
            return null;
        }
    }

    private static string ToUrlSafe(string base64) =>
        base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string FromUrlSafe(string base64)
    {
        var s = base64.Replace('-', '+').Replace('_', '/');
        return s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
    }
}
