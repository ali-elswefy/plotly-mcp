using ModelContextProtocol;

namespace PlotlyTools.Charts;

/// <summary>
/// Treemaps and sunbursts: the same hierarchy (path columns, outer level first) drawn as nested rectangles or rings.
/// Leaves get the aggregated values; every parent is the total of its children.
/// </summary>
internal static partial class HierarchyBuilder
{
    public const int MaxLevels = 6;

    private sealed partial record Node(string Id, string Label, string ParentId, int Depth);

    /// <param name="traceType">"treemap" or "sunburst".</param>
    public static BuiltChart Build(HierarchySpec spec, ChartTable table, string traceType)
    {
        var aggregation = Aggregator.Parse(spec.Aggregate);
        var path = (spec.Path ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (path.Count == 0)
        {
            throw new McpException($"Name the hierarchy in path, outer level first, e.g. ['Region', 'Country']. Available columns: {string.Join(", ", table.Columns)}.");
        }

        if (path.Count > MaxLevels)
        {
            throw new McpException($"path has {path.Count} levels; at most {MaxLevels} can be drawn.");
        }

        var maxDepth = ChartBuilder.InRange(spec.MaxDepth, "maxDepth", 0, 1, path.Count);
        var levels = path.Select(column => table.Labels(column, "path")).ToList();
        var values = Aggregator.Inputs(table, spec.Values ?? "", "values", aggregation);

        // Nodes are identified by number, not by joined label text, so any label is safe and the same label can
        // appear under different parents.
        var nodes = new List<Node>();
        var nodeIds = new Dictionary<(string Parent, string Label), string>();
        var leafOfRow = new string[table.RowCount];
        for (var r = 0; r < table.RowCount; r++)
        {
            var parent = "";
            for (var depth = 0; depth < levels.Count; depth++)
            {
                var label = levels[depth][r];
                if (!nodeIds.TryGetValue((parent, label), out var id))
                {
                    id = $"n{nodes.Count}";
                    nodeIds[(parent, label)] = id;
                    nodes.Add(new Node(id, label, parent, depth));
                }

                parent = id;
            }

            leafOfRow[r] = parent;
        }

        var leaves = Aggregator.Combine(leafOfRow, values, Enumerable.Range(0, table.RowCount), aggregation);
        var negative = leaves.Keys.Zip(leaves.Values).FirstOrDefault(l => l.Second < 0);
        if (negative.First is not null)
        {
            var label = nodes.First(n => n.Id == negative.First).Label;
            throw new McpException($"'{label}' totals {negative.Second}; a {traceType} can't show negative values. Use a bar chart instead.");
        }

        // Empty leaves are left out; each kept leaf adds its value to itself and every ancestor.
        var byId = nodes.ToDictionary(n => n.Id);
        var totals = new Dictionary<string, double>();
        for (var i = 0; i < leaves.Keys.Count; i++)
        {
            if (leaves.Values[i] is not { } value)
            {
                continue;
            }

            for (var id = leaves.Keys[i]; id.Length > 0; id = byId[id].ParentId)
            {
                totals[id] = totals.GetValueOrDefault(id) + value;
            }
        }

        var kept = nodes.Where(n => totals.ContainsKey(n.Id)).ToList();
        var trace = ChartBuilder.NewTrace(traceType, null);
        trace.SetValue("ids", kept.Select(n => n.Id).ToArray());
        trace.SetValue("labels", kept.Select(n => n.Label).ToArray());
        trace.SetValue("parents", kept.Select(n => n.ParentId).ToArray());
        trace.SetValue("values", kept.Select(n => totals[n.Id]).ToArray());
        trace.SetValue("branchvalues", "total");
        trace.SetValue("textinfo", traceType == "treemap" ? "label+value" : "label+percent parent");
        if (maxDepth > 0)
        {
            trace.SetValue("maxdepth", maxDepth);
        }

        var notes = new List<string>();
        if (leaves.HadDuplicates)
        {
            notes.Add($"Rows with the same path were combined ({aggregation.ToString().ToLowerInvariant()}).");
        }

        if (aggregation is Aggregation.Avg or Aggregation.Min or Aggregation.Max && path.Count > 1)
        {
            notes.Add("Parents show the total of their children, not their own " + aggregation.ToString().ToLowerInvariant() + ".");
        }

        var leafCount = kept.Count(n => n.Depth == levels.Count - 1);
        return new BuiltChart(ChartBuilder.Combine([trace], ChartBuilder.NewLayout(spec.Title)), 1, leafCount, notes);
    }
}
