namespace NotificationHub.Application.Abstractions;

public interface IUnsubscribeTokenService
{
    string CreateToken(Guid organizationId, string email);
    UnsubscribeTokenPayload? ReadToken(string token);
}

public record UnsubscribeTokenPayload(Guid OrganizationId, string Email);
