using ModelContextProtocol;
using PuppeteerSharp;

namespace PlotlyTools.Rendering;

// Screenshotting a whole page (dashboards) straight through PuppeteerSharp.
internal sealed partial class ImageRenderer
{
    // Only the starting height: the screenshot covers the full page, however tall it is.
    private const int ViewportHeight = 800;

    /// <summary>
    /// A PNG of the whole page at <paramref name="width"/> pixels wide. The page must define renderDashboard(),
    /// which draws its charts after plotly.js is injected and then sets window.dashboardReady.
    /// </summary>
    public async Task<byte[]> ScreenshotAsync(string html, int width, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new McpException("Image output isn't set up on this server (no browser configured). Use format 'html' instead.");
        }

        var timeout = TimeSpan.FromSeconds(_options.RenderTimeoutSeconds);
        var timeoutMs = (int)timeout.TotalMilliseconds;
        try
        {
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = _options.BrowserExecutablePath,
                Timeout = timeoutMs,
            });
            await using var page = await browser.NewPageAsync();
            await page.SetViewportAsync(new ViewPortOptions { Width = width, Height = ViewportHeight });
            await page.SetContentAsync(html, new NavigationOptions { Timeout = timeoutMs, WaitUntil = [WaitUntilNavigation.Load] });

            // plotly.js is injected from Plotly.NET's embedded copy rather than loaded from the CDN, so screenshots
            // work without internet access.
            await page.AddScriptTagAsync(new AddTagOptions { Content = PlotlyJs.Source });
            await page.EvaluateExpressionAsync("renderDashboard()");
            await page.WaitForExpressionAsync("window.dashboardReady === true", new WaitForFunctionOptions { Timeout = timeoutMs });
            cancellationToken.ThrowIfCancellationRequested();

            return await page.ScreenshotDataAsync(new ScreenshotOptions { FullPage = true, Type = ScreenshotType.Png });
        }
        catch (WaitTaskTimeoutException)
        {
            throw new McpException($"Rendering the image took longer than {_options.RenderTimeoutSeconds} s. Plot less data, or use format 'html'.");
        }
        catch (PuppeteerException ex)
        {
            throw new McpException($"The server couldn't render the image ({ex.Message}). Use format 'html' instead.");
        }
    }
}
