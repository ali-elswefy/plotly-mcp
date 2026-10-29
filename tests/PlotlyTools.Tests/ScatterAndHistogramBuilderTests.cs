using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Scatter/bubble charts and histograms.</summary>
public partial class ScatterAndHistogramBuilderTests
{
    private const string Products = """
        {"columns":["Name","Category","Price","Quantity","Revenue"],
         "rows":[["Pen","Office",1.5,200,300],["Desk","Furniture",250,4,1000],["Chair","Furniture",120,10,1200],["Paper","Office",5,80,400]]}
        """;

    private static ScatterChartSpec Scatter() => new("Price", "Quantity", null, null, null, null, null, null, null, null);

    private static HistogramSpec Histogram() => new("Price", null, null, null, null, null, null, null);

    [Fact]
    public void Scatter_draws_markers_for_every_row()
    {
        var built = ScatterChartBuilder.Build(Scatter(), TestData.Table(Products), 10);

        var trace = TestData.Trace(built);
        Assert.Equal("markers", trace.GetProperty("mode").GetString());
        Assert.Equal([1.5, 250.0, 120.0, 5.0], TestData.Numbers(trace.GetProperty("x")));
        Assert.Equal(4, built.Points);
    }

    [Fact]
    public void Scatter_splits_by_series()
    {
        var built = ScatterChartBuilder.Build(Scatter() with { Series = "Category" }, TestData.Table(Products), 10);

        Assert.Equal(2, built.Traces);
        Assert.Equal("Furniture", TestData.Trace(built, 1).GetProperty("name").GetString());
        Assert.Equal([4.0, 10.0], TestData.Numbers(TestData.Trace(built, 1).GetProperty("y")));
    }

    [Fact]
    public void Bubble_sizes_by_area_with_the_largest_at_40_pixels()
    {
        var marker = TestData.Trace(ScatterChartBuilder.Build(Scatter() with { Size = "Revenue" }, TestData.Table(Products), 10)).GetProperty("marker");

        Assert.Equal("area", marker.GetProperty("sizemode").GetString());
        Assert.Equal(2 * 1200.0 / (40 * 40), marker.GetProperty("sizeref").GetDouble(), precision: 6);
        Assert.Equal([300.0, 1000.0, 1200.0, 400.0], TestData.Numbers(marker.GetProperty("size")));
    }

    [Fact]
    public void Bubble_needs_a_positive_size()
    {
        var table = TestData.Table("""{"columns":["x","y","s"],"rows":[[1,2,0],[2,3,null]]}""");

        var ex = Assert.Throws<McpException>(() => ScatterChartBuilder.Build(new ScatterChartSpec("x", "y", null, "s", null, null, null, null, null, null), table, 10));
        Assert.Contains("(size) needs at least one value above zero", ex.Message);
    }

    [Fact]
    public void Scatter_labels_and_log_axes()
    {
        var built = ScatterChartBuilder.Build(Scatter() with { Label = "Name", LogX = true }, TestData.Table(Products), 10);

        Assert.Equal(["Pen", "Desk", "Chair", "Paper"], TestData.Texts(TestData.Trace(built).GetProperty("text")));
        Assert.Equal("log", TestData.Layout(built).GetProperty("xaxis").GetProperty("type").GetString());
        Assert.False(TestData.Layout(built).GetProperty("yaxis").TryGetProperty("type", out _));
    }

    [Fact]
    public void Scatter_y_must_be_numeric()
    {
        var ex = Assert.Throws<McpException>(() => ScatterChartBuilder.Build(Scatter() with { Y = "Name" }, TestData.Table(Products), 10));
        Assert.Equal("Column 'Name' (y) must hold numbers, but row 1 has 'Pen'.", ex.Message);
    }

    [Fact]
    public void Histogram_passes_raw_values_and_counts_by_default()
    {
        var built = HistogramBuilder.Build(Histogram(), TestData.Table(Products), 10);

        var trace = TestData.Trace(built);
        Assert.Equal("histogram", trace.GetProperty("type").GetString());
        Assert.Equal(4, trace.GetProperty("x").GetArrayLength());
        Assert.False(trace.TryGetProperty("histnorm", out _));
        Assert.False(trace.TryGetProperty("nbinsx", out _));
        Assert.Equal("Count", TestData.Layout(built).GetProperty("yaxis").GetProperty("title").GetProperty("text").GetString());
    }

    [Theory]
    [InlineData("percent", "percent", "Percent")]
    [InlineData("density", "probability density", "Density")]
    public void Histogram_normalizes(string normalize, string histnorm, string title)
    {
        var built = HistogramBuilder.Build(Histogram() with { Normalize = normalize, Bins = 20 }, TestData.Table(Products), 10);

        Assert.Equal(histnorm, TestData.Trace(built).GetProperty("histnorm").GetString());
        Assert.Equal(20, TestData.Trace(built).GetProperty("nbinsx").GetInt32());
        Assert.Equal(title, TestData.Layout(built).GetProperty("yaxis").GetProperty("title").GetProperty("text").GetString());
    }

    [Fact]
    public void Histogram_overlays_series_see_through()
    {
        var built = HistogramBuilder.Build(Histogram() with { Series = "Category", Cumulative = true }, TestData.Table(Products), 10);

        Assert.Equal(2, built.Traces);
        Assert.Equal(0.6, TestData.Trace(built, 0).GetProperty("opacity").GetDouble());
        Assert.True(TestData.Trace(built, 0).GetProperty("cumulative").GetProperty("enabled").GetBoolean());
        Assert.Equal("overlay", TestData.Layout(built).GetProperty("barmode").GetString());
        Assert.Equal("Cumulative count", TestData.Layout(built).GetProperty("yaxis").GetProperty("title").GetProperty("text").GetString());
    }

    [Fact]
    public void Histogram_bins_are_limited()
    {
        var ex = Assert.Throws<McpException>(() => HistogramBuilder.Build(Histogram() with { Bins = 0 }, TestData.Table(Products), 10));
        Assert.Equal("bins must be between 1 and 500.", ex.Message);
    }
}
