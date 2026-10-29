# PlotlyTools

An MCP server that draws charts from tabular data with [Plotly.NET](https://plotly.net/). Agents pass a table
(a SQL `run_select` result works as-is) and get the chart back as an interactive **HTML page** or a **PNG/SVG
image**, after a short JSON summary. It runs on **localhost** for now.

The tools also exist as a **headless library** (`src/PlotlyTools`), so any other MCP server can host them with
one line (`WithPlotlyTools`).

**Status: phase 3.** 13 chart tools and a dashboard tool. **No authentication yet.**

| Document | For |
|---|---|
| [docs/plan.md](docs/plan.md) | Architecture, design decisions, phases |
| [docs/tool-catalog.md](docs/tool-catalog.md) | Every tool's parameters and behaviour |
| [AGENTS.md](AGENTS.md) | Conventions (partial classes, tool rules) for contributors and AI agents |
| [memory.md](memory.md) | Log of every change |

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 / ASP.NET Core (set once in `Directory.Build.props`) |
| MCP | `ModelContextProtocol.AspNetCore` 2.3.0 (official C# SDK), Streamable HTTP, stateless |
| Charts | `Plotly.NET` 5.1.0; pages load plotly.js 2.27.1 from the CDN |
| Images | `Plotly.NET.ImageExport` 6.1.0, rendering in an installed Chrome or Edge |

## Tools

| Tool | Use it for |
|---|---|
| `create_bar_chart` | Comparing values across categories |
| `create_line_chart` | Trends over time |
| `create_area_chart` | A total over time and what it's made of |
| `create_scatter_chart` | How two numbers relate; bubble charts |
| `create_histogram` | One column's distribution |
| `create_box_plot` | Distributions compared across groups (box or violin) |
| `create_heatmap` | A value across two categories |
| `create_pie_chart` | Shares of a whole (pie or donut) |
| `create_funnel_chart` | Values dropping through stages |
| `create_treemap`, `create_sunburst` | Shares within a hierarchy |
| `create_waterfall_chart` | Changes adding up to a total |
| `create_indicator` | One KPI number, its change, or a gauge |
| `create_dashboard` | Several of the charts above in one page: a grid of cards with headings and text notes |

Every chart tool takes `format` (`html`, `png`, `svg`), `theme` (`light`, `dark`) and, for images, `width` /
`height`. Dashboards come as `html` or `png` (a screenshot of the page), with `width`.

**Example:** [docs/examples/mock-dashboard.html](docs/examples/mock-dashboard.html) is a dashboard with every chart
type, made from mock data by one `create_dashboard` call ([request](docs/examples/mock-dashboard.request.json),
[PNG preview](docs/examples/mock-dashboard.png)). [embed-example.html](docs/examples/embed-example.html) shows it
embedded in another page with an `<iframe>`; `build-mock-dashboard.mjs` regenerates both files.

Endpoints: `POST /mcp` (MCP) and `GET /health`.

## Run

```bash
dotnet run --project src/PlotlyTools.Server
```

```bash
curl http://localhost:5200/health
```

```bash
dotnet test PlotlyTools.sln
```

Connect Claude Code:

```bash
claude mcp add --transport http plotly-tools http://localhost:5200/mcp
```

To run a second instance, change the port with the `Urls` environment variable or `--urls`. `ASPNETCORE_URLS`
doesn't work, because `Urls` in `appsettings.json` takes precedence over it.

## Configuration

Section `PlotlyTools` in `src/PlotlyTools.Server/appsettings.json`:

| Setting | Default | Meaning |
|---|---|---|
| `BrowserExecutablePath` | empty | Installed Chrome or Edge for images. **Empty turns `png` / `svg` off**; Chromium is never downloaded |
| `OutputDirectory` | empty | When set, every result is also saved there |
| `MaxRows` | `10000` | Rows read per chart; the rest are dropped and the summary says so |
| `MaxSeries` | `50` | Distinct series values before a tool asks the agent to filter |
| `DefaultTheme` | `Light` | |
| `DefaultImageWidth`, `DefaultImageHeight` | `1000`, `600` | |
| `RenderTimeoutSeconds` | `30` | |

**Machine-local settings** such as the browser path go in `src/PlotlyTools.Server/appsettings.Local.json`, which
is gitignored:

```json
{ "PlotlyTools": { "BrowserExecutablePath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe" } }
```

On a server, use environment variables instead, e.g. `PlotlyTools__BrowserExecutablePath`. Precedence, lowest
to highest: `appsettings.json` → user secrets → `appsettings.Local.json` → environment variables → command line.

**Browser:** Chrome is verified. A managed Edge may refuse to start under automation (`Failed to launch
browser!`); use Chrome there. Each image takes about 1.3 s.

## Hosting the tools in another MCP server

Reference `src/PlotlyTools` and add one line to that server's registration:

```csharp
builder.Services
    .AddMcpServer(o => o.ServerInstructions = "..." + PlotlyToolsInfo.Instructions)
    .WithHttpTransport()
    .WithPlotlyTools(builder.Configuration.GetSection("PlotlyTools"));
```

The host should turn plain `McpException`s into tool errors, as `src/PlotlyTools.Server/Program.cs` does.

## Deploying (later)

- **Hosting:** IIS (ASP.NET Core Hosting Bundle 8), a Windows service or a container. Set `Urls`.
- **HTTPS:** terminate TLS at IIS or a reverse proxy. Responses are SSE, so proxies must not buffer `/mcp`.
- **Images:** install Chrome on the host and set `PlotlyTools__BrowserExecutablePath`.
- **Exposure:** until auth exists, anyone who can reach the URL can use every tool; keep it on an internal network.
