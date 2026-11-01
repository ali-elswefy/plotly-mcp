namespace PlotlyTools.Charts;

/// <summary>Line charts: values over an ordered x axis, usually time, one line per y column or series value.</summary>
internal static partial class LineChartBuilder
{
    public static BuiltChart Build(LineChartSpec spec, ChartTable table, int maxSeries)
    {
        var shape = ChartBuilder.Choose(spec.Shape, "shape", "linear", ("linear", "linear"), ("spline", "spline"), ("step", "hv"));

        var series = ChartBuilder.OrderedSeries(table, spec.X, spec.Y, spec.Series, maxSeries);
        var traces = series.Select(s =>
        {
            var trace = ChartBuilder.NewTrace("scatter", s.Name);
            trace.SetValue("x", s.X);
            trace.SetValue("y", s.Y);
            trace.SetValue("mode", spec.Markers == true ? "lines+markers" : "lines");
            trace.SetValue("line", new Dictionary<string, object> { ["shape"] = shape });
            return trace;
        }).ToList();

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("xaxis", ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.X, "x")]));
        layout.SetValue("yaxis", ChartBuilder.Axis(spec.YTitle ?? ChartBuilder.ValueTitle(table, spec.Y)));
        layout.SetValue("hovermode", "x unified");

        return new BuiltChart(ChartBuilder.Combine(traces, layout), traces.Count, series.Sum(s => s.X.Length), []);
    }
}
