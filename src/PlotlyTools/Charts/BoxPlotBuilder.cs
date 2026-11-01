namespace PlotlyTools.Charts;

/// <summary>Box and violin plots: one numeric column's distribution, per group and series.</summary>
internal static partial class BoxPlotBuilder
{
    public static BuiltChart Build(BoxPlotSpec spec, ChartTable table, int maxSeries)
    {
        var violin = ChartBuilder.Choose(spec.Style, "style", false, ("box", false), ("violin", true));
        var points = ChartBuilder.Choose<object>(spec.Points, "points", "outliers", ("outliers", "outliers"), ("all", "all"), ("none", false));

        var values = table.Numbers(spec.Y, "y");
        var groups = string.IsNullOrWhiteSpace(spec.X) ? null : table.Labels(spec.X, "x");
        var series = table.Groups(spec.Series, maxSeries);

        var traces = series.Select(part =>
        {
            var trace = ChartBuilder.NewTrace(violin ? "violin" : "box", part.Name ?? table.Columns[table.IndexOf(spec.Y, "y")]);
            trace.SetValue("y", ChartBuilder.Pick(values, part.Rows));
            if (groups is not null)
            {
                trace.SetValue("x", ChartBuilder.Pick(groups, part.Rows));
            }

            if (violin)
            {
                trace.SetValue("points", points);
                trace.SetValue("box", new Dictionary<string, object> { ["visible"] = true });
                trace.SetValue("meanline", new Dictionary<string, object> { ["visible"] = true });
            }
            else
            {
                trace.SetValue("boxpoints", points);
                trace.SetValue("boxmean", true);
            }

            return trace;
        }).ToList();

        var layout = ChartBuilder.NewLayout(spec.Title);
        if (series.Count > 1 && groups is not null)
        {
            // Several series within each group sit side by side rather than on top of each other.
            layout.SetValue(violin ? "violinmode" : "boxmode", "group");
        }

        layout.SetValue("xaxis", groups is null
            ? ChartBuilder.Axis(spec.XTitle)
            : ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.X, "x")], ("type", "category")));
        layout.SetValue("yaxis", ChartBuilder.Axis(spec.YTitle ?? table.Columns[table.IndexOf(spec.Y, "y")]));

        return new BuiltChart(ChartBuilder.Combine(traces, layout), traces.Count, values.Count(v => v is not null), []);
    }
}
