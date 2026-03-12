import { ApiError } from "@/lib/api";

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
  pct: number;   // signed: negative for outflows, positive for inflows
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

// ── Layout computation types ────────────────────────────────────────────────

export interface PositionedNode extends FlowNode {
  y: number; // top Y in SVG coordinate space
  h: number; // height in SVG coordinate space (min MIN_NODE_H)
}

export interface HubSlice {
  y1: number;
  y2: number;
}

export interface SankeyLayout {
  outNodes: PositionedNode[];
  inNodes: PositionedNode[];
  totalOut: number;
  totalIn: number;
  net: number;
  hubY: number;
  hubH: number;
  outHubSlices: HubSlice[];
  inHubSlices: HubSlice[];
  linkOpacity: number;
}

// ── SVG layout constants (from sankey-mockup.html) ──────────────────────────

export const SANKEY_CONSTANTS = {
  W: 1200,
  PAD_T: 40,
  PAD_B: 24,
  NODE_W: 12,
  NODE_GAP: 5,
  NODE_R: 3,
  MIN_NODE_H: 6,
  LEFT_LABEL_END: 252,
  LEFT_NODE_X: 258,
  HUB_X: 555,
  HUB_W: 90,
  RIGHT_NODE_X: 930,
  RIGHT_LABEL_START: 952,
  LINK_OPACITY_FEW: 0.85,
  LINK_OPACITY_MANY: 0.7,
  LINK_OPACITY_THRESHOLD: 6,
} as const;

// ── Pure layout computation ─────────────────────────────────────────────────

export function computeSankeyLayout(
  data: FlowSide,
  svgHeight: number,
): SankeyLayout {
  const { PAD_T, PAD_B, NODE_GAP, MIN_NODE_H, LINK_OPACITY_FEW, LINK_OPACITY_MANY, LINK_OPACITY_THRESHOLD } =
    SANKEY_CONSTANTS;

  const totalOut = data.out.reduce((s, n) => s + n.value, 0);
  const totalIn = data.in.reduce((s, n) => s + n.value, 0);
  const net = totalIn - totalOut;
  const availH = svgHeight - PAD_T - PAD_B;
  const hubY = PAD_T;
  const hubH = availH;

  function positionNodes(nodes: FlowNode[], total: number): PositionedNode[] {
    if (nodes.length === 0) return [];
    const gaps = (nodes.length - 1) * NODE_GAP;
    const scale = total > 0 ? (availH - gaps) / total : 0;
    let y = PAD_T;
    return nodes.map((node) => {
      const h = Math.max(node.value * scale, MIN_NODE_H);
      const positioned = { ...node, y, h };
      y += h + NODE_GAP;
      return positioned;
    });
  }

  function hubSlices(nodes: FlowNode[], total: number): HubSlice[] {
    if (nodes.length === 0 || total === 0) return [];
    let y = hubY;
    return nodes.map((node) => {
      const bh = (node.value / total) * hubH;
      const slice = { y1: y, y2: y + bh };
      y += bh;
      return slice;
    });
  }

  const outNodes = positionNodes(data.out, totalOut);
  const inNodes = positionNodes(data.in, totalIn);
  const outHubSlices = hubSlices(data.out, totalOut);
  const inHubSlices = hubSlices(data.in, totalIn);
  const maxSide = Math.max(data.out.length, data.in.length);
  const linkOpacity = maxSide > LINK_OPACITY_THRESHOLD ? LINK_OPACITY_MANY : LINK_OPACITY_FEW;

  return {
    outNodes,
    inNodes,
    totalOut,
    totalIn,
    net,
    hubY,
    hubH,
    outHubSlices,
    inHubSlices,
    linkOpacity,
  };
}

export function buildLinkPath(
  sx: number,
  sy1: number,
  sy2: number,
  tx: number,
  ty1: number,
  ty2: number,
): string {
  const dx = tx - sx;
  const cp = dx * 0.55;
  return (
    `M${sx},${sy1} C${sx + cp},${sy1} ${tx - cp},${ty1} ${tx},${ty1}` +
    ` L${tx},${ty2} C${tx - cp},${ty2} ${sx + cp},${sy2} ${sx},${sy2} Z`
  );
}

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
        text,
      );
    }
    return res.json() as Promise<OwnershipPeriodsResponse>;
  } catch (err) {
    if (err instanceof ApiError) throw err;
    throw new ApiError(
      err instanceof Error ? err.message : "Unknown error fetching periods",
      undefined,
      err,
    );
  }
}

export async function fetchOwnershipFlow(
  from: string,
  to: string,
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
        text,
      );
    }
    return res.json() as Promise<OwnershipFlowResponse>;
  } catch (err) {
    if (err instanceof ApiError) throw err;
    throw new ApiError(
      err instanceof Error ? err.message : "Unknown error fetching flow",
      undefined,
      err,
    );
  }
}
