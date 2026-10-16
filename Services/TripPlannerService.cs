using System.Text.Json;
using Microsoft.Extensions.AI;
using TravelPlanner.Web.Models;

namespace TravelPlanner.Web.Services;

/// <summary>
/// Agent orchestration: system prompt + MCP tools + LLM tool-calling loop.
/// (Swap this class for a LangChain.NET chain/agent without touching the controller/UI.)
/// </summary>
public class TripPlannerService
{
    private readonly IChatClient _chat;
    private readonly McpHub _hub;

    public TripPlannerService(IChatClient chat, McpHub hub) => (_chat, _hub) = (chat, hub);

    private static string SystemPrompt => $"""
        You are an expert travel agent. Today's date is {DateTime.UtcNow:yyyy-MM-dd}.
        You have tools for weather, hotels and flights. You MUST call the tools before answering - never invent prices or forecasts.
        Dates passed to tools must be in yyyy-MM-dd format. Call each tool with concrete values; do not ask the user questions, make reasonable assumptions instead.

        Workflow:
        1. Work out destination, number of days and a sensible start date (about 10 days from today if not given, so weather data is available).
        2. Call get_weather_forecast for the destination (and for each city if multi-city).
        3. Call search_flights (round trip) from the traveller's origin.
        4. Call search_hotels for each city / stay.
        5. Write the final answer in Markdown:
           - Trip summary (dates, budget estimate in INR)
           - Flight recommendation (and why)
           - Hotel recommendation (and why)
           - Day-by-day itinerary (Day 1..N) with morning / afternoon / evening, adapted to the weather
           - Packing tips and a cost breakdown table
        Keep it practical and concise.
        """;

    public async Task<PlanResult> PlanAsync(string prompt, string? origin, CancellationToken ct)
    {
        var tools = await _hub.GetToolsAsync(ct);

        var user = string.IsNullOrWhiteSpace(origin) ? prompt : $"{prompt}\nI am departing from: {origin}.";
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, user)
        };

        var response = await _chat.GetResponseAsync(messages,
            new ChatOptions { Tools = [.. tools], Temperature = 0.3f }, ct);

        return new PlanResult(response.Text, BuildTrace(response.Messages));
    }

    // Collect every tool call + result so the UI can show what the agent did.
    private List<ToolCallTrace> BuildTrace(IList<ChatMessage> msgs)
    {
        var results = msgs.SelectMany(m => m.Contents).OfType<FunctionResultContent>()
                          .ToDictionary(r => r.CallId, r => r.Result);

        return msgs.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Select(c =>
        {
            var res = results.TryGetValue(c.CallId, out var r) ? r?.ToString() ?? "" : "";
            return new ToolCallTrace(
                _hub.ServerFor(c.Name), c.Name,
                JsonSerializer.Serialize(c.Arguments),
                res.Length > 600 ? res[..600] + "..." : res);
        }).ToList();
    }
}
