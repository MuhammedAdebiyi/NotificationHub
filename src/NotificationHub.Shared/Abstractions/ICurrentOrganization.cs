namespace NotificationHub.Shared.Abstractions;

public interface ICurrentOrganization
{
    Guid? OrganizationId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}