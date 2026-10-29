using ModelContextProtocol.Server;

namespace PlotlyTools.Tools;

/// <summary>Chart tools for values across categories or over an axis: create_bar_chart, create_line_chart,
/// create_area_chart, create_scatter_chart and create_histogram (one file per tool).</summary>
[McpServerToolType]
internal sealed partial class BasicChartTools
{
    private readonly ChartToolRunner _runner;

    public BasicChartTools(ChartToolRunner runner)
    {
        _runner = runner;
    }
}
