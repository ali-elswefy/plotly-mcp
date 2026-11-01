using ModelContextProtocol;

namespace PlotlyTools.Charts;

// Splitting the data into traces: one per y column, or one per value of the series column.
internal static partial class ChartBuilder
{
    /// <summary>One trace to draw: its legend name, the value column it plots, and the rows it covers.</summary>
    public sealed partial record SeriesPart(string Name, string ValueColumn, IReadOnlyList<int> Rows);

    /// <summary>
    /// The traces for value columns <paramref name="y"/>: one per column, or, with <paramref name="series"/>, one per
    /// series value over the single y column. Both at once would need a trace per pair, which reads badly, so it's refused.
    /// </summary>
    public static IReadOnlyList<SeriesPart> SplitSeries(ChartTable table, IReadOnlyList<string>? y, string? series, int maxSeries)
    {
        var columns = (y ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (columns.Count == 0)
        {
            throw new McpException($"Name at least one column for y. Available columns: {string.Join(", ", table.Columns)}.");
        }

        if (!string.IsNullOrWhiteSpace(series))
        {
            if (columns.Count > 1)
            {
                throw new McpException("Use either several y columns or a series column, not both.");
            }

            return SplitSeries(table, columns[0], series, maxSeries);
        }

        if (columns.Count > maxSeries)
        {
            throw new McpException($"{columns.Count} y columns were given; at most {maxSeries} series can be drawn.");
        }

        var allRows = Enumerable.Range(0, table.RowCount).ToList();
        return columns.Select(c => new SeriesPart(table.Columns[table.IndexOf(c, "y")], c, allRows)).ToList();
    }

    /// <summary>The traces for one value column: a single trace, or one per value of <paramref name="series"/>.</summary>
    public static IReadOnlyList<SeriesPart> SplitSeries(ChartTable table, string valueColumn, string? series, int maxSeries)
    {
        var valueName = table.Columns[table.IndexOf(valueColumn, "y")];
        return table.Groups(series, maxSeries)
            .Select(g => new SeriesPart(g.Name ?? valueName, valueColumn, g.Rows))
            .ToList();
    }

    /// <summary>
    /// Line-style series: each trace's rows sorted by x, so lines are drawn left to right whatever order the rows came in.
    /// </summary>
    public static IReadOnlyList<(string Name, object?[] X, double?[] Y)> OrderedSeries(
        ChartTable table, string? x, IReadOnlyList<string>? y, string? series, int maxSeries)
    {
        var xs = table.Values(x, "x");
        return SplitSeries(table, y, series, maxSeries)
            .Select(part =>
            {
                var ys = table.Numbers(part.ValueColumn, "y");
                var rows = part.Rows.OrderBy(r => xs[r], AxisOrder.Instance).ToList();
                return (part.Name, Pick(xs, rows), Pick(ys, rows));
            })
            .ToList();
    }

    /// <summary>The default title for a value axis: the column's name when there's one value column, else none (the legend names them).</summary>
    public static string? ValueTitle(ChartTable table, IReadOnlyList<string>? y) =>
        y is { Count: 1 } ? table.Columns[table.IndexOf(y[0], "y")] : null;
}
