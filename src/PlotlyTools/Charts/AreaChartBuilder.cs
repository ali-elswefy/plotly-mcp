using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>Area charts: filled lines, stacked by default to show a total and its make-up.</summary>
internal static partial class AreaChartBuilder
{
    public static BuiltChart Build(AreaChartSpec spec, ChartTable table, int maxSeries)
    {
        var stacked = spec.Stacked ?? true;
        var normalize = spec.Normalize ?? false;
        if (normalize && !stacked)
        {
            throw new McpException("normalize stacks the areas to 100%, so it needs stacked: true.");
        }

        var series = ChartBuilder.OrderedSeries(table, spec.X, spec.Y, spec.Series, maxSeries);
        var traces = series.Select((s, i) =>
        {
            var trace = ChartBuilder.NewTrace("scatter", s.Name);
            trace.SetValue("x", s.X);
            trace.SetValue("y", s.Y);
            trace.SetValue("mode", "lines");
            if (stacked)
            {
                // plotly.js stacks every trace in one stackgroup; groupnorm on the first one normalizes the group.
                trace.SetValue("stackgroup", "one");
                if (normalize && i == 0)
                {
                    trace.SetValue("groupnorm", "percent");
                }
            }
            else
            {
                trace.SetValue("fill", "tozeroy");
            }

            return trace;
        }).ToList();

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("xaxis", ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.X, "x")]));
        layout.SetValue("yaxis", ChartBuilder.Axis(spec.YTitle ?? (normalize ? "Percent of total" : ChartBuilder.ValueTitle(table, spec.Y))));
        layout.SetValue("hovermode", "x unified");

        return new BuiltChart(ChartBuilder.Combine(traces, layout), traces.Count, series.Sum(s => s.X.Length), []);
    }
}
