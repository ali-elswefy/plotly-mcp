using ModelContextProtocol.Server;

namespace PlotlyTools.Tools;

/// <summary>Chart tools for how values are spread: create_box_plot and create_heatmap (one file per tool).</summary>
[McpServerToolType]
internal sealed partial class DistributionChartTools
{
    private readonly ChartToolRunner _runner;

    public DistributionChartTools(ChartToolRunner runner)
    {
        _runner = runner;
    }
}
