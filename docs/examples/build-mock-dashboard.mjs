// Builds a mock dashboard that uses every chart type, through the PlotlyTools server's create_dashboard tool.
// Usage, with the server running (it needs MaxDashboardItems of at least 19):
//   node docs/examples/build-mock-dashboard.mjs http://localhost:5200/mcp docs/examples docs/examples
import { writeFileSync, mkdirSync } from "node:fs";

const [url, examplesDir, previewDir] = process.argv.slice(2);
mkdirSync(examplesDir, { recursive: true });

// Seeded random numbers, so the mock data is the same on every run.
let seed = 20251029;
const random = () => {
  seed = (seed + 0x6d2b79f5) | 0;
  let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
  t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
};
const normal = () => Math.sqrt(-2 * Math.log(1 - random())) * Math.cos(2 * Math.PI * random());
const round = (value, digits = 1) => Number(value.toFixed(digits));

const months = Array.from({ length: 12 }, (_, i) => `2025-${String(i + 1).padStart(2, "0")}-01`);
const season = m => 1 + 0.25 * Math.sin(((m - 3) / 12) * 2 * Math.PI); // peaks in early summer

// Revenue by channel and month ($k): line chart, and summed by channel for the donut.
const channels = { Online: [180, 6], Retail: [240, 2], Wholesale: [130, 3] };
const monthlyRevenue = {
  columns: ["Month", "Channel", "Revenue"],
  rows: months.flatMap((month, m) =>
    Object.entries(channels).map(([channel, [base, growth]]) => [month, channel, round((base + growth * m) * season(m) * (1 + 0.04 * normal()))])),
};
const revenueTotal = monthlyRevenue.rows.reduce((sum, row) => sum + row[2], 0);

// Units by product category and month: stacked area.
const categories = { Tents: 420, Packs: 760, Apparel: 1350, Footwear: 980 };
const monthlyUnits = {
  columns: ["Month", "Category", "Units"],
  rows: months.flatMap((month, m) =>
    Object.entries(categories).map(([category, base]) => [month, category, Math.round(base * season(m) * (1 + 0.06 * normal()))])),
};

// Every revenue figure below is split from the same total, so the cards agree with each other.
const channelTotals = Object.fromEntries(Object.keys(channels).map(c =>
  [c, monthlyRevenue.rows.filter(r => r[1] === c).reduce((sum, r) => sum + r[2], 0)]));
const regionWeights = { North: 0.21, South: 0.18, East: 0.25, West: 0.19, Central: 0.17 };
const channelRegionRows = Object.entries(channelTotals).flatMap(([channel, total]) => {
  // Wholesale has no customers in the South.
  const regions = Object.entries(regionWeights).filter(([region]) => !(channel === "Wholesale" && region === "South"));
  const weightSum = regions.reduce((sum, [, w]) => sum + w, 0);
  return regions.map(([region, w]) => [channel, region, Math.round((total * w) / weightSum)]);
});

// Revenue against target by region ($k): grouped bars.
const targetFactor = { North: 0.95, South: 1.06, East: 0.9, West: 1.07, Central: 0.99 };
const regionRevenue = {
  columns: [{ name: "Region", type: "nvarchar" }, { name: "Revenue", type: "decimal" }, { name: "Target", type: "decimal" }],
  rows: Object.keys(regionWeights).map(region => {
    const revenue = channelRegionRows.filter(r => r[1] === region).reduce((sum, r) => sum + r[2], 0);
    return [region, revenue, Math.round(revenue * targetFactor[region] / 10) * 10];
  }),
};

// Profit bridge ($k): waterfall with an explicit total row.
const profitBridge = {
  columns: ["Item", "Amount", "Measure"],
  rows: [["Revenue", Math.round(revenueTotal), "relative"], ["Cost of goods", -3480, "relative"], ["Shipping", -460, "relative"],
         ["Marketing", -720, "relative"], ["Salaries", -1310, "relative"], ["Other income", 85, "relative"],
         ["Operating profit", null, "total"]],
};

// Online sales funnel.
const funnel = {
  columns: ["Stage", "Sessions"],
  rows: [["Site visits", 120400], ["Product views", 54300], ["Added to cart", 14200], ["Checkout started", 6150], ["Orders", 4380]],
};

// Customers: orders against average order value, bubble size = revenue.
const segments = { Consumer: [4, 95], "Small business": [14, 260], Enterprise: [38, 720] };
const customers = {
  columns: ["Customer", "Segment", "Orders", "AvgOrderValue", "Revenue"],
  rows: Object.entries(segments).flatMap(([segment, [orders, value]], s) =>
    Array.from({ length: 20 }, (_, i) => {
      const count = Math.max(1, Math.round(orders * (1 + 0.45 * normal())));
      const aov = round(Math.max(20, value * (1 + 0.3 * normal())), 0);
      return [`Customer ${String(s * 20 + i + 1).padStart(3, "0")}`, segment, count, aov, count * aov];
    })),
};

// Order values ($): skewed, as real order values are.
const orders = {
  columns: ["OrderValue"],
  rows: Array.from({ length: 480 }, () => [round(Math.exp(4.6 + 0.55 * normal()), 2)]),
};

// Delivery days by carrier and region: box plots.
const carriers = { Swift: [1.8, 0.6], Northline: [2.9, 0.9], ParcelGo: [3.6, 1.2], "Budget Freight": [5.2, 1.8] };
const deliveries = {
  columns: ["Carrier", "Zone", "Days"],
  rows: Object.entries(carriers).flatMap(([carrier, [mean, spread]]) =>
    ["Domestic", "International"].flatMap(zone => Array.from({ length: 40 }, () =>
      [carrier, zone, round(Math.max(0.5, mean + (zone === "International" ? 1.5 : 0) + spread * normal()), 1)]))),
};

// Orders by weekday and hour: heatmap in long format.
const weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const hourly = {
  columns: ["Weekday", "Hour", "Orders"],
  rows: weekdays.flatMap((day, d) => Array.from({ length: 12 }, (_, h) => {
    const hour = h + 8;
    const busy = Math.exp(-((hour - (d >= 5 ? 14 : 12.5)) ** 2) / 10) * (d >= 5 ? 1.35 : 1) + (hour >= 18 ? 0.35 : 0);
    return [day, `${String(hour).padStart(2, "0")}:00`, Math.round(40 + 160 * busy * (1 + 0.1 * normal()))];
  })),
};

// Revenue by category and subcategory ($k): treemap.
const productMix = {
  columns: ["Category", "Subcategory", "Revenue"],
  rows: [["Tents", "Backpacking", 610], ["Tents", "Family", 455], ["Tents", "Shelters", 120],
         ["Packs", "Daypacks", 520], ["Packs", "Trekking", 690], ["Packs", "Travel", 310],
         ["Apparel", "Jackets", 880], ["Apparel", "Base layers", 365], ["Apparel", "Trousers", 410], ["Apparel", "Accessories", 190],
         ["Footwear", "Hiking boots", 720], ["Footwear", "Trail runners", 540], ["Footwear", "Sandals", 180]],
};

// Revenue by channel and region ($k): sunburst, from the same split as the region bars.
const channelRegions = { columns: ["Channel", "Region", "Revenue"], rows: channelRegionRows };

const request = {
  title: "Northwind Outdoor — Sales & Operations, FY2025",
  description: "Mock data. Every chart type PlotlyTools can draw, in one dashboard. Money in thousands of dollars unless noted.",
  columns: 3,
  items: [
    { heading: "Headline numbers" },
    { type: "indicator", value: round(revenueTotal / 1000, 2), reference: 6.05, prefix: "$", suffix: "M", title: "Revenue FY2025 (vs FY2024)" },
    { type: "indicator", value: 93.4, reference: 95, mode: "gauge", min: 80, max: 100, suffix: "%", title: "On-time delivery (target 95%)" },
    { type: "waterfall", data: profitBridge, labels: "Item", values: "Amount", measure: "Measure", title: "Profit bridge ($k)", yTitle: "$k" },

    { heading: "Sales" },
    { type: "line", data: monthlyRevenue, x: "Month", y: ["Revenue"], series: "Channel", markers: true, shape: "spline", title: "Monthly revenue by channel ($k)", yTitle: "$k", columnSpan: 2 },
    { type: "pie", data: monthlyRevenue, labels: "Channel", values: "Revenue", hole: 0.55, title: "Revenue share by channel" },
    { type: "area", data: monthlyUnits, x: "Month", y: ["Units"], series: "Category", title: "Units sold by category", columnSpan: 2 },
    { type: "bar", data: regionRevenue, x: "Region", y: ["Revenue", "Target"], sort: "desc", title: "Revenue vs target by region ($k)", yTitle: "$k" },

    { heading: "Customers & operations" },
    { type: "funnel", data: funnel, stage: "Stage", values: "Sessions", title: "Online conversion funnel" },
    { type: "scatter", data: customers, x: "Orders", y: "AvgOrderValue", size: "Revenue", series: "Segment", label: "Customer", title: "Customers: orders vs average order value (bubble = revenue)", yTitle: "Average order value ($)", columnSpan: 2 },
    { type: "histogram", data: orders, x: "OrderValue", bins: 30, title: "Order value distribution", xTitle: "Order value ($)" },
    { type: "box", data: deliveries, y: "Days", x: "Carrier", series: "Zone", title: "Delivery days by carrier and zone", columnSpan: 2 },

    { heading: "Product mix & demand" },
    { type: "heatmap", data: hourly, x: "Hour", y: "Weekday", z: "Orders", colorScale: "YlOrRd", title: "Orders by weekday and hour", columnSpan: 2 },
    { type: "treemap", data: productMix, path: ["Category", "Subcategory"], values: "Revenue", title: "Revenue by category ($k)" },
    { type: "sunburst", data: channelRegions, path: ["Channel", "Region"], values: "Revenue", title: "Revenue by channel and region ($k)" },
    { text: "About this dashboard\n\nAll figures are generated mock data for Northwind Outdoor, a fictional retailer.\nEach card was drawn by one of the 13 PlotlyTools chart builders through a single create_dashboard call; hover, zoom and click the legends to explore.", columnSpan: 2 },
  ],
};
writeFileSync(`${examplesDir}/mock-dashboard.request.json`, JSON.stringify(request, null, 2));

const headers = { "Content-Type": "application/json", Accept: "application/json, text/event-stream", "MCP-Protocol-Version": "2025-06-18" };
async function call(method, params, id) {
  const response = await fetch(url, { method: "POST", headers, body: JSON.stringify({ jsonrpc: "2.0", id, method, params }) });
  const text = await response.text();
  const data = text.split("\n").filter(l => l.startsWith("data:")).map(l => l.slice(5).trim());
  return JSON.parse(data.length ? data[data.length - 1] : text);
}

await call("initialize", { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "mock-dashboard", version: "1" } }, 1);
for (const [id, format] of [[2, "html"], [3, "png"]]) {
  const started = Date.now();
  const result = await call("tools/call", { name: "create_dashboard", arguments: { ...request, format, width: 1500 } }, id);
  if (result.error || result.result.isError) {
    console.log(`${format}: ERROR ${JSON.stringify(result.error ?? result.result.content[0].text)}`);
    continue;
  }
  const [summary, block] = result.result.content;
  if (format === "html") {
    writeFileSync(`${examplesDir}/mock-dashboard.html`, block.resource.text);
  } else {
    writeFileSync(`${previewDir}/mock-dashboard.png`, Buffer.from(block.data, "base64"));
  }
  console.log(`${format} (${Date.now() - started} ms): ${summary.text}`);
}
