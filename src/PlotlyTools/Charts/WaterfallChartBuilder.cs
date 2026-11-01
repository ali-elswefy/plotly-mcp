using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>Waterfall charts: positive and negative steps, in row order, adding up to totals.</summary>
internal static partial class WaterfallChartBuilder
{
    public const string TotalLabel = "Total";

    private static readonly (string Name, string Value)[] Measures = [("relative", "relative"), ("total", "total"), ("absolute", "absolute")];

    public static BuiltChart Build(WaterfallChartSpec spec, ChartTable table)
    {
        var labels = table.Labels(spec.Labels, "labels").ToList();
        var values = table.Numbers(spec.Values, "values").Select(v => (double?)(v ?? 0)).ToList();
        var measures = string.IsNullOrWhiteSpace(spec.Measure)
            ? Enumerable.Repeat("relative", table.RowCount).ToList()
            : ReadMeasures(table, spec.Measure);

        // Without a measure column, a closing total bar is what makes the steps readable as a waterfall.
        if (string.IsNullOrWhiteSpace(spec.Measure) && (spec.ShowTotal ?? true))
        {
            labels.Add(labels.Contains(TotalLabel) ? $"{TotalLabel} (all)" : TotalLabel);
            values.Add(null);
            measures.Add("total");
        }

        var trace = ChartBuilder.NewTrace("waterfall", null);
        trace.SetValue("x", labels);
        trace.SetValue("y", values);
        trace.SetValue("measure", measures);
        trace.SetValue("connector", new Dictionary<string, object> { ["line"] = new Dictionary<string, object> { ["color"] = "#9ca3af", ["width"] = 1 } });
        trace.SetValue("increasing", Marker("#10b981"));
        trace.SetValue("decreasing", Marker("#e11d48"));
        trace.SetValue("totals", Marker(ChartStyle.LightColorway[0]));

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("showlegend", false);
        layout.SetValue("xaxis", ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.Labels, "labels")], ("type", "category")));
        layout.SetValue("yaxis", ChartBuilder.Axis(spec.YTitle ?? table.Columns[table.IndexOf(spec.Values, "values")]));

        return new BuiltChart(ChartBuilder.Combine([trace], layout), 1, labels.Count, []);
    }

    private static List<string> ReadMeasures(ChartTable table, string column) =>
        table.Labels(column, "measure").Select((text, row) => text == ChartTable.BlankLabel
            ? "relative"
            : Measures.FirstOrDefault(m => m.Name.Equals(text.Trim(), StringComparison.OrdinalIgnoreCase)).Value
              ?? throw new McpException(
                  $"Column '{table.Columns[table.IndexOf(column, "measure")]}' (measure) must hold 'relative', 'total' or 'absolute', but row {row + 1} has '{text}'."))
        .ToList();

    private static Dictionary<string, object> Marker(string color) =>
        new() { ["marker"] = new Dictionary<string, object> { ["color"] = color } };
}
