using PlotlyTools.Charts;

namespace PlotlyTools.Configuration;

/// <summary>Bound from the host's "PlotlyTools" configuration section by <c>WithPlotlyTools</c>.</summary>
public sealed partial class PlotlyToolsOptions
{
    public const string SectionName = "PlotlyTools";

    public const int MinImageSize = 100;
    public const int MaxImageSize = 4000;

    /// <summary>Installed Chrome or Edge used to render images. Empty turns png and svg output off.</summary>
    public string? BrowserExecutablePath { get; set; }

    /// <summary>When set, every result is also saved here; relative paths are under the app folder.</summary>
    public string? OutputDirectory { get; set; }

    /// <summary>Rows read from a chart's data (and across a dashboard); the rest are dropped.</summary>
    public int MaxRows { get; set; } = 10_000;

    /// <summary>Distinct series values a chart may have before the tool asks the agent to filter.</summary>
    public int MaxSeries { get; set; } = 50;

    /// <summary>Items in one dashboard.</summary>
    public int MaxDashboardItems { get; set; } = 12;

    /// <summary>Theme when a call doesn't pass one.</summary>
    public ChartTheme DefaultTheme { get; set; } = ChartTheme.Light;

    /// <summary>Image width in pixels when a call doesn't pass one.</summary>
    public int DefaultImageWidth { get; set; } = 1000;

    /// <summary>Image height in pixels when a call doesn't pass one.</summary>
    public int DefaultImageHeight { get; set; } = 600;

    /// <summary>Image rendering is abandoned after this many seconds.</summary>
    public int RenderTimeoutSeconds { get; set; } = 30;
}
