using System.Net;
using Plotly.NET;
using PlotlyTools.Charts;

namespace PlotlyTools.Rendering;

/// <summary>
/// Standalone HTML pages. Written here rather than with Plotly.NET's toEmbeddedHTML so the page is responsive,
/// pins one plotly.js version and keeps agent data out of the markup.
/// </summary>
internal static partial class HtmlPage
{
    /// <summary>The plotly.js version Plotly.NET 5.1.0 is built for (its Globals.PLOTLYJS_VERSION); a test keeps them in step.</summary>
    public const string PlotlyJsVersion = "2.27.1";

    public const string PlotlyJsUrl = $"https://cdn.plot.ly/plotly-{PlotlyJsVersion}.min.js";

    private const string ConfigJson = """{"responsive":true,"displaylogo":false}""";

    public static string Render(GenericChart chart, string? title, ChartTheme theme) => $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(title) ? "Chart" : title)}}</title>
        <script src="{{PlotlyJsUrl}}" charset="utf-8"></script>
        <style>
        html, body { margin: 0; height: 100%; background: {{ChartStyle.Background(theme)}}; }
        #chart { width: 100%; height: 100vh; min-height: 360px; }
        </style>
        </head>
        <body>
        <div id="chart"></div>
        <script>
        const figure = {{ScriptJson(GenericChart.toFigureJson(chart))}};
        Plotly.newPlot("chart", figure.data, figure.layout, {{ConfigJson}});
        </script>
        </body>
        </html>
        """;

    /// <summary>
    /// JSON made safe to place inside a script element. In valid JSON '&lt;' only occurs inside strings, where the
    /// < escape means the same thing, so agent text such as "&lt;/script&gt;" can't close the element.
    /// </summary>
    public static string ScriptJson(string json) => json.Replace("<", "\\u003c", StringComparison.Ordinal);
}
