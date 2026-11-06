using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Validation;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler
    : IRequestHandler<CreateNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _repository;
    private readonly INotificationQueue _queue;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IEmailProviderFactory _emailProviderFactory;

    public CreateNotificationCommandHandler(
        INotificationRepository repository,
        INotificationQueue queue,
        IOrganizationRepository organizationRepository,
        ITemplateRepository templateRepository,
        IEmailProviderFactory emailProviderFactory)
    {
        _repository = repository;
        _queue = queue;
        _organizationRepository = organizationRepository;
        _templateRepository = templateRepository;
        _emailProviderFactory = emailProviderFactory;
    }

    public async Task<Result<Guid>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var exists = await _repository.IdempotencyKeyExistsAsync(
                request.OrganizationId, request.IdempotencyKey, cancellationToken);

            if (exists)
                return Result<Guid>.Failure("Duplicate request: idempotency key already used.");
        }

        var organization = await _organizationRepository.GetByIdAsync(
            request.OrganizationId, cancellationToken);

        if (organization is null)
            return Result<Guid>.Failure(
                "Organization not found.", NotificationErrorCodes.OrganizationNotFound);

        Template? template = null;
        if (request.TemplateId is { } templateId)
        {
            template = await _templateRepository.GetByIdAsync(
                templateId, request.OrganizationId, cancellationToken);

            if (template is null)
                return Result<Guid>.Failure(
                    $"Template {templateId} was not found in this organization.",
                    NotificationErrorCodes.TemplateNotFound);
        }

        var rejected = await VerifyRequestAddressesAsync(request, cancellationToken);
        if (rejected is not null)
            return rejected;

        // Resolve once, persist the outcome — the worker and every retry read
        // these columns instead of re-running resolution.
        var replyTo = AddressResolution.ResolveReplyTo(
            request.ReplyTo,
            template?.ReplyTo,
            organization.ReplyToEmail,
            organization.FromEmail);

        var sender = AddressResolution.ResolveSender(
            request.SenderEmail,
            request.SenderName,
            template?.SenderEmail,
            template?.SenderName,
            organization.FromEmail,
            organization.FromName);

        var notification = new Notification
        {
            OrganizationId = request.OrganizationId,
            CreatedByUserId = request.UserId,
            RecipientEmail = request.RecipientEmail,
            Type = request.Type,
            Channel = request.Channel,
            Payload = request.Payload,
            TemplateId = template?.Id,
            ReplyTo = replyTo.Address,
            ReplyToSource = replyTo.Source,
            SenderEmail = sender.Email,
            SenderName = sender.Name,
            SenderSource = sender.Source,
        };

        await _repository.AddAsync(notification, cancellationToken);

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            await _repository.AddIdempotencyKeyAsync(new IdempotencyKey
            {
                OrganizationId = request.OrganizationId,
                Key = request.IdempotencyKey,
            }, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        // Push to Redis queue for async processing
        await _queue.EnqueueAsync(notification.Id, cancellationToken);

        return Result<Guid>.Success(notification.PublicId);
    }

    /// <summary>
    /// Rejects request-supplied reply-to/sender addresses whose domain is not
    /// verified with any configured provider, naming the domain in the error.
    /// Only addresses the caller supplied are checked — organization and
    /// template values are owned by the dashboard and were already accepted
    /// when they were saved. Enforcement is skipped when no provider reports
    /// a verified domain (SMTP-only orgs, failed listings) so an org can never
    /// be locked out of sending, mirroring OrgController's sender check.
    /// </summary>
    private async Task<Result<Guid>?> VerifyRequestAddressesAsync(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var candidates = new List<(string Value, string Code)>();

        if (!string.IsNullOrWhiteSpace(request.ReplyTo))
            candidates.Add((request.ReplyTo, NotificationErrorCodes.UnverifiedReplyToDomain));

        if (!string.IsNullOrWhiteSpace(request.SenderEmail))
            candidates.Add((request.SenderEmail, NotificationErrorCodes.UnverifiedSenderDomain));

        if (candidates.Count == 0)
            return null;

        var domains = await _emailProviderFactory.ListDomainsWithHealthAsync(
            request.OrganizationId, cancellationToken);

        var readyDomains = domains.Domains
            .Where(d => d.DeliverabilityReady)
            .Select(d => d.Domain.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (readyDomains.Count == 0)
            return null;

        foreach (var (value, code) in candidates)
        {
            var domain = EmailAddressRules.TryGetDomain(value);
            if (domain is null)
                continue; // malformed addresses are rejected by the validator first

            var isVerified = readyDomains.Any(d =>
                domain == d ||
                domain.EndsWith("." + d, StringComparison.Ordinal));

            if (!isVerified)
            {
                return Result<Guid>.Failure(
                    $"Domain '{domain}' is not verified with your email providers. " +
                    $"Verified domains: {string.Join(", ", readyDomains)}.",
                    code);
            }
        }

        return null;
    }
}
