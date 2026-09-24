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

        try
        {
            await _mediator.Send(new SendVerificationEmailCommand(user.Id), cancellationToken);
        }
        catch
        {
            // Delivery failure must not change the response — otherwise the
            // caller learns the account exists (and a provider outage would
            // turn a UX-friendly endpoint into a 500).
        }

        return Result.Success();
    }
}
