using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PlotlyTools.Charts;
using PlotlyTools.Tools;

namespace PlotlyTools.Tests;

/// <summary>The tools end to end, as the host calls them: registration, parameters to result.</summary>
public partial class ChartToolsTests
{
    private const string Sales = """{"columns":["Region","Sales"],"rows":[["North",120],["South",98],["East",143]]}""";

    [Fact]
    public void Registers_every_chart_tool()
    {
        using var services = TestData.Services();

        var names = services.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).Order();

        Assert.Equal(
            [
                "create_area_chart", "create_bar_chart", "create_box_plot", "create_dashboard", "create_funnel_chart", "create_heatmap",
                "create_histogram", "create_indicator", "create_line_chart", "create_pie_chart", "create_scatter_chart",
                "create_sunburst", "create_treemap", "create_waterfall_chart",
            ],
            names);
    }

    [Fact]
    public async Task Indicator_takes_values_not_data()
    {
        using var services = TestData.Services();
        var tools = ActivatorUtilities.CreateInstance<FinanceChartTools>(services);

        var result = await tools.CreateIndicatorAsync(1_250_000, reference: 1_100_000, prefix: "$", title: "Revenue");

        var summary = Summary(result);
        Assert.Equal("indicator", summary.GetProperty("chartType").GetString());
        Assert.Equal(1, summary.GetProperty("points").GetInt32());
        Assert.False(summary.GetProperty("truncated").GetBoolean());
        var page = (TextResourceContents)Assert.IsType<EmbeddedResourceBlock>(result.Content[1]).Resource;
        Assert.Contains("\"type\":\"indicator\"", page.Text);
    }

    [Fact]
    public void Tools_are_read_only_and_closed_world()
    {
        using var services = TestData.Services();

        Assert.All(services.GetServices<McpServerTool>(), tool =>
        {
            Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint);
            Assert.False(tool.ProtocolTool.Annotations?.OpenWorldHint);
        });
    }

    [Fact]
    public async Task Bar_chart_returns_a_summary_and_a_page()
    {
        using var services = TestData.Services();
        var tools = ActivatorUtilities.CreateInstance<BasicChartTools>(services);

        var result = await tools.CreateBarChartAsync(TestData.Input(Sales), "Region", ["Sales"], title: "Sales by region");

        var summary = Summary(result);
        Assert.Equal("bar", summary.GetProperty("chartType").GetString());
        Assert.Equal("Sales by region", summary.GetProperty("title").GetString());
        Assert.Equal(3, summary.GetProperty("points").GetInt32());
        var page = (TextResourceContents)Assert.IsType<EmbeddedResourceBlock>(result.Content[1]).Resource;
        Assert.Contains("<title>Sales by region</title>", page.Text);
        Assert.Contains("\"type\":\"bar\"", page.Text);
    }

    [Fact]
    public async Task Says_when_rows_were_dropped()
    {
        using var services = TestData.Services(new() { ["MaxRows"] = "2" });
        var tools = ActivatorUtilities.CreateInstance<PartToWholeChartTools>(services);

        var result = await tools.CreatePieChartAsync(TestData.Input(Sales), "Region", "Sales");

        var summary = Summary(result);
        Assert.True(summary.GetProperty("truncated").GetBoolean());
        Assert.StartsWith("Only the first 2 of 3 rows were plotted.", summary.GetProperty("note").GetString());
        Assert.Equal(2, summary.GetProperty("points").GetInt32());
    }

    [Fact]
    public async Task Bad_output_parameters_fail_before_any_work()
    {
        using var services = TestData.Services();
        var tools = ActivatorUtilities.CreateInstance<BasicChartTools>(services);

        // The data is invalid too, but the format is checked first.
        var ex = await Assert.ThrowsAsync<McpException>(() => tools.CreateLineChartAsync(new ChartDataInput(null, null), "x", ["y"], format: "gif"));
        Assert.Equal("Unknown format 'gif'. Use 'html', 'png' or 'svg'.", ex.Message);
    }

    [BrowserFact]
    public async Task Histogram_as_png()
    {
        using var services = TestData.Services(new() { ["BrowserExecutablePath"] = TestData.Browser });
        var tools = ActivatorUtilities.CreateInstance<BasicChartTools>(services);

        var result = await tools.CreateHistogramAsync(TestData.Input(Sales), "Sales", format: "png", width: 600, height: 400);

        Assert.Equal("png", Summary(result).GetProperty("format").GetString());
        Assert.Equal("image/png", Assert.IsType<ImageContentBlock>(result.Content[1]).MimeType);
    }

    private static JsonElement Summary(CallToolResult result) =>
        JsonDocument.Parse(Assert.IsType<TextContentBlock>(result.Content[0]).Text).RootElement;
}
