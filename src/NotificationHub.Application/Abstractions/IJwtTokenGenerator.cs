using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string Generate(User user);
    string Generate(User user, Guid organizationId, string role);
}