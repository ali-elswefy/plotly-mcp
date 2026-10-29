using PlotlyTools.Charts;

namespace PlotlyTools.Dashboards;

/// <summary>What a dashboard card holds.</summary>
internal enum DashboardCardKind
{
    Chart,
    Heading,
    Text,
}

/// <summary>One item of a dashboard, parsed: a built chart, a heading or a text note, with its size in the grid.</summary>
internal sealed partial record DashboardCard(DashboardCardKind Kind, BuiltChart? Chart, string? Text, int ColumnSpan, int RowSpan)
{
    public static DashboardCard ForChart(BuiltChart chart, int columnSpan, int rowSpan) => new(DashboardCardKind.Chart, chart, null, columnSpan, rowSpan);

    public static DashboardCard ForHeading(string text) => new(DashboardCardKind.Heading, null, text, 0, 1);

    public static DashboardCard ForText(string text, int columnSpan) => new(DashboardCardKind.Text, null, text, columnSpan, 1);
}

/// <summary>All of a dashboard's cards, with totals and notes for the summary.</summary>
internal sealed partial record ParsedDashboard(IReadOnlyList<DashboardCard> Cards, int Points, bool Truncated, IReadOnlyList<string> Notes)
{
    public int Charts => Cards.Count(c => c.Kind == DashboardCardKind.Chart);
}
