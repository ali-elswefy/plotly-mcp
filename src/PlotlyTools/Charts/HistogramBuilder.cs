namespace PlotlyTools.Charts;

/// <summary>Histograms: how the values of one numeric column are distributed, overlaid per series value.</summary>
internal static partial class HistogramBuilder
{
    public const int MaxBins = 500;

    public static BuiltChart Build(HistogramSpec spec, ChartTable table, int maxSeries)
    {
        var bins = ChartBuilder.InRange(spec.Bins, "bins", 0, 1, MaxBins);
        var (histnorm, valueTitle) = ChartBuilder.Choose(
            spec.Normalize, "normalize", ("", "Count"),
            ("count", ("", "Count")),
            ("percent", ("percent", "Percent")),
            ("probability", ("probability", "Probability")),
            ("density", ("probability density", "Density")));

        var values = table.Numbers(spec.X, "x");
        var groups = table.Groups(spec.Series, maxSeries);
        var traces = groups.Select(group =>
        {
            var trace = ChartBuilder.NewTrace("histogram", group.Name);
            trace.SetValue("x", ChartBuilder.Pick(values, group.Rows));
            if (bins > 0)
            {
                trace.SetValue("nbinsx", bins);
            }

            if (histnorm.Length > 0)
            {
                trace.SetValue("histnorm", histnorm);
            }

            if (spec.Cumulative == true)
            {
                trace.SetValue("cumulative", new Dictionary<string, object> { ["enabled"] = true });
            }

            if (groups.Count > 1)
            {
                // Overlaid series must stay see-through, or the last one drawn hides the others.
                trace.SetValue("opacity", 0.6);
            }

            return trace;
        }).ToList();

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("barmode", "overlay");
        layout.SetValue("xaxis", ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.X, "x")]));
        layout.SetValue("yaxis", ChartBuilder.Axis(spec.YTitle ?? (spec.Cumulative == true ? $"Cumulative {valueTitle.ToLowerInvariant()}" : valueTitle)));

        return new BuiltChart(ChartBuilder.Combine(traces, layout), traces.Count, values.Count(v => v is not null), []);
    }
}
