using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Combining values per category, with nulls handled as in SQL.</summary>
public partial class AggregatorTests
{
    private static readonly string[] Keys = ["a", "b", "a", "c", "a"];
    private static readonly double?[] Values = [1, 2, null, null, 5];

    [Theory]
    [InlineData("sum", new[] { 6.0, 2.0 })]
    [InlineData("avg", new[] { 3.0, 2.0 })]
    [InlineData("count", new[] { 2.0, 1.0, 0.0 })]
    [InlineData("min", new[] { 1.0, 2.0 })]
    [InlineData("max", new[] { 5.0, 2.0 })]
    public void Skips_nulls_and_keeps_first_seen_order(string aggregation, double[] expectedStart)
    {
        var combined = Aggregator.Combine(Keys, Values, Enumerable.Range(0, Keys.Length), Aggregator.Parse(aggregation));

        Assert.Equal(["a", "b", "c"], combined.Keys);
        Assert.Equal(expectedStart.Cast<double?>(), combined.Values.Take(expectedStart.Length));
        Assert.True(combined.HadDuplicates);
    }

    [Fact]
    public void A_category_with_only_nulls_stays_null()
    {
        var combined = Aggregator.Combine(Keys, Values, Enumerable.Range(0, Keys.Length), Aggregation.Sum);

        Assert.Null(combined.Values[2]);
    }

    [Fact]
    public void Uses_only_the_given_rows()
    {
        var combined = Aggregator.Combine(Keys, Values, [1, 3], Aggregation.Sum);

        Assert.Equal(["b", "c"], combined.Keys);
        Assert.False(combined.HadDuplicates);
    }
}
