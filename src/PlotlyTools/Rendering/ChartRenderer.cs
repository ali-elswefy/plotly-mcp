using System.Text;
using Plotly.NET;
using PlotlyTools.Charts;

namespace PlotlyTools.Rendering;

/// <summary>
/// The last step of every chart tool: applies <see cref="ChartStyle"/>, renders the requested format and saves
/// the result when saving is on. Styling happens here so no chart can skip it.
/// </summary>
internal sealed partial class ChartRenderer(ImageRenderer images, OutputFiles files)
{
    private readonly ImageRenderer _images = images;
    private readonly OutputFiles _files = files;

    /// <param name="kind">Chart type, used in the file name, e.g. "bar".</param>
    public async Task<RenderedChart> RenderAsync(GenericChart chart, string kind, string? title, OutputRequest output, CancellationToken cancellationToken)
    {
        var styled = ChartStyle.Apply(chart, output.Theme);
        var content = output.Format == OutputFormat.Html
            ? Encoding.UTF8.GetBytes(HtmlPage.Render(styled, title, output.Theme))
            : await _images.RenderAsync(styled, output, cancellationToken);

        var fileName = OutputFiles.NewFileName(kind, output);
        var path = await _files.SaveAsync(fileName, content, cancellationToken);
        return new RenderedChart(output, fileName, content, path);
    }
}
