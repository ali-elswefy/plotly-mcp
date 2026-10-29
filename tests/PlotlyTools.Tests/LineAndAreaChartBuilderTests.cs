using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Line and area charts: rows sorted by x, series, line shapes and stacking.</summary>
public partial class LineAndAreaChartBuilderTests
{
    // Rows deliberately out of order; dates as ISO text, as SQL results return them.
    private const string Monthly = """
        {"columns":["Month","Product","Sales"],
         "rows":[["2025-03-01","A",30],["2025-01-01","A",10],["2025-02-01","A",20],["2025-01-01","B",5],["2025-02-01","B",null]]}
        """;

    private static LineChartSpec Line(string? series = "Product") => new("Month", ["Sales"], series, null, null, null, null, null);

    private static AreaChartSpec Area(bool? stacked = null, bool? normalize = null) => new("Month", ["Sales"], "Product", stacked, normalize, null, null, null);

    [Fact]
    public void Line_sorts_each_series_by_x()
    {
        var built = LineChartBuilder.Build(Line(), TestData.Table(Monthly), 10);

        var a = TestData.Trace(built, 0);
        Assert.Equal("A", a.GetProperty("name").GetString());
        Assert.Equal(["2025-01-01", "2025-02-01", "2025-03-01"], TestData.Texts(a.GetProperty("x")));
        Assert.Equal([10.0, 20.0, 30.0], TestData.Numbers(a.GetProperty("y")));
        Assert.Equal([5.0, null], TestData.Numbers(TestData.Trace(built, 1).GetProperty("y")));
        Assert.Equal(5, built.Points);
    }

    [Fact]
    public void Line_sorts_numbers_numerically()
    {
        var table = TestData.Table("""{"columns":["x","y"],"rows":[[10,1],[9,2],[100,3]]}""");

        var built = LineChartBuilder.Build(new LineChartSpec("x", ["y"], null, null, null, null, null, null), table, 10);

        Assert.Equal([9.0, 10.0, 100.0], TestData.Numbers(TestData.Trace(built).GetProperty("x")));
    }

    [Theory]
    [InlineData(null, false, "linear", "lines")]
    [InlineData("step", true, "hv", "lines+markers")]
    [InlineData("SPLINE", false, "spline", "lines")]
    public void Line_shape_and_markers(string? shape, bool markers, string expectedShape, string expectedMode)
    {
        var built = LineChartBuilder.Build(Line() with { Shape = shape, Markers = markers }, TestData.Table(Monthly), 10);

        var trace = TestData.Trace(built);
        Assert.Equal(expectedShape, trace.GetProperty("line").GetProperty("shape").GetString());
        Assert.Equal(expectedMode, trace.GetProperty("mode").GetString());
    }

    [Fact]
    public void Line_titles_axes_with_column_names()
    {
        var layout = TestData.Layout(LineChartBuilder.Build(Line(series: null), TestData.Table(Monthly), 10));

        Assert.Equal("Month", layout.GetProperty("xaxis").GetProperty("title").GetProperty("text").GetString());
        Assert.Equal("Sales", layout.GetProperty("yaxis").GetProperty("title").GetProperty("text").GetString());
    }

    [Fact]
    public void Area_stacks_by_default()
    {
        var built = AreaChartBuilder.Build(Area(), TestData.Table(Monthly), 10);

        Assert.Equal("one", TestData.Trace(built, 0).GetProperty("stackgroup").GetString());
        Assert.Equal("one", TestData.Trace(built, 1).GetProperty("stackgroup").GetString());
        Assert.False(TestData.Trace(built, 0).TryGetProperty("groupnorm", out _));
    }

    [Fact]
    public void Area_normalize_sets_groupnorm_on_the_first_trace_only()
    {
        var built = AreaChartBuilder.Build(Area(normalize: true), TestData.Table(Monthly), 10);

        Assert.Equal("percent", TestData.Trace(built, 0).GetProperty("groupnorm").GetString());
        Assert.False(TestData.Trace(built, 1).TryGetProperty("groupnorm", out _));
        Assert.Equal("Percent of total", TestData.Layout(built).GetProperty("yaxis").GetProperty("title").GetProperty("text").GetString());
    }

    [Fact]
    public void Area_unstacked_fills_to_zero()
    {
        var trace = TestData.Trace(AreaChartBuilder.Build(Area(stacked: false), TestData.Table(Monthly), 10));

        Assert.Equal("tozeroy", trace.GetProperty("fill").GetString());
        Assert.False(trace.TryGetProperty("stackgroup", out _));
    }

    [Fact]
    public void Area_normalize_needs_stacking()
    {
        var ex = Assert.Throws<McpException>(() => AreaChartBuilder.Build(Area(stacked: false, normalize: true), TestData.Table(Monthly), 10));
        Assert.Contains("needs stacked: true", ex.Message);
    }
}
