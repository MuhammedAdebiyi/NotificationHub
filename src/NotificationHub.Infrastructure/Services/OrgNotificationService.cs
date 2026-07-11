using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Services;

public class OrgNotificationService : IOrgNotificationService
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<OrgNotificationService> _logger;

    public OrgNotificationService(
        IOrganizationMemberRepository memberRepository,
        IEmailProvider emailProvider,
        ILogger<OrgNotificationService> logger)
    {
        _memberRepository = memberRepository;
        _emailProvider = emailProvider;
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

        foreach (var member in members.Where(m => m.Role != "revoked" && m.User?.Email != null))
        {
            try
            {
                await _emailProvider.SendAsync(new EmailMessage(
                    From: "NotificationHub <noreply@coursevaultai.app>",
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