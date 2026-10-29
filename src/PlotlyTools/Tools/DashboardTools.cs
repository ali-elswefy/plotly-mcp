using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using PlotlyTools.Configuration;
using PlotlyTools.Rendering;

namespace PlotlyTools.Tools;

/// <summary>Dashboard tools: create_dashboard (one file per tool).</summary>
[McpServerToolType]
internal sealed partial class DashboardTools
{
    private readonly ChartRenderer _renderer;
    private readonly PlotlyToolsOptions _options;

    public DashboardTools(ChartRenderer renderer, IOptions<PlotlyToolsOptions> options)
    {
        _renderer = renderer;
        _options = options.Value;
    }
}
