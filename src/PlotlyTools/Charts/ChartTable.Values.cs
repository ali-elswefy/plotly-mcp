using System.Globalization;
using System.Text.Json;
using ModelContextProtocol;

namespace PlotlyTools.Charts;

// Reading one column as numbers, axis values or text labels.
internal sealed partial class ChartTable
{
    /// <summary>Label used for null values in categories and series.</summary>
    public const string BlankLabel = "(blank)";

    /// <summary>
    /// The column as numbers; nulls stay null (a gap in the chart). Numeric text is accepted because some drivers
    /// return decimals as strings.
    /// </summary>
    public IReadOnlyList<double?> Numbers(string? column, string role)
    {
        var index = IndexOf(column, role);
        var values = new double?[_rows.Count];
        for (var r = 0; r < _rows.Count; r++)
        {
            var cell = _rows[r][index];
            values[r] = cell.ValueKind switch
            {
                JsonValueKind.Number => cell.GetDouble(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String when double.TryParse(cell.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => throw new McpException($"Column '{Columns[index]}' ({role}) must hold numbers, but row {r + 1} has {Describe(cell)}."),
            };
        }

        return values;
    }

    /// <summary>
    /// The column as axis values: numbers stay numbers and text stays text, so plotly.js can tell numeric, date and
    /// category axes apart itself.
    /// </summary>
    public IReadOnlyList<object?> Values(string? column, string role)
    {
        var index = IndexOf(column, role);
        var values = new object?[_rows.Count];
        for (var r = 0; r < _rows.Count; r++)
        {
            var cell = _rows[r][index];
            values[r] = cell.ValueKind switch
            {
                JsonValueKind.Number => cell.GetDouble(),
                JsonValueKind.String => cell.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => throw SingleValueError(index, role, r),
            };
        }

        return values;
    }

    /// <summary>1 where the column has a value and null where it's empty, so counting works on any column, like SQL COUNT(column).</summary>
    public IReadOnlyList<double?> Presence(string? column, string role) =>
        Values(column, role).Select(v => v is null ? (double?)null : 1).ToList();

    /// <summary>The column as text, for categories, series names and hover labels.</summary>
    public IReadOnlyList<string> Labels(string? column, string role)
    {
        var index = IndexOf(column, role);
        var labels = new string[_rows.Count];
        for (var r = 0; r < _rows.Count; r++)
        {
            var cell = _rows[r][index];
            labels[r] = cell.ValueKind switch
            {
                JsonValueKind.String => cell.GetString() is { Length: > 0 } text ? text : BlankLabel,
                JsonValueKind.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null or JsonValueKind.Undefined => BlankLabel,
                _ => throw SingleValueError(index, role, r),
            };
        }

        return labels;
    }

    private McpException SingleValueError(int index, string role, int row) =>
        new($"Column '{Columns[index]}' ({role}) must hold single values, but row {row + 1} has a nested object or list.");

    private static string Describe(JsonElement cell) => cell.ValueKind switch
    {
        JsonValueKind.String => $"'{Shorten(cell.GetString() ?? "")}'",
        JsonValueKind.True or JsonValueKind.False => cell.GetRawText(),
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "a list",
        _ => cell.ValueKind.ToString().ToLowerInvariant(),
    };

    // Echoing a long value back would bloat the error; the start is enough to recognise it.
    private static string Shorten(string text) => text.Length <= 40 ? text : text[..40] + "…";
}
