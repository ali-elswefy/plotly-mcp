using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_bar_chart tool.
internal sealed partial class BasicChartTools
{
    [McpServerTool(Name = "create_bar_chart", Title = "Create a bar chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a bar chart comparing values across categories and returns it as an HTML page or an image, after a " +
        "short JSON summary. Use it for totals or averages per category (sales per region, tickets per status). " +
        "For trends over time use create_line_chart; for shares of one whole use create_pie_chart.")]
    public Task<CallToolResult> CreateBarChartAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Category column, one bar (or group of bars) per value, e.g. 'Region'.")] string x,
        [Description("Value column(s), e.g. ['Sales'] or ['Sales2024', 'Sales2025']; each column becomes a series.")] string[] y,
        [Description(ParameterDescriptions.Series + " Use with a single y column.")] string? series = null,
        [Description("'vertical' (default) or 'horizontal'. Horizontal suits long category names.")] string? orientation = null,
        [Description("How several series are drawn: 'grouped' (default, side by side), 'stacked', or 'percent' (stacked to 100%).")] string? barMode = null,
        [Description(ParameterDescriptions.Aggregate)] string? aggregate = null,
        [Description("Order of the categories: 'none' (default, as they first appear), 'asc' or 'desc' by total value.")] string? sort = null,
        [Description("Print each bar's value on it. Default false.")] bool? showValues = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description(ParameterDescriptions.YTitle)] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new BarChartSpec(x, y, series, orientation, barMode, aggregate, sort, showValues, title, xTitle, yTitle);
        return _runner.RunAsync(
            "bar", data, title, format, theme, width, height,
            table => BarChartBuilder.Build(spec, table, _runner.MaxSeries),
            cancellationToken);
    }
}
