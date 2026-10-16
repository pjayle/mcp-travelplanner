using Microsoft.Extensions.AI;
using OllamaSharp;
using TravelPlanner.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// ---- MCP hub: starts all configured MCP servers once, shares their tools ----
builder.Services.Configure<List<McpServerConfig>>(builder.Configuration.GetSection("McpServers"));
builder.Services.AddSingleton<McpHub>();

// ---- Local LLM via Ollama (OllamaApiClient implements IChatClient) ----
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var endpoint = cfg["Ollama:Endpoint"] ?? "http://localhost:11434";
    var model = cfg["Ollama:Model"] ?? "llama3.1:8b";
    var timeout = TimeSpan.FromMinutes(int.TryParse(cfg["Ollama:TimeoutMinutes"], out var m) ? m : 10);

    // Local models are slow: raise the default 100s HttpClient timeout.
    var http = new HttpClient { BaseAddress = new Uri(endpoint), Timeout = timeout };
    IChatClient inner = new OllamaApiClient(http, model);

    return new ChatClientBuilder(inner)
        .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = 12)
        .Build();
});

builder.Services.AddScoped<TripPlannerService>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
