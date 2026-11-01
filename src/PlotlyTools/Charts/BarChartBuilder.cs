using Plotly.NET;

namespace PlotlyTools.Charts;

/// <summary>Bar charts: one value per category, aggregated, as grouped, stacked or 100% bars.</summary>
internal static partial class BarChartBuilder
{
    public static BuiltChart Build(BarChartSpec spec, ChartTable table, int maxSeries)
    {
        var horizontal = ChartBuilder.Choose(spec.Orientation, "orientation", false, ("vertical", false), ("horizontal", true));
        var barMode = ChartBuilder.Choose(spec.BarMode, "barMode", "grouped", ("grouped", "grouped"), ("stacked", "stacked"), ("percent", "percent"));
        var aggregation = Aggregator.Parse(spec.Aggregate);
        var categoryOrder = ChartBuilder.Choose<string?>(spec.Sort, "sort", null, ("none", null), ("asc", "total ascending"), ("desc", "total descending"));

        var categories = table.Labels(spec.X, "x");
        var traces = new List<Trace>();
        var points = 0;
        var combined = false;
        foreach (var part in ChartBuilder.SplitSeries(table, spec.Y, spec.Series, maxSeries))
        {
            var values = Aggregator.Inputs(table, part.ValueColumn, "y", aggregation);
            var bars = Aggregator.Combine(categories, values, part.Rows, aggregation);
            combined |= bars.HadDuplicates;
            points += bars.Keys.Count;

            var trace = ChartBuilder.NewTrace("bar", part.Name);
            trace.SetValue(horizontal ? "y" : "x", bars.Keys);
            trace.SetValue(horizontal ? "x" : "y", bars.Values);
            if (horizontal)
            {
                trace.SetValue("orientation", "h");
            }

            if (spec.ShowValues == true)
            {
                trace.SetValue("texttemplate", horizontal ? "%{x}" : "%{y}");
                trace.SetValue("textposition", "auto");
            }

            traces.Add(trace);
        }

        var xName = table.Columns[table.IndexOf(spec.X, "x")];
        var valueTitle = barMode == "percent"
            ? "Percent of total"
            : ChartBuilder.ValueTitle(table, spec.Y) is { } yName ? Aggregator.Describe(aggregation, yName) : null;

        // Categories stay categories even when they look like numbers (years, codes).
        var categorySettings = new List<(string, object)> { ("type", "category") };
        if (categoryOrder is not null)
        {
            categorySettings.Add(("categoryorder", categoryOrder));
        }

        if (horizontal)
        {
            // plotly.js puts the first category at the bottom; reversed, it reads top-down like a table.
            categorySettings.Add(("autorange", "reversed"));
        }

        // xTitle and yTitle name the horizontal and vertical axes, whichever way the bars point.
        var categoryAxis = ChartBuilder.Axis((horizontal ? spec.YTitle : spec.XTitle) ?? xName, [.. categorySettings]);
        var valueAxis = ChartBuilder.Axis((horizontal ? spec.XTitle : spec.YTitle) ?? valueTitle);

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("barmode", barMode == "grouped" ? "group" : "stack");
        if (barMode == "percent")
        {
            layout.SetValue("barnorm", "percent");
        }

        layout.SetValue("xaxis", horizontal ? valueAxis : categoryAxis);
        layout.SetValue("yaxis", horizontal ? categoryAxis : valueAxis);

        var notes = combined
            ? new[] { $"Rows with the same {xName} were combined ({aggregation.ToString().ToLowerInvariant()})." }
            : [];
        return new BuiltChart(ChartBuilder.Combine(traces, layout), traces.Count, points, notes);
    }
}
