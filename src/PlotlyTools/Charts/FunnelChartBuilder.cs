namespace PlotlyTools.Charts;

/// <summary>Funnel charts: values dropping through ordered stages, kept in the order the stages first appear.</summary>
internal static partial class FunnelChartBuilder
{
    public static BuiltChart Build(FunnelChartSpec spec, ChartTable table, int maxSeries)
    {
        var aggregation = Aggregator.Parse(spec.Aggregate);
        var stages = table.Labels(spec.Stage, "stage");
        var values = Aggregator.Inputs(table, spec.Values ?? "", "values", aggregation);

        var points = 0;
        var combined = false;
        var groups = table.Groups(spec.Series, maxSeries);
        var traces = groups.Select(group =>
        {
            var funnel = Aggregator.Combine(stages, values, group.Rows, aggregation);
            combined |= funnel.HadDuplicates;
            points += funnel.Keys.Count;

            var trace = ChartBuilder.NewTrace("funnel", group.Name);
            trace.SetValue("y", funnel.Keys);
            trace.SetValue("x", funnel.Values);

            // Each stage's share of the first stage is what a funnel is read for.
            trace.SetValue("textinfo", "value+percent initial");
            return trace;
        }).ToList();

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("funnelmode", "group");
        layout.SetValue("yaxis", ChartBuilder.Axis(null, ("type", "category")));

        var notes = combined
            ? new[] { $"Rows with the same {table.Columns[table.IndexOf(spec.Stage, "stage")]} were combined ({aggregation.ToString().ToLowerInvariant()})." }
            : [];
        return new BuiltChart(ChartBuilder.Combine(traces, layout), traces.Count, points, notes);
    }
}
