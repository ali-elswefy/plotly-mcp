using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_line_chart tool.
internal sealed partial class BasicChartTools
{
    [McpServerTool(Name = "create_line_chart", Title = "Create a line chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a line chart of values over an ordered axis, usually time, and returns it as an HTML page or an " +
        "image, after a short JSON summary. Rows are sorted by x, so they can come in any order. Use it for trends " +
        "(monthly sales, daily counts). To show totals and their make-up over time use create_area_chart.")]
    public Task<CallToolResult> CreateLineChartAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Ordered column for the horizontal axis: dates (ISO text such as '2025-01-31'), numbers or ordered text, e.g. 'Month'.")] string x,
        [Description("Value column(s), e.g. ['Sales']; each column becomes a line.")] string[] y,
        [Description(ParameterDescriptions.Series + " Use with a single y column.")] string? series = null,
        [Description("Show a marker at each point. Default false.")] bool? markers = null,
        [Description("'linear' (default), 'spline' (smoothed) or 'step'.")] string? shape = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description(ParameterDescriptions.YTitle)] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new LineChartSpec(x, y, series, markers, shape, title, xTitle, yTitle);
        return _runner.RunAsync(
            "line", data, title, format, theme, width, height,
            table => LineChartBuilder.Build(spec, table, _runner.MaxSeries),
            cancellationToken);
    }
}
