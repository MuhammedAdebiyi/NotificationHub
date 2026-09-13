// Test NotificationHub C# SDK
// Usage: dotnet run --project tests/test-csharp/test-csharp.csproj
// Or:    NH_API_KEY=nhub_live_xxx dotnet script tests/test-csharp.csx

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static readonly string ApiKey = Environment.GetEnvironmentVariable("NH_API_KEY") ?? "";
    static readonly string BaseUrl = Environment.GetEnvironmentVariable("NH_BASE_URL") ?? "https://api.notificationhub.space";
    static readonly string Email = Environment.GetEnvironmentVariable("TEST_EMAIL") ?? "test@notificationhub.space";
    static readonly HttpClient Http = new();

    static async Task Main()
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            Console.WriteLine("Set NH_API_KEY env var");
            Environment.Exit(1);
        }

        Http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

        Console.WriteLine("=== NotificationHub C# SDK Test ===\n");

        // 1. Send
        Console.WriteLine("1) Sending notification...");
        var sendBody = JsonSerializer.Serialize(new
        {
            recipientEmail = Email,
            type = "transactional",
            channel = "email",
            payload = new
            {
                subject = $"Test from C# SDK - {DateTime.Now:HH:mm:ss}",
                html = "<h1>Hello!</h1><p>This is a test from the C# SDK.</p>"
            }
        });

        var sendResp = await Http.PostAsync($"{BaseUrl}/api/v1/notifications",
            new StringContent(sendBody, Encoding.UTF8, "application/json"));

        var sendText = await sendResp.Content.ReadAsStringAsync();
        if (!sendResp.IsSuccessStatusCode)
        {
            Console.WriteLine($"   ✗ Failed ({sendResp.StatusCode}): {sendText}");
            Environment.Exit(1);
        }

        var sendJson = JsonDocument.Parse(sendText);
        var publicId = sendJson.RootElement.GetProperty("publicId").GetString();
        Console.WriteLine($"   ✓ Sent! ID: {publicId}");

        // 2. Get detail
        Console.WriteLine("\n2) Getting notification detail...");
        await Task.Delay(2000);
        var detailResp = await Http.GetAsync($"{BaseUrl}/api/v1/notifications/{publicId}");
        var detailText = await detailResp.Content.ReadAsStringAsync();
        var detail = JsonDocument.Parse(detailText).RootElement;
        Console.WriteLine($"   ✓ Status: {detail.GetProperty("status")} | Provider: {detail.GetProperty("provider").GetString() ?? "pending"}");

        // 3. List
        Console.WriteLine("\n3) Listing recent notifications...");
        var listResp = await Http.GetAsync($"{BaseUrl}/api/v1/notifications?page=1&pageSize=3");
        var listText = await listResp.Content.ReadAsStringAsync();
        var list = JsonDocument.Parse(listText).RootElement;
        Console.WriteLine($"   ✓ Total: {list.GetProperty("totalCount")} notifications");

        Console.WriteLine("\n=== All tests passed ===");
    }
}
