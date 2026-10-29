using System.Text.Json;
using ModelContextProtocol.Protocol;
using PlotlyTools.Charts;
using PlotlyTools.Dashboards;
using PlotlyTools.Rendering;

namespace PlotlyTools.Tools;

/// <summary>Builds a chart or dashboard tool's result: the JSON summary, then the HTML page or the image.</summary>
internal static partial class ChartToolResult
{
    private static readonly JsonSerializerOptions SummaryJson = new(JsonSerializerDefaults.Web);

    /// <param name="table">The data the chart was drawn from; null for charts drawn from values passed directly.</param>
    public static CallToolResult Create(RenderedChart chart, string chartType, string? title, BuiltChart built, ChartTable? table)
    {
        var notes = built.Notes.ToList();
        var truncated = table?.Truncated ?? false;
        if (table is { Truncated: true })
        {
            notes.Insert(0, $"Only the first {table.RowCount} of {table.TotalRows} rows were plotted. Aggregate or filter in SQL to plot everything.");
        }

        var summary = new ChartSummary(
            chartType,
            chart.Output.FileExtension,
            string.IsNullOrWhiteSpace(title) ? null : title,
            built.Traces,
            built.Points,
            truncated,
            chart.FilePath,
            notes.Count == 0 ? null : string.Join(" ", notes));

        return Result(JsonSerializer.Serialize(summary, SummaryJson), chart);
    }

    public static CallToolResult CreateDashboard(RenderedChart page, string title, ParsedDashboard dashboard)
    {
        var summary = new DashboardSummary(
            title,
            page.Output.FileExtension,
            dashboard.Cards.Count,
            dashboard.Charts,
            dashboard.Points,
            dashboard.Truncated,
            page.FilePath,
            dashboard.Notes.Count == 0 ? null : string.Join(" ", dashboard.Notes));

        return Result(JsonSerializer.Serialize(summary, SummaryJson), page);
    }

    private static CallToolResult Result(string summaryJson, RenderedChart output) => new()
    {
        Content = [new TextContentBlock { Text = summaryJson }, output.ToContentBlock()],
    };
}
