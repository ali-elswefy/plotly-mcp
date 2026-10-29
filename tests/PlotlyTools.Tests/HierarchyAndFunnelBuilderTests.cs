using System.Text.Json;
using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Treemaps and sunbursts (one hierarchy builder) and funnels.</summary>
public partial class HierarchyAndFunnelBuilderTests
{
    private const string Sales = """
        {"columns":["Region","City","Sales"],
         "rows":[["North","Oslo",10],["North","Bergen",5],["South","Rome",20],["South","Oslo",1],["North","Oslo",4],["South","Milan",null]]}
        """;

    private static HierarchySpec Spec(int? maxDepth = null) => new(["Region", "City"], "Sales", null, maxDepth, null);

    /// <summary>The nodes as "parent label/label = value", so tests can read the tree.</summary>
    private static string[] Nodes(JsonElement trace)
    {
        var ids = TestData.Texts(trace.GetProperty("ids"));
        var labels = TestData.Texts(trace.GetProperty("labels"));
        var parents = TestData.Texts(trace.GetProperty("parents"));
        var values = TestData.Numbers(trace.GetProperty("values"));
        var labelOf = ids.Zip(labels).ToDictionary(p => p.First, p => p.Second);
        return labels.Select((label, i) => $"{(parents[i] == "" ? "" : labelOf[parents[i]] + "/")}{label} = {values[i]}").ToArray();
    }

    [Theory]
    [InlineData("treemap")]
    [InlineData("sunburst")]
    public void Builds_the_tree_with_parents_as_totals(string type)
    {
        var built = HierarchyBuilder.Build(Spec(), TestData.Table(Sales), type);

        var trace = TestData.Trace(built);
        Assert.Equal(type, trace.GetProperty("type").GetString());
        Assert.Equal("total", trace.GetProperty("branchvalues").GetString());

        // Oslo appears under both regions as separate nodes; Milan has no value and is left out.
        Assert.Equal(["North = 19", "North/Oslo = 14", "North/Bergen = 5", "South = 21", "South/Rome = 20", "South/Oslo = 1"], Nodes(trace));
        Assert.Equal(4, built.Points);
        Assert.Equal(["Rows with the same path were combined (sum)."], built.Notes);
    }

    [Fact]
    public void Max_depth_and_averages()
    {
        var built = HierarchyBuilder.Build(Spec(maxDepth: 1) with { Aggregate = "avg" }, TestData.Table(Sales), "treemap");

        Assert.Equal(1, TestData.Trace(built).GetProperty("maxdepth").GetInt32());
        Assert.Contains("Parents show the total of their children, not their own avg.", built.Notes);
    }

    [Fact]
    public void Refuses_negative_values_and_bad_paths()
    {
        var table = TestData.Table("""{"columns":["a","v"],"rows":[["x",-2]]}""");
        Assert.Equal(
            "'x' totals -2; a sunburst can't show negative values. Use a bar chart instead.",
            Assert.Throws<McpException>(() => HierarchyBuilder.Build(new HierarchySpec(["a"], "v", null, null, null), table, "sunburst")).Message);

        Assert.StartsWith("Name the hierarchy in path", Assert.Throws<McpException>(() => HierarchyBuilder.Build(Spec() with { Path = [] }, TestData.Table(Sales), "treemap")).Message);
        Assert.Equal("maxDepth must be between 1 and 2.", Assert.Throws<McpException>(() => HierarchyBuilder.Build(Spec(maxDepth: 3), TestData.Table(Sales), "treemap")).Message);
    }

    [Fact]
    public void Funnel_keeps_stage_order_and_shows_share_of_the_first()
    {
        var table = TestData.Table("""
            {"columns":["Stage","Channel","Count"],
             "rows":[["Visits","Web",1000],["Visits","Shop",400],["Offers","Web",300],["Offers","Shop",150],["Orders","Web",90],["Orders","Shop",60]]}
            """);

        var built = FunnelChartBuilder.Build(new FunnelChartSpec("Stage", "Count", null, null, null), table, 10);

        var trace = TestData.Trace(built);
        Assert.Equal("funnel", trace.GetProperty("type").GetString());
        Assert.Equal(["Visits", "Offers", "Orders"], TestData.Texts(trace.GetProperty("y")));
        Assert.Equal([1400.0, 450.0, 150.0], TestData.Numbers(trace.GetProperty("x")));
        Assert.Equal("value+percent initial", trace.GetProperty("textinfo").GetString());

        var bySeries = FunnelChartBuilder.Build(new FunnelChartSpec("Stage", "Count", "Channel", null, null), table, 10);
        Assert.Equal(2, bySeries.Traces);
        Assert.Equal([1000.0, 300.0, 90.0], TestData.Numbers(TestData.Trace(bySeries, 0).GetProperty("x")));
        Assert.Empty(bySeries.Notes);
    }
}
