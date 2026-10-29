using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_scatter_chart tool.
internal sealed partial class BasicChartTools
{
    [McpServerTool(Name = "create_scatter_chart", Title = "Create a scatter or bubble chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a scatter chart, one point per row, and returns it as an HTML page or an image, after a short JSON " +
        "summary. Use it to show how two numeric columns relate (price against quantity). Give a size column to make " +
        "it a bubble chart. For a single column's distribution use create_histogram.")]
    public Task<CallToolResult> CreateScatterChartAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Column for the horizontal axis: numbers or dates, e.g. 'Price'.")] string x,
        [Description("Numeric column for the vertical axis, e.g. 'Quantity'.")] string y,
        [Description(ParameterDescriptions.Series)] string? series = null,
        [Description("Optional numeric column for marker size; makes a bubble chart. Bubble area is proportional to the value.")] string? size = null,
        [Description("Optional column whose text is shown when hovering over a point, e.g. 'ProductName'.")] string? label = null,
        [Description("Logarithmic horizontal axis. Default false.")] bool? logX = null,
        [Description("Logarithmic vertical axis. Default false.")] bool? logY = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description(ParameterDescriptions.YTitle)] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new ScatterChartSpec(x, y, series, size, label, logX, logY, title, xTitle, yTitle);
        return _runner.RunAsync(
            "scatter", data, title, format, theme, width, height,
            table => ScatterChartBuilder.Build(spec, table, _runner.MaxSeries),
            cancellationToken);
    }
}
