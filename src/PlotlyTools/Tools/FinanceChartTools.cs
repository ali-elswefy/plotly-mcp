using ModelContextProtocol.Server;

namespace PlotlyTools.Tools;

/// <summary>Chart tools for totals and how they change: create_waterfall_chart and create_indicator (one file per tool).</summary>
[McpServerToolType]
internal sealed partial class FinanceChartTools
{
    private readonly ChartToolRunner _runner;

    public FinanceChartTools(ChartToolRunner runner)
    {
        _runner = runner;
    }
}
