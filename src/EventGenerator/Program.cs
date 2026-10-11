using System.Net.Http.Json;
using Shared.Contracts;

var endpoint = Environment.GetEnvironmentVariable("INGESTION_API_URL") ?? "http://localhost:8080";
var targetRate = int.TryParse(Environment.GetEnvironmentVariable("TARGET_RATE"), out var parsed) ? parsed : 1000;

Console.WriteLine($"Starting generator at {targetRate} events/sec against {endpoint}");
using var client = new HttpClient { BaseAddress = new Uri(endpoint) };
var interval = TimeSpan.FromMilliseconds(1000d / Math.Max(targetRate, 1));
using var timer = new PeriodicTimer(interval);

while (await timer.WaitForNextTickAsync())
{
    var request = new EventSubmissionRequest
    {
        SourceId = $"generator-{Guid.NewGuid():N}",
        EventType = (EventType)Random.Shared.Next(Enum.GetValues<EventType>().Length),
        Severity = (Severity)Random.Shared.Next(Enum.GetValues<Severity>().Length),
        Payload = "{\"sensor\":\"demo\"}",
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    try
    {
        var response = await client.PostAsJsonAsync("/api/events", request);
        Console.WriteLine($"Sent event: {(int)response.StatusCode} {response.ReasonPhrase}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Send failed: {ex.Message}");
    }
}
