using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_histogram tool.
internal sealed partial class BasicChartTools
{
    [McpServerTool(Name = "create_histogram", Title = "Create a histogram", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a histogram of one numeric column, counting how many values fall in each range, and returns it as an " +
        "HTML page or an image, after a short JSON summary. Use it for distributions (order sizes, ticket ages). " +
        "Pass the raw values, one per row, not pre-counted bins; a series column overlays one histogram per value.")]
    public Task<CallToolResult> CreateHistogramAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Numeric column whose values are counted, e.g. 'OrderTotal'.")] string x,
        [Description(ParameterDescriptions.Series + " Histograms are overlaid.")] string? series = null,
        [Description($"Number of bins (1-500). Default: chosen automatically.")] int? bins = null,
        [Description("Bar height: 'count' (default), 'percent', 'probability' or 'density'.")] string? normalize = null,
        [Description("Show the cumulative distribution. Default false.")] bool? cumulative = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description("Title of the vertical axis. Defaults to 'Count' (or the normalize mode).")] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new HistogramSpec(x, series, bins, normalize, cumulative, title, xTitle, yTitle);
        return _runner.RunAsync(
            "histogram", data, title, format, theme, width, height,
            table => HistogramBuilder.Build(spec, table, _runner.MaxSeries),
            cancellationToken);
    }
}
