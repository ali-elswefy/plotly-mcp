using ModelContextProtocol;

namespace PlotlyTools.Charts;

// Parsing the named options agents pass as text (barMode, orientation, shape…) and checking numeric ranges.
internal static partial class ChartBuilder
{
    /// <summary>The value for <paramref name="value"/> among <paramref name="choices"/>, case-insensitively; empty means <paramref name="fallback"/>.</summary>
    public static T Choose<T>(string? value, string parameter, T fallback, params (string Name, T Value)[] choices)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        foreach (var (name, choice) in choices)
        {
            if (name.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return choice;
            }
        }

        throw new McpException($"Unknown {parameter} '{value}'. Use {OrList(choices.Select(c => c.Name))}.");
    }

    public static int InRange(int? value, string parameter, int fallback, int min, int max) => value switch
    {
        null => fallback,
        _ when value >= min && value <= max => value.Value,
        _ => throw new McpException($"{parameter} must be between {min} and {max}."),
    };

    public static double InRange(double? value, string parameter, double fallback, double min, double max) => value switch
    {
        null => fallback,
        _ when value >= min && value <= max => value.Value,
        _ => throw new McpException($"{parameter} must be between {min} and {max}."),
    };

    /// <summary>"'a', 'b' or 'c'".</summary>
    public static string OrList(IEnumerable<string> names)
    {
        var quoted = names.Select(n => $"'{n}'").ToArray();
        return quoted.Length == 1 ? quoted[0] : $"{string.Join(", ", quoted[..^1])} or {quoted[^1]}";
    }
}
