using System.Reflection;
using System.Text.RegularExpressions;
using Plotly.NET;
using PlotlyTools.Charts;
using PlotlyTools.Rendering;
using Chart = Plotly.NET.CSharp.Chart;

namespace PlotlyTools.Tests;

/// <summary>The standalone HTML page: draws the chart, loads plotly.js once, and keeps agent text out of the markup.</summary>
public partial class HtmlPageTests
{
    [Fact]
    public void Draws_the_chart_with_plotly_js_from_the_cdn()
    {
        var html = HtmlPage.Render(TestData.SalesChart(), "Sales by region", ChartTheme.Light);

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Single(Regex.Matches(html, Regex.Escape(HtmlPage.PlotlyJsUrl)));
        Assert.Contains("Plotly.newPlot(\"chart\"", html);
        Assert.Contains("\"responsive\":true", html);
        Assert.Contains("<title>Sales by region</title>", html);
    }

    [Fact]
    public void Uses_the_theme_background()
    {
        Assert.Contains("background: #111827", HtmlPage.Render(TestData.SalesChart(), null, ChartTheme.Dark));
    }

    [Fact]
    public void Agent_text_cannot_close_the_script_or_inject_markup()
    {
        var chart = Chart.Column<double, string, string>(values: [1.0], Keys: new[] { "</script><script>alert(1)</script>" });

        var html = HtmlPage.Render(chart, "<b>Title</b>", ChartTheme.Light);

        Assert.DoesNotContain("<script>alert(1)", html);
        Assert.Contains("\\u003c/script>\\u003cscript>alert(1)", html);
        Assert.Contains("<title>&lt;b&gt;Title&lt;/b&gt;</title>", html);
    }

    [Fact]
    public void Plotly_js_version_matches_Plotly_NET()
    {
        // Plotly.NET builds figures for one plotly.js version; the page must load that version.
        var globals = typeof(GenericChart).Assembly.GetTypes().Single(t => t.FullName == "Globals");
        var version = globals.GetField("PLOTLYJS_VERSION", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);

        Assert.Equal(HtmlPage.PlotlyJsVersion, version);
    }
}
