using ModelContextProtocol;

namespace PlotlyTools.Charts;

// Splitting rows by the value of a series column.
internal sealed partial class ChartTable
{
    /// <summary>The rows with one value of the series column; <see cref="Name"/> is null when there's no series column.</summary>
    public sealed partial record RowGroup(string? Name, IReadOnlyList<int> Rows);

    /// <summary>
    /// Rows grouped by <paramref name="column"/>, in the order each value first appears. Without a column, one group
    /// holds every row.
    /// </summary>
    public IReadOnlyList<RowGroup> Groups(string? column, int maxSeries)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return [new RowGroup(null, Enumerable.Range(0, RowCount).ToList())];
        }

        var labels = Labels(column, "series");
        var groups = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var order = new List<string>();
        for (var r = 0; r < labels.Count; r++)
        {
            if (!groups.TryGetValue(labels[r], out var rows))
            {
                groups[labels[r]] = rows = [];
                order.Add(labels[r]);
            }

            rows.Add(r);
        }

        return order.Count > maxSeries
            ? throw new McpException(
                $"Column '{Columns[IndexOf(column, "series")]}' (series) has {order.Count} distinct values; at most {maxSeries} series " +
                "can be drawn. Filter the data, or group small values together in SQL.")
            : order.Select(name => new RowGroup(name, groups[name])).ToList();
    }
}
