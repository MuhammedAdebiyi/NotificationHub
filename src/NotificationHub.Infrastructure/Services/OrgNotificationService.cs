using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Services;

public class OrgNotificationService : IOrgNotificationService
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IEmailProviderFactory _emailProviderFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<OrgNotificationService> _logger;

    public OrgNotificationService(
        IOrganizationMemberRepository memberRepository,
        IEmailProviderFactory emailProviderFactory,
        AppDbContext context,
        ILogger<OrgNotificationService> logger)
    {
        _memberRepository = memberRepository;
        _emailProviderFactory = emailProviderFactory;
        _context = context;
        _logger = logger;
    }

    public async Task NotifyAsync(
        Guid organizationId,
        string subject,
        string html,
        string text,
        CancellationToken cancellationToken = default)
    {
        var members = await _memberRepository.GetByOrgAsync(organizationId, cancellationToken);
        var emailProvider = await _emailProviderFactory.GetProviderAsync(organizationId, cancellationToken);

        var org = await _context.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => new { o.FromName, o.FromEmail })
            .FirstOrDefaultAsync(cancellationToken);

        var fromEmail = string.IsNullOrWhiteSpace(org?.FromEmail)
            ? "notifications@mail.notificationhub.space"
            : org!.FromEmail;

        var fromName = string.IsNullOrWhiteSpace(org?.FromName)
            ? "NotificationHub"
            : org!.FromName;

        var from = $"{fromName} <{fromEmail}>";

        foreach (var member in members.Where(m => m.Role != "revoked" && m.User?.Email != null))
        {
            try
            {
                await emailProvider.SendAsync(new EmailMessage(
                    From: from,
                    To: member.User!.Email,
                    Subject: subject,
                    Html: html,
                    Text: text
                ), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify {Email}", member.User!.Email);
            }
        }
    }
}
