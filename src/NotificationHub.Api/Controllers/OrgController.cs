using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/org")]
[Authorize]
public class OrgController : ControllerBase
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IOrgInviteRepository _inviteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _orgRepository;
    private readonly IEmailProvider _emailProvider;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICurrentOrganization _currentOrg;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

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
        IClock clock)
    {
        _memberRepository = memberRepository;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _orgRepository = orgRepository;
        _emailProvider = emailProvider;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _currentOrg = currentOrg;
        _currentUser = currentUser;
        _clock = clock;
    }

    // GET /api/v1/org/members
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
            user = new
            {
                m.User!.Id,
                m.User.FullName,
                m.User.Email,
                m.User.IsEmailVerified,
            }
        }));
    }

    // DELETE /api/v1/org/members/{id}
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

    // GET /api/v1/org/invites
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

    // POST /api/v1/org/invites
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

        var link = $"https://notification-hub-chi.vercel.app/accept-invite?token={token}";
        await _emailProvider.SendAsync(new EmailMessage(
            From: "noreply@mail.notificationhub.io",
            To: request.Email,
            Subject: $"You've been invited to join {org?.Name ?? "an organization"} on NotificationHub",
            Html: $"""
                <h2>You've been invited</h2>
                <p>You've been invited to join <strong>{org?.Name ?? "an organization"}</strong> on NotificationHub as a <strong>{invite.Role}</strong>.</p>
                <p><a href="{link}" style="background:#7c3aed;color:white;padding:10px 20px;border-radius:6px;text-decoration:none;">Accept Invite</a></p>
                <p style="color:#888;font-size:12px;">This invite expires in 7 days. If you don't have an account yet, you'll be able to create one after clicking the link.</p>
                """,
            Text: $"You've been invited to join {org?.Name ?? "an organization"} on NotificationHub. Accept here: {link} (expires in 7 days)"
        ), cancellationToken);

        return Ok(new { invited = true, email = request.Email });
    }

    // DELETE /api/v1/org/invites/{id}
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

    // GET /api/v1/org/invites/validate?token=xxx  — no [Authorize], public endpoint
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

    // POST /api/v1/org/invites/accept  — no [Authorize], public endpoint
    [HttpPost("invites/accept")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (invite is null || invite.IsAccepted || invite.ExpiresAt < _clock.UtcNow)
            return BadRequest(new { error = "Invite is invalid or has expired." });

        // Look up or create the user
        var user = await _userRepository.GetByEmailAsync(invite.Email, cancellationToken);

        if (user is null)
        {
            // New user — fullName and password required
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { error = "Full name and password are required to create your account." });

            user = new User
            {
                FullName = request.FullName,
                Email = invite.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                IsEmailVerified = true, // invite email proves ownership
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        // Check not already a member
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

        // Issue JWT scoped to the invited org
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
public record AcceptInviteRequest(string Token, string? FullName, string? Password);