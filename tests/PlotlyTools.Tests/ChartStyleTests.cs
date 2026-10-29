using System.Text.Json;
using Plotly.NET;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>The shared look: our small template instead of Plotly.NET's, and no fixed size.</summary>
public partial class ChartStyleTests
{
    [Fact]
    public void Removes_the_fixed_size()
    {
        var before = Layout(TestData.SalesChart());
        var after = Layout(ChartStyle.Apply(TestData.SalesChart(), ChartTheme.Light));

        Assert.True(before.TryGetProperty("width", out _), "Plotly.NET is expected to set a width; if not, this test can go.");
        Assert.False(after.TryGetProperty("width", out _));
        Assert.False(after.TryGetProperty("height", out _));
    }

    [Theory]
    [InlineData(ChartTheme.Light, "#ffffff", "#2563eb")]
    [InlineData(ChartTheme.Dark, "#111827", "#60a5fa")]
    public void Replaces_the_template(ChartTheme theme, string background, string firstColor)
    {
        var template = Layout(ChartStyle.Apply(TestData.SalesChart(), theme)).GetProperty("template").GetProperty("layout");

        Assert.Equal(background, template.GetProperty("paper_bgcolor").GetString());
        Assert.Equal(firstColor, template.GetProperty("colorway")[0].GetString());
    }

    [Fact]
    public void Keeps_the_figure_small()
    {
        var json = GenericChart.toFigureJson(ChartStyle.Apply(TestData.SalesChart(), ChartTheme.Light));

        // Plotly.NET's default template alone is about 7 KB.
        Assert.True(json.Length < 2000, $"Figure is {json.Length} characters.");
    }

    [Fact]
    public void Keeps_the_data()
    {
        var data = Figure(ChartStyle.Apply(TestData.SalesChart(), ChartTheme.Light)).GetProperty("data")[0];

        Assert.Equal("bar", data.GetProperty("type").GetString());
        Assert.Equal(4, data.GetProperty("y").GetArrayLength());
    }

    private static JsonElement Figure(GenericChart chart) => JsonDocument.Parse(GenericChart.toFigureJson(chart)).RootElement;

    private static JsonElement Layout(GenericChart chart) => Figure(chart).GetProperty("layout");
}
