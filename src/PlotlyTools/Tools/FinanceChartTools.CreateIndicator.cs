using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_indicator tool.
internal sealed partial class FinanceChartTools
{
    [McpServerTool(Name = "create_indicator", Title = "Create a KPI indicator or gauge", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws one KPI number, optionally with its change from a reference value or as a gauge, and returns it as an " +
        "HTML page or an image, after a short JSON summary. Takes the numbers directly, not a table: compute the value " +
        "first (e.g. with a SQL SUM). Use it for headline figures (revenue this month against last month).")]
    public Task<CallToolResult> CreateIndicatorAsync(
        [Description("The number to show, e.g. 1250000.")] double value,
        [Description("Optional value to compare with, e.g. last month's 1100000; shows the change.")] double? reference = null,
        [Description("'number' (default without reference), 'delta' (default with reference: number plus change) or 'gauge'.")] string? mode = null,
        [Description("Gauge minimum. Default 0.")] double? min = null,
        [Description("Gauge maximum. Default: a round number above the value.")] double? max = null,
        [Description("Text before the number, e.g. '$'.")] string? prefix = null,
        [Description("Text after the number, e.g. '%'.")] string? suffix = null,
        [Description("Label shown above the number, e.g. 'Revenue this month'.")] string? title = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new IndicatorSpec(value, reference, mode, min, max, prefix, suffix, title);
        return _runner.RunAsync(
            "indicator", title, format, theme, width, height,
            () => IndicatorBuilder.Build(spec),
            cancellationToken);
    }
}
