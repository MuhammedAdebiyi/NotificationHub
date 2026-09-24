using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Auth.Commands.SendVerificationEmail;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.ResendVerification;

public class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;

    public ResendVerificationCommandHandler(
        IUserRepository userRepository,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _mediator = mediator;
    }

    public async Task<Result> Handle(
        ResendVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Anti-enumeration: same response whether the email is unknown,
        // already verified, or freshly resent.
        if (user is null || user.IsEmailVerified)
            return Result.Success();

        await _mediator.Send(new SendVerificationEmailCommand(user.Id), cancellationToken);

        return Result.Success();
    }
}
