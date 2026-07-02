using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IVerificationTokenRepository
{
    Task AddAsync(VerificationToken token, CancellationToken cancellationToken = default);
    Task<VerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}