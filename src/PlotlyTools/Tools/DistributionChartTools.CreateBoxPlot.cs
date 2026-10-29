using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_box_plot tool.
internal sealed partial class DistributionChartTools
{
    [McpServerTool(Name = "create_box_plot", Title = "Create a box or violin plot", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a box plot (or violin plot) of one numeric column's distribution, optionally one box per group, and " +
        "returns it as an HTML page or an image, after a short JSON summary. Use it to compare spreads, medians and " +
        "outliers across groups (delivery times per carrier). Pass the raw values, one per row. For one distribution's " +
        "shape in detail use create_histogram.")]
    public Task<CallToolResult> CreateBoxPlotAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Numeric column whose distribution is drawn, e.g. 'DeliveryDays'.")] string y,
        [Description("Optional group column, one box per value, e.g. 'Carrier'.")] string? x = null,
        [Description(ParameterDescriptions.Series + " Boxes for each series sit side by side within a group.")] string? series = null,
        [Description("'box' (default) or 'violin', which also shows the shape of the distribution.")] string? style = null,
        [Description("Which raw points to draw: 'outliers' (default), 'all' or 'none'.")] string? points = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description(ParameterDescriptions.YTitle)] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new BoxPlotSpec(y, x, series, style, points, title, xTitle, yTitle);
        return _runner.RunAsync(
            "box", data, title, format, theme, width, height,
            table => BoxPlotBuilder.Build(spec, table, _runner.MaxSeries),
            cancellationToken);
    }
}
