using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Auth.Commands.SendVerificationEmail;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.Signup;

public class SignupCommandHandler : IRequestHandler<SignupCommand, Result<SignupResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IMediator _mediator;

    public SignupCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mediator = mediator;
    }

    public async Task<Result<SignupResult>> Handle(
        SignupCommand request,
        CancellationToken cancellationToken)
    {
        var emailTaken = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailTaken)
            return Result<SignupResult>.Failure("Email is already registered.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsEmailVerified = false,
        };

        var preference = new UserPreference { UserId = user.Id };
        user.Preferences = preference;

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.Generate(user);

        // Fire-and-continue: don't let a flaky email provider block signup itself.
        // If this fails, signup still succeeds — user can request a resend later.
        try
        {
            await _mediator.Send(new SendVerificationEmailCommand(user.Id), cancellationToken);
        }
        catch
        {
            // Deliberately swallowed for now — signup must not fail because email delivery failed.
            // TODO: log this once Serilog (Phase 5) is in.
        }

        return Result<SignupResult>.Success(new SignupResult(token, user.Id, user.Email));
    }
}