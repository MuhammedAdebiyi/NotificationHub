using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Auth.Commands.ForgotPassword;
using NotificationHub.Application.Features.Auth.Commands.Login;
using NotificationHub.Application.Features.Auth.Commands.ResetPassword;
using NotificationHub.Application.Features.Auth.Commands.Signup;
using NotificationHub.Application.Features.Auth.Commands.VerifyEmail;

namespace NotificationHub.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthController(
        IMediator mediator,
        IUserRepository userRepository,
        IOrganizationMemberRepository memberRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        var command = new SignupCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.OrgName);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            message = "Account created. Check your email to verify before logging in.",
            email = result.Value!.Email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        var value = result.Value!;

        // Multi-org — return org list, no token
        if (value.RequiresOrgSelection)
        {
            return Ok(new
            {
                token = (string?)null,
                userId = value.UserId,
                email = value.Email,
                fullName = value.FullName,
                requiresOrgSelection = true,
                organizations = value.Organizations?.Select(o => new
                {
                    organizationId = o.OrganizationId,
                    orgName = o.OrgName,
                    role = o.Role,
                })
            });
        }

        // No org — signal frontend to show no-org page
        if (value.Token is null)
        {
            return Ok(new
            {
                token = (string?)null,
                userId = value.UserId,
                email = value.Email,
                fullName = value.FullName,
                requiresOrgSelection = false,
                organizations = (object?)null,
            });
        }

        // Single org — return token
        return Ok(new
        {
            token = value.Token,
            userId = value.UserId,
            organizationId = value.OrganizationId,
            email = value.Email,
            fullName = value.FullName,
            requiresOrgSelection = false,
            organizations = (object?)null,
        });
    }

    [HttpPost("select-org")]
    [AllowAnonymous]
    public async Task<IActionResult> SelectOrg(
        [FromBody] SelectOrgRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Unauthorized(new { error = "Invalid user." });

        var membership = await _memberRepository.GetByUserAndOrgAsync(
            request.UserId, request.OrganizationId, cancellationToken);

        if (membership is null)
            return Forbid();

        if (membership.Role == "revoked")
            return StatusCode(403, new { error = "access_revoked" });

        var token = _jwtTokenGenerator.Generate(user, membership.OrganizationId, membership.Role);

        return Ok(new
        {
            token,
            userId = user.Id,
            organizationId = membership.OrganizationId,
            email = user.Email,
            fullName = user.FullName,
            requiresOrgSelection = false,
            organizations = (object?)null,
        });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "Token is required." });

        var command = new VerifyEmailCommand(token);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            token = result.Value!.Token,
            userId = result.Value.UserId,
            organizationId = result.Value.OrganizationId,
            email = result.Value.Email
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);
        await _mediator.Send(command);
        return Ok(new { message = "If that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(request.Token, request.Password, request.ConfirmPassword);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Password reset successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var fullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var orgId = User.FindFirst("org_id")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new { userId, email, fullName, organizationId = orgId, role });
    }
}

public record SignupRequest(string FullName, string Email, string Password, string ConfirmPassword, string OrgName);
public record LoginRequest(string Email, string Password);
public record SelectOrgRequest(Guid UserId, Guid OrganizationId);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string Password, string ConfirmPassword);