using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

// Reading columns as numbers, axis values and labels.
public partial class ChartTableTests
{
    private const string Mixed = """
        {"columns":["Label","Amount","Flag"],
         "rows":[["a",1.5,true],["b","2.25",false],[null,null,null],["",3,true]]}
        """;

    [Fact]
    public void Numbers_accept_numeric_text_and_keep_nulls_as_gaps()
    {
        var numbers = TestData.Table(Mixed).Numbers("Amount", "y");

        Assert.Equal([1.5, 2.25, null, 3.0], numbers);
    }

    [Fact]
    public void Numbers_reject_text_naming_the_row()
    {
        var ex = Assert.Throws<McpException>(() => TestData.Table(Mixed).Numbers("Label", "y"));

        Assert.Equal("Column 'Label' (y) must hold numbers, but row 1 has 'a'.", ex.Message);
    }

    [Fact]
    public void Numbers_reject_booleans()
    {
        var ex = Assert.Throws<McpException>(() => TestData.Table(Mixed).Numbers("Flag", "size"));

        Assert.Contains("row 1 has true", ex.Message);
    }

    [Fact]
    public void Values_keep_json_types()
    {
        var table = TestData.Table(Mixed);

        Assert.Equal<object?>(["a", "b", null, ""], table.Values("Label", "x"));
        Assert.Equal<object?>([1.5, "2.25", null, 3.0], table.Values("Amount", "x"));
        Assert.Equal<object?>([true, false, null, true], table.Values("Flag", "x"));
    }

    [Fact]
    public void Labels_are_text_with_blanks_named()
    {
        var table = TestData.Table(Mixed);

        Assert.Equal(["a", "b", ChartTable.BlankLabel, ChartTable.BlankLabel], table.Labels("Label", "series"));
        Assert.Equal(["1.5", "2.25", ChartTable.BlankLabel, "3"], table.Labels("Amount", "series"));
    }

    [Fact]
    public void Nested_values_are_rejected()
    {
        var table = TestData.Table("""{"columns":["x"],"rows":[[[1,2]]]}""");

        var ex = Assert.Throws<McpException>(() => table.Values("x", "x"));
        Assert.Contains("must hold single values, but row 1 has a nested object or list", ex.Message);
    }
}
