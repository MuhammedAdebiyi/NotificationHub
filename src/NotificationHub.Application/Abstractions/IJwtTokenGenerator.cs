using NotificationHub.Domain.Entities;

namespace NotificationHub.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}