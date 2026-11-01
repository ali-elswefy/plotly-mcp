namespace PlotlyTools.Charts;

/// <summary>How values that share a category are combined into one.</summary>
internal enum Aggregation
{
    Sum,
    Avg,
    Count,
    Min,
    Max,
}

/// <summary>
/// Combines values per category for charts that show one value per category (bar, pie). Nulls are skipped, and a
/// category whose values are all null stays null, as in SQL.
/// </summary>
internal static partial class Aggregator
{
    public sealed partial record Combined(IReadOnlyList<string> Keys, IReadOnlyList<double?> Values, bool HadDuplicates);

    public static Aggregation Parse(string? value) => ChartBuilder.Choose(
        value, "aggregate", Aggregation.Sum,
        ("sum", Aggregation.Sum), ("avg", Aggregation.Avg), ("count", Aggregation.Count), ("min", Aggregation.Min), ("max", Aggregation.Max));

    /// <summary>The values to combine: 1-or-null for count (any column can be counted), numbers otherwise.</summary>
    public static IReadOnlyList<double?> Inputs(ChartTable table, string column, string role, Aggregation aggregation) =>
        aggregation == Aggregation.Count ? table.Presence(column, role) : table.Numbers(column, role);

    /// <summary>One value per distinct key among <paramref name="rows"/>, keys in first-seen order.</summary>
    public static Combined Combine(IReadOnlyList<string> keys, IReadOnlyList<double?> values, IEnumerable<int> rows, Aggregation aggregation)
    {
        var groups = new Dictionary<string, List<double>>(StringComparer.Ordinal);
        var order = new List<string>();
        var duplicates = false;
        foreach (var row in rows)
        {
            if (!groups.TryGetValue(keys[row], out var group))
            {
                groups[keys[row]] = group = [];
                order.Add(keys[row]);
            }
            else
            {
                duplicates = true;
            }

            if (values[row] is { } value)
            {
                group.Add(value);
            }
        }

        var combined = order.Select(key => Apply(groups[key], aggregation)).ToList();
        return new Combined(order, combined, duplicates);
    }

    private static double? Apply(List<double> values, Aggregation aggregation) => aggregation switch
    {
        Aggregation.Count => values.Count,
        _ when values.Count == 0 => null,
        Aggregation.Sum => values.Sum(),
        Aggregation.Avg => values.Average(),
        Aggregation.Min => values.Min(),
        Aggregation.Max => values.Max(),
        _ => throw new ArgumentOutOfRangeException(nameof(aggregation)),
    };

    /// <summary>The value axis title for an aggregated column, e.g. "Average Sales".</summary>
    public static string Describe(Aggregation aggregation, string column) => aggregation switch
    {
        Aggregation.Avg => $"Average {column}",
        Aggregation.Count => $"Count of {column}",
        Aggregation.Min => $"Minimum {column}",
        Aggregation.Max => $"Maximum {column}",
        _ => column,
    };
}
