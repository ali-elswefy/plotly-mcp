using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlotlyTools.Charts;
using PlotlyTools.Configuration;
using PlotlyTools.Rendering;

namespace PlotlyTools.Tests;

/// <summary>Host registration: options are bound and validated, and the services resolve.</summary>
public partial class WithPlotlyToolsTests
{
    [Fact]
    public void Binds_options_from_the_section()
    {
        using var services = TestData.Services(new() { ["MaxRows"] = "500", ["DefaultTheme"] = "Dark", ["OutputDirectory"] = "out" });

        var options = services.GetRequiredService<IOptions<PlotlyToolsOptions>>().Value;
        Assert.Equal(500, options.MaxRows);
        Assert.Equal(ChartTheme.Dark, options.DefaultTheme);
        Assert.True(services.GetRequiredService<OutputFiles>().IsEnabled);
    }

    [Fact]
    public void Defaults_leave_images_and_saving_off()
    {
        using var services = TestData.Services();

        Assert.False(services.GetRequiredService<ImageRenderer>().IsConfigured);
        Assert.False(services.GetRequiredService<OutputFiles>().IsEnabled);
        Assert.NotNull(services.GetRequiredService<ChartRenderer>());
    }

    [Theory]
    [InlineData("MaxRows", "0", "MaxRows, MaxSeries and MaxDashboardItems must be at least 1")]
    [InlineData("DefaultImageWidth", "50", "DefaultImageWidth and DefaultImageHeight must be between")]
    [InlineData("RenderTimeoutSeconds", "0", "RenderTimeoutSeconds must be at least 1")]
    [InlineData("BrowserExecutablePath", @"C:\no\such\browser.exe", "BrowserExecutablePath doesn't point at a file")]
    public void Rejects_invalid_settings(string key, string value, string message)
    {
        using var services = TestData.Services(new() { [key] = value });

        var ex = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<PlotlyToolsOptions>>().Value);
        Assert.Contains(message, ex.Message);
    }
}
