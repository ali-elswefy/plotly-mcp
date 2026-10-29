using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using PlotlyTools;
using PlotlyTools.Configuration;

// The standalone PlotlyTools MCP server. The tools themselves live in the PlotlyTools library, which other MCP
// servers can also host headless with WithPlotlyTools.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // When run as a Windows service the working directory is System32,
    // so resolve appsettings.json next to the binary instead.
    ContentRootPath = AppContext.BaseDirectory,
});

// Machine-local settings such as the browser path; gitignored, never committed.
// Inserted just before the (unprefixed) environment variables, so it overrides appsettings*.json and user
// secrets while a server's environment variables and the command line still override it.
var sources = builder.Configuration.Sources;
var environmentIndex = sources.ToList().FindLastIndex(s => s is EnvironmentVariablesConfigurationSource { Prefix: null or "" });
sources.Insert(
    environmentIndex < 0 ? sources.Count : environmentIndex,
    new JsonConfigurationSource { Path = "appsettings.Local.json", Optional = true, ReloadOnChange = false });

builder.Services.AddHealthChecks();

builder.Services
    .AddMcpServer(o =>
    {
        o.ServerInfo = new() { Name = "plotly-tools-mcp", Version = "0.2.0" };
        o.ServerInstructions = PlotlyToolsInfo.Instructions;
    })
    .WithHttpTransport(o =>
    {
        // No per-client state in memory, so any instance behind a load balancer can serve any request.
        o.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithPlotlyTools(builder.Configuration.GetSection(PlotlyToolsOptions.SectionName))
    .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
    {
        try
        {
            return await next(context, cancellationToken);
        }
        catch (McpException ex) when (ex.GetType() == typeof(McpException))
        {
            // Tools throw plain McpException for expected, agent-facing errors (unknown column, bad format).
            // Return them as tool errors here so they aren't logged as server failures with a stack trace.
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.Message }] };
        }
    }));

var app = builder.Build();

app.MapHealthChecks("/health");

// No authentication yet: keep this behind a trusted network until an auth layer lands.
app.MapMcp("/mcp");

app.Run();
