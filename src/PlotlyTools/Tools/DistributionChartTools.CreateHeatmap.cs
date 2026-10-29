using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_heatmap tool.
internal sealed partial class DistributionChartTools
{
    [McpServerTool(Name = "create_heatmap", Title = "Create a heatmap", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a heatmap of a value across two categories (a grid coloured by value) and returns it as an HTML page " +
        "or an image, after a short JSON summary. Use it for patterns over two dimensions (tickets per weekday and " +
        "hour, sales per region and month). Pass long-format data: one row per x/y pair, as GROUP BY returns it.")]
    public Task<CallToolResult> CreateHeatmapAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description("Category column for the columns of the grid, e.g. 'Hour'.")] string x,
        [Description("Category column for the rows of the grid, e.g. 'Weekday'.")] string y,
        [Description("Numeric column for the colour, e.g. 'TicketCount'.")] string z,
        [Description(ParameterDescriptions.Aggregate)] string? aggregate = null,
        [Description("plotly.js colour scale: 'Viridis' (default), 'Cividis', 'Blues', 'Greens', 'Greys', 'Reds', 'YlGnBu', 'YlOrRd', 'RdBu', 'Portland', 'Picnic', 'Hot', 'Electric', 'Earth', 'Jet', 'Rainbow', 'Bluered' or 'Blackbody'.")] string? colorScale = null,
        [Description("Print each cell's value in it. Default false.")] bool? showValues = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.XTitle)] string? xTitle = null,
        [Description(ParameterDescriptions.YTitle)] string? yTitle = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new HeatmapSpec(x, y, z, aggregate, colorScale, showValues, title, xTitle, yTitle);
        return _runner.RunAsync(
            "heatmap", data, title, format, theme, width, height,
            table => HeatmapBuilder.Build(spec, table),
            cancellationToken);
    }
}
