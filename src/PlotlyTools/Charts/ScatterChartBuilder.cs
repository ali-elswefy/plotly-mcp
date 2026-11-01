using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>Scatter charts: two numeric columns against each other; a size column makes it a bubble chart.</summary>
internal static partial class ScatterChartBuilder
{
    /// <summary>Diameter in pixels of the largest bubble.</summary>
    private const double MaxBubbleSize = 40;

    public static BuiltChart Build(ScatterChartSpec spec, ChartTable table, int maxSeries)
    {
        var xs = table.Values(spec.X, "x");
        var ys = table.Numbers(spec.Y, "y");
        var sizes = string.IsNullOrWhiteSpace(spec.Size) ? null : table.Numbers(spec.Size, "size");
        var labels = string.IsNullOrWhiteSpace(spec.Label) ? null : table.Labels(spec.Label, "label");

        // Bubble area, not diameter, is proportional to the value, so a value twice as big doesn't look four times as big.
        var sizeRef = 0.0;
        if (sizes is not null)
        {
            var largest = sizes.Max() ?? 0;
            sizeRef = largest > 0
                ? 2 * largest / (MaxBubbleSize * MaxBubbleSize)
                : throw new McpException($"Column '{table.Columns[table.IndexOf(spec.Size, "size")]}' (size) needs at least one value above zero.");
        }

        var parts = ChartBuilder.SplitSeries(table, spec.Y ?? "", spec.Series, maxSeries);
        var traces = parts.Select(part =>
        {
            var trace = ChartBuilder.NewTrace("scatter", part.Name);
            trace.SetValue("x", ChartBuilder.Pick(xs, part.Rows));
            trace.SetValue("y", ChartBuilder.Pick(ys, part.Rows));
            trace.SetValue("mode", "markers");
            if (sizes is not null)
            {
                trace.SetValue("marker", new Dictionary<string, object>
                {
                    ["size"] = ChartBuilder.Pick(sizes, part.Rows),
                    ["sizemode"] = "area",
                    ["sizeref"] = sizeRef,
                    ["sizemin"] = 3,
                });
            }

            if (labels is not null)
            {
                trace.SetValue("text", ChartBuilder.Pick(labels, part.Rows));
            }

            return trace;
        }).ToList();

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("xaxis", spec.LogX == true
            ? ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.X, "x")], ("type", "log"))
            : ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.X, "x")]));
        layout.SetValue("yaxis", spec.LogY == true
            ? ChartBuilder.Axis(spec.YTitle ?? table.Columns[table.IndexOf(spec.Y, "y")], ("type", "log"))
            : ChartBuilder.Axis(spec.YTitle ?? table.Columns[table.IndexOf(spec.Y, "y")]));

        return new BuiltChart(ChartBuilder.Combine(traces, layout), traces.Count, table.RowCount, []);
    }
}
