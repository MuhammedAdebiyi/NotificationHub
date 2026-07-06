using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IOrganizationMemberRepository
{
    Task<IReadOnlyList<OrganizationMember>> GetByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetByUserAndOrgAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(OrganizationMember member, CancellationToken cancellationToken = default);
    Task RemoveAsync(OrganizationMember member, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}