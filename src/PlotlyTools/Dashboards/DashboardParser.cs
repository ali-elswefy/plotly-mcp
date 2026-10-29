using System.Text.Json;
using ModelContextProtocol;
using PlotlyTools.Charts;
using PlotlyTools.Configuration;

namespace PlotlyTools.Dashboards;

/// <summary>
/// Turns the agent's dashboard items into cards. A chart item is the chart tool's own parameters plus "type" (and
/// "data"), read into the same spec and built by the same builder as the tool, so a chart looks the same either way.
/// Errors name the item they come from, e.g. "items[2] (bar): Column 'Sales' (y) was not found…".
/// </summary>
internal static partial class DashboardParser
{
    public const int MaxColumns = 4;
    public const int MaxRowSpan = 3;

    private static readonly JsonSerializerOptions ItemJson = new(JsonSerializerDefaults.Web);

    public static ParsedDashboard Parse(IReadOnlyList<JsonElement>? items, int columns, PlotlyToolsOptions options)
    {
        if (items is not { Count: > 0 })
        {
            throw new McpException("items is empty. Pass the dashboard's charts, e.g. [{\"type\": \"bar\", \"data\": {...}, \"x\": \"Region\", \"y\": [\"Sales\"]}].");
        }

        if (items.Count > options.MaxDashboardItems)
        {
            throw new McpException($"The dashboard has {items.Count} items; at most {options.MaxDashboardItems} are allowed. Split it into several dashboards.");
        }

        var cards = new List<DashboardCard>();
        var notes = new List<string>();
        var points = 0;
        var truncated = false;

        // MaxRows applies to the whole dashboard, so one call can't plot more data than one chart could.
        var rowsLeft = options.MaxRows;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var label = $"items[{i}]";
            try
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new McpException("must be an object: a chart ({\"type\": \"bar\", ...}), {\"heading\": \"...\"} or {\"text\": \"...\"}.");
                }

                if (String(item, "heading") is { } heading)
                {
                    cards.Add(DashboardCard.ForHeading(heading));
                    continue;
                }

                if (String(item, "text") is { } text)
                {
                    cards.Add(DashboardCard.ForText(text, Span(item, "columnSpan", columns)));
                    continue;
                }

                var type = String(item, "type")
                    ?? throw new McpException("needs a 'type' (a chart such as 'bar'), a 'heading' or a 'text'.");
                var chartType = FindChartType(type);
                label = $"items[{i}] ({chartType.Name})";
                CheckParameters(item, chartType);

                ChartTable? table = null;
                if (chartType.NeedsData)
                {
                    if (rowsLeft <= 0)
                    {
                        throw new McpException($"The dashboard's data already reaches the limit of {options.MaxRows} rows in total. Aggregate in SQL, or split the dashboard.");
                    }

                    table = ChartTable.Parse(Read<ChartDataInput>(item, "data"), rowsLeft);
                    rowsLeft -= table.RowCount;
                    if (table.Truncated)
                    {
                        truncated = true;
                        notes.Add($"{label}: only the first {table.RowCount} of {table.TotalRows} rows were plotted (the dashboard's limit is {options.MaxRows} rows in total).");
                    }
                }

                var spec = item.Deserialize(chartType.SpecType, ItemJson)!;
                var built = chartType.Build(spec, table, options.MaxSeries);
                points += built.Points;
                notes.AddRange(built.Notes.Select(n => $"{label}: {n}"));
                cards.Add(DashboardCard.ForChart(built, Span(item, "columnSpan", columns), Span(item, "rowSpan", MaxRowSpan)));
            }
            catch (McpException ex) when (ex.GetType() == typeof(McpException))
            {
                throw new McpException($"{label}: {ex.Message}");
            }
            catch (JsonException ex)
            {
                // System.Text.Json's messages name the property (Path: $.y), which is enough for the agent to fix it.
                throw new McpException($"{label}: {ex.Message.Split(". ")[0]}.");
            }
        }

        if (!cards.Any(c => c.Kind == DashboardCardKind.Chart))
        {
            throw new McpException("A dashboard needs at least one chart item (an item with a 'type').");
        }

        return new ParsedDashboard(cards, points, truncated, notes);
    }

    private static string? String(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static T? Read<T>(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) ? value.Deserialize<T>(ItemJson) : default;

    private static int Span(JsonElement item, string name, int max)
    {
        if (!item.TryGetProperty(name, out var value))
        {
            return 1;
        }

        return value.TryGetInt32(out var span) && span >= 1
            ? Math.Min(span, max)
            : throw new McpException($"{name} must be a whole number from 1 to {max}.");
    }
}
