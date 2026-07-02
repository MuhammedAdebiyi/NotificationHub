using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Auth;

public class TokenGenerator : ITokenGenerator
{
    public string Generate(int length = 32) => RandomTokenGenerator.Generate(length);
}