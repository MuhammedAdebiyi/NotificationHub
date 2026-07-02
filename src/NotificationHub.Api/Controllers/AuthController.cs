using MediatR;
using NotificationHub.Application.Features.Auth.Commands.VerifyEmail;
using Microsoft.AspNetCore.Mvc;
using NotificationHub.Application.Features.Auth.Commands.Login;
using NotificationHub.Application.Features.Auth.Commands.Signup;

namespace NotificationHub.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        var command = new SignupCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.ConfirmPassword);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            token = result.Value!.Token,
            userId = result.Value.UserId,
            email = result.Value.Email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(new
        {
            token = result.Value!.Token,
            userId = result.Value.UserId,
            email = result.Value.Email,
            fullName = result.Value.FullName
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

        return Ok(new { verified = true });
    }
    
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var fullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return Ok(new { userId, email, fullName });
    }
}

public record SignupRequest(string FullName, string Email, string Password, string ConfirmPassword);
public record LoginRequest(string Email, string Password);