using System.Text.Json;
using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Dashboards;

// The chart types a dashboard item can be, each with the spec and builder its chart tool uses.
internal static partial class DashboardParser
{
    /// <summary>Item properties that aren't chart parameters.</summary>
    private static readonly string[] LayoutProperties = ["type", "columnSpan", "rowSpan"];

    /// <summary>Chart tool parameters the dashboard sets for every chart.</summary>
    private static readonly string[] DashboardWideParameters = ["format", "theme", "width", "height"];

    /// <param name="Name">The item's "type", the same as the chart tool's chartType ("bar", "pie"…).</param>
    private sealed partial record ChartType(string Name, Type SpecType, bool NeedsData, Func<object, ChartTable?, int, BuiltChart> Build)
    {
        /// <summary>The parameters an item of this type takes: the spec's, named as the tool names them.</summary>
        public IReadOnlyList<string> Parameters { get; } = SpecType.GetConstructors().Single().GetParameters()
            .Select(p => char.ToLowerInvariant(p.Name![0]) + p.Name[1..])
            .Concat(NeedsData ? ["data"] : [])
            .ToList();
    }

    private static readonly ChartType[] ChartTypes =
    [
        Tabular<BarChartSpec>("bar", BarChartBuilder.Build),
        Tabular<LineChartSpec>("line", LineChartBuilder.Build),
        Tabular<AreaChartSpec>("area", AreaChartBuilder.Build),
        Tabular<ScatterChartSpec>("scatter", ScatterChartBuilder.Build),
        Tabular<HistogramSpec>("histogram", HistogramBuilder.Build),
        Tabular<BoxPlotSpec>("box", BoxPlotBuilder.Build),
        Tabular<HeatmapSpec>("heatmap", (spec, table, _) => HeatmapBuilder.Build(spec, table)),
        Tabular<PieChartSpec>("pie", (spec, table, _) => PieChartBuilder.Build(spec, table)),
        Tabular<FunnelChartSpec>("funnel", FunnelChartBuilder.Build),
        Tabular<HierarchySpec>("treemap", (spec, table, _) => HierarchyBuilder.Build(spec, table, "treemap")),
        Tabular<HierarchySpec>("sunburst", (spec, table, _) => HierarchyBuilder.Build(spec, table, "sunburst")),
        Tabular<WaterfallChartSpec>("waterfall", (spec, table, _) => WaterfallChartBuilder.Build(spec, table)),
        new("indicator", typeof(IndicatorSpec), NeedsData: false, (spec, _, _) => IndicatorBuilder.Build((IndicatorSpec)spec)),
    ];

    /// <summary>The item type names, for tool descriptions and errors.</summary>
    public static IEnumerable<string> ChartTypeNames => ChartTypes.Select(t => t.Name);

    private static ChartType Tabular<TSpec>(string name, Func<TSpec, ChartTable, int, BuiltChart> build) =>
        new(name, typeof(TSpec), NeedsData: true, (spec, table, maxSeries) => build((TSpec)spec, table!, maxSeries));

    private static ChartType FindChartType(string type) =>
        ChartTypes.FirstOrDefault(t => t.Name.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new McpException($"unknown type '{type}'. Use {ChartBuilder.OrList(ChartTypeNames)}.");

    /// <summary>
    /// Refuses properties the chart doesn't take. Unknown properties would otherwise be ignored silently, and a
    /// typo ("yColumn") would draw a different chart from the one the agent asked for.
    /// </summary>
    private static void CheckParameters(JsonElement item, ChartType chartType)
    {
        foreach (var property in item.EnumerateObject())
        {
            var name = property.Name;
            if (LayoutProperties.Contains(name, StringComparer.OrdinalIgnoreCase) || chartType.Parameters.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            throw DashboardWideParameters.Contains(name, StringComparer.OrdinalIgnoreCase)
                ? new McpException($"'{name}' is set once for the whole dashboard, not per item.")
                : new McpException($"unknown parameter '{name}'. A {chartType.Name} item takes {string.Join(", ", chartType.Parameters)}, plus columnSpan and rowSpan.");
        }
    }
}
