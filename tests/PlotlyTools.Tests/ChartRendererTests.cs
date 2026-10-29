using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using PlotlyTools.Charts;
using PlotlyTools.Rendering;
using PlotlyTools.Tools;

namespace PlotlyTools.Tests;

/// <summary>From a chart to a tool result: HTML or image content, the summary, and optional saving.</summary>
public partial class ChartRendererTests
{
    private static readonly OutputRequest Html = new(OutputFormat.Html, ChartTheme.Light, 1000, 600);

    [Fact]
    public async Task Html_result_is_a_summary_then_an_html_resource()
    {
        using var services = TestData.Services();
        var rendered = await services.GetRequiredService<ChartRenderer>().RenderAsync(TestData.SalesChart(), "bar", "Sales", Html, default);

        var built = new BuiltChart(TestData.SalesChart(), Traces: 1, Points: 4, Notes: []);
        var table = TestData.Table("""{"columns":["x"],"rows":[[1]]}""");
        var result = ChartToolResult.Create(rendered, "bar", "Sales", built, table);

        Assert.Equal(2, result.Content.Count);
        var summary = JsonDocument.Parse(Assert.IsType<TextContentBlock>(result.Content[0]).Text).RootElement;
        Assert.Equal("bar", summary.GetProperty("chartType").GetString());
        Assert.Equal("html", summary.GetProperty("format").GetString());
        Assert.Equal(4, summary.GetProperty("points").GetInt32());
        Assert.False(summary.TryGetProperty("filePath", out _));
        Assert.False(summary.TryGetProperty("note", out _));

        var page = Assert.IsType<TextResourceContents>(Assert.IsType<EmbeddedResourceBlock>(result.Content[1]).Resource);
        Assert.Equal("text/html", page.MimeType);
        Assert.StartsWith("plotly://charts/bar-", page.Uri);
        Assert.StartsWith("<!DOCTYPE html>", page.Text);
    }

    [Fact]
    public async Task Html_output_is_styled()
    {
        using var services = TestData.Services();
        var rendered = await services.GetRequiredService<ChartRenderer>().RenderAsync(TestData.SalesChart(), "bar", null, Html, default);

        var html = System.Text.Encoding.UTF8.GetString(rendered.Content);
        Assert.Contains(ChartStyle.LightColorway[0], html);
        Assert.DoesNotContain("\"width\":600", html);
    }

    [Fact]
    public async Task Saves_files_when_an_output_directory_is_set()
    {
        var directory = Path.Combine(Path.GetTempPath(), "plotlytools-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var services = TestData.Services(new() { ["OutputDirectory"] = directory });
            var rendered = await services.GetRequiredService<ChartRenderer>().RenderAsync(TestData.SalesChart(), "bar", null, Html, default);

            Assert.NotNull(rendered.FilePath);
            Assert.Equal(directory, Path.GetDirectoryName(rendered.FilePath));
            Assert.Equal(rendered.Content, await File.ReadAllBytesAsync(rendered.FilePath));

            var page = (TextResourceContents)((EmbeddedResourceBlock)rendered.ToContentBlock()).Resource;
            Assert.StartsWith("file:///", page.Uri);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(OutputFormat.Png)]
    [InlineData(OutputFormat.Svg)]
    public async Task Images_are_refused_without_a_browser(OutputFormat format)
    {
        using var services = TestData.Services();
        var renderer = services.GetRequiredService<ChartRenderer>();

        var ex = await Assert.ThrowsAsync<McpException>(() => renderer.RenderAsync(TestData.SalesChart(), "bar", null, Html with { Format = format }, default));
        Assert.Contains("Use format 'html' instead", ex.Message);
    }

    [BrowserFact]
    public async Task Renders_a_png()
    {
        using var services = TestData.Services(new() { ["BrowserExecutablePath"] = TestData.Browser });
        var rendered = await services.GetRequiredService<ChartRenderer>().RenderAsync(TestData.SalesChart(), "bar", null, Html with { Format = OutputFormat.Png, Width = 800, Height = 500 }, default);

        // PNG signature, then the IHDR chunk's width and height (big-endian) at bytes 16-23.
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], rendered.Content[..4]);
        Assert.Equal(800, BigEndian(rendered.Content, 16));
        Assert.Equal(500, BigEndian(rendered.Content, 20));

        var image = Assert.IsType<ImageContentBlock>(rendered.ToContentBlock());
        Assert.Equal("image/png", image.MimeType);
    }

    [BrowserFact]
    public async Task Renders_an_svg()
    {
        using var services = TestData.Services(new() { ["BrowserExecutablePath"] = TestData.Browser });
        var rendered = await services.GetRequiredService<ChartRenderer>().RenderAsync(TestData.SalesChart(), "bar", null, Html with { Format = OutputFormat.Svg }, default);

        var svg = System.Text.Encoding.UTF8.GetString(rendered.Content);
        Assert.StartsWith("<svg", svg);
        Assert.Contains("North", svg);
        Assert.Equal("image/svg+xml", Assert.IsType<ImageContentBlock>(rendered.ToContentBlock()).MimeType);
    }

    private static int BigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}
