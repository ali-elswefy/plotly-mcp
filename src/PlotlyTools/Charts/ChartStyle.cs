using Microsoft.FSharp.Core;
using Plotly.NET;

namespace PlotlyTools.Charts;

/// <summary>
/// The one look every chart shares. Replaces Plotly.NET's default template (about 7 KB in every figure) with a small
/// one, and removes the fixed 600×600 size Plotly.NET writes into every layout.
/// </summary>
internal static partial class ChartStyle
{
    public static readonly string[] LightColorway =
        ["#2563eb", "#f97316", "#10b981", "#e11d48", "#8b5cf6", "#0891b2", "#ca8a04", "#db2777", "#4b5563", "#65a30d"];

    public static readonly string[] DarkColorway =
        ["#60a5fa", "#fb923c", "#34d399", "#fb7185", "#a78bfa", "#22d3ee", "#facc15", "#f472b6", "#9ca3af", "#a3e635"];

    private const string FontFamily = "Segoe UI, Roboto, Helvetica, Arial, sans-serif";

    /// <summary>Page and plot background for <paramref name="theme"/>; HTML pages use it too.</summary>
    public static string Background(ChartTheme theme) => theme == ChartTheme.Dark ? "#111827" : "#ffffff";

    public static GenericChart Apply(GenericChart chart, ChartTheme theme)
    {
        // Built per chart: templates are mutable objects, so sharing one between charts isn't safe.
        var styled = chart.WithTemplate(Template.init<Trace>(TemplateLayout(theme), null));

        // Without a fixed size, HTML pages size the chart to the page and images use the size asked for.
        return GenericChart.mapLayout(FSharpFunc<Layout, Layout>.FromConverter(RemoveFixedSize), styled);
    }

    private static Layout RemoveFixedSize(Layout layout)
    {
        layout.Remove("width");
        layout.Remove("height");
        return layout;
    }

    private static Layout TemplateLayout(ChartTheme theme)
    {
        var dark = theme == ChartTheme.Dark;
        var text = dark ? "#e5e7eb" : "#1f2937";
        var grid = dark ? "#374151" : "#e5e7eb";
        var line = dark ? "#4b5563" : "#d1d5db";

        var axis = new Dictionary<string, object>
        {
            ["gridcolor"] = grid,
            ["linecolor"] = line,
            ["zerolinecolor"] = line,
            ["automargin"] = true,
        };

        var layout = new Layout();
        layout.SetValue("font", new Dictionary<string, object> { ["family"] = FontFamily, ["size"] = 13, ["color"] = text });
        layout.SetValue("paper_bgcolor", Background(theme));
        layout.SetValue("plot_bgcolor", Background(theme));
        layout.SetValue("colorway", dark ? DarkColorway : LightColorway);
        layout.SetValue("title", new Dictionary<string, object> { ["x"] = 0.02, ["xanchor"] = "left" });
        layout.SetValue("margin", new Dictionary<string, object> { ["l"] = 60, ["r"] = 30, ["t"] = 60, ["b"] = 60 });
        layout.SetValue("hovermode", "closest");
        layout.SetValue("hoverlabel", new Dictionary<string, object> { ["align"] = "left" });
        layout.SetValue("xaxis", axis);
        layout.SetValue("yaxis", axis);
        return layout;
    }
}
