using ModelContextProtocol;
using PlotlyTools.Charts;
using PlotlyTools.Configuration;
using PlotlyTools.Rendering;
using PlotlyTools.Tools;

namespace PlotlyTools.Tests;

/// <summary>The output parameters every tool shares: format, theme and image size, with the server defaults.</summary>
public partial class ToolArgumentsTests
{
    private static readonly PlotlyToolsOptions Options = new() { DefaultTheme = ChartTheme.Dark, DefaultImageWidth = 900, DefaultImageHeight = 500 };

    [Fact]
    public void Fills_in_the_server_defaults()
    {
        Assert.Equal(new OutputRequest(OutputFormat.Html, ChartTheme.Dark, 900, 500), ToolArguments.Output(null, null, null, null, Options));
    }

    [Theory]
    [InlineData("PNG", OutputFormat.Png)]
    [InlineData(" svg ", OutputFormat.Svg)]
    [InlineData("", OutputFormat.Html)]
    public void Parses_formats(string format, OutputFormat expected)
    {
        Assert.Equal(expected, ToolArguments.ParseFormat(format));
    }

    [Theory]
    [InlineData("jpg", null, null, "Unknown format 'jpg'. Use 'html', 'png' or 'svg'.")]
    [InlineData(null, "blue", null, "Unknown theme 'blue'. Use 'light' or 'dark'.")]
    [InlineData(null, null, 50, "width must be between 100 and 4000 pixels.")]
    [InlineData(null, null, 5000, "width must be between 100 and 4000 pixels.")]
    public void Rejects_bad_values(string? format, string? theme, int? width, string message)
    {
        var ex = Assert.Throws<McpException>(() => ToolArguments.Output(format, theme, width, null, Options));
        Assert.Equal(message, ex.Message);
    }
}
