using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.Signup;

public class SignupCommandHandler : IRequestHandler<SignupCommand, Result<SignupResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public SignupCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
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

        return Result<SignupResult>.Success(new SignupResult(token, user.Id, user.Email));
    }
}