using Plotly.NET;

namespace PlotlyTools.Rendering;

/// <summary>
/// The plotly.js source Plotly.NET ships as an embedded resource, for pages rendered in the headless browser, so
/// rendering needs no internet access.
/// </summary>
internal static partial class PlotlyJs
{
    public static readonly string ResourceName = $"Plotly.NET.plotly-{HtmlPage.PlotlyJsVersion}.min.js";

    private static readonly Lazy<string> Text = new(() =>
    {
        using var stream = typeof(GenericChart).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Plotly.NET has no embedded {ResourceName}; update HtmlPage.PlotlyJsVersion to the version it ships.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    public static string Source => Text.Value;
}
