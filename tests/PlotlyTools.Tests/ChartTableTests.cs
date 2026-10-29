using ModelContextProtocol;
using PlotlyTools.Charts;

namespace PlotlyTools.Tests;

/// <summary>Parsing the agent's data: column names, row shape, the row cap, and finding columns by name.</summary>
public partial class ChartTableTests
{
    [Fact]
    public void Parses_names_and_rows()
    {
        var table = TestData.Table("""{"columns":["Region","Sales"],"rows":[["North",120.5],["South",98]]}""");

        Assert.Equal(["Region", "Sales"], table.Columns);
        Assert.Equal(2, table.RowCount);
        Assert.False(table.Truncated);
    }

    [Fact]
    public void Accepts_run_select_columns()
    {
        // sql_mcp's run_select returns columns as { name, type } objects.
        var table = TestData.Table("""{"columns":[{"name":"Region","type":"nvarchar"},{"name":"Sales","type":"decimal"}],"rows":[["North",1]]}""");

        Assert.Equal(["Region", "Sales"], table.Columns);
    }

    [Fact]
    public void Caps_rows_and_reports_truncation()
    {
        var table = TestData.Table("""{"columns":["x"],"rows":[[1],[2],[3]]}""", maxRows: 2);

        Assert.Equal(2, table.RowCount);
        Assert.Equal(3, table.TotalRows);
        Assert.True(table.Truncated);
    }

    [Theory]
    [InlineData("""{"columns":[],"rows":[[1]]}""", "data.columns is empty")]
    [InlineData("""{"rows":[[1]]}""", "data.columns is empty")]
    [InlineData("""{"columns":["x"],"rows":[]}""", "data.rows is empty")]
    [InlineData("""{"columns":["x","y"],"rows":[[1,2],[3]]}""", "Row 2 has 1 values, but there are 2 columns")]
    [InlineData("""{"columns":["x",""],"rows":[[1,2]]}""", "Column 2 has no name")]
    [InlineData("""{"columns":["x","x"],"rows":[[1,2]]}""", "Column 'x' appears more than once")]
    [InlineData("""{"columns":["x",5],"rows":[[1,2]]}""", "data.columns[1] must be a column name")]
    public void Rejects_malformed_data(string json, string message)
    {
        var ex = Assert.Throws<McpException>(() => TestData.Table(json));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void Finds_columns_exactly_then_ignoring_case()
    {
        var table = TestData.Table("""{"columns":["Region","Sales"],"rows":[["North",1]]}""");

        Assert.Equal(1, table.IndexOf("Sales", "y"));
        Assert.Equal(1, table.IndexOf("sales", "y"));
    }

    [Fact]
    public void Prefers_the_exact_match_when_names_differ_only_in_case()
    {
        var table = TestData.Table("""{"columns":["sales","Sales"],"rows":[[1,2]]}""");

        Assert.Equal(1, table.IndexOf("Sales", "y"));
        Assert.Throws<McpException>(() => table.IndexOf("SALES", "y"));
    }

    [Fact]
    public void Unknown_column_lists_the_available_ones()
    {
        var table = TestData.Table("""{"columns":["Region","Sales"],"rows":[["North",1]]}""");

        var ex = Assert.Throws<McpException>(() => table.IndexOf("Revenue", "y"));
        Assert.Equal("Column 'Revenue' (y) was not found. Available columns: Region, Sales.", ex.Message);
    }

    [Fact]
    public void Missing_column_name_asks_for_one()
    {
        var table = TestData.Table("""{"columns":["Region"],"rows":[["North"]]}""");

        var ex = Assert.Throws<McpException>(() => table.IndexOf(" ", "x"));
        Assert.StartsWith("Name the column to use for x.", ex.Message);
    }
}
