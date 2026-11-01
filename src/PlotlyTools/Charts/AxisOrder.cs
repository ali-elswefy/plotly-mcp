namespace PlotlyTools.Charts;

/// <summary>
/// Orders axis values as plotly.js would place them: numbers by value, text by ordinal comparison (ISO dates sort
/// correctly that way), numbers before text, and nulls last.
/// </summary>
internal sealed partial class AxisOrder : IComparer<object?>
{
    public static readonly AxisOrder Instance = new();

    public int Compare(object? a, object? b) => (a, b) switch
    {
        (null, null) => 0,
        (null, _) => 1,
        (_, null) => -1,
        (double x, double y) => x.CompareTo(y),
        (double, _) => -1,
        (_, double) => 1,
        _ => string.CompareOrdinal(Convert.ToString(a, System.Globalization.CultureInfo.InvariantCulture), Convert.ToString(b, System.Globalization.CultureInfo.InvariantCulture)),
    };
}
