using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Shared.Abstractions;
using System.Text.Json;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/org")]
[Authorize]
public class OrgController : ControllerBase
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IOrgInviteRepository _inviteRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationQueue _notificationQueue;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _orgRepository;
    private readonly IEmailProvider _emailProvider;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ITemplateRepository _templateRepository;
    private readonly IConfiguration _configuration;

    public OrgController(
        IOrganizationMemberRepository memberRepository,
        IOrgInviteRepository inviteRepository,
        IUserRepository userRepository,
        IOrganizationRepository orgRepository,
        IEmailProvider emailProvider,
        ITokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ICurrentOrganization currentOrg,
        ICurrentUser currentUser,
        IClock clock,
        INotificationRepository notificationRepository,
        INotificationQueue notificationQueue,
        ITemplateRepository templateRepository,
        IConfiguration configuration)
    {
        _memberRepository = memberRepository;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _orgRepository = orgRepository;
        _emailProvider = emailProvider;
        _notificationRepository = notificationRepository;
        _notificationQueue = notificationQueue;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
        _clock = clock;
        _templateRepository = templateRepository;
        _configuration = configuration;
    }
    [HttpGet("/api/v1/org/info")]
    [Authorize]
    public async Task<IActionResult> GetOrgInfo(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var org = await _orgRepository.GetByIdAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        if (org is null)
            return NotFound(new { error = "Organization not found." });

        return Ok(new
        {
            id = org.Id,
            name = org.Name,
            slug = org.Slug,
            plan = org.Plan,
            fromName = org.FromName,
            createdAt = org.CreatedAt,
        });
    }
    [HttpGet("members")]
    public async Task<IActionResult> GetMembers(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var members = await _memberRepository.GetByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        return Ok(members.Select(m => new
        {
            m.Id,
            m.Role,
            m.JoinedAt,
            m.RevokedAt,
            m.IsRevoked,
            user = new
            {
                m.User!.Id,
                m.User.FullName,
                m.User.Email,
                m.User.IsEmailVerified,
            }
        }));
    }

    [HttpGet("members/{id:guid}")]
    public async Task<IActionResult> GetMember(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var orgId = _currentOrg.OrganizationId.Value;

        var member = await _memberRepository.GetByIdAsync(id, orgId, cancellationToken);
        if (member is null)
            return NotFound(new { error = "Member not found." });

        var notificationCount = await _notificationRepository.CountByUserAsync(orgId, member.UserId, cancellationToken);
        var templateCount = await _templateRepository.CountByUserAsync(orgId, member.UserId, cancellationToken);

        return Ok(new
        {
            member.Id,
            member.Role,
            member.JoinedAt,
            member.InvitedAt,
            member.RevokedAt,
            IsRevoked = member.Role == "revoked",
            user = new
            {
                member.User!.Id,
                member.User.FullName,
                member.User.Email,
                member.User.IsEmailVerified,
                member.User.CreatedAt,
            },
            activity = new
            {
                notificationsSent = notificationCount,
                templatesCreated = templateCount,
            }
        });
    }

    [HttpDelete("members/{id:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role != "owner" && _currentOrg.Role != "admin")
            return Forbid();

        var member = await _memberRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (member is null)
            return NotFound(new { error = "Member not found." });

        if (member.Role == "owner")
            return BadRequest(new { error = "Cannot remove the organization owner." });

        if (member.UserId == _currentUser.UserId)
            return BadRequest(new { error = "Cannot remove yourself." });

        await _memberRepository.RemoveAsync(member, cancellationToken);
        await _memberRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { removed = true });
    }

    [HttpPut("members/{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role != "owner")
            return Forbid();

        var allowedRoles = new[] { "admin", "member" };
        if (!allowedRoles.Contains(request.Role))
            return BadRequest(new { error = "Role must be 'admin' or 'member'." });

        var member = await _memberRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (member is null)
            return NotFound(new { error = "Member not found." });

        if (member.Role == "owner")
            return BadRequest(new { error = "Cannot change the owner's role." });

        if (member.UserId == _currentUser.UserId)
            return BadRequest(new { error = "Cannot change your own role." });

        member.Role = request.Role;
        await _memberRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { updated = true, role = member.Role });
    }

    [HttpPut("members/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeMember(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role != "owner" && _currentOrg.Role != "admin")
            return Forbid();

        var member = await _memberRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (member is null)
            return NotFound(new { error = "Member not found." });

        if (member.Role == "owner")
            return BadRequest(new { error = "Cannot revoke the organization owner." });

        if (member.UserId == _currentUser.UserId)
            return BadRequest(new { error = "Cannot revoke yourself." });

        member.Role = "revoked";
        member.RevokedAt = _clock.UtcNow;
        await _memberRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { revoked = true });
    }

    [HttpPut("members/{id:guid}/restore")]
    public async Task<IActionResult> RestoreMember(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role != "owner" && _currentOrg.Role != "admin")
            return Forbid();

        var member = await _memberRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (member is null)
            return NotFound(new { error = "Member not found." });

        member.Role = "member";
        member.RevokedAt = null;
        await _memberRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { restored = true });
    }

    [HttpGet("invites")]
    public async Task<IActionResult> GetInvites(CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        var invites = await _inviteRepository.GetPendingByOrgAsync(
            _currentOrg.OrganizationId.Value, cancellationToken);

        return Ok(invites.Select(i => new
        {
            i.Id,
            i.Email,
            i.Role,
            i.ExpiresAt,
            i.CreatedAt,
        }));
    }

    [HttpPost("invites")]
    public async Task<IActionResult> SendInvite(
        [FromBody] SendInviteRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role != "owner" && _currentOrg.Role != "admin")
            return Forbid();

        var orgId = _currentOrg.OrganizationId.Value;

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            var existingMember = await _memberRepository.GetByUserAndOrgAsync(
                existingUser.Id, orgId, cancellationToken);
            if (existingMember is not null)
                return BadRequest(new { error = "This user is already a member of your organization." });
        }

        var alreadyInvited = await _inviteRepository.PendingInviteExistsAsync(
            orgId, request.Email, cancellationToken);
        if (alreadyInvited)
            return BadRequest(new { error = "A pending invite already exists for this email." });

        var org = await _orgRepository.GetByIdAsync(orgId, cancellationToken);
        var token = _tokenGenerator.Generate(32);

        var invite = new OrgInvite
        {
            OrganizationId = orgId,
            Email = request.Email.ToLowerInvariant(),
            Token = token,
            Role = request.Role ?? "member",
            ExpiresAt = _clock.UtcNow.AddDays(7),
            IsAccepted = false,
        };

        await _inviteRepository.AddAsync(invite, cancellationToken);
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        var frontendUrl = _configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
        var link = $"{frontendUrl}/accept-invite?token={token}";

        await _emailProvider.SendAsync(new EmailMessage(
            From: "NotificationHub <no-reply@coursevaultai.app>",
            To: request.Email,
            Subject: $"You've been invited to join {org?.Name ?? "an organization"} on NotificationHub",
            Html: $"""
                <h2>You've been invited</h2>
                <p>You've been invited to join <strong>{org?.Name ?? "an organization"}</strong> on NotificationHub as a <strong>{invite.Role}</strong>.</p>
                <p><a href="{link}" style="background:#7c3aed;color:white;padding:10px 20px;border-radius:6px;text-decoration:none;">Accept Invite</a></p>
                <p style="color:#888;font-size:12px;">This invite expires in 7 days.</p>
                """,
            Text: $"You've been invited to join {org?.Name ?? "an organization"} on NotificationHub. Accept here: {link}"
        ), cancellationToken);

        return Ok(new { invited = true, email = request.Email });
    }

    [HttpDelete("invites/{id:guid}")]
    public async Task<IActionResult> CancelInvite(Guid id, CancellationToken cancellationToken)
    {
        if (_currentOrg.OrganizationId is null)
            return Unauthorized(new { error = "No organization context." });

        if (_currentOrg.Role != "owner" && _currentOrg.Role != "admin")
            return Forbid();

        var invite = await _inviteRepository.GetByIdAsync(
            id, _currentOrg.OrganizationId.Value, cancellationToken);

        if (invite is null)
            return NotFound(new { error = "Invite not found." });

        invite.IsAccepted = true;
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { cancelled = true });
    }

    [HttpGet("invites/validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateInvite(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByTokenAsync(token, cancellationToken);

        if (invite is null || invite.IsAccepted || invite.ExpiresAt < _clock.UtcNow)
            return BadRequest(new { error = "Invite is invalid or has expired." });

        return Ok(new
        {
            email = invite.Email,
            role = invite.Role,
            organizationName = invite.Organization?.Name ?? "an organization",
            expiresAt = invite.ExpiresAt,
        });
    }

    [HttpPost("invites/accept")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (invite is null || invite.IsAccepted || invite.ExpiresAt < _clock.UtcNow)
            return BadRequest(new { error = "Invite is invalid or has expired." });

        var user = await _userRepository.GetByEmailAsync(invite.Email, cancellationToken);

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { error = "Full name and password are required to create your account." });

            user = new User
            {
                FullName = request.FullName,
                Email = invite.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                IsEmailVerified = true,
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        var alreadyMember = await _memberRepository.GetByUserAndOrgAsync(
            user.Id, invite.OrganizationId, cancellationToken);

        if (alreadyMember is null)
        {
            var member = new OrganizationMember
            {
                OrganizationId = invite.OrganizationId,
                UserId = user.Id,
                Role = invite.Role,
                InvitedAt = invite.CreatedAt,
                JoinedAt = _clock.UtcNow,
            };
            await _memberRepository.AddAsync(member, cancellationToken);
        }

        invite.IsAccepted = true;
        await _inviteRepository.SaveChangesAsync(cancellationToken);
        await _memberRepository.SaveChangesAsync(cancellationToken);

        var orgName = invite.Organization?.Name ?? "your organization";
        var frontendUrl = _configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";

        // Queue welcome email to new member — async via Worker
        var welcomeNotification = new Notification
        {
            OrganizationId = invite.OrganizationId,
            RecipientEmail = user.Email,
            Type = "InviteAccepted_Welcome",
            Channel = NotificationChannel.Email,
            Payload = JsonSerializer.Serialize(new
            {
                subject = $"Welcome to {orgName} on NotificationHub",
                body = $"Hi {user.FullName}, you've successfully joined {orgName} as a {invite.Role}. Go to your dashboard: {frontendUrl}/dashboard"
            }),
        };
        await _notificationRepository.AddAsync(welcomeNotification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
        await _notificationQueue.EnqueueAsync(welcomeNotification.Id, cancellationToken);

        // Queue notification to org owner — async via Worker
        var orgMembers = await _memberRepository.GetByOrgAsync(invite.OrganizationId, cancellationToken);
        var owner = orgMembers.FirstOrDefault(m => m.Role == "owner");
        if (owner?.User is not null)
        {
            var ownerNotification = new Notification
            {
                OrganizationId = invite.OrganizationId,
                RecipientEmail = owner.User.Email,
                Type = "InviteAccepted_OwnerAlert",
                Channel = NotificationChannel.Email,
                Payload = JsonSerializer.Serialize(new
                {
                    subject = $"{user.FullName} accepted your invite",
                    body = $"{user.FullName} ({user.Email}) has joined {orgName} as a {invite.Role}. View your team: {frontendUrl}/users"
                }),
            };
            await _notificationRepository.AddAsync(ownerNotification, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
            await _notificationQueue.EnqueueAsync(ownerNotification.Id, cancellationToken);
        }

        var jwt = _jwtTokenGenerator.Generate(user, invite.OrganizationId, invite.Role);

        return Ok(new
        {
            token = jwt,
            userId = user.Id,
            organizationId = invite.OrganizationId,
            email = user.Email,
        });
    }
}

public record SendInviteRequest(string Email, string? Role);
public record UpdateRoleRequest(string Role);
public record AcceptInviteRequest(string Token, string? FullName, string? Password);