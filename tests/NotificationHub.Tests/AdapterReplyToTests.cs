using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationHub.Application.Abstractions;
using NotificationHub.Infrastructure.Email.Providers;
using Xunit;

namespace NotificationHub.Tests;

/// <summary>
/// Every adapter must translate the shared EmailMessage.ReplyTo into the
/// provider's own payload shape — same value, provider-specific key.
/// </summary>
public class AdapterReplyToTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        public List<(Uri Uri, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add((request.RequestUri!, body));
            return _responder(request);
        }
    }

    private static HttpResponseMessage Json(string body, int status = 200) =>
        new((System.Net.HttpStatusCode)status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };

    private static EmailMessage Message(string? replyTo) =>
        new(
            From: "Example <news@example.com>",
            To: "to@example.com",
            Subject: "Hello",
            Html: "<p>Hello</p>",
            Text: "Hello",
            ReplyTo: replyTo);

    private static async Task<JsonElement> SendAndGetPayloadAsync(
        Func<RecordingHandler, IEmailProvider> factory, string? replyTo)
    {
        var handler = new RecordingHandler(_ => Json("{\"id\":\"em_1\",\"messageId\":\"msg_1\"}"));
        var provider = factory(handler);

        await provider.SendAsync(Message(replyTo));

        var request = Assert.Single(handler.Requests);
        return JsonDocument.Parse(request.Body).RootElement.Clone();
    }

    [Fact]
    public async Task Resend_sends_reply_to_as_a_single_string()
    {
        var payload = await SendAndGetPayloadAsync(
            h => new ResendAdapter(new HttpClient(h), "key", NullLogger<ResendAdapter>.Instance),
            replyTo: "support@example.com");

        Assert.Equal("support@example.com", payload.GetProperty("reply_to").GetString());
    }

    [Fact]
    public async Task Brevo_sends_reply_to_as_an_array_of_addresses()
    {
        var payload = await SendAndGetPayloadAsync(
            h => new BrevoAdapter(new HttpClient(h), "key", NullLogger<BrevoAdapter>.Instance),
            replyTo: "support@example.com");

        var replyTo = payload.GetProperty("replyTo");
        Assert.Equal(JsonValueKind.Array, replyTo.ValueKind);
        Assert.Equal("support@example.com", replyTo[0].GetProperty("email").GetString());
    }

    [Fact]
    public async Task SendByte_sends_reply_to_as_a_single_string()
    {
        var payload = await SendAndGetPayloadAsync(
            h => new SendByteAdapter(new HttpClient(h), "key", NullLogger<SendByteAdapter>.Instance),
            replyTo: "support@example.com");

        Assert.Equal("support@example.com", payload.GetProperty("reply_to").GetString());
    }

    [Fact]
    public async Task SendGrid_sends_reply_to_as_an_address_object()
    {
        var payload = await SendAndGetPayloadAsync(
            h => new SendGridAdapter(new HttpClient(h), "key", NullLogger<SendGridAdapter>.Instance),
            replyTo: "support@example.com");

        Assert.Equal("support@example.com",
            payload.GetProperty("reply_to").GetProperty("email").GetString());
    }

    [Fact]
    public async Task No_reply_to_means_no_reply_to_key_in_any_payload()
    {
        var resend = await SendAndGetPayloadAsync(
            h => new ResendAdapter(new HttpClient(h), "key", NullLogger<ResendAdapter>.Instance),
            replyTo: null);
        Assert.False(resend.TryGetProperty("reply_to", out _));

        var brevo = await SendAndGetPayloadAsync(
            h => new BrevoAdapter(new HttpClient(h), "key", NullLogger<BrevoAdapter>.Instance),
            replyTo: null);
        Assert.False(brevo.TryGetProperty("replyTo", out _));

        var sendByte = await SendAndGetPayloadAsync(
            h => new SendByteAdapter(new HttpClient(h), "key", NullLogger<SendByteAdapter>.Instance),
            replyTo: null);
        Assert.False(sendByte.TryGetProperty("reply_to", out _));

        var sendGrid = await SendAndGetPayloadAsync(
            h => new SendGridAdapter(new HttpClient(h), "key", NullLogger<SendGridAdapter>.Instance),
            replyTo: null);
        Assert.False(sendGrid.TryGetProperty("reply_to", out _));
    }

    [Fact]
    public async Task Adapters_still_send_from_to_subject_and_html()
    {
        var handler = new RecordingHandler(_ => Json("{\"id\":\"em_1\"}"));
        var adapter = new ResendAdapter(new HttpClient(handler), "key", NullLogger<ResendAdapter>.Instance);

        await adapter.SendAsync(Message("support@example.com"));

        var payload = JsonDocument.Parse(handler.Requests.Single().Body).RootElement;
        Assert.Equal("Example <news@example.com>", payload.GetProperty("from").GetString());
        Assert.Equal("Hello", payload.GetProperty("subject").GetString());
        Assert.Equal("to@example.com", payload.GetProperty("to")[0].GetString());
        Assert.Equal("<p>Hello</p>", payload.GetProperty("html").GetString());
        Assert.Equal(new Uri("https://api.resend.com/emails"), handler.Requests.Single().Uri);
    }
}
