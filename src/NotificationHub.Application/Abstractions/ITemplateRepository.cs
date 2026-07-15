using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Template> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Template template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountByOrgAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountByUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}