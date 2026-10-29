using System.Text;
using Plotly.NET;
using PlotlyTools.Charts;
using PlotlyTools.Dashboards;

namespace PlotlyTools.Rendering;

// Rendering a whole dashboard: every chart styled like a standalone one, then the page or its screenshot.
internal sealed partial class ChartRenderer
{
    public async Task<RenderedChart> RenderDashboardAsync(
        string title, string? description, int columns, ParsedDashboard dashboard, OutputRequest output, CancellationToken cancellationToken)
    {
        var figures = dashboard.Cards
            .Where(c => c.Kind == DashboardCardKind.Chart)
            .Select(c => GenericChart.toFigureJson(ChartStyle.Apply(c.Chart!.Chart, output.Theme)))
            .ToList();

        var content = output.Format == OutputFormat.Html
            ? Encoding.UTF8.GetBytes(DashboardPage.Render(title, description, columns, dashboard.Cards, figures, output.Theme, loadPlotlyJs: true))
            : await _images.ScreenshotAsync(
                DashboardPage.Render(title, description, columns, dashboard.Cards, figures, output.Theme, loadPlotlyJs: false),
                output.Width,
                cancellationToken);

        var fileName = OutputFiles.NewFileName("dashboard", output);
        var path = await _files.SaveAsync(fileName, content, cancellationToken);
        return new RenderedChart(output, fileName, content, path);
    }
}
