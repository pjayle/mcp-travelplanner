namespace TravelPlanner.Web.Models;

public record PlanRequest(string Prompt, string? Origin);

public record ToolCallTrace(string Server, string Tool, string Arguments, string Result);

public record PlanResult(string Markdown, List<ToolCallTrace> ToolCalls);
