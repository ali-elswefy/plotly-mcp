using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_treemap tool.
internal sealed partial class PartToWholeChartTools
{
    [McpServerTool(Name = "create_treemap", Title = "Create a treemap", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a treemap: a hierarchy as nested rectangles sized by value, and returns it as an HTML page or an " +
        "image, after a short JSON summary. Use it for shares within a hierarchy with many items (sales by region, " +
        "country and city). Parents are the total of their children. For the same data as rings use create_sunburst.")]
    public Task<CallToolResult> CreateTreemapAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description(PathDescription)] string[] path,
        [Description("Numeric column with each row's amount, e.g. 'Sales'.")] string values,
        [Description(ParameterDescriptions.Aggregate)] string? aggregate = null,
        [Description("Levels shown at once (1 to the number of path columns); click a box to go deeper. Default: all.")] int? maxDepth = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new HierarchySpec(path, values, aggregate, maxDepth, title);
        return _runner.RunAsync(
            "treemap", data, title, format, theme, width, height,
            table => HierarchyBuilder.Build(spec, table, "treemap"),
            cancellationToken);
    }
}
