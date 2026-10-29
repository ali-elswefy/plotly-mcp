using System.Net;
using System.Text;
using PlotlyTools.Charts;
using PlotlyTools.Rendering;

namespace PlotlyTools.Dashboards;

/// <summary>
/// The dashboard page: a title, then a responsive grid of cards, each chart drawn by plotly.js from its figure.
/// The same page is the HTML output and, screenshotted, the PNG output, so both show the same thing.
/// </summary>
internal static partial class DashboardPage
{
    /// <summary>Height of one grid row; a chart card is this tall per row it spans.</summary>
    public const int RowHeight = 380;

    /// <summary>Page width in pixels for PNG output when a call doesn't pass one.</summary>
    public const int DefaultWidth = 1400;

    private const int Gap = 16;

    /// <param name="figures">The styled figure JSON of each chart card, in card order.</param>
    /// <param name="loadPlotlyJs">
    /// False when the caller injects plotly.js itself (screenshots, which work offline); the page then waits for a
    /// renderDashboard() call instead of drawing on load.
    /// </param>
    public static string Render(
        string title, string? description, int columns, IReadOnlyList<DashboardCard> cards, IReadOnlyList<string> figures, ChartTheme theme, bool loadPlotlyJs)
    {
        var dark = theme == ChartTheme.Dark;
        var charts = new StringBuilder();
        var body = new StringBuilder();
        var chartIndex = 0;
        foreach (var card in cards)
        {
            switch (card.Kind)
            {
                case DashboardCardKind.Heading:
                    body.AppendLine($"""<h2 class="heading">{WebUtility.HtmlEncode(card.Text)}</h2>""");
                    break;
                case DashboardCardKind.Text:
                    body.AppendLine($"""<section class="card text" style="grid-column: span {card.ColumnSpan}">{WebUtility.HtmlEncode(card.Text)}</section>""");
                    break;
                default:
                    var height = card.RowSpan * RowHeight + (card.RowSpan - 1) * Gap;
                    body.AppendLine($"""<section class="card" style="grid-column: span {card.ColumnSpan}; height: {height}px"><div class="chart" id="chart-{chartIndex}"></div></section>""");
                    charts.Append(charts.Length == 0 ? "" : ",").Append($$"""{"id":"chart-{{chartIndex}}","figure":{{figures[chartIndex]}}}""");
                    chartIndex++;
                    break;
            }
        }

        var heading = WebUtility.HtmlEncode(title);
        var intro = string.IsNullOrWhiteSpace(description) ? "" : $"""<p class="description">{WebUtility.HtmlEncode(description)}</p>""";
        var script = loadPlotlyJs ? $"""<script src="{HtmlPage.PlotlyJsUrl}" charset="utf-8"></script>""" : "";

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{heading}}</title>
            {{script}}
            <style>
            :root { --page: {{(dark ? "#030712" : "#f3f4f6")}}; --card: {{ChartStyle.Background(theme)}}; --border: {{(dark ? "#1f2937" : "#e5e7eb")}}; --text: {{(dark ? "#e5e7eb" : "#1f2937")}}; --muted: {{(dark ? "#9ca3af" : "#6b7280")}}; }
            * { box-sizing: border-box; }
            body { margin: 0; background: var(--page); color: var(--text); font-family: Segoe UI, Roboto, Helvetica, Arial, sans-serif; }
            header { padding: 24px 24px 0; }
            h1 { margin: 0; font-size: 24px; font-weight: 600; }
            .description { margin: 6px 0 0; color: var(--muted); }
            .grid { display: grid; grid-template-columns: repeat({{columns}}, minmax(0, 1fr)); gap: {{Gap}}px; padding: {{Gap}}px 24px 24px; }
            .card { background: var(--card); border: 1px solid var(--border); border-radius: 10px; overflow: hidden; min-width: 0; }
            .chart { width: 100%; height: 100%; }
            .text { padding: 16px 20px; white-space: pre-wrap; line-height: 1.5; align-self: start; }
            .heading { grid-column: 1 / -1; margin: 12px 0 0; font-size: 18px; font-weight: 600; }
            @media (max-width: 800px) { .grid { grid-template-columns: 1fr; padding: 12px; } .card { grid-column: 1 / -1 !important; } }
            </style>
            </head>
            <body>
            <header><h1>{{heading}}</h1>{{intro}}</header>
            <main class="grid">
            {{body}}</main>
            <script>
            const charts = {{HtmlPage.ScriptJson($"[{charts}]")}};
            const config = {"responsive":true,"displaylogo":false};
            function renderDashboard() {
              return Promise.all(charts.map(c => Plotly.newPlot(c.id, c.figure.data, c.figure.layout, config)))
                .then(() => { window.dashboardReady = true; });
            }
            if (window.Plotly) { renderDashboard(); }
            </script>
            </body>
            </html>
            """;
    }
}
