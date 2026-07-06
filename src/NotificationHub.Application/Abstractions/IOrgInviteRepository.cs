using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IOrgInviteRepository
{
    Task<IReadOnlyList<OrgInvite>> GetPendingByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrgInvite?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrgInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> PendingInviteExistsAsync(Guid organizationId, string email, CancellationToken cancellationToken = default);
    Task AddAsync(OrgInvite invite, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}