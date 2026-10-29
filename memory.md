# memory.md

A log of every change and action in this repository, newest first. Add an entry for each piece of work:
the date, what was done, which files changed, and any decision made along the way.

## 2026-10-29: Mock dashboard example (every chart type, embeddable)

- Added `docs/examples/`:
  - `mock-dashboard.html`: the HTML page `create_dashboard` returned for "Northwind Outdoor — Sales &
    Operations, FY2025" (fictional retailer, mock data). 19 items: 4 headings, 14 charts covering all 13
    chart types (2 indicators), 1 text note; 3 columns; ~31 KB, plotly.js from the CDN. Self-contained, so it
    can be opened directly or embedded with an `<iframe>`.
  - `mock-dashboard.request.json`: the exact `create_dashboard` arguments (mock data included).
  - `mock-dashboard.png`: the same dashboard as a PNG (1500 px wide), from the same call with `format: "png"`.
  - `embed-example.html`: a host page embedding the dashboard in a responsive `<iframe>`.
  - `build-mock-dashboard.mjs`: Node script that builds the seeded mock data, writes the request, and calls
    the server for the HTML and PNG.
- Mock data is seeded (same output every run), and every revenue figure is split from one total ($7.33M in the
  KPI, waterfall, region bars and sunburst); the first draft had them disagreeing. Dropped the log x axis on the
  scatter: plotly's minor tick labels were cluttered.
- The dashboard needs `MaxDashboardItems` ≥ 19; the server was run with `--PlotlyTools:MaxDashboardItems=20`.
  The committed default is still 12.
- Checked: the PNG preview; and, served over HTTP, `embed-example.html` in the browser pane, where all 14 charts
  drew inside the iframe (`dashboardReady` true, no empty chart divs).
- `README.md`: added an "Example" paragraph pointing at these files. No code changed.

## 2026-10-28: Phase 3 done (create_dashboard)

- **Tool:** `create_dashboard` (`Tools/DashboardTools.cs`, `.CreateDashboard.cs`), registered in
  `WithPlotlyTools`; 14 tools in total. Parameters: title, items, columns (1–4, default 2), description, theme,
  format (`html` | `png`), width (default 1400).
- **Dashboards/:** `DashboardCard` (+ `ParsedDashboard`), `DashboardParser` (items → cards; MaxDashboardItems;
  MaxRows across the whole dashboard; errors prefixed `items[i] (type):`), `DashboardParser.ChartTypes` (13 item
  types → the chart tools' specs and builders; unknown parameters refused with the valid list; format / theme /
  width / height refused per item), `DashboardPage` (title, description, responsive CSS grid, 380 px chart rows,
  headings across the grid, text notes; `renderDashboard()` sets `window.dashboardReady`).
- **Rendering/:** `ChartRenderer.Dashboard.cs` (styles every chart, renders page or screenshot, saves),
  `ImageRenderer.Screenshot.cs` (PuppeteerSharp: launch the configured browser, set content, inject plotly.js,
  render, full-page PNG), `PlotlyJs` (reads Plotly.NET's embedded `plotly-2.27.1.min.js`).
- **Tools/:** `DashboardSummary` wire shape; `ChartToolResult.CreateDashboard`.
- **Decisions:**
  - Dashboard PNGs are a screenshot of the dashboard page, not Plotly.NET's `Chart.Grid` as planned: they keep
    headings, text, card layout and per-chart titles/legends, and mixed chart types need no axis remapping.
    Consequence: no SVG for dashboards (refused with a pointer to single charts).
  - plotly.js for screenshots comes from Plotly.NET's embedded resource, so dashboard PNGs need no internet.
  - Text cards keep their own height (`align-self: start`); found when checking the PNG, where a note stretched
    to the height of the chart beside it.
- **Tests:** 156 passing with Chrome (22 new: parser, page, tool end to end, PNG dashboard width and height).
  Rebuild: 0 warnings.
- **Over MCP (server, port 5201, saving to `charts/`):** `tools/list` returns 14 tools; an 8-item dashboard
  (3 indicators, heading, line, donut, bar, text) in html (~250 ms), png light and png dark (~1.5 s each); a bad
  column came back as `items[0] (bar): Column 'Revenue' (y) was not found…`. Checked both PNGs, and the HTML in
  the browser pane at desktop and phone width (one column). Server stopped afterwards.
- **Docs:** `docs/plan.md` (status, dashboard design, layout, phases, decisions), `docs/tool-catalog.md`
  (create_dashboard as built), `README.md`, `AGENTS.md` (adding a chart tool now includes dashboards).
- `PlotlyToolsInfo.Instructions` now points agents at `create_dashboard` for several charts on one page.

## 2026-10-28: Phase 2 done; standalone MCP server

- **Change of plan:** PlotlyTools is now an MCP server of its own. The headless library and `WithPlotlyTools`
  stay, so other MCP servers can still host the tools.
- **Server:** added `src/PlotlyTools.Server` (Program.cs, appsettings.json, Properties/launchSettings.json), built
  like sql_mcp's server: stateless Streamable HTTP on `http://localhost:5200`, `/mcp` + `/health`, content root
  = binary folder, `appsettings.Local.json` inserted before environment variables, McpException → tool error
  filter, server info `plotly-tools-mcp` 0.2.0. Committed settings keep the browser path empty and saving off.
- **Removed** `samples/PlotlyTools.DevHost` (the server replaces it) and the `samples` solution folder; moved its
  machine-local `appsettings.Local.json` (Chrome path) to the server.
- **Library:** added `PlotlyToolsInfo.Instructions` (which tool for which question), used as the server's
  instructions and available to other hosts.
- **Tools (7 new, 13 total):** `create_box_plot`, `create_heatmap` (`DistributionChartTools`);
  `create_funnel_chart`, `create_treemap`, `create_sunburst` (`PartToWholeChartTools`); `create_waterfall_chart`,
  `create_indicator` (`FinanceChartTools`). Registered in `WithPlotlyTools`.
- **Builders:** `BoxPlotBuilder`, `HeatmapBuilder`, `FunnelChartBuilder`, `HierarchyBuilder` (shared by treemap
  and sunburst), `WaterfallChartBuilder`, `IndicatorBuilder`; specs added to `ChartSpecs.cs`.
  `ChartToolRunner` got an overload without data (indicator), and `ChartToolResult.Create` takes a null table.
- **Decisions:**
  - Heatmap and hierarchy keys use positions / generated node ids, never joined label text, so any label is safe.
  - Heatmap: at most 40,000 cells; first y category at the top; Blues, Greens, Greys, YlGnBu and YlOrRd are
    reversed so higher values get the stronger colour (found when checking the rendered PNG).
  - Treemap / sunburst: parents are the total of their children (noted for avg/min/max); same label under
    different parents stays separate; empty leaves dropped; negatives refused.
  - Waterfall: rows in order, closing total added unless a measure column is given; empty values count as 0.
  - Indicator: title on the indicator itself; gauge max defaults to a round number ~10% above the value.
- **Tests:** 134 passing with Chrome (24 new). Rebuild: 0 warnings.
- **Over MCP (server, port 5201):** `initialize` returns the server info and instructions; `tools/list` returns
  13 tools; every phase 2 tool called (png, svg and html); bad colour scale and `delta` without reference return
  tool errors. Checked the box, heatmap, funnel, treemap, waterfall and gauge PNGs. Server stopped afterwards.
- **Docs:** new `README.md` (run, connect, configure, host elsewhere); `AGENTS.md` (server commands, "two ways to
  run"); `docs/plan.md` (status, standalone server section, layout, phases); `docs/tool-catalog.md` (phase 2
  details).

## 2026-10-28: Phase 1 done (six chart tools)

- **Tools:** `create_bar_chart`, `create_line_chart`, `create_area_chart`, `create_scatter_chart`,
  `create_histogram` (`Tools/BasicChartTools*.cs`) and `create_pie_chart` (`Tools/PartToWholeChartTools*.cs`),
  one file per tool, registered in `WithPlotlyTools`.
- **Builders (`Charts/`):** `ChartSpecs.cs` (one spec record per chart + `BuiltChart`), `BarChartBuilder`,
  `LineChartBuilder`, `AreaChartBuilder`, `ScatterChartBuilder`, `HistogramBuilder`, `PieChartBuilder`.
  Shared: `ChartBuilder` (+ `.Choices`, `.Series`), `Aggregator`, `AxisOrder`, `ChartTable.Groups`,
  `ChartTable.Presence`.
- **Tool plumbing (`Tools/`):** `ChartToolRunner` (output params → parse → build → render → result),
  `ParameterDescriptions`; `ChartToolResult.Create` now takes the `BuiltChart` and `ChartTable` and adds the
  truncation note itself.
- **Decisions:**
  - Traces are built as core Plotly.NET `Trace2D` objects with plotly.js attribute names, not with
    Plotly.NET.CSharp (it needs one .NET type per column; agent data mixes types). Moved the
    `Plotly.NET.CSharp` package from the library to the test project.
  - Tool classes are `internal` with a public constructor (the SDK needs the constructor; `ChartToolRunner` is
    internal). Hosts get them through `WithPlotlyTools`.
  - Specs keep tool parameter names with null = default, ready for dashboards to read from JSON.
  - Bar: category axis always; horizontal bars read top-down; xTitle/yTitle always mean horizontal/vertical
    axis. Aggregation skips nulls like SQL; `count` works on any column.
  - Pie: largest `maxSlices - 1` kept, the rest merged into "Other"; negative totals refused.
- **Tests:** 110 passing with Chrome (60 new: builders on figure JSON, aggregation, tool registration and
  annotations, end-to-end tool calls, truncation note, PNG through a tool). Rebuild: 0 warnings.
- **Over MCP (dev host, port 5201):** `tools/list` returns the six tools with their schemas; all six called in
  html / png / svg; unknown column and bad format return tool errors with the expected messages. Images took
  about 1.2–1.5 s, HTML 10–200 ms. Checked the PNGs (bar dark theme, normalized area, donut) and a saved HTML
  page in the browser pane: all render correctly.
- **Docs:** `docs/plan.md` (status, builder design, tool class visibility, layout, decisions), `AGENTS.md`
  (public surface, "Adding a chart tool" steps), `docs/tool-catalog.md` (implemented list and parameter details).

## 2026-10-28: Phase 0 done

- **Restructure:** moved `PlotlyTools.csproj` to `src/PlotlyTools/`; removed the empty `Tools.cs`. Added
  `tests/PlotlyTools.Tests/` and `samples/PlotlyTools.DevHost/`. Recreated `PlotlyTools.sln` (classic format,
  solution folders `src`, `tests`, `samples`). Added `Directory.Build.props` (net8.0, copied from sql_mcp) and
  `.gitignore` (`dotnet new gitignore`, plus `appsettings.Local.json` and `charts/`).
- **Guides:** added `AGENTS.md` (commands, change-log rule, solution format, partial-class conventions, tool rules,
  tests) and `CLAUDE.md` (`@AGENTS.md`).
- **Packages:** ModelContextProtocol 2.3.0, Plotly.NET 5.1.0, Plotly.NET.CSharp 0.13.0, Plotly.NET.ImageExport 6.1.0,
  Microsoft.Extensions.Options.ConfigurationExtensions 10.0.13. Tests: sql_mcp's xUnit set plus
  Microsoft.Extensions.Configuration / DependencyInjection 10.0.13. Dev host: ModelContextProtocol.AspNetCore 2.3.0.
- **Library (`src/PlotlyTools/`):**
  - `Configuration/PlotlyToolsOptions.cs`: settings from the plan, image size limits 100–4000.
  - `Charts/ChartDataInput.cs` (tool parameter shape), `Charts/ChartTable*.cs` (parse, row cap, column lookup
    exact then case-insensitive, `Numbers` / `Values` / `Labels` with agent-facing errors), `ChartTheme`,
    `ChartStyle` (small light/dark template, removes the fixed 600×600 size).
  - `Rendering/`: `ChartRenderer` (style → HTML or image → optional save), `HtmlPage` (own page, plotly.js 2.27.1
    from CDN, `<` escaped in the embedded JSON), `ImageRenderer` (ImageExport with the configured browser,
    plotly.js embedded, timeout, refuses without a browser), `OutputFiles`, `OutputRequest`, `RenderedChart`
    (HTML → `EmbeddedResourceBlock`, images → `ImageContentBlock`).
  - `Tools/`: `ChartSummary` (wire shape), `ChartToolResult` (summary + chart blocks), `ToolArguments`.
  - `McpServerBuilderExtensions.WithPlotlyTools`: binds and validates options (fails at start on a bad browser
    path), registers services. No tool types yet.
- **Dev host:** `samples/PlotlyTools.DevHost` on `http://localhost:5200`, stateless HTTP, sql_mcp's McpException
  filter, `/health`. Local `appsettings.Local.json` (gitignored) points at Chrome on this machine.
- **Decisions:**
  - HTML pages come from our own `HtmlPage` rather than Plotly.NET's `toEmbeddedHTML` / `toChartHTML`.
  - Styling is applied in `ChartRenderer`, so every output passes through it.
  - Edge failed to launch under PuppeteerSharp here; Chrome works. Docs say Chrome is verified.
- **Verified:**
  - `dotnet build PlotlyTools.sln --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test`: 50 passed with `PLOTLYTOOLS_BROWSER` set to Chrome (PNG size and SVG content checked);
    48 passed + 2 skipped without it.
  - Dev host on port 5201: `/health` Healthy, MCP `initialize` OK. `tools/list` isn't offered yet because no
    tools are registered. Host stopped afterwards.
- **Not verified:** image rendering without internet access (plotly.js is embedded, but this wasn't tested offline).
- Updated `docs/plan.md`: status, Plotly.NET findings (timing, Edge), HtmlPage and styling notes, layout, phase 0 done.

## 2026-10-28: Plan changed to HTML or image output only

- Decided: tools output **HTML or an image** only. No figure JSON returned, no chart store.
- Checked the APIs in a scratch app (Plotly.NET 5.1.0, Plotly.NET.ImageExport 6.1.0, ModelContextProtocol 2.3.0):
  - Building a chart returns an in-memory `GenericChart`; `toChartHTML` / `toEmbeddedHTML` give HTML.
  - ImageExport renders PNG / JPG / SVG through PuppeteerSharp; `PuppeteerSharpRendererOptions.localBrowserExecutablePath`
    (static, settable) points it at an installed browser.
  - Default charts have a fixed 600×600 size and a ~7 KB template (of an 8.4 KB figure); styling must replace both.
  - MCP content blocks available: `ImageContentBlock`, `EmbeddedResourceBlock`, `ResourceLinkBlock`, `TextContentBlock`.
- Rewrote `docs/plan.md`:
  - New "What Plotly.NET gives us" section.
  - Output section: `format` = `html` | `png` | `svg`; result = JSON summary block + the HTML page
    (`EmbeddedResourceBlock`, `text/html`) or the image (`ImageContentBlock`).
  - Images need `BrowserExecutablePath` (installed Chrome/Edge); without it image formats are refused, and
    Chromium is never downloaded. plotly.js is embedded when rendering images, so no internet is needed.
  - Saving to disk is optional (`OutputDirectory`, empty by default).
  - One spec + builder per chart type, shared by the chart tools and dashboards.
  - Dashboards take chart definitions inline; HTML = our own grid template, images = `Chart.Grid`.
  - Removed `Storage/` and the chart store; added `Rendering/`. Tools are now `ReadOnly`, `Idempotent`.
  - Config: added `BrowserExecutablePath`, `MaxDashboardItems`, `DefaultImageWidth/Height`, `RenderTimeoutSeconds`;
    removed `EmbedPlotlyJs`. Phase 3 is now only `create_dashboard`.
- Updated `docs/tool-catalog.md`: new common parameters (`format`, `width`, `height`) and result blocks;
  removed `list_charts`, `get_chart` and `includeFigure`; rewrote `create_dashboard` for inline items.
  16 planned tools are now 14.
- No code changed. The scratch app lives in the session scratchpad, not in the repo.

## 2026-10-28: .NET 8 and a classic solution file

- Decided: target .NET 8. Changed `PlotlyTools.csproj` from `net10.0` to `net8.0`.
- There was no solution file. Added `PlotlyTools.sln` in the classic format (Format Version 12.00,
  `MinimumVisualStudioVersion = 10.0.40219.1`) with `dotnet new sln --format sln`, because the .NET 10 SDK
  otherwise creates `.slnx`, which older Visual Studio versions can't open. It contains `PlotlyTools.csproj`.
- `dotnet build PlotlyTools.sln`: succeeded, 0 warnings.
- Updated `docs/plan.md`: target framework moved from open decisions to decided, solution format noted,
  `Class1.cs` references changed to `Tools.cs` (the file was renamed outside this log).

## 2026-10-28: Planning

- Checked whether the `D:\plotly.rs` build can be used from .NET. It can't directly: only Rust `.rlib` files
  and a proc-macro DLL, no exported C functions. Chose Plotly.NET instead.
- Studied `D:\sql_mcp` (AI_SqlMCP) as the reference for conventions: partial classes split one MCP tool per
  file, `snake_case` tool names, `McpException` for agent-facing errors, result records in `Tools/*Results.cs`,
  `Directory.Build.props`, `AGENTS.md` + `CLAUDE.md`.
- Checked package versions on NuGet: Plotly.NET 5.1.0, Plotly.NET.CSharp 0.13.0, Plotly.NET.ImageExport 6.1.0
  (pulls in PuppeteerSharp, so left out), ModelContextProtocol 2.3.0.
- Wrote the plan.
  - Added `docs/plan.md`: architecture, host integration (`WithPlotlyTools`), data shape, chart store,
    dashboards, conventions, layout, configuration, phases 0–4, testing, open decisions.
  - Added `docs/tool-catalog.md`: parameters and results for the 16 planned tools.
  - Added `memory.md` (this file).
- Open decisions (defaults in `docs/plan.md`): target framework `net8.0` vs the csproj's current `net10.0`;
  file paths vs URLs in results; Plotly.NET.CSharp vs core Plotly.NET; image export out of scope.
- No code changed. `PlotlyTools.csproj` and `Class1.cs` are untouched.
