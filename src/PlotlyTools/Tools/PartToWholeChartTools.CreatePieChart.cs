using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_pie_chart tool.
internal sealed partial class PartToWholeChartTools
{
    [McpServerTool(Name = "create_pie_chart", Title = "Create a pie or donut chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a pie or donut chart of each category's share of the total and returns it as an HTML page or an " +
        "image, after a short JSON summary. Use it for a handful of categories that add up to a whole (tickets by " +
        "priority). Small slices beyond maxSlices are merged into 'Other'. To compare amounts precisely, or with " +
        "negative values, use create_bar_chart.")]
    public Task<CallToolResult> CreatePieChartAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Category column, one slice per value, e.g. 'Priority'.")] string labels,
        [Description("Numeric column with each row's amount, e.g. 'TicketCount'.")] string values,
        [Description(ParameterDescriptions.Aggregate)] string? aggregate = null,
        [Description("Size of the hole in the middle, 0 to 0.9. 0 (default) is a pie; around 0.5 makes a donut.")] double? hole = null,
        [Description("Most slices to draw (2-50); smaller ones are merged into 'Other'. Default 8.")] int? maxSlices = null,
        [Description("Label slices with their percentage (default true) instead of their value.")] bool? showPercent = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new PieChartSpec(labels, values, aggregate, hole, maxSlices, showPercent, title);
        return _runner.RunAsync(
            "pie", data, title, format, theme, width, height,
            table => PieChartBuilder.Build(spec, table),
            cancellationToken);
    }
}
