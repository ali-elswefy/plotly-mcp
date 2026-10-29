using System.Text;
using ModelContextProtocol.Protocol;

namespace PlotlyTools.Rendering;

/// <summary>A finished chart or dashboard: the bytes in the requested format, and where it was saved, if anywhere.</summary>
internal sealed partial record RenderedChart(OutputRequest Output, string FileName, byte[] Content, string? FilePath)
{
    /// <summary>The page as an HTML resource, or the image itself.</summary>
    public ContentBlock ToContentBlock() => Output.Format == OutputFormat.Html
        ? new EmbeddedResourceBlock
        {
            Resource = new TextResourceContents
            {
                // A saved page gets its file URI; otherwise the URI only names the page, nothing serves it.
                Uri = FilePath is null ? $"plotly://charts/{FileName}" : new Uri(FilePath).AbsoluteUri,
                MimeType = Output.MimeType,
                Text = Encoding.UTF8.GetString(Content),
            },
        }
        : ImageContentBlock.FromBytes(Content, Output.MimeType);
}
