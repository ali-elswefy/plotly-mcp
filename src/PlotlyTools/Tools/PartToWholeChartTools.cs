using ModelContextProtocol.Server;

namespace PlotlyTools.Tools;

/// <summary>Chart tools for shares of a whole: create_pie_chart, create_funnel_chart, create_treemap and
/// create_sunburst (one file per tool).</summary>
[McpServerToolType]
internal sealed partial class PartToWholeChartTools
{
    private const string PathDescription =
        "Hierarchy columns, outer level first, e.g. ['Region', 'Country', 'City']. Up to 6 levels.";

    private readonly ChartToolRunner _runner;

    public PartToWholeChartTools(ChartToolRunner runner)
    {
        _runner = runner;
    }
}
