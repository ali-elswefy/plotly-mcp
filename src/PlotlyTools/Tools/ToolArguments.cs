using ModelContextProtocol;
using PlotlyTools.Charts;
using PlotlyTools.Configuration;
using PlotlyTools.Rendering;

namespace PlotlyTools.Tools;

/// <summary>Parses the output parameters every chart and dashboard tool shares, filling in the server defaults.</summary>
internal static partial class ToolArguments
{
    public static OutputRequest Output(string? format, string? theme, int? width, int? height, PlotlyToolsOptions options) =>
        new(ParseFormat(format), ParseTheme(theme, options.DefaultTheme), ImageSize(width, "width", options.DefaultImageWidth), ImageSize(height, "height", options.DefaultImageHeight));

    public static OutputFormat ParseFormat(string? format) => format?.Trim().ToLowerInvariant() switch
    {
        null or "" or "html" => OutputFormat.Html,
        "png" => OutputFormat.Png,
        "svg" => OutputFormat.Svg,
        _ => throw new McpException($"Unknown format '{format}'. Use 'html', 'png' or 'svg'."),
    };

    public static ChartTheme ParseTheme(string? theme, ChartTheme defaultTheme) => theme?.Trim().ToLowerInvariant() switch
    {
        null or "" => defaultTheme,
        "light" => ChartTheme.Light,
        "dark" => ChartTheme.Dark,
        _ => throw new McpException($"Unknown theme '{theme}'. Use 'light' or 'dark'."),
    };

    private static int ImageSize(int? value, string name, int defaultValue) => value switch
    {
        null => defaultValue,
        >= PlotlyToolsOptions.MinImageSize and <= PlotlyToolsOptions.MaxImageSize => value.Value,
        _ => throw new McpException($"{name} must be between {PlotlyToolsOptions.MinImageSize} and {PlotlyToolsOptions.MaxImageSize} pixels."),
    };
}
