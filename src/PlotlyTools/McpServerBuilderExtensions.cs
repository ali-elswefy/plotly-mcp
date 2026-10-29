using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlotlyTools.Configuration;
using PlotlyTools.Rendering;
using PlotlyTools.Tools;

namespace PlotlyTools;

/// <summary>How a host MCP server adds this library's tools.</summary>
public static partial class McpServerBuilderExtensions
{
    /// <summary>
    /// Registers the PlotlyTools options, services and tools. Pass the host's "PlotlyTools" configuration section.
    /// The tool types are registered here one by one, so the host doesn't need to scan this assembly.
    /// </summary>
    public static IMcpServerBuilder WithPlotlyTools(this IMcpServerBuilder builder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.AddOptions<PlotlyToolsOptions>()
            .Bind(configuration)
            .Validate(
                o => o.MaxRows >= 1 && o.MaxSeries >= 1 && o.MaxDashboardItems >= 1,
                "PlotlyTools: MaxRows, MaxSeries and MaxDashboardItems must be at least 1.")
            .Validate(
                o => IsImageSize(o.DefaultImageWidth) && IsImageSize(o.DefaultImageHeight),
                $"PlotlyTools: DefaultImageWidth and DefaultImageHeight must be between {PlotlyToolsOptions.MinImageSize} and {PlotlyToolsOptions.MaxImageSize}.")
            .Validate(o => o.RenderTimeoutSeconds >= 1, "PlotlyTools:RenderTimeoutSeconds must be at least 1.")
            .Validate(
                o => string.IsNullOrWhiteSpace(o.BrowserExecutablePath) || File.Exists(o.BrowserExecutablePath),
                "PlotlyTools:BrowserExecutablePath doesn't point at a file. Set it to an installed Chrome or Edge, or leave it empty to turn image output off.")
            .ValidateOnStart();

        builder.Services.TryAddSingleton<ImageRenderer>();
        builder.Services.TryAddSingleton<OutputFiles>();
        builder.Services.TryAddSingleton<ChartRenderer>();
        builder.Services.TryAddSingleton<ChartToolRunner>();

        return builder
            .WithTools<BasicChartTools>()
            .WithTools<DistributionChartTools>()
            .WithTools<PartToWholeChartTools>()
            .WithTools<FinanceChartTools>()
            .WithTools<DashboardTools>();
    }

    private static bool IsImageSize(int pixels) => pixels is >= PlotlyToolsOptions.MinImageSize and <= PlotlyToolsOptions.MaxImageSize;
}
