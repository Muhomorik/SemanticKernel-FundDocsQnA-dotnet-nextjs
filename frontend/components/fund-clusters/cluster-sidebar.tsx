"use client";

import { useId, useMemo } from "react";
import { X } from "lucide-react";
import { useTheme } from "next-themes";
import {
  Allocation,
  categoryShades,
  disagreements,
  familyColour,
  formatShare,
  Fund,
  Isolate,
  MethodFile,
  SidebarTab,
  SIMILAR_SHOWN,
  symbolFor,
  VERDICTS,
  verdictColour,
  allocationLabel,
  FamilyId,
} from "@/lib/fund-clusters";

export interface ClusterSidebarProps {
  doc: MethodFile;
  funds: Fund[];
  tab: SidebarTab;
  onTabChange: (tab: SidebarTab) => void;
  isolate: Isolate;
  onIsolate: (isolate: Isolate) => void;
  pinned: string | null;
  onPin: (isin: string | null) => void;
}

const TABS: { id: SidebarTab; label: string; hint: string }[] = [
  {
    id: "categories",
    label: "Categories",
    hint: "Share of each category's clustered funds that agree. Select one to highlight it.",
  },
  {
    id: "clusters",
    label: "Clusters",
    hint: "Clusters by size, with their largest categories. Select one to highlight it.",
  },
  {
    id: "disagreements",
    label: "Disagreements",
    hint: "Funds placed in a cluster led by another category. Select one to find it on the map.",
  },
];

// ── Small building blocks ────────────────────────────────────────────────────

function Swatch({
  family,
  shade,
  dark,
}: {
  family: FamilyId;
  shade: number;
  dark: boolean;
}) {
  const shape = {
    circle: "rounded-full",
    diamond: "rotate-45 scale-[0.85] rounded-[1px]",
    rect: "rounded-[1px]",
  }[symbolFor(family)];
  return (
    <span
      aria-hidden="true"
      className={`inline-block h-2.5 w-2.5 flex-none ${shape}`}
      style={{ background: familyColour(family, shade, dark) }}
    />
  );
}

function Row({
  swatch,
  name,
  num,
  detail,
  share,
  active,
  onClick,
}: {
  swatch: React.ReactNode;
  name: string;
  num?: string;
  detail?: string;
  share?: number | null;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <li>
      <button
        type="button"
        aria-pressed={active}
        onClick={onClick}
        className="hover:bg-accent/60 aria-pressed:bg-warm/10 grid w-full grid-cols-[12px_minmax(0,1fr)_auto] items-center gap-x-2 gap-y-1 px-4 py-1.5 text-left text-sm transition-colors aria-pressed:shadow-[inset_3px_0_var(--warm)]"
      >
        {swatch}
        <span className="truncate">{name}</span>
        <span className="text-muted-foreground text-xs tabular-nums">
          {num}
        </span>
        {detail && (
          <span className="text-muted-foreground col-start-2 col-end-4 text-xs">
            {detail}
          </span>
        )}
        {share !== undefined && (
          <span className="bg-border col-start-2 col-end-4 h-1 overflow-hidden rounded-full">
            <span
              className="bg-warm/70 block h-full"
              style={{ width: `${(share ?? 0) * 100}%` }}
            />
          </span>
        )}
      </button>
    </li>
  );
}

function allocations(items: Allocation[]): string {
  return (
    items
      .map(([name, value]) => `${allocationLabel(name)} ${value.toFixed(1)}%`)
      .join(", ") || "–"
  );
}

// ── Pinned fund ──────────────────────────────────────────────────────────────

function PinnedFund({
  isin,
  doc,
  fundsByIsin,
  dark,
  onPin,
}: {
  isin: string;
  doc: MethodFile;
  fundsByIsin: Map<string, Fund>;
  dark: boolean;
  onPin: (isin: string | null) => void;
}) {
  const headingId = useId();
  const fund = fundsByIsin.get(isin);
  const point = doc.points.find((p) => p.isin === isin);
  if (!fund || !point) return null;

  const cluster = doc.clusters.find((c) => c.id === point.cluster);
  const similar = (doc.neighbours[isin] ?? []).slice(0, SIMILAR_SHOWN);
  const ring = verdictColour(point.verdict, dark);

  return (
    <section
      aria-labelledby={headingId}
      className="border-border/60 max-h-[50%] shrink-0 overflow-auto border-b px-4 py-3.5"
    >
      <div className="flex items-start justify-between gap-2">
        <h2 id={headingId} className="font-serif text-base font-medium">
          {fund.name}
        </h2>
        <button
          type="button"
          aria-label="Clear selection"
          onClick={() => onPin(null)}
          className="text-muted-foreground hover:text-foreground -mt-0.5 -mr-1 rounded p-1"
        >
          <X className="h-4 w-4" />
        </button>
      </div>
      <dl className="text-muted-foreground mt-1 space-y-0.5 text-xs leading-relaxed">
        <dd>
          {fund.company}, {fund.isin}
          {fund.isIndex ? ", index fund" : ""}
        </dd>
        <dd>Category: {fund.category}</dd>
        <dd>
          Cluster:{" "}
          {cluster
            ? `${cluster.name} (${cluster.size} funds, ${formatShare(cluster.purity)} pure)`
            : "Not in a cluster"}
        </dd>
        <dd style={ring ? { color: ring } : undefined}>
          {VERDICTS[point.verdict]}
        </dd>
        {point.isolated && <dd>Few close neighbours</dd>}
        <dd>Countries: {allocations(fund.topCountries)}</dd>
        <dd>Sectors: {allocations(fund.topSectors)}</dd>
      </dl>

      <h3 className="text-muted-foreground mt-3 mb-1 text-xs font-medium">
        Most similar funds, numbered on the map
      </h3>
      <ol aria-label="Most similar funds" className="-mx-4">
        {similar.map((n, i) => {
          const other = fundsByIsin.get(n.isin);
          if (!other) return null;
          return (
            <li key={n.isin}>
              <button
                type="button"
                onClick={() => onPin(n.isin)}
                className="hover:bg-accent/60 grid w-full grid-cols-[1.25rem_12px_minmax(0,1fr)_auto] items-center gap-x-2 px-4 py-1 text-left text-sm"
              >
                <span className="text-muted-foreground text-xs tabular-nums">
                  {i + 1}
                </span>
                <Swatch family={other.family} shade={other.shade} dark={dark} />
                <span className="truncate">{other.name}</span>
                <span className="text-muted-foreground text-xs tabular-nums">
                  {n.distance.toFixed(3)}
                </span>
                <span className="text-muted-foreground col-start-3 col-end-5 truncate text-xs">
                  {other.category}
                </span>
              </button>
            </li>
          );
        })}
      </ol>
    </section>
  );
}

// ── Sidebar ──────────────────────────────────────────────────────────────────

export function ClusterSidebar({
  doc,
  funds,
  tab,
  onTabChange,
  isolate,
  onIsolate,
  pinned,
  onPin,
}: ClusterSidebarProps) {
  const panelId = useId();
  const dark = useTheme().resolvedTheme === "dark";
  const fundsByIsin = useMemo(
    () => new Map(funds.map((f) => [f.isin, f])),
    [funds]
  );
  const shades = useMemo(() => categoryShades(funds), [funds]);
  const clusterNames = useMemo(
    () => new Map(doc.clusters.map((c) => [c.id, c.name])),
    [doc]
  );

  function categorySwatch(category: string) {
    const s = shades.get(category) ?? { family: "other" as const, shade: 0 };
    return <Swatch family={s.family} shade={s.shade} dark={dark} />;
  }

  function toggle(next: NonNullable<Isolate>) {
    const same = isolate?.type === next.type && isolate.value === next.value;
    onIsolate(same ? null : next);
  }

  function rows() {
    if (tab === "categories") {
      return [...doc.categories]
        .sort((a, b) => b.funds - a.funds)
        .map((c) => (
          <Row
            key={c.category}
            swatch={categorySwatch(c.category)}
            name={c.category}
            num={`${c.funds} ${c.funds === 1 ? "fund" : "funds"}, ${formatShare(c.agreeShare)}`}
            share={c.agreeShare}
            active={
              isolate?.type === "category" && isolate.value === c.category
            }
            onClick={() => toggle({ type: "category", value: c.category })}
          />
        ));
    }
    if (tab === "clusters") {
      return [...doc.clusters]
        .sort((a, b) => b.size - a.size)
        .map((c) => (
          <Row
            key={c.id}
            swatch={categorySwatch(c.leadingCategory)}
            name={c.name}
            num={`${c.size}, ${formatShare(c.purity)} pure`}
            detail={Object.entries(c.categories)
              .sort((a, b) => b[1] - a[1])
              .slice(0, 3)
              .map(([name, count]) => `${name} ${count}`)
              .join(", ")}
            active={isolate?.type === "cluster" && isolate.value === c.id}
            onClick={() => toggle({ type: "cluster", value: c.id })}
          />
        ));
    }
    return disagreements(doc.points).map((p) => {
      const fund = fundsByIsin.get(p.isin);
      if (!fund) return null;
      return (
        <Row
          key={p.isin}
          swatch={
            <span
              aria-hidden="true"
              className="inline-block h-2.5 w-2.5 rounded-full border-2"
              style={{
                borderColor: verdictColour(p.verdict, dark) ?? undefined,
              }}
            />
          }
          name={fund.name}
          detail={`${fund.category} in ${clusterNames.get(p.cluster) ?? "–"}`}
          active={pinned === p.isin}
          onClick={() => onPin(p.isin)}
        />
      );
    });
  }

  const hint = TABS.find((t) => t.id === tab)?.hint;

  return (
    <aside className="bg-card border-border/50 flex max-h-[min(78vh,820px)] min-h-[420px] flex-col overflow-hidden rounded-lg border">
      {pinned && (
        <PinnedFund
          isin={pinned}
          doc={doc}
          fundsByIsin={fundsByIsin}
          dark={dark}
          onPin={onPin}
        />
      )}
      <div role="tablist" className="border-border/60 flex border-b">
        {TABS.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            aria-controls={panelId}
            onClick={() => onTabChange(id)}
            className="text-muted-foreground aria-selected:text-foreground aria-selected:border-warm flex-1 border-b-2 border-transparent px-2 pt-2.5 pb-2 text-sm font-medium transition-colors"
          >
            {label}
          </button>
        ))}
      </div>
      <p className="text-muted-foreground px-4 pt-2 pb-1 text-xs">{hint}</p>
      <ul
        id={panelId}
        role="tabpanel"
        className="min-h-0 flex-1 overflow-auto pb-2"
      >
        {rows()}
      </ul>
    </aside>
  );
}
