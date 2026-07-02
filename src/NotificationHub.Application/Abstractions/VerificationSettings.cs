namespace NotificationHub.Application.Abstractions;

public class VerificationSettings
{
    public const string SectionName = "Verification";
    public string FrontendBaseUrl { get; set; } = string.Empty;
}