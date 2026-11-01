using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>Pie and donut charts: shares of a whole, with small slices merged into "Other".</summary>
internal static partial class PieChartBuilder
{
    public const int DefaultMaxSlices = 8;
    public const int MaxSlicesLimit = 50;
    public const string OtherLabel = "Other";

    public static BuiltChart Build(PieChartSpec spec, ChartTable table)
    {
        var aggregation = Aggregator.Parse(spec.Aggregate);
        var hole = ChartBuilder.InRange(spec.Hole, "hole", 0, 0, 0.9);
        var maxSlices = ChartBuilder.InRange(spec.MaxSlices, "maxSlices", DefaultMaxSlices, 2, MaxSlicesLimit);

        var labels = table.Labels(spec.Labels, "labels");
        var values = Aggregator.Inputs(table, spec.Values ?? "", "values", aggregation);
        var combined = Aggregator.Combine(labels, values, Enumerable.Range(0, table.RowCount), aggregation);

        var slices = combined.Keys
            .Zip(combined.Values, (label, value) => (Label: label, Value: value))
            .Where(s => s.Value is not null)
            .Select(s => (s.Label, Value: s.Value!.Value))
            .ToList();

        var negative = slices.FirstOrDefault(s => s.Value < 0);
        if (negative.Label is not null)
        {
            throw new McpException($"'{negative.Label}' totals {negative.Value}; a pie can't show negative values. Use a bar chart instead.");
        }

        var notes = new List<string>();
        if (combined.HadDuplicates)
        {
            notes.Add($"Rows with the same {table.Columns[table.IndexOf(spec.Labels, "labels")]} were combined ({aggregation.ToString().ToLowerInvariant()}).");
        }

        if (slices.Count > maxSlices)
        {
            // Keep the largest slices and merge the rest, so the pie stays readable.
            var kept = slices.OrderByDescending(s => s.Value).Take(maxSlices - 1).ToList();
            var rest = slices.Except(kept).ToList();
            var otherLabel = kept.Any(s => s.Label == OtherLabel) ? $"{OtherLabel} (merged)" : OtherLabel;
            notes.Add($"The {rest.Count} smallest slices were merged into '{otherLabel}'.");
            slices = [.. kept, (otherLabel, rest.Sum(s => s.Value))];
        }

        var trace = ChartBuilder.NewTrace("pie", null);
        trace.SetValue("labels", slices.Select(s => s.Label).ToArray());
        trace.SetValue("values", slices.Select(s => s.Value).ToArray());
        trace.SetValue("hole", hole);
        trace.SetValue("textinfo", spec.ShowPercent ?? true ? "percent" : "value");

        var layout = ChartBuilder.NewLayout(spec.Title);
        return new BuiltChart(ChartBuilder.Combine([trace], layout), 1, slices.Count, notes);
    }
}
