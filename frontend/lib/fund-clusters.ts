import { ApiError } from "@/lib/api";

// ── API types (mirror FundClustering/README.md output schema) ──────────────

export interface MethodEntry {
  id: string;
  name: string;
  file: string;
}

export interface MethodsManifest {
  week: string;
  default: string;
  methods: MethodEntry[];
}

export type FamilyId =
  "nordic" | "europe" | "usa" | "global" | "em" | "asia" | "sector" | "other";

/** `[name, percent]`, largest first. */
export type Allocation = [string, number];

export interface Fund {
  isin: string;
  name: string;
  company: string;
  category: string;
  family: FamilyId;
  shade: number;
  isIndex: boolean;
  topCountries: Allocation[];
  topSectors: Allocation[];
}

export interface FundsFile {
  week: string;
  funds: Fund[];
}

export type Verdict = "agree" | "neighbouring" | "different" | "none";

export interface ClusterPoint {
  isin: string;
  x: number;
  y: number;
  cluster: number;
  verdict: Verdict;
  isolated: boolean;
}

export interface Cluster {
  id: number;
  name: string;
  size: number;
  purity: number;
  leadingCategory: string;
  labelX: number;
  labelY: number;
  categories: Record<string, number>;
}

export interface CategoryStat {
  category: string;
  family: FamilyId;
  funds: number;
  agreeShare: number | null;
}

export type Ring = [number, number][];

export interface TerritoryPart {
  cluster: number;
  rings: Ring[];
}

export interface Neighbour {
  isin: string;
  distance: number;
}

export interface MethodSummary {
  funds: number;
  clusters: number;
  notInCluster: number;
  agree: number;
  neighbouring: number;
  different: number;
  ari: number | null;
  nmi: number | null;
}

export interface MethodFile {
  week: string;
  method: string;
  name: string;
  description: string;
  summary: MethodSummary;
  points: ClusterPoint[];
  clusters: Cluster[];
  categories: CategoryStat[];
  territory: TerritoryPart[];
  neighbours: Record<string, Neighbour[]>;
}

// ── Constants ───────────────────────────────────────────────────────────────

export const NOT_IN_CLUSTER = -1;
export const SIMILAR_SHOWN = 10;

export const FAMILIES: Record<
  FamilyId,
  { name: string; hue: number; grey?: boolean }
> = {
  nordic: { name: "Nordics", hue: 205 },
  europe: { name: "Europe", hue: 140 },
  usa: { name: "USA", hue: 268 },
  global: { name: "Global", hue: 24 },
  em: { name: "Emerging markets", hue: 178 },
  asia: { name: "Asia & Japan", hue: 328 },
  sector: { name: "Sector funds", hue: 48 },
  other: { name: "Other", hue: 0, grey: true },
};

export const VERDICTS: Record<Verdict, string> = {
  agree: "Agrees with its category",
  neighbouring: "Neighbouring category (same region family)",
  different: "Different family",
  none: "Not in a cluster",
};

// ── Helpers ─────────────────────────────────────────────────────────────────

export type FundSymbol = "circle" | "diamond" | "rect";

/**
 * Colour of a fund's family. `shade` (the category's position in its family)
 * nudges hue and lightness so region variants of one family stay apart.
 */
export function familyColour(
  family: string,
  shade: number,
  dark: boolean,
  alpha = 1
): string {
  const f = FAMILIES[family as FamilyId] ?? FAMILIES.other;
  const hue = f.hue + ((shade % 3) - 1) * 8;
  const saturation = f.grey ? 0 : 62;
  const lightness = (dark ? 52 : 38) + (shade % 5) * 7;
  return `hsla(${hue}, ${saturation}%, ${lightness}%, ${alpha})`;
}

export function symbolFor(family: FamilyId): FundSymbol {
  if (family === "sector") return "diamond";
  if (family === "other") return "rect";
  return "circle";
}

export function isDisagreement(point: ClusterPoint): boolean {
  return point.verdict === "neighbouring" || point.verdict === "different";
}

/** Disagreeing funds, different family first, then neighbouring. */
export function disagreements(points: ClusterPoint[]): ClusterPoint[] {
  const order: Partial<Record<Verdict, number>> = {
    different: 0,
    neighbouring: 1,
  };
  return points
    .filter(isDisagreement)
    .sort((a, b) => order[a.verdict]! - order[b.verdict]!);
}

export function signedArea(ring: Ring): number {
  let area = 0;
  for (let i = 0; i < ring.length - 1; i++) {
    area += ring[i][0] * ring[i + 1][1] - ring[i + 1][0] * ring[i][1];
  }
  return area / 2;
}

/**
 * Outer ring counter-clockwise, holes clockwise, so the default non-zero
 * fill leaves the holes empty.
 */
export function orientRings(rings: Ring[]): Ring[] {
  return rings.map((ring, i) => {
    const wantPositive = i === 0;
    return signedArea(ring) > 0 === wantPositive ? ring : [...ring].reverse();
  });
}

export function mapExtent(points: ClusterPoint[]): {
  xMax: number;
  yMax: number;
} {
  let xMax = 0;
  let yMax = 0;
  for (const p of points) {
    xMax = Math.max(xMax, p.x);
    yMax = Math.max(yMax, p.y);
  }
  return { xMax, yMax };
}

export function searchFunds(funds: Fund[], query: string, limit = 8): Fund[] {
  const q = query.trim().toLowerCase();
  if (q.length < 2) return [];
  return funds
    .filter(
      (f) =>
        f.name.toLowerCase().includes(q) || f.isin.toLowerCase().includes(q)
    )
    .slice(0, limit);
}

/** Category → its family and shade, taken from the first fund in it. */
export function categoryShades(
  funds: Fund[]
): Map<string, { family: FamilyId; shade: number }> {
  const result = new Map<string, { family: FamilyId; shade: number }>();
  for (const f of funds) {
    if (!result.has(f.category)) {
      result.set(f.category, { family: f.family, shade: f.shade });
    }
  }
  return result;
}

export function formatShare(share: number | null): string {
  return share == null ? "–" : `${Math.round(share * 100)}%`;
}

export function allocationLabel(name: string): string {
  const text = name.replace(/_/g, " ");
  return text.charAt(0).toUpperCase() + text.slice(1);
}

// ── API functions ───────────────────────────────────────────────────────────

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

async function fetchJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  try {
    const res = await fetch(`${API_URL}/api/fund-clusters/${path}`, { signal });
    if (!res.ok) {
      const text = await res.text();
      throw new ApiError(
        `Fund cluster fetch failed: ${res.statusText}`,
        res.status,
        text
      );
    }
    return res.json() as Promise<T>;
  } catch (err) {
    if (err instanceof ApiError) throw err;
    throw new ApiError(
      err instanceof Error
        ? err.message
        : "Unknown error fetching fund clusters",
      undefined,
      err
    );
  }
}

export function fetchClusterMethods(
  signal?: AbortSignal
): Promise<MethodsManifest> {
  return fetchJson<MethodsManifest>("methods", signal);
}

export function fetchClusterFunds(signal?: AbortSignal): Promise<FundsFile> {
  return fetchJson<FundsFile>("funds", signal);
}

export function fetchClusterMethod(
  id: string,
  signal?: AbortSignal
): Promise<MethodFile> {
  return fetchJson<MethodFile>(`methods/${encodeURIComponent(id)}`, signal);
}

// ── UI state types ──────────────────────────────────────────────────────────

export type MapView = "all" | "disagree";

export interface MapLayers {
  territory: boolean;
  names: boolean;
  rings: boolean;
}

export type Isolate =
  | { type: "category"; value: string }
  | { type: "cluster"; value: number }
  | null;

export type SidebarTab = "categories" | "clusters" | "disagreements";

/** Ring colour for a disagreement verdict, or null when no ring is drawn. */
export function verdictColour(verdict: Verdict, dark: boolean): string | null {
  if (verdict === "neighbouring") return dark ? "#e0a12a" : "#c58300";
  if (verdict === "different") return dark ? "#ec5b5f" : "#d0353a";
  return null;
}
