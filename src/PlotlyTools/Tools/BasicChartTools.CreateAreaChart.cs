using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_area_chart tool.
internal sealed partial class BasicChartTools
{
    [McpServerTool(Name = "create_area_chart", Title = "Create an area chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws an area chart (filled lines, stacked by default) over an ordered axis and returns it as an HTML page " +
        "or an image, after a short JSON summary. Use it to show a total over time and what it's made of (sales per " +
        "product per month). For separate trends without a total use create_line_chart.")]
    public Task<CallToolResult> CreateAreaChartAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Ordered column for the horizontal axis: dates (ISO text), numbers or ordered text, e.g. 'Month'.")] string x,
        [Description("Value column(s), e.g. ['Sales']; each column becomes an area.")] string[] y,
        [Description(ParameterDescriptions.Series + " Use with a single y column.")] string? series = null,
        [Description("Stack the areas on top of each other. Default true.")] bool? stacked = null,
        [Description("Stack to 100%, to show shares instead of amounts. Needs stacked. Default false.")] bool? normalize = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description(ParameterDescriptions.YTitle)] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new AreaChartSpec(x, y, series, stacked, normalize, title, xTitle, yTitle);
        return _runner.RunAsync(
            "area", data, title, format, theme, width, height,
            table => AreaChartBuilder.Build(spec, table, _runner.MaxSeries),
            cancellationToken);
    }
}
