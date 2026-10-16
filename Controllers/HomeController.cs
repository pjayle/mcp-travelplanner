using Markdig;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Web.Models;
using TravelPlanner.Web.Services;

namespace TravelPlanner.Web.Controllers;

public class HomeController : Controller
{
    // DisableHtml() => raw HTML from the LLM is escaped (XSS-safe).
    private static readonly MarkdownPipeline Md = new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().Build();
    private readonly TripPlannerService _planner;
    private readonly ILogger<HomeController> _log;

    public HomeController(TripPlannerService planner, ILogger<HomeController> log)
        => (_planner, _log) = (planner, log);

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Plan([FromBody] PlanRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "Please describe your trip." });
        try
        {
            var result = await _planner.PlanAsync(req.Prompt, req.Origin, ct);
            return Json(new { html = Markdown.ToHtml(result.Markdown, Md), toolCalls = result.ToolCalls });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Planning failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public IActionResult Error() => Content("Something went wrong.");
}
