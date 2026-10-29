using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_funnel_chart tool.
internal sealed partial class PartToWholeChartTools
{
    [McpServerTool(Name = "create_funnel_chart", Title = "Create a funnel chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a funnel chart of values dropping through ordered stages, with each stage's share of the first, and " +
        "returns it as an HTML page or an image, after a short JSON summary. Use it for conversion steps (leads → " +
        "offers → orders). Stages keep the order they first appear in the rows, so sort them in SQL.")]
    public Task<CallToolResult> CreateFunnelChartAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Stage column, in funnel order, e.g. 'Stage'.")] string stage,
        [Description("Numeric column with each stage's amount, e.g. 'Count'.")] string values,
        [Description(ParameterDescriptions.Series + " Funnels are drawn side by side.")] string? series = null,
        [Description(ParameterDescriptions.Aggregate)] string? aggregate = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new FunnelChartSpec(stage, values, series, aggregate, title);
        return _runner.RunAsync(
            "funnel", data, title, format, theme, width, height,
            table => FunnelChartBuilder.Build(spec, table, _runner.MaxSeries),
            cancellationToken);
    }
}
