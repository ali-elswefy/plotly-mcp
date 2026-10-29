using PlotlyTools.Charts;

namespace PlotlyTools.Rendering;

/// <summary>How one call wants its output, with the server defaults already filled in.</summary>
internal sealed partial record OutputRequest(OutputFormat Format, ChartTheme Theme, int Width, int Height)
{
    public string MimeType => Format switch
    {
        OutputFormat.Png => "image/png",
        OutputFormat.Svg => "image/svg+xml",
        _ => "text/html",
    };

    public string FileExtension => Format.ToString().ToLowerInvariant();
}
