using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Box/violin plots and heatmaps.</summary>
public partial class BoxAndHeatmapBuilderTests
{
    private const string Deliveries = """
        {"columns":["Carrier","Region","Days"],
         "rows":[["Fast","North",2],["Fast","South",3],["Slow","North",7],["Slow","South",9],["Fast","North",1],["Slow","North",null]]}
        """;

    private static BoxPlotSpec Box() => new("Days", "Carrier", null, null, null, null, null, null);

    private static HeatmapSpec Heat() => new("Region", "Carrier", "Days", null, null, null, null, null, null);

    [Fact]
    public void Box_draws_one_box_per_group_with_outliers()
    {
        var built = BoxPlotBuilder.Build(Box(), TestData.Table(Deliveries), 10);

        var trace = TestData.Trace(built);
        Assert.Equal("box", trace.GetProperty("type").GetString());
        Assert.Equal(["Fast", "Fast", "Slow", "Slow", "Fast", "Slow"], TestData.Texts(trace.GetProperty("x")));
        Assert.Equal("outliers", trace.GetProperty("boxpoints").GetString());
        Assert.Equal("category", TestData.Layout(built).GetProperty("xaxis").GetProperty("type").GetString());
        Assert.Equal(5, built.Points);
    }

    [Fact]
    public void Box_series_are_grouped_side_by_side()
    {
        var built = BoxPlotBuilder.Build(Box() with { Series = "Region" }, TestData.Table(Deliveries), 10);

        Assert.Equal(2, built.Traces);
        Assert.Equal("group", TestData.Layout(built).GetProperty("boxmode").GetString());
    }

    [Fact]
    public void Violin_with_all_points_and_no_groups()
    {
        var built = BoxPlotBuilder.Build(Box() with { X = null, Style = "violin", Points = "none" }, TestData.Table(Deliveries), 10);

        var trace = TestData.Trace(built);
        Assert.Equal("violin", trace.GetProperty("type").GetString());
        Assert.False(trace.GetProperty("points").GetBoolean());
        Assert.True(trace.GetProperty("box").GetProperty("visible").GetBoolean());
        Assert.False(trace.TryGetProperty("x", out _));
        Assert.Equal("Days", trace.GetProperty("name").GetString());
    }

    [Fact]
    public void Heatmap_builds_a_grid_with_empty_cells_and_combines_duplicates()
    {
        var built = HeatmapBuilder.Build(Heat(), TestData.Table(Deliveries));

        var trace = TestData.Trace(built);
        Assert.Equal(["North", "South"], TestData.Texts(trace.GetProperty("x")));
        Assert.Equal(["Fast", "Slow"], TestData.Texts(trace.GetProperty("y")));
        var z = trace.GetProperty("z");
        Assert.Equal([3.0, 3.0], TestData.Numbers(z[0]));
        Assert.Equal([7.0, 9.0], TestData.Numbers(z[1]));
        Assert.Equal(4, built.Points);
        Assert.Equal(["Rows with the same Region and Carrier were combined (sum)."], built.Notes);
        Assert.False(trace.TryGetProperty("reversescale", out _));
        Assert.Equal("reversed", TestData.Layout(built).GetProperty("yaxis").GetProperty("autorange").GetString());
    }

    [Fact]
    public void Heatmap_leaves_missing_cells_null()
    {
        var table = TestData.Table("""{"columns":["x","y","v"],"rows":[["a","p",1],["b","q",2]]}""");

        var z = TestData.Trace(HeatmapBuilder.Build(new HeatmapSpec("x", "y", "v", null, null, null, null, null, null), table)).GetProperty("z");

        Assert.Equal([1.0, null], TestData.Numbers(z[0]));
        Assert.Equal([null, 2.0], TestData.Numbers(z[1]));
    }

    [Fact]
    public void Heatmap_colour_scale_is_matched_case_insensitively()
    {
        var trace = TestData.Trace(HeatmapBuilder.Build(Heat() with { ColorScale = "blues", ShowValues = true, Aggregate = "avg" }, TestData.Table(Deliveries)));

        Assert.Equal("Blues", trace.GetProperty("colorscale").GetString());
        Assert.True(trace.GetProperty("reversescale").GetBoolean());
        Assert.Equal("%{z}", trace.GetProperty("texttemplate").GetString());
        Assert.Equal("Average Days", trace.GetProperty("colorbar").GetProperty("title").GetProperty("text").GetString());
    }

    [Fact]
    public void Heatmap_refuses_unknown_colour_scales_and_huge_grids()
    {
        Assert.StartsWith("Unknown colorScale 'Rainbows'.", Assert.Throws<McpException>(() => HeatmapBuilder.Build(Heat() with { ColorScale = "Rainbows" }, TestData.Table(Deliveries))).Message);

        var rows = string.Join(",", Enumerable.Range(0, 201).Select(i => $"[\"x{i}\",\"y{i}\",1]"));
        var table = TestData.Table($$"""{"columns":["x","y","v"],"rows":[{{rows}}]}""", maxRows: 1000);
        var ex = Assert.Throws<McpException>(() => HeatmapBuilder.Build(new HeatmapSpec("x", "y", "v", null, null, null, null, null, null), table));
        Assert.StartsWith("The heatmap would have 201 × 201 cells", ex.Message);
    }
}
