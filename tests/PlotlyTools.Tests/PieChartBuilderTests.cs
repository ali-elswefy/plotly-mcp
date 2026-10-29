using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Pie charts: combining labels, merging small slices into "Other", and refusing negative totals.</summary>
public partial class PieChartBuilderTests
{
    private const string Tickets = """
        {"columns":["Priority","Count"],
         "rows":[["High",10],["Low",40],["Medium",25],["High",5],["Urgent",2],["Trivial",1]]}
        """;

    private static PieChartSpec Spec(int? maxSlices = null) => new("Priority", "Count", null, null, maxSlices, null, null);

    [Fact]
    public void Combines_labels_into_one_slice_each()
    {
        var built = PieChartBuilder.Build(Spec(), TestData.Table(Tickets));

        var trace = TestData.Trace(built);
        Assert.Equal("pie", trace.GetProperty("type").GetString());
        Assert.Equal(["High", "Low", "Medium", "Urgent", "Trivial"], TestData.Texts(trace.GetProperty("labels")));
        Assert.Equal([15.0, 40.0, 25.0, 2.0, 1.0], TestData.Numbers(trace.GetProperty("values")));
        Assert.Equal("percent", trace.GetProperty("textinfo").GetString());
        Assert.Equal(["Rows with the same Priority were combined (sum)."], built.Notes);
    }

    [Fact]
    public void Merges_the_smallest_slices_into_other()
    {
        var built = PieChartBuilder.Build(Spec(maxSlices: 3), TestData.Table(Tickets));

        var trace = TestData.Trace(built);
        Assert.Equal(["Low", "Medium", "Other"], TestData.Texts(trace.GetProperty("labels")));
        Assert.Equal([40.0, 25.0, 18.0], TestData.Numbers(trace.GetProperty("values")));
        Assert.Equal(3, built.Points);
        Assert.Contains("The 3 smallest slices were merged into 'Other'.", built.Notes);
    }

    [Fact]
    public void Donut_hole_and_value_labels()
    {
        var trace = TestData.Trace(PieChartBuilder.Build(Spec() with { Hole = 0.5, ShowPercent = false }, TestData.Table(Tickets)));

        Assert.Equal(0.5, trace.GetProperty("hole").GetDouble());
        Assert.Equal("value", trace.GetProperty("textinfo").GetString());
    }

    [Fact]
    public void Refuses_negative_totals()
    {
        var table = TestData.Table("""{"columns":["k","v"],"rows":[["a",5],["b",-3]]}""");

        var ex = Assert.Throws<McpException>(() => PieChartBuilder.Build(new PieChartSpec("k", "v", null, null, null, null, null), table));
        Assert.Equal("'b' totals -3; a pie can't show negative values. Use a bar chart instead.", ex.Message);
    }

    [Theory]
    [InlineData(0.95, null, "hole must be between 0 and 0.9.")]
    [InlineData(null, 1, "maxSlices must be between 2 and 50.")]
    public void Checks_ranges(double? hole, int? maxSlices, string message)
    {
        var ex = Assert.Throws<McpException>(() => PieChartBuilder.Build(Spec(maxSlices) with { Hole = hole }, TestData.Table(Tickets)));
        Assert.Equal(message, ex.Message);
    }
}
