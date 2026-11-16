using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Email;
using Xunit;

namespace NotificationHub.Tests;

/// <summary>
/// The Settings-page promise: when the default provider refuses a message the
/// next one takes over — and the reply-to the request was accepted with must
/// reach whichever provider ends up sending.
/// </summary>
public class ProviderFallbackTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private sealed class RecordingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add((request.RequestUri!, body));

            // First provider always refuses; the second accepts.
            return request.RequestUri!.Host == "api.resend.com"
                ? Status(500, "{\"message\":\"server error\"}")
                : Status(200, "{\"messageId\":\"brevo-1\"}");
        }

        public List<(Uri Uri, string Body)> Requests { get; } = new();

        private static HttpResponseMessage Status(int status, string body) =>
            new((System.Net.HttpStatusCode)status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
    }

    /// <summary>Routes every provider through one fake transport.</summary>
    private sealed class TestableFactory : UserEmailProviderFactory
    {
        private readonly HttpMessageHandler _handler;

        public TestableFactory(IServiceProvider services, HttpMessageHandler handler)
            : base(services, services.GetRequiredService<ILogger<UserEmailProviderFactory>>())
        {
            _handler = handler;
        }

        protected override HttpClient CreateHttpClient() =>
            new(_handler, disposeHandler: false);
    }

    private static UserEmailProviderFactory BuildFactory(HttpMessageHandler handler)
    {
        var configs = new List<EmailProviderConfig>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                ProviderType = "Resend",
                EncryptedApiKey = "resend-key",
                IsActive = true,
                IsDefault = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                ProviderType = "Brevo",
                EncryptedApiKey = "brevo-key",
                IsActive = true,
                IsDefault = false,
            },
        };

        var configRepository = new Mock<IEmailProviderConfigRepository>();
        configRepository
            .Setup(r => r.GetAllByOrgAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configRepository.Object);
        services.AddSingleton(encryption.Object);

        return new TestableFactory(services.BuildServiceProvider(), handler);
    }

    [Fact]
    public async Task Fallback_sends_the_identical_reply_to_through_the_next_provider()
    {
        var handler = new RecordingHandler();
        var factory = BuildFactory(handler);

        var message = new EmailMessage(
            From: "Example <news@example.com>",
            To: "to@example.com",
            Subject: "Hello",
            Html: "<p>Hello</p>",
            ReplyTo: "support@example.com");

        var result = await factory.SendWithFallbackAsync(OrgId, message);

        // Resend refused, Brevo accepted — the chain moved on.
        Assert.Equal("Brevo", result.ProviderType);
        Assert.Equal("brevo-1", result.MessageId);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("Resend", failure.ProviderType);

        Assert.Equal(2, handler.Requests.Count);

        // Attempt 1 — Resend, reply_to as a string.
        var first = handler.Requests[0];
        Assert.Equal("api.resend.com", first.Uri.Host);
        var firstPayload = JsonDocument.Parse(first.Body).RootElement;
        Assert.Equal("support@example.com", firstPayload.GetProperty("reply_to").GetString());

        // Attempt 2 — Brevo, same address in its single-object shape.
        var second = handler.Requests[1];
        Assert.Equal("api.brevo.com", second.Uri.Host);
        var secondPayload = JsonDocument.Parse(second.Body).RootElement;
        Assert.Equal("support@example.com",
            secondPayload.GetProperty("replyTo").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Every_provider_failing_surfaces_a_failure_instead_of_silently_dropping()
    {
        var handler = new AlwaysFailingHandler();
        var factory = BuildFactory(handler);

        var message = new EmailMessage(
            From: "Example <news@example.com>",
            To: "to@example.com",
            Subject: "Hello",
            Html: "<p>Hello</p>",
            ReplyTo: "support@example.com");

        var ex = await Assert.ThrowsAsync<ProviderSendFailedException>(() =>
            factory.SendWithFallbackAsync(OrgId, message));

        Assert.Equal(2, ex.Failures.Count);
        Assert.All(ex.Failures, f => Assert.False(string.IsNullOrEmpty(f.ProviderType)));
    }

    private sealed class AlwaysFailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    "{\"message\":\"nope\"}",
                    System.Text.Encoding.UTF8,
                    "application/json"),
            });
    }
}
