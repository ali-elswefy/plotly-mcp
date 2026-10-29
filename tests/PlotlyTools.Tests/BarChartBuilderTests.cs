using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Bar charts: aggregation, series, orientation, bar modes and sorting.</summary>
public partial class BarChartBuilderTests
{
    private const string Sales = """
        {"columns":["Region","Year","Sales","Target"],
         "rows":[["North",2024,100,90],["South",2024,80,85],["North",2025,120,110],["South",2025,null,95]]}
        """;

    private static BuiltChart Build(BarChartSpec spec, string json = Sales) => BarChartBuilder.Build(spec, TestData.Table(json), maxSeries: 10);

    private static BarChartSpec Spec(string? x = "Region", string[]? y = null, string? series = null) =>
        new(x, y ?? ["Sales"], series, null, null, null, null, null, null, null, null);

    [Fact]
    public void Sums_rows_that_share_a_category_and_says_so()
    {
        var built = Build(Spec());

        var trace = TestData.Trace(built);
        Assert.Equal("bar", trace.GetProperty("type").GetString());
        Assert.Equal(["North", "South"], TestData.Texts(trace.GetProperty("x")));
        Assert.Equal([220.0, 80.0], TestData.Numbers(trace.GetProperty("y")));
        Assert.Equal(2, built.Points);
        Assert.Equal(["Rows with the same Region were combined (sum)."], built.Notes);
    }

    [Theory]
    [InlineData("avg", new[] { 110.0, 80.0 }, "Average Sales")]
    [InlineData("count", new[] { 2.0, 1.0 }, "Count of Sales")]
    [InlineData("max", new[] { 120.0, 80.0 }, "Maximum Sales")]
    public void Aggregates_and_titles_the_value_axis(string aggregate, double[] expected, string axisTitle)
    {
        var built = Build(Spec() with { Aggregate = aggregate });

        Assert.Equal(expected.Cast<double?>(), TestData.Numbers(TestData.Trace(built).GetProperty("y")));
        Assert.Equal(axisTitle, TestData.Layout(built).GetProperty("yaxis").GetProperty("title").GetProperty("text").GetString());
    }

    [Fact]
    public void Count_works_on_text_columns()
    {
        var built = Build(Spec(x: "Year", y: ["Region"]) with { Aggregate = "count" });

        Assert.Equal([2.0, 2.0], TestData.Numbers(TestData.Trace(built).GetProperty("y")));
    }

    [Fact]
    public void Draws_one_trace_per_y_column()
    {
        var built = Build(Spec(y: ["Sales", "Target"]));

        Assert.Equal(2, built.Traces);
        Assert.Equal("Target", TestData.Trace(built, 1).GetProperty("name").GetString());
        Assert.Equal([200.0, 180.0], TestData.Numbers(TestData.Trace(built, 1).GetProperty("y")));
    }

    [Fact]
    public void Draws_one_trace_per_series_value()
    {
        var built = Build(Spec(series: "Year"));

        Assert.Equal(2, built.Traces);
        Assert.Equal("2024", TestData.Trace(built, 0).GetProperty("name").GetString());
        Assert.Equal([100.0, 80.0], TestData.Numbers(TestData.Trace(built, 0).GetProperty("y")));
        Assert.Empty(built.Notes);
    }

    [Fact]
    public void Refuses_several_y_columns_with_a_series()
    {
        var ex = Assert.Throws<McpException>(() => Build(Spec(y: ["Sales", "Target"], series: "Year")));
        Assert.Equal("Use either several y columns or a series column, not both.", ex.Message);
    }

    [Fact]
    public void Refuses_too_many_series()
    {
        var ex = Assert.Throws<McpException>(() => BarChartBuilder.Build(Spec(series: "Region"), TestData.Table(Sales), maxSeries: 1));
        Assert.Contains("(series) has 2 distinct values; at most 1 series", ex.Message);
    }

    [Fact]
    public void Keeps_categories_as_categories()
    {
        var built = Build(Spec(x: "Year"));

        Assert.Equal("category", TestData.Layout(built).GetProperty("xaxis").GetProperty("type").GetString());
    }

    [Fact]
    public void Horizontal_bars_swap_the_axes()
    {
        var built = Build(Spec() with { Orientation = "horizontal", XTitle = "Revenue" });

        var trace = TestData.Trace(built);
        var layout = TestData.Layout(built);
        Assert.Equal("h", trace.GetProperty("orientation").GetString());
        Assert.Equal(["North", "South"], TestData.Texts(trace.GetProperty("y")));
        Assert.Equal("reversed", layout.GetProperty("yaxis").GetProperty("autorange").GetString());
        Assert.Equal("Revenue", layout.GetProperty("xaxis").GetProperty("title").GetProperty("text").GetString());
        Assert.Equal("Region", layout.GetProperty("yaxis").GetProperty("title").GetProperty("text").GetString());
    }

    [Theory]
    [InlineData("grouped", "group", false)]
    [InlineData("stacked", "stack", false)]
    [InlineData("percent", "stack", true)]
    public void Sets_the_bar_mode(string barMode, string expected, bool normalized)
    {
        var layout = TestData.Layout(Build(Spec(series: "Year") with { BarMode = barMode }));

        Assert.Equal(expected, layout.GetProperty("barmode").GetString());
        Assert.Equal(normalized, layout.TryGetProperty("barnorm", out _));
    }

    [Fact]
    public void Sorts_by_total()
    {
        var axis = TestData.Layout(Build(Spec() with { Sort = "desc" })).GetProperty("xaxis");

        Assert.Equal("total descending", axis.GetProperty("categoryorder").GetString());
    }

    [Fact]
    public void Shows_values_on_request()
    {
        Assert.Equal("%{y}", TestData.Trace(Build(Spec() with { ShowValues = true })).GetProperty("texttemplate").GetString());
    }

    [Theory]
    [InlineData("orientation", "sideways", "Unknown orientation 'sideways'. Use 'vertical' or 'horizontal'.")]
    [InlineData("barMode", "overlay", "Unknown barMode 'overlay'. Use 'grouped', 'stacked' or 'percent'.")]
    [InlineData("aggregate", "median", "Unknown aggregate 'median'. Use 'sum', 'avg', 'count', 'min' or 'max'.")]
    public void Rejects_unknown_options(string parameter, string value, string message)
    {
        var spec = parameter switch
        {
            "orientation" => Spec() with { Orientation = value },
            "barMode" => Spec() with { BarMode = value },
            _ => Spec() with { Aggregate = value },
        };

        Assert.Equal(message, Assert.Throws<McpException>(() => Build(spec)).Message);
    }

    [Fact]
    public void Refuses_missing_y()
    {
        var ex = Assert.Throws<McpException>(() => Build(Spec() with { Y = [] }));
        Assert.StartsWith("Name at least one column for y.", ex.Message);
    }
}
