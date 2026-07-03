using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Auth.Commands.SendVerificationEmail;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.Signup;

public class SignupCommandHandler : IRequestHandler<SignupCommand, Result<SignupResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IMediator _mediator;

    public SignupCommandHandler(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
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

        // 1. Create user
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsEmailVerified = false,
        };
        await _userRepository.AddAsync(user, cancellationToken);

        // 2. Create org
        var slug = GenerateSlug(request.OrgName);
        var organization = new Organization
        {
            Name = request.OrgName,
            Slug = slug,
            Plan = "free",
            FromName = request.OrgName,
        };
        await _organizationRepository.AddAsync(organization, cancellationToken);

        // 3. Create membership (owner)
        var member = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = user.Id,
            Role = "owner",
            JoinedAt = DateTime.UtcNow,
        };
        await _organizationRepository.AddMemberAsync(member, cancellationToken);

        // 4. Persist everything
        await _userRepository.SaveChangesAsync(cancellationToken);

        // 5. Generate JWT with org context
        var token = _jwtTokenGenerator.Generate(user, organization.Id, "owner");

        // 6. Fire verification email — swallowed intentionally
        try
        {
            await _mediator.Send(new SendVerificationEmailCommand(user.Id), cancellationToken);
        }
        catch
        {
            // Signup must not fail because email delivery failed.
            // TODO: log this once Serilog (Phase 9) is in.
        }

        return Result<SignupResult>.Success(
            new SignupResult(token, user.Id, organization.Id, user.Email));
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Strip anything that isn't a letter, digit, or hyphen
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // Collapse consecutive hyphens
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }
}