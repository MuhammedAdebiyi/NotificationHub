using Microsoft.EntityFrameworkCore;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Repositories;

public class VerificationTokenRepository : IVerificationTokenRepository
{
    private readonly AppDbContext _context;

    public VerificationTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(VerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.VerificationTokens.AddAsync(token, cancellationToken);
    }

    public async Task<VerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.VerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}