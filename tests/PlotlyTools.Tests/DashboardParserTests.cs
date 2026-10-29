using System.Text.Json;
using ModelContextProtocol;
using PlotlyTools.Configuration;
using PlotlyTools.Dashboards;

namespace PlotlyTools.Tests;

/// <summary>Reading dashboard items: chart items through the chart tools' specs, headings, text, and the limits.</summary>
public partial class DashboardParserTests
{
    private const string Sales = """{"columns":["Region","Sales"],"rows":[["North",120],["South",98],["East",143]]}""";

    private static readonly PlotlyToolsOptions Options = new() { MaxDashboardItems = 5, MaxRows = 5 };

    private static ParsedDashboard Parse(string itemsJson, int columns = 2, PlotlyToolsOptions? options = null) =>
        DashboardParser.Parse(JsonSerializer.Deserialize<JsonElement[]>(itemsJson), columns, options ?? Options);

    private static string Error(string itemsJson, PlotlyToolsOptions? options = null) =>
        Assert.Throws<McpException>(() => Parse(itemsJson, options: options)).Message;

    [Fact]
    public void Reads_charts_headings_and_text_in_order()
    {
        var dashboard = Parse($$"""
            [
              {"heading": "Overview"},
              {"type": "bar", "data": {{Sales}}, "x": "Region", "y": ["Sales"], "title": "Sales", "columnSpan": 2},
              {"type": "Indicator", "value": 361, "reference": 300, "title": "Total"},
              {"text": "Excludes returns.", "columnSpan": 9}
            ]
            """);

        Assert.Equal([DashboardCardKind.Heading, DashboardCardKind.Chart, DashboardCardKind.Chart, DashboardCardKind.Text], dashboard.Cards.Select(c => c.Kind));
        Assert.Equal(2, dashboard.Charts);
        Assert.Equal(4, dashboard.Points);
        Assert.Equal(2, dashboard.Cards[1].ColumnSpan);
        Assert.Equal(2, dashboard.Cards[3].ColumnSpan); // capped at the grid's columns
        Assert.Equal("Overview", dashboard.Cards[0].Text);
    }

    [Fact]
    public void Chart_items_get_the_tools_defaults_and_notes()
    {
        var dashboard = Parse($$"""[{"type": "pie", "data": {"columns":["k","v"],"rows":[["a",1],["a",2],["b",3]]}, "labels": "k", "values": "v"}]""");

        var trace = TestData.Trace(dashboard.Cards[0].Chart!);
        Assert.Equal("percent", trace.GetProperty("textinfo").GetString());
        Assert.Equal(["items[0] (pie): Rows with the same k were combined (sum)."], dashboard.Notes);
    }

    [Fact]
    public void Item_errors_name_the_item()
    {
        Assert.Equal(
            "items[1] (bar): Column 'Revenue' (y) was not found. Available columns: Region, Sales.",
            Error($$"""[{"heading":"x"}, {"type": "bar", "data": {{Sales}}, "x": "Region", "y": ["Revenue"]}]"""));
    }

    [Theory]
    [InlineData("""[{"type": "radar"}]""", "items[0]: unknown type 'radar'. Use 'bar', 'line'")]
    [InlineData("""[{"title": "x"}]""", "items[0]: needs a 'type'")]
    [InlineData("""[42]""", "items[0]: must be an object")]
    [InlineData("""[{"type": "indicator", "value": 1, "format": "png"}]""", "items[0] (indicator): 'format' is set once for the whole dashboard")]
    [InlineData("""[{"type": "indicator", "value": 1, "rowSpan": 0}]""", "items[0] (indicator): rowSpan must be a whole number from 1 to 3.")]
    [InlineData("""[{"type": "indicator", "value": "lots"}]""", "items[0] (indicator): The JSON value could not be converted")]
    [InlineData("""[{"heading": "Only a heading"}]""", "A dashboard needs at least one chart item")]
    [InlineData("""[]""", "items is empty.")]
    public void Rejects_bad_items(string items, string message)
    {
        Assert.StartsWith(message, Error(items));
    }

    [Fact]
    public void Unknown_parameters_are_refused_with_the_valid_ones()
    {
        var message = Error($$"""[{"type": "bar", "data": {{Sales}}, "x": "Region", "yColumn": "Sales"}]""");

        Assert.StartsWith("items[0] (bar): unknown parameter 'yColumn'. A bar item takes x, y, series, orientation", message);
        Assert.Contains("data, plus columnSpan and rowSpan.", message);
    }

    [Fact]
    public void Limits_items()
    {
        var items = "[" + string.Join(",", Enumerable.Repeat("""{"type":"indicator","value":1}""", 6)) + "]";

        Assert.StartsWith("The dashboard has 6 items; at most 5 are allowed.", Error(items));
    }

    [Fact]
    public void Max_rows_apply_across_the_whole_dashboard()
    {
        var bar = $$"""{"type": "bar", "data": {{Sales}}, "x": "Region", "y": ["Sales"]}""";

        var dashboard = Parse($"[{bar}, {bar}]");

        Assert.True(dashboard.Truncated);
        Assert.Equal(5, dashboard.Points);
        Assert.Contains("items[1] (bar): only the first 2 of 3 rows were plotted (the dashboard's limit is 5 rows in total).", dashboard.Notes);
        Assert.StartsWith("items[2] (bar): The dashboard's data already reaches the limit of 5 rows", Error($"[{bar}, {bar}, {bar}]"));
    }
}
