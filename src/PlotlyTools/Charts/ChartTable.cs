using System.Text.Json;
using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>
/// A parsed <see cref="ChartDataInput"/>: named columns and rows, capped at MaxRows. Builders read columns by the
/// role they play (x, y, series…) through it, so every tool words unknown-column and wrong-type errors the same way.
/// </summary>
internal sealed partial class ChartTable
{
    private readonly IReadOnlyList<JsonElement[]> _rows;

    private ChartTable(IReadOnlyList<string> columns, IReadOnlyList<JsonElement[]> rows, int totalRows)
    {
        Columns = columns;
        _rows = rows;
        TotalRows = totalRows;
    }

    public IReadOnlyList<string> Columns { get; }

    /// <summary>Rows kept, at most MaxRows.</summary>
    public int RowCount => _rows.Count;

    /// <summary>Rows the agent sent, before the cap.</summary>
    public int TotalRows { get; }

    public bool Truncated => TotalRows > RowCount;

    /// <summary>
    /// The index of <paramref name="column"/>, which plays <paramref name="role"/> in the chart. An exact match wins;
    /// otherwise a single case-insensitive match is accepted, since agents often change the case of SQL names.
    /// </summary>
    public int IndexOf(string? column, string role)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            throw new McpException($"Name the column to use for {role}. Available columns: {string.Join(", ", Columns)}.");
        }

        var exact = IndexWhere(c => c.Equals(column, StringComparison.Ordinal));
        if (exact >= 0)
        {
            return exact;
        }

        var matches = Enumerable.Range(0, Columns.Count)
            .Where(i => Columns[i].Equals(column.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.Count == 1
            ? matches[0]
            : throw new McpException($"Column '{column}' ({role}) was not found. Available columns: {string.Join(", ", Columns)}.");
    }

    private int IndexWhere(Func<string, bool> predicate)
    {
        for (var i = 0; i < Columns.Count; i++)
        {
            if (predicate(Columns[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
