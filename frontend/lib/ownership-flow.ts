import { ApiError } from "@/lib/api";
import {
  sankey,
  sankeyCenter,
  sankeyLinkHorizontal,
  type SankeyNode,
  type SankeyLink,
} from "d3-sankey";

// ── API types (mirror backend DTOs) ────────────────────────────────────────

export interface OwnershipPeriod {
  label: string;
  from: string; // "YYYY-MM-DD"
  to: string;
}

export interface OwnershipPeriodsResponse {
  weekly: OwnershipPeriod[];
  monthly: OwnershipPeriod[];
}

export interface FlowNode {
  name: string;
  value: number; // absolute owner delta (always positive)
  pct: number; // signed: negative for outflows, positive for inflows
}

export interface FlowSide {
  out: FlowNode[];
  in: FlowNode[];
}

export interface OwnershipFlowResponse {
  periodLabel: string;
  cat: FlowSide;
  fund: FlowSide;
}

// ── UI state types ──────────────────────────────────────────────────────────

export type PeriodGroup = "weekly" | "monthly";

export interface SelectedPeriod {
  group: PeriodGroup;
  index: number;
  period: OwnershipPeriod;
}

export interface TooltipState {
  visible: boolean;
  x: number;
  y: number;
  name: string;
  value: number;
  side: "out" | "in";
}

// ── d3-sankey layout types ─────────────────────────────────────────────────

/** Extra properties on each node (beyond what d3-sankey adds). */
export interface NodeExtra {
  id: string;
  displayName: string;
  side: "out" | "hub" | "in";
  pct: number;
}

/** Extra properties on each link (beyond what d3-sankey adds). */
export interface LinkExtra {
  side: "out" | "in" | "phantom";
  displayName: string;
  pct: number;
}

/** A positioned node after d3-sankey layout. */
export type LayoutNode = SankeyNode<NodeExtra, LinkExtra>;

/** A positioned link after d3-sankey layout. */
export type LayoutLink = SankeyLink<NodeExtra, LinkExtra>;

/** Result of computing the sankey layout. */
export interface SankeyLayoutResult {
  nodes: LayoutNode[];
  links: LayoutLink[];
  outNodes: LayoutNode[];
  inNodes: LayoutNode[];
  hubNode: LayoutNode | undefined;
  outLinks: LayoutLink[];
  inLinks: LayoutLink[];
  totalOut: number;
  totalIn: number;
  net: number;
  linkOpacity: number;
}

// ── Layout constants ───────────────────────────────────────────────────────

export const SANKEY_LAYOUT = {
  SVG_W: 1200,
  MARGIN: { left: 260, right: 260, top: 40, bottom: 24 },
  NODE_W: 14,
  NODE_PAD: 8,
  NODE_R: 3,
  HUB_RENDER_W: 90,
  LABEL_GAP: 10,
  LINK_OPACITY_FEW: 0.85,
  LINK_OPACITY_MANY: 0.7,
  LINK_OPACITY_THRESHOLD: 6,
} as const;

// ── Build d3-sankey graph from API data ────────────────────────────────────

export function buildSankeyGraph(data: FlowSide): {
  nodes: NodeExtra[];
  links: (LinkExtra & { source: string; target: string; value: number })[];
} {
  const nodes: NodeExtra[] = [];
  const ids = new Set<string>();

  function addNode(
    id: string,
    displayName: string,
    side: "out" | "hub" | "in",
    pct: number
  ) {
    if (ids.has(id)) return;
    ids.add(id);
    nodes.push({ id, displayName, side, pct });
  }

  // Left column: outflow sources
  data.out.forEach((d) => addNode("out_" + d.name, d.name, "out", d.pct));

  // Center: hub
  addNode("hub", "Investor Pool", "hub", 0);

  // Right column: inflow destinations
  data.in.forEach((d) => addNode("in_" + d.name, d.name, "in", d.pct));

  // Links: out → hub, hub → in
  const links: (LinkExtra & {
    source: string;
    target: string;
    value: number;
  })[] = [];

  data.out.forEach((d) => {
    links.push({
      source: "out_" + d.name,
      target: "hub",
      value: d.value,
      side: "out",
      displayName: d.name,
      pct: d.pct,
    });
  });

  data.in.forEach((d) => {
    links.push({
      source: "hub",
      target: "in_" + d.name,
      value: d.value,
      side: "in",
      displayName: d.name,
      pct: d.pct,
    });
  });

  // Phantom links for empty sides (keeps hub positioned)
  if (data.in.length === 0) {
    addNode("in_phantom", "", "in", 0);
    links.push({
      source: "hub",
      target: "in_phantom",
      value: 1,
      side: "phantom",
      displayName: "",
      pct: 0,
    });
  }
  if (data.out.length === 0) {
    addNode("out_phantom", "", "out", 0);
    links.push({
      source: "out_phantom",
      target: "hub",
      value: 1,
      side: "phantom",
      displayName: "",
      pct: 0,
    });
  }

  return { nodes, links };
}

// ── Compute d3-sankey layout ───────────────────────────────────────────────

export function computeSankeyLayout(
  data: FlowSide,
  svgHeight: number
): SankeyLayoutResult {
  const {
    SVG_W,
    MARGIN,
    NODE_W,
    NODE_PAD,
    LINK_OPACITY_FEW,
    LINK_OPACITY_MANY,
    LINK_OPACITY_THRESHOLD,
  } = SANKEY_LAYOUT;

  const graph = buildSankeyGraph(data);

  const sankeyGen = sankey<NodeExtra, LinkExtra>()
    .nodeId((d: NodeExtra) => d.id)
    .nodeWidth(NODE_W)
    .nodePadding(NODE_PAD)
    .nodeAlign(sankeyCenter)
    .extent([
      [MARGIN.left, MARGIN.top],
      [SVG_W - MARGIN.right, svgHeight - MARGIN.bottom],
    ]);

  // d3-sankey mutates input — clone first
  const result = sankeyGen({
    nodes: graph.nodes.map((n) => ({ ...n })),
    links: graph.links.map((l) => ({ ...l })),
  });

  const nodes = result.nodes as LayoutNode[];
  const links = result.links as LayoutLink[];

  const outNodes = nodes.filter(
    (n) => n.side === "out" && n.id !== "out_phantom"
  );
  const inNodes = nodes.filter((n) => n.side === "in" && n.id !== "in_phantom");
  const hubNode = nodes.find((n) => n.side === "hub");
  const outLinks = links.filter((l) => l.side === "out");
  const inLinks = links.filter((l) => l.side === "in");

  const totalOut = data.out.reduce((s, n) => s + n.value, 0);
  const totalIn = data.in.reduce((s, n) => s + n.value, 0);
  const net = totalIn - totalOut;
  const maxSide = Math.max(data.out.length, data.in.length);
  const linkOpacity =
    maxSide > LINK_OPACITY_THRESHOLD ? LINK_OPACITY_MANY : LINK_OPACITY_FEW;

  return {
    nodes,
    links,
    outNodes,
    inNodes,
    hubNode,
    outLinks,
    inLinks,
    totalOut,
    totalIn,
    net,
    linkOpacity,
  };
}

// ── Link path generator (singleton) ────────────────────────────────────────

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const sankeyLinkPath = sankeyLinkHorizontal<any, any>();

// ── Formatting helpers ──────────────────────────────────────────────────────

export function formatOwnerCount(n: number): string {
  return n.toLocaleString("en-US");
}

export function truncateLabel(s: string, max = 32): string {
  return s.length > max ? s.slice(0, max - 1) + "\u2026" : s;
}

// ── API functions ───────────────────────────────────────────────────────────

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export async function fetchOwnershipPeriods(): Promise<OwnershipPeriodsResponse> {
  try {
    const res = await fetch(`${API_URL}/api/ownership-flow/periods`);
    if (!res.ok) {
      const text = await res.text();
      throw new ApiError(
        `Periods fetch failed: ${res.statusText}`,
        res.status,
        text
      );
    }
    return res.json() as Promise<OwnershipPeriodsResponse>;
  } catch (err) {
    if (err instanceof ApiError) throw err;
    throw new ApiError(
      err instanceof Error ? err.message : "Unknown error fetching periods",
      undefined,
      err
    );
  }
}

export async function fetchOwnershipFlow(
  from: string,
  to: string
): Promise<OwnershipFlowResponse> {
  try {
    const url = new URL(`${API_URL}/api/ownership-flow`);
    url.searchParams.set("from", from);
    url.searchParams.set("to", to);
    const res = await fetch(url.toString());
    if (!res.ok) {
      const text = await res.text();
      throw new ApiError(
        `Flow fetch failed: ${res.statusText}`,
        res.status,
        text
      );
    }
    return res.json() as Promise<OwnershipFlowResponse>;
  } catch (err) {
    if (err instanceof ApiError) throw err;
    throw new ApiError(
      err instanceof Error ? err.message : "Unknown error fetching flow",
      undefined,
      err
    );
  }
}
