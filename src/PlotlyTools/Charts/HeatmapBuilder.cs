using System.Globalization;
using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>Heatmaps: a value over two category axes, from long-format data (one row per cell).</summary>
internal static partial class HeatmapBuilder
{
    /// <summary>Beyond this the page gets slow and the cells too small to read.</summary>
    public const int MaxCells = 40_000;

    /// <summary>plotly.js's built-in colour scales; names are matched case-insensitively.</summary>
    public static readonly string[] ColorScales =
        ["Viridis", "Cividis", "Blues", "Greens", "Greys", "Reds", "YlGnBu", "YlOrRd", "RdBu", "Portland", "Picnic", "Hot", "Electric", "Earth", "Jet", "Rainbow", "Bluered", "Blackbody"];

    /// <summary>
    /// Single-hue scales plotly.js defines dark-to-light. Reversed, a higher value always gets the stronger colour,
    /// as it does with Reds.
    /// </summary>
    private static readonly HashSet<string> DarkLowScales = ["Blues", "Greens", "Greys", "YlGnBu", "YlOrRd"];

    public static BuiltChart Build(HeatmapSpec spec, ChartTable table)
    {
        var aggregation = Aggregator.Parse(spec.Aggregate);
        var colorScale = ChartBuilder.Choose(spec.ColorScale, "colorScale", "Viridis", [.. ColorScales.Select(s => (s, s))]);

        var xs = table.Labels(spec.X, "x");
        var ys = table.Labels(spec.Y, "y");
        var values = Aggregator.Inputs(table, spec.Z ?? "", "z", aggregation);

        var xKeys = xs.Distinct().ToList();
        var yKeys = ys.Distinct().ToList();
        if ((long)xKeys.Count * yKeys.Count > MaxCells)
        {
            throw new McpException(
                $"The heatmap would have {xKeys.Count} × {yKeys.Count} cells; at most {MaxCells:N0} can be drawn. " +
                "Group the categories into fewer values in SQL.");
        }

        // Each cell is keyed by its column and row positions, so any label text is safe in a key.
        var xIndex = xKeys.Select((key, i) => (key, i)).ToDictionary(p => p.key, p => p.i);
        var yIndex = yKeys.Select((key, i) => (key, i)).ToDictionary(p => p.key, p => p.i);
        var cellKeys = xs.Zip(ys, (x, y) => $"{xIndex[x]}:{yIndex[y]}").ToList();
        var cells = Aggregator.Combine(cellKeys, values, Enumerable.Range(0, table.RowCount), aggregation);

        // Rows are y, columns are x; cells with no data stay null and are drawn empty.
        var z = yKeys.Select(_ => new double?[xKeys.Count]).ToArray();
        for (var c = 0; c < cells.Keys.Count; c++)
        {
            var position = cells.Keys[c].Split(':');
            z[int.Parse(position[1], CultureInfo.InvariantCulture)][int.Parse(position[0], CultureInfo.InvariantCulture)] = cells.Values[c];
        }

        var trace = ChartBuilder.NewTrace("heatmap", null);
        trace.SetValue("x", xKeys);
        trace.SetValue("y", yKeys);
        trace.SetValue("z", z);
        trace.SetValue("colorscale", colorScale);
        if (DarkLowScales.Contains(colorScale))
        {
            trace.SetValue("reversescale", true);
        }
        trace.SetValue("hoverongaps", false);
        if (spec.ShowValues == true)
        {
            trace.SetValue("texttemplate", "%{z}");
        }

        var zName = table.Columns[table.IndexOf(spec.Z, "z")];
        trace.SetValue("colorbar", new Dictionary<string, object> { ["title"] = new Dictionary<string, object> { ["text"] = Aggregator.Describe(aggregation, zName) } });

        var layout = ChartBuilder.NewLayout(spec.Title);
        layout.SetValue("xaxis", ChartBuilder.Axis(spec.XTitle ?? table.Columns[table.IndexOf(spec.X, "x")], ("type", "category")));

        // First category at the top, so the grid reads like a table.
        layout.SetValue("yaxis", ChartBuilder.Axis(spec.YTitle ?? table.Columns[table.IndexOf(spec.Y, "y")], ("type", "category"), ("autorange", "reversed")));

        var notes = cells.HadDuplicates
            ? new[] { $"Rows with the same {table.Columns[table.IndexOf(spec.X, "x")]} and {table.Columns[table.IndexOf(spec.Y, "y")]} were combined ({aggregation.ToString().ToLowerInvariant()})." }
            : [];
        return new BuiltChart(ChartBuilder.Combine([trace], layout), 1, cells.Values.Count(v => v is not null), notes);
    }
}
