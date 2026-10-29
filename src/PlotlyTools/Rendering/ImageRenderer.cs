using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.FSharp.Core;
using ModelContextProtocol;
using Plotly.NET;
using Plotly.NET.ImageExport;
using PlotlyTools.Configuration;
using PuppeteerSharp;

namespace PlotlyTools.Rendering;

/// <summary>
/// Renders charts to PNG or SVG with Plotly.NET.ImageExport, which loads the chart in a headless browser
/// (PuppeteerSharp), and screenshots whole pages such as dashboards. Each render starts its own browser, about 1.5 s.
/// </summary>
internal sealed partial class ImageRenderer
{
    private readonly PlotlyToolsOptions _options;

    public ImageRenderer(IOptions<PlotlyToolsOptions> options)
    {
        _options = options.Value;

        if (IsConfigured)
        {
            // ImageExport reads the browser from this static setting. Left unset it would download Chromium,
            // which is why image output stays off without a configured path.
            PuppeteerSharpRendererOptions.localBrowserExecutablePath = FSharpOption<string>.Some(_options.BrowserExecutablePath!);
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BrowserExecutablePath);

    public async Task<byte[]> RenderAsync(GenericChart chart, OutputRequest output, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new McpException("Image output isn't set up on this server (no browser configured). Use format 'html' instead.");
        }

        // plotly.js goes inside the page the browser loads, so rendering doesn't need internet access.
        var embedded = GenericChart.setDisplayOptions(
            DisplayOptions.init(PlotlyJSReference: FSharpOption<PlotlyJSReference>.Some(PlotlyJSReference.Full)),
            chart);

        var width = FSharpOption<int>.Some(output.Width);
        var height = FSharpOption<int>.Some(output.Height);
        var render = output.Format == OutputFormat.Svg
            ? embedded.ToSVGStringAsync(Width: width, Height: height)
            : embedded.ToBase64PNGStringAsync(Width: width, Height: height);

        string rendered;
        try
        {
            // ImageExport takes no cancellation token: on timeout the render is abandoned, not stopped, and its
            // browser exits when it finishes.
            rendered = await render.WaitAsync(TimeSpan.FromSeconds(_options.RenderTimeoutSeconds), cancellationToken);
        }
        catch (TimeoutException)
        {
            throw new McpException($"Rendering the image took longer than {_options.RenderTimeoutSeconds} s. Plot less data, or use format 'html'.");
        }
        catch (PuppeteerException ex)
        {
            throw new McpException($"The server couldn't render the image ({ex.Message}). Use format 'html' instead.");
        }

        return output.Format == OutputFormat.Svg
            ? Encoding.UTF8.GetBytes(rendered)
            : Convert.FromBase64String(StripDataUri(rendered));
    }

    // PNGs come back as a data URI: "data:image/png;base64,...".
    private static string StripDataUri(string value)
    {
        var comma = value.IndexOf(',', StringComparison.Ordinal);
        return value.StartsWith("data:", StringComparison.Ordinal) && comma >= 0 ? value[(comma + 1)..] : value;
    }
}
