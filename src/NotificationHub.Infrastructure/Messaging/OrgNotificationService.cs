using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Infrastructure.Messaging;

public class OrgNotificationService : IOrgNotificationService
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationQueue _queue;
    private readonly ILogger<OrgNotificationService> _logger;

    public OrgNotificationService(
        IOrganizationMemberRepository memberRepository,
        INotificationRepository notificationRepository,
        INotificationQueue queue,
        ILogger<OrgNotificationService> logger)
    {
        _memberRepository = memberRepository;
        _notificationRepository = notificationRepository;
        _queue = queue;
        _logger = logger;
    }

    public async Task NotifyOrgAsync(
        Guid organizationId,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var members = await _memberRepository.GetByOrgAsync(organizationId, cancellationToken);
        var active = members.Where(m => m.Role != "revoked" && m.User?.Email != null).ToList();

        _logger.LogInformation(
            "Queuing org notification '{Subject}' to {Count} members in org {OrgId}",
            subject, active.Count, organizationId);

        foreach (var member in active)
        {
            var notification = new Notification
            {
                OrganizationId = organizationId,
                RecipientEmail = member.User!.Email,
                Type = "OrgAlert",
                Channel = NotificationChannel.Email,
                Payload = JsonSerializer.Serialize(new { subject, body = htmlBody }),
            };

            await _notificationRepository.AddAsync(notification, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
            await _queue.EnqueueAsync(notification.Id, cancellationToken);
        }
    }
}