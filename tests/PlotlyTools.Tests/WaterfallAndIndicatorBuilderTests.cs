using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Waterfall charts and KPI indicators.</summary>
public partial class WaterfallAndIndicatorBuilderTests
{
    private const string Bridge = """
        {"columns":["Item","Amount","Kind"],
         "rows":[["Opening",100,"absolute"],["Income",50,null],["Costs",-30,"Relative"],["Closing",null,"total"]]}
        """;

    private static WaterfallChartSpec Waterfall(string? measure = null, bool? showTotal = null) =>
        new("Item", "Amount", measure, showTotal, null, null, null);

    private static IndicatorSpec Indicator(double? value = 1_250_000, double? reference = null, string? mode = null) =>
        new(value, reference, mode, null, null, "$", null, "Revenue");

    [Fact]
    public void Waterfall_adds_a_closing_total_by_default()
    {
        var built = WaterfallChartBuilder.Build(Waterfall(), TestData.Table(Bridge));

        var trace = TestData.Trace(built);
        Assert.Equal("waterfall", trace.GetProperty("type").GetString());
        Assert.Equal(["Opening", "Income", "Costs", "Closing", "Total"], TestData.Texts(trace.GetProperty("x")));
        Assert.Equal(["relative", "relative", "relative", "relative", "total"], TestData.Texts(trace.GetProperty("measure")));
        Assert.Equal([100.0, 50.0, -30.0, 0.0, null], TestData.Numbers(trace.GetProperty("y")));
        Assert.Equal(5, built.Points);
    }

    [Fact]
    public void Waterfall_reads_the_measure_column()
    {
        var trace = TestData.Trace(WaterfallChartBuilder.Build(Waterfall(measure: "Kind"), TestData.Table(Bridge)));

        Assert.Equal(["absolute", "relative", "relative", "total"], TestData.Texts(trace.GetProperty("measure")));
        Assert.Equal(4, trace.GetProperty("x").GetArrayLength());
    }

    [Fact]
    public void Waterfall_without_total_and_with_a_bad_measure()
    {
        Assert.Equal(4, TestData.Trace(WaterfallChartBuilder.Build(Waterfall(showTotal: false), TestData.Table(Bridge))).GetProperty("x").GetArrayLength());

        var ex = Assert.Throws<McpException>(() => WaterfallChartBuilder.Build(Waterfall(measure: "Item"), TestData.Table(Bridge)));
        Assert.Equal("Column 'Item' (measure) must hold 'relative', 'total' or 'absolute', but row 1 has 'Opening'.", ex.Message);
    }

    [Fact]
    public void Indicator_number_with_prefix_and_title_on_the_trace()
    {
        var built = IndicatorBuilder.Build(Indicator());

        var trace = TestData.Trace(built);
        Assert.Equal("indicator", trace.GetProperty("type").GetString());
        Assert.Equal("number", trace.GetProperty("mode").GetString());
        Assert.Equal("$", trace.GetProperty("number").GetProperty("prefix").GetString());
        Assert.Equal("Revenue", trace.GetProperty("title").GetProperty("text").GetString());
        Assert.False(TestData.Layout(built).TryGetProperty("title", out _));
    }

    [Fact]
    public void Indicator_with_a_reference_shows_the_change()
    {
        var trace = TestData.Trace(IndicatorBuilder.Build(Indicator(reference: 1_100_000)));

        Assert.Equal("number+delta", trace.GetProperty("mode").GetString());
        Assert.Equal(1_100_000, trace.GetProperty("delta").GetProperty("reference").GetDouble());
    }

    [Fact]
    public void Gauge_gets_a_round_maximum_and_a_reference_marker()
    {
        var trace = TestData.Trace(IndicatorBuilder.Build(Indicator(reference: 1_100_000, mode: "gauge")));

        Assert.Equal("gauge+number+delta", trace.GetProperty("mode").GetString());
        var gauge = trace.GetProperty("gauge");
        Assert.Equal([0.0, 1_500_000.0], TestData.Numbers(gauge.GetProperty("axis").GetProperty("range")));
        Assert.Equal(1_100_000, gauge.GetProperty("threshold").GetProperty("value").GetDouble());
    }

    [Theory]
    [InlineData(1_250_000, 1_500_000)]
    [InlineData(87, 100)]
    [InlineData(0.42, 0.5)]
    [InlineData(-5, 1)]
    public void Nice_maximum(double value, double expected)
    {
        Assert.Equal(expected, IndicatorBuilder.NiceMax(value), precision: 9);
    }

    [Fact]
    public void Indicator_errors()
    {
        Assert.Equal(
            "mode 'delta' shows the change from reference, so pass reference too.",
            Assert.Throws<McpException>(() => IndicatorBuilder.Build(Indicator(mode: "delta"))).Message);
        Assert.StartsWith("Pass the number to show in value", Assert.Throws<McpException>(() => IndicatorBuilder.Build(Indicator(value: double.NaN))).Message);
        Assert.StartsWith(
            "The gauge needs max greater than min",
            Assert.Throws<McpException>(() => IndicatorBuilder.Build(Indicator(mode: "gauge") with { Min = 10, Max = 5 })).Message);
    }
}
