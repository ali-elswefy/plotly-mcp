using Plotly.NET;

namespace PlotlyTools.Charts;

/// <summary>
/// Helpers the chart builders share. Traces and layouts are Plotly.NET objects filled with plotly.js attribute
/// names: the data comes in mixed JSON types, which Plotly.NET.CSharp's typed constructors can't take, and the
/// plotly.js names are the documented, stable ones.
/// </summary>
internal static partial class ChartBuilder
{
    public static Trace2D NewTrace(string type, string? name)
    {
        var trace = new Trace2D(type);
        if (name is not null)
        {
            trace.SetValue("name", name);
        }

        return trace;
    }

    /// <summary>A layout with the chart title; builders add their own settings.</summary>
    public static Layout NewLayout(string? title)
    {
        var layout = new Layout();
        if (!string.IsNullOrWhiteSpace(title))
        {
            layout.SetValue("title", new Dictionary<string, object> { ["text"] = title });
        }

        return layout;
    }

    /// <summary>An axis with an optional title plus any other plotly.js axis settings.</summary>
    public static Dictionary<string, object> Axis(string? title, params (string Name, object Value)[] settings)
    {
        var axis = settings.ToDictionary(s => s.Name, s => s.Value);
        if (!string.IsNullOrWhiteSpace(title))
        {
            axis["title"] = new Dictionary<string, object> { ["text"] = title };
        }

        return axis;
    }

    // Without Plotly.NET's defaults: ChartStyle supplies the template, and there's no fixed size to remove.
    public static GenericChart Combine(IEnumerable<Trace> traces, Layout layout) =>
        GenericChart.addLayout(layout, GenericChart.ofTraceObjects(false, traces));

    public static T[] Pick<T>(IReadOnlyList<T> values, IReadOnlyList<int> rows)
    {
        var picked = new T[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            picked[i] = values[rows[i]];
        }

        return picked;
    }
}
