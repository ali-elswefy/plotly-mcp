using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plotly.NET;
using PlotlyTools.Charts;
using Chart = Plotly.NET.CSharp.Chart;

namespace PlotlyTools.Tests;

/// <summary>Shared inputs: data as agents send it, a sample chart, and a service provider built the way a host builds one.</summary>
internal static partial class TestData
{
    private static readonly JsonSerializerOptions AgentJson = new(JsonSerializerDefaults.Web);

    /// <summary>Data parsed from the JSON an agent would send, e.g. <c>{"columns":[...],"rows":[...]}</c>.</summary>
    public static ChartDataInput Input(string json) => JsonSerializer.Deserialize<ChartDataInput>(json, AgentJson)!;

    public static ChartTable Table(string json, int maxRows = 100) => ChartTable.Parse(Input(json), maxRows);

    /// <summary>The plotly.js figure a builder made, to assert on its traces and layout.</summary>
    public static JsonElement Figure(BuiltChart built) => JsonDocument.Parse(GenericChart.toFigureJson(built.Chart)).RootElement;

    public static JsonElement Trace(BuiltChart built, int index = 0) => Figure(built).GetProperty("data")[index];

    public static JsonElement Layout(BuiltChart built) => Figure(built).GetProperty("layout");

    /// <summary>A JSON array's items as doubles (nulls as null), for comparing trace values.</summary>
    public static double?[] Numbers(JsonElement array) =>
        array.EnumerateArray().Select(v => v.ValueKind == JsonValueKind.Null ? (double?)null : v.GetDouble()).ToArray();

    public static string[] Texts(JsonElement array) => array.EnumerateArray().Select(v => v.ToString() ?? "").ToArray();

    public static GenericChart SalesChart(string name = "Sales") =>
        Chart.Column<double, string, string>(values: [120.5, 98.0, 143.2, 87.9], Keys: new[] { "North", "South", "East", "West" }, Name: name);

    public static ServiceProvider Services(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        var services = new ServiceCollection();
        services.AddMcpServer().WithPlotlyTools(configuration);
        return services.BuildServiceProvider();
    }

    /// <summary>The browser for image tests, from PLOTLYTOOLS_BROWSER; null skips them.</summary>
    public static string? Browser =>
        Environment.GetEnvironmentVariable("PLOTLYTOOLS_BROWSER") is { Length: > 0 } path && File.Exists(path) ? path : null;
}

/// <summary>A test that renders images, so it needs a browser: skipped unless PLOTLYTOOLS_BROWSER points at one.</summary>
public sealed partial class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (TestData.Browser is null)
        {
            Skip = "Set PLOTLYTOOLS_BROWSER to an installed Chrome or Edge to run image tests.";
        }
    }
}
