using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace TravelPlanner.Web.Services;

public class McpServerConfig
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "dotnet";
    public List<string> Args { get; set; } = new();
}

/// <summary>
/// Connects to every configured MCP server (stdio), aggregates their tools into one
/// list and remembers which server owns which tool. This is the "multi-server" layer.
/// </summary>
public sealed class McpHub : IAsyncDisposable
{
    private readonly List<McpServerConfig> _configs;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<McpHub> _log;
    private readonly List<IMcpClient> _clients = new();
    private readonly Dictionary<string, string> _toolToServer = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<AITool>? _tools;

    public McpHub(IOptions<List<McpServerConfig>> cfg, IWebHostEnvironment env, ILogger<McpHub> log)
        => (_configs, _env, _log) = (cfg.Value, env, log);

    public string ServerFor(string tool) => _toolToServer.GetValueOrDefault(tool, "unknown");

    public async Task<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken ct = default)
    {
        if (_tools is not null) return _tools;
        await _gate.WaitAsync(ct);
        try
        {
            if (_tools is not null) return _tools;
            var all = new List<AITool>();

            foreach (var s in _configs)
            {
                // resolve relative dll paths against the web project's content root
                var args = s.Args.Select(a => a.EndsWith(".dll")
                    ? Path.GetFullPath(Path.Combine(_env.ContentRootPath, a)) : a).ToList();

                var transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = s.Name,
                    Command = s.Command,
                    Arguments = args
                });

                var client = await McpClientFactory.CreateAsync(transport, cancellationToken: ct);
                _clients.Add(client);

                foreach (var tool in await client.ListToolsAsync(cancellationToken: ct))
                {
                    _toolToServer[tool.Name] = s.Name;
                    all.Add(tool); // McpClientTool is an AIFunction -> usable directly by IChatClient
                    _log.LogInformation("MCP[{Server}] tool registered: {Tool}", s.Name, tool.Name);
                }
            }
            return _tools = all;
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var c in _clients) await c.DisposeAsync();
    }
}
