using System.Text.Json;
using ModelContextProtocol;

namespace PlotlyTools.Charts;

// Parsing and validating the agent's data.
internal sealed partial class ChartTable
{
    public static ChartTable Parse(ChartDataInput? data, int maxRows)
    {
        if (data?.Columns is not { Count: > 0 } columnInput)
        {
            throw new McpException("data.columns is empty. Pass the column names, e.g. [\"Region\", \"Sales\"].");
        }

        var columns = columnInput.Select(ColumnName).ToList();
        var duplicate = columns.GroupBy(c => c, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new McpException($"Column '{duplicate.Key}' appears more than once in data.columns. Give every column a distinct name.");
        }

        if (data.Rows is not { Count: > 0 } rowInput)
        {
            throw new McpException("data.rows is empty, so there is nothing to plot.");
        }

        var rows = new List<JsonElement[]>(Math.Min(rowInput.Count, maxRows));
        foreach (var row in rowInput.Take(maxRows))
        {
            if (row is null || row.Count != columns.Count)
            {
                throw new McpException(
                    $"Row {rows.Count + 1} has {row?.Count ?? 0} values, but there are {columns.Count} columns. " +
                    "Each row needs one value per column, in column order.");
            }

            rows.Add([.. row]);
        }

        return new ChartTable(columns, rows, rowInput.Count);

        string ColumnName(JsonElement column, int index)
        {
            var name = column.ValueKind switch
            {
                JsonValueKind.String => column.GetString(),
                JsonValueKind.Object => NameProperty(column),
                _ => throw new McpException($"data.columns[{index}] must be a column name or an object with a 'name'."),
            };

            // Unaliased SQL expressions such as COUNT(*) come back without a name.
            return string.IsNullOrWhiteSpace(name)
                ? throw new McpException($"Column {index + 1} has no name. Name every column (in SQL, alias it: COUNT(*) AS Total).")
                : name;
        }
    }

    private static string? NameProperty(JsonElement column)
    {
        foreach (var property in column.EnumerateObject())
        {
            if (property.Name.Equals("name", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }
}
