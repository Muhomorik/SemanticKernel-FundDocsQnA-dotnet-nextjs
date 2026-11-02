import {
  allocationLabel,
  categoryShades,
  familyColour,
  isDisagreement,
  orientRings,
  SIMILAR_SHOWN,
  symbolFor,
  verdictColour,
  VERDICTS,
  type Allocation,
  type ClusterPoint,
  type Fund,
  type Isolate,
  type MapLayers,
  type MapView,
  type MethodFile,
  type Ring,
} from "@/lib/fund-clusters";

/** Padding around the funds, in map units. */
export const MARGIN = 0.03;

export interface ChartTheme {
  ink: string;
  halo: string;
  dotEdge: string;
  accent: string;
  territoryAlpha: number;
}

export interface ChartState {
  view: MapView;
  layers: MapLayers;
  isolate: Isolate;
  pinned: string | null;
  dark: boolean;
}

export interface Extent {
  xMax: number;
  yMax: number;
}

export interface Grid {
  left: number;
  top: number;
  width: number;
  height: number;
}

/** Loosely typed ECharts series option — the chart is the source of truth. */
export type SeriesOption = Record<string, unknown> & { id: string };

const LIGHT: ChartTheme = {
  ink: "#2b241f",
  halo: "#fdfcfa",
  dotEdge: "rgba(255, 255, 255, 0.85)",
  accent: "#9a5a3c",
  territoryAlpha: 0.2,
};

const DARK: ChartTheme = {
  ink: "#ebe6df",
  halo: "#221e1b",
  dotEdge: "rgba(0, 0, 0, 0.55)",
  accent: "#d08c69",
  territoryAlpha: 0.24,
};

const DIMMED = 0.12;
const ZOOM_HALF_HEIGHT = 0.09;

export function chartTheme(dark: boolean): ChartTheme {
  return dark ? DARK : LIGHT;
}

function aspect(extent: Extent): number {
  return (extent.xMax + 2 * MARGIN) / (extent.yMax + 2 * MARGIN);
}

/** Largest grid with the map's aspect ratio that fits the box, centred. */
export function fitGrid(
  width: number,
  height: number,
  extent: Extent,
  pad = 12
): Grid {
  const ratio = aspect(extent);
  let w = width - 2 * pad;
  let h = w / ratio;
  if (h > height - 2 * pad) {
    h = height - 2 * pad;
    w = h * ratio;
  }
  return { left: (width - w) / 2, top: (height - h) / 2, width: w, height: h };
}

/** Axis windows that centre the map on a fund, keeping the aspect ratio. */
export function zoomWindow(
  point: ClusterPoint,
  extent: Extent
): { x: [number, number]; y: [number, number] } {
  const halfY = ZOOM_HALF_HEIGHT;
  const halfX = halfY * aspect(extent);
  return {
    x: [point.x - halfX, point.x + halfX],
    y: [point.y - halfY, point.y + halfY],
  };
}

interface RenderApi {
  coord: (point: [number, number]) => number[];
}

function ringPath(ring: Ring, api: RenderApi): string {
  const pixels = ring.map((p) => api.coord(p));
  return (
    "M" +
    pixels.map(([x, y]) => `${x.toFixed(1)} ${y.toFixed(1)}`).join("L") +
    "Z"
  );
}

function matchesIsolate(
  isolate: Isolate,
  point: ClusterPoint,
  fund: Fund
): boolean {
  if (!isolate) return true;
  if (isolate.type === "category") return fund.category === isolate.value;
  return point.cluster === isolate.value;
}

/** All ECharts series for the map, from the files and the UI state. */
export function buildSeries(
  doc: MethodFile,
  funds: Fund[],
  state: ChartState
): SeriesOption[] {
  const theme = chartTheme(state.dark);
  const fundsByIsin = new Map(funds.map((f) => [f.isin, f]));
  const shades = categoryShades(funds);
  const clusters = new Map(doc.clusters.map((c) => [c.id, c]));
  const pointsByIsin = new Map(doc.points.map((p) => [p.isin, p]));

  const categoryColour = (category: string, alpha: number) => {
    const s = shades.get(category) ?? { family: "other", shade: 0 };
    return familyColour(s.family, s.shade, state.dark, alpha);
  };

  // Territory: one custom path per part, holes left empty by ring orientation.
  const parts = state.layers.territory
    ? doc.territory.map((part) => ({
        cluster: part.cluster,
        rings: orientRings(part.rings),
      }))
    : [];
  const territory: SeriesOption = {
    id: "territory",
    type: "custom",
    silent: true,
    clip: true,
    z: 1,
    data: parts.map((part) => part.rings[0][0]),
    renderItem(params: { dataIndex: number }, api: RenderApi) {
      const part = parts[params.dataIndex];
      const cluster = clusters.get(part.cluster);
      const dimmed =
        state.isolate?.type === "cluster" &&
        state.isolate.value !== part.cluster;
      const alpha = dimmed ? theme.territoryAlpha / 3 : theme.territoryAlpha;
      return {
        type: "path",
        shape: { pathData: part.rings.map((r) => ringPath(r, api)).join(" ") },
        style: {
          fill: categoryColour(cluster?.leadingCategory ?? "", alpha),
          stroke: theme.halo,
          lineWidth: 1.2,
        },
      };
    },
  };

  const points: SeriesOption = {
    id: "points",
    type: "scatter",
    z: 3,
    emphasis: { scale: 1.6 },
    data: doc.points.map((point) => {
      const fund = fundsByIsin.get(point.isin);
      const family = fund?.family ?? "other";
      const visible = state.view === "all" || isDisagreement(point);
      const focused = fund ? matchesIsolate(state.isolate, point, fund) : false;
      const ring =
        state.layers.rings && focused
          ? verdictColour(point.verdict, state.dark)
          : null;
      const symbol = symbolFor(family);
      return {
        value: [point.x, point.y],
        symbol,
        symbolSize: visible ? (symbol === "diamond" ? 10 : 8) : 0,
        itemStyle: {
          color: familyColour(family, fund?.shade ?? 0, state.dark),
          opacity: focused ? 1 : DIMMED,
          borderColor: ring ?? theme.dotEdge,
          borderWidth: ring ? 2 : 0.8,
        },
      };
    }),
  };

  // Bigger clusters first, so their names win when labels collide.
  const labelled = state.layers.names
    ? [...doc.clusters].sort((a, b) => b.size - a.size)
    : [];
  const labels: SeriesOption = {
    id: "labels",
    type: "scatter",
    silent: true,
    z: 4,
    symbolSize: 0,
    data: labelled.map((c) => ({ value: [c.labelX, c.labelY], name: c.name })),
    labelLayout: { hideOverlap: true },
    label: {
      show: true,
      formatter: (p: { name: string }) => p.name,
      fontFamily: "Fraunces, Georgia, serif",
      fontStyle: "italic",
      fontWeight: 500,
      fontSize: 11,
      color: theme.ink,
      textBorderColor: theme.halo,
      textBorderWidth: 3,
    },
  };

  const at = (isin: string): [number, number] | null => {
    const p = pointsByIsin.get(isin);
    return p ? [p.x, p.y] : null;
  };
  const similarData = state.pinned
    ? (doc.neighbours[state.pinned] ?? [])
        .slice(0, SIMILAR_SHOWN)
        .map((n) => at(n.isin))
        .filter((p): p is [number, number] => p !== null)
    : [];
  const pinned = state.pinned ? at(state.pinned) : null;

  const similar: SeriesOption = {
    id: "similar",
    type: "scatter",
    silent: true,
    z: 5,
    symbolSize: 15,
    itemStyle: {
      color: "transparent",
      borderColor: theme.ink,
      borderWidth: 1.5,
    },
    label: {
      show: true,
      position: "right",
      fontSize: 10,
      color: theme.ink,
      textBorderColor: theme.halo,
      textBorderWidth: 2,
      formatter: (p: { dataIndex: number }) => String(p.dataIndex + 1),
    },
    data: similarData,
  };

  const pin: SeriesOption = {
    id: "pin",
    type: "scatter",
    silent: true,
    z: 6,
    symbolSize: 22,
    itemStyle: {
      color: "transparent",
      borderColor: theme.accent,
      borderWidth: 3,
    },
    data: pinned ? [pinned] : [],
  };

  return [territory, points, labels, similar, pin];
}

const HTML_ESCAPES: Record<string, string> = {
  "&": "&amp;",
  "<": "&lt;",
  ">": "&gt;",
  '"': "&quot;",
  "'": "&#39;",
};

function esc(text: string): string {
  return text.replace(/[&<>"']/g, (c) => HTML_ESCAPES[c]);
}

function allocationText(items: Allocation[]): string {
  return (
    items
      .map(([name, value]) => `${allocationLabel(name)} ${value.toFixed(1)}%`)
      .join(", ") || "–"
  );
}

/** Hover card for a fund on the map, as escaped HTML for the ECharts tooltip. */
export function tooltipHtml(
  point: ClusterPoint,
  fund: Fund,
  clusterName: string | null,
  dark: boolean
): string {
  const colour = verdictColour(point.verdict, dark);
  const lines = [
    `<strong>${esc(fund.name)}</strong>`,
    esc(`${fund.company}, ${fund.isin}${fund.isIndex ? ", index fund" : ""}`),
    esc(`Category: ${fund.category}`),
    esc(clusterName ? `Cluster: ${clusterName}` : "Not in a cluster"),
    `<span style="color:${colour ?? "inherit"}">${esc(VERDICTS[point.verdict])}</span>`,
    point.isolated ? "Few close neighbours" : "",
    esc(`Countries: ${allocationText(fund.topCountries)}`),
    esc(`Sectors: ${allocationText(fund.topSectors)}`),
  ];
  return `<div style="max-width:320px;white-space:normal">${lines.filter(Boolean).join("<br>")}</div>`;
}
