using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;

namespace PlotlyTools.Tools;

// The create_sunburst tool.
internal sealed partial class PartToWholeChartTools
{
    [McpServerTool(Name = "create_sunburst", Title = "Create a sunburst chart", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Draws a sunburst: a hierarchy as rings, inner ring first, each slice sized by value, and returns it as an " +
        "HTML page or an image, after a short JSON summary. Use it for shares within a hierarchy of a few levels " +
        "(budget by department and team). Parents are the total of their children. For many small items use create_treemap.")]
    public Task<CallToolResult> CreateSunburstAsync(
        [Description(ParameterDescriptions.Data)] ChartDataInput data,
        [Description(PathDescription)] string[] path,
        [Description("Numeric column with each row's amount, e.g. 'Budget'.")] string values,
        [Description(ParameterDescriptions.Aggregate)] string? aggregate = null,
        [Description("Rings shown at once (1 to the number of path columns); click a slice to go deeper. Default: all.")] int? maxDepth = null,
        [Description(ParameterDescriptions.Title)] string? title = null,
        [Description(ParameterDescriptions.Theme)] string? theme = null,
        [Description(ParameterDescriptions.Format)] string? format = null,
        [Description(ParameterDescriptions.Width)] int? width = null,
        [Description(ParameterDescriptions.Height)] int? height = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new HierarchySpec(path, values, aggregate, maxDepth, title);
        return _runner.RunAsync(
            "sunburst", data, title, format, theme, width, height,
            table => HierarchyBuilder.Build(spec, table, "sunburst"),
            cancellationToken);
    }
}
