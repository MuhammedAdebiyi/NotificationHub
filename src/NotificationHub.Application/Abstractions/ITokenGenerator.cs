namespace NotificationHub.Application.Abstractions;

public interface ITokenGenerator
{
    string Generate(int length = 32);
}
