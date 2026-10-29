using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using PlotlyTools.Charts;
using PlotlyTools.Dashboards;
using PlotlyTools.Rendering;
using PlotlyTools.Tools;

namespace PlotlyTools.Tests;

/// <summary>The dashboard page and the create_dashboard tool end to end.</summary>
public partial class DashboardTests
{
    private const string Sales = """{"columns":["Region","Sales"],"rows":[["North",120],["South",98],["East",143]]}""";

    private static readonly JsonElement[] Items = JsonSerializer.Deserialize<JsonElement[]>($$"""
        [
          {"type": "bar", "data": {{Sales}}, "x": "Region", "y": ["Sales"], "title": "Sales by region", "columnSpan": 2},
          {"heading": "<Shares>"},
          {"type": "pie", "data": {{Sales}}, "labels": "Region", "values": "Sales", "rowSpan": 2},
          {"type": "indicator", "value": 361, "reference": 330, "prefix": "$", "title": "Total"},
          {"text": "Line one\n<b>not bold</b>"}
        ]
        """)!;

    private static async Task<CallToolResult> Create(string? format = null, int? width = null, Dictionary<string, string?>? settings = null)
    {
        using var services = TestData.Services(settings);
        var tools = ActivatorUtilities.CreateInstance<DashboardTools>(services);
        return await tools.CreateDashboardAsync("Q3 <overview>", Items, columns: 2, description: "All regions", format: format, width: width);
    }

    [Fact]
    public async Task Html_dashboard_draws_every_chart_once_with_plotly_js_loaded_once()
    {
        var result = await Create();

        var summary = JsonDocument.Parse(((TextContentBlock)result.Content[0]).Text).RootElement;
        Assert.Equal("Q3 <overview>", summary.GetProperty("title").GetString());
        Assert.Equal(5, summary.GetProperty("items").GetInt32());
        Assert.Equal(3, summary.GetProperty("charts").GetInt32());
        Assert.Equal(7, summary.GetProperty("points").GetInt32());

        var html = ((TextResourceContents)Assert.IsType<EmbeddedResourceBlock>(result.Content[1]).Resource).Text;
        Assert.Single(Regex.Matches(html, Regex.Escape(HtmlPage.PlotlyJsUrl)));
        Assert.Equal(3, Regex.Matches(html, """<div class="chart" id="chart-\d">""").Count);
        Assert.Contains("grid-template-columns: repeat(2, minmax(0, 1fr))", html);
        Assert.Contains("grid-column: span 2; height: 380px", html);
        Assert.Contains("grid-column: span 1; height: 776px", html);
        Assert.Contains("if (window.Plotly) { renderDashboard(); }", html);
    }

    [Fact]
    public async Task Text_is_encoded_and_charts_are_styled()
    {
        var html = ((TextResourceContents)((EmbeddedResourceBlock)(await Create()).Content[1]).Resource).Text;

        Assert.Contains("<title>Q3 &lt;overview&gt;</title>", html);
        Assert.Contains("""<h2 class="heading">&lt;Shares&gt;</h2>""", html);
        Assert.Contains("&lt;b&gt;not bold&lt;/b&gt;", html);
        Assert.Contains(ChartStyle.LightColorway[0], html);
        Assert.DoesNotContain("\"width\":600", html);
    }

    [Fact]
    public void Screenshot_page_waits_for_injected_plotly_js()
    {
        var page = DashboardPage.Render("t", null, 1, [], [], ChartTheme.Dark, loadPlotlyJs: false);

        Assert.DoesNotContain("cdn.plot.ly", page);
        Assert.Contains("--page: #030712", page);
    }

    [Fact]
    public void Plotly_js_is_embedded_in_Plotly_NET()
    {
        Assert.StartsWith("/**", PlotlyJs.Source);
        Assert.True(PlotlyJs.Source.Length > 1_000_000);
    }

    [Theory]
    [InlineData("svg", null, "Dashboards come as 'html' or 'png'.")]
    [InlineData("png", null, "Image output isn't set up on this server")]
    [InlineData(null, 50, "width must be between 100 and 4000 pixels.")]
    public async Task Refuses_unsupported_output(string? format, int? width, string message)
    {
        var ex = await Assert.ThrowsAsync<McpException>(() => Create(format, width));
        Assert.StartsWith(message, ex.Message);
    }

    [BrowserFact]
    public async Task Png_dashboard_is_the_whole_page_at_the_requested_width()
    {
        var result = await Create("png", 1200, new() { ["BrowserExecutablePath"] = TestData.Browser });

        var image = Assert.IsType<ImageContentBlock>(result.Content[1]);
        Assert.Equal("image/png", image.MimeType);
        var png = image.DecodedData.ToArray();
        Assert.Equal(1200, BigEndian(png, 16));

        // Title, the bar row, the heading, two rows for the pie: well over one grid row tall.
        Assert.True(BigEndian(png, 20) > 2 * DashboardPage.RowHeight, $"Image is only {BigEndian(png, 20)} px tall.");
    }

    private static int BigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}
