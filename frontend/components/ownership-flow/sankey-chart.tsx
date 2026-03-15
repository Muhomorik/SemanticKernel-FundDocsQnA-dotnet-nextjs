"use client";

import { useId, useMemo } from "react";
import { useTheme } from "next-themes";
import {
  computeSankeyLayout,
  FlowSide,
  formatOwnerCount,
  type LayoutNode,
  sankeyLinkPath,
  SANKEY_LAYOUT,
  TooltipState,
  truncateLabel,
} from "@/lib/ownership-flow";
import { SankeyEmpty } from "./sankey-empty";

const { NODE_R, HUB_RENDER_W, LABEL_GAP, SVG_W } = SANKEY_LAYOUT;

// ── Theme-aware colors ───────────────────────────────────────────────────────

const C_SHARED = {
  outNode: "oklch(0.58 0.18 25)",
  outNodeEnd: "oklch(0.52 0.16 25)",
  outLink: "oklch(0.58 0.18 25)",
  inNode: "oklch(0.58 0.13 155)",
  inNodeEnd: "oklch(0.52 0.11 155)",
  inLink: "oklch(0.58 0.13 155)",
  hubLink: "oklch(0.55 0.08 45)",
  labelDim: "oklch(0.68 0.02 80)",
  outVal: "oklch(0.72 0.18 25)",
  inVal: "oklch(0.72 0.14 155)",
} as const;

const C_DARK = {
  hub0: "oklch(0.38 0.06 45)",
  hub1: "oklch(0.30 0.05 45)",
  hubText: "oklch(0.92 0.02 50)",
  hubSubtext: "oklch(0.75 0.05 50)",
} as const;

const C_LIGHT = {
  hub0: "oklch(0.82 0.06 60)",
  hub1: "oklch(0.76 0.07 55)",
  hubText: "oklch(0.25 0.04 45)",
  hubSubtext: "oklch(0.40 0.04 50)",
} as const;

// ── Component ────────────────────────────────────────────────────────────────

interface SankeyChartProps {
  data: FlowSide;
  svgHeight: number;
  onTooltipChange: (
    updater: TooltipState | ((prev: TooltipState) => TooltipState)
  ) => void;
}

export function SankeyChart({
  data,
  svgHeight,
  onTooltipChange,
}: SankeyChartProps) {
  const uid = useId();
  const { resolvedTheme } = useTheme();
  const C = { ...C_SHARED, ...(resolvedTheme === "dark" ? C_DARK : C_LIGHT) };

  const layout = useMemo(
    () => computeSankeyLayout(data, svgHeight),
    [data, svgHeight]
  );

  if (!data.out.length && !data.in.length)
    return <SankeyEmpty variant="none" />;

  const {
    outNodes,
    inNodes,
    hubNode,
    outLinks,
    inLinks,
    totalOut,
    totalIn,
    net,
    linkOpacity,
  } = layout;

  // Hub rendering position — use d3-sankey position but render wider
  const hubX = hubNode ? (hubNode.x0! + hubNode.x1!) / 2 - HUB_RENDER_W / 2 : 0;
  const hubY = hubNode?.y0 ?? 0;
  const hubH = hubNode ? hubNode.y1! - hubNode.y0! : 0;
  const hubCx = hubNode ? (hubNode.x0! + hubNode.x1!) / 2 : 0;
  const hubCy = hubNode ? (hubNode.y0! + hubNode.y1!) / 2 : 0;

  const netLabel =
    net > 0
      ? `▲ +${formatOwnerCount(net)}`
      : net < 0
        ? `▼ ${formatOwnerCount(net)}`
        : null;
  const netColor = net > 0 ? C.inVal : C.outVal;

  function handleEnter(
    e: React.MouseEvent,
    name: string,
    value: number,
    side: "out" | "in"
  ) {
    onTooltipChange({
      visible: true,
      x: e.clientX,
      y: e.clientY,
      name,
      value,
      side,
    });
  }

  function handleMove(e: React.MouseEvent) {
    onTooltipChange((prev) => ({ ...prev, x: e.clientX, y: e.clientY }));
  }

  function handleLeave() {
    onTooltipChange((prev) => ({ ...prev, visible: false }));
  }

  return (
    <svg
      viewBox={`0 0 ${SVG_W} ${svgHeight}`}
      width="100%"
      preserveAspectRatio="xMidYMid meet"
      role="img"
      aria-label="Ownership flow Sankey diagram"
      style={{ overflow: "visible" }}
    >
      <defs>
        {/* Hub gradient */}
        <linearGradient id={`${uid}-hub`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={C.hub0} />
          <stop offset="100%" stopColor={C.hub1} />
        </linearGradient>

        {/* Out node gradients */}
        {outNodes.map((_, i) => (
          <linearGradient
            key={i}
            id={`${uid}-gO${i}`}
            x1="0"
            y1="0"
            x2="0"
            y2="1"
          >
            <stop offset="0%" stopColor={C.outNode} />
            <stop offset="100%" stopColor={C.outNodeEnd} />
          </linearGradient>
        ))}

        {/* In node gradients */}
        {inNodes.map((_, i) => (
          <linearGradient
            key={i}
            id={`${uid}-gI${i}`}
            x1="0"
            y1="0"
            x2="0"
            y2="1"
          >
            <stop offset="0%" stopColor={C.inNode} />
            <stop offset="100%" stopColor={C.inNodeEnd} />
          </linearGradient>
        ))}

        {/* Out link gradients */}
        {outLinks.map((_, i) => (
          <linearGradient
            key={i}
            id={`${uid}-lgO${i}`}
            x1="0"
            y1="0"
            x2="1"
            y2="0"
          >
            <stop offset="0%" stopColor={C.outLink} stopOpacity={linkOpacity} />
            <stop
              offset="100%"
              stopColor={C.hubLink}
              stopOpacity={linkOpacity}
            />
          </linearGradient>
        ))}

        {/* In link gradients */}
        {inLinks.map((_, i) => (
          <linearGradient
            key={i}
            id={`${uid}-lgI${i}`}
            x1="0"
            y1="0"
            x2="1"
            y2="0"
          >
            <stop offset="0%" stopColor={C.hubLink} stopOpacity={linkOpacity} />
            <stop
              offset="100%"
              stopColor={C.inLink}
              stopOpacity={linkOpacity}
            />
          </linearGradient>
        ))}
      </defs>

      {/* Outflow links (node → hub) */}
      {outLinks.map((link, i) => (
        <path
          key={i}
          d={sankeyLinkPath(link) || ""}
          stroke={`url(#${uid}-lgO${i})`}
          strokeWidth={Math.max(1, link.width ?? 0)}
          fill="none"
          className="cursor-pointer transition-opacity hover:opacity-100"
          style={{ opacity: linkOpacity }}
          onMouseEnter={(e) =>
            handleEnter(e, link.displayName, link.value ?? 0, "out")
          }
          onMouseMove={handleMove}
          onMouseLeave={handleLeave}
          aria-label={`${link.displayName}: ${formatOwnerCount(link.value ?? 0)} investors left`}
        />
      ))}

      {/* Inflow links (hub → node) */}
      {inLinks.map((link, i) => (
        <path
          key={i}
          d={sankeyLinkPath(link) || ""}
          stroke={`url(#${uid}-lgI${i})`}
          strokeWidth={Math.max(1, link.width ?? 0)}
          fill="none"
          className="cursor-pointer transition-opacity hover:opacity-100"
          style={{ opacity: linkOpacity }}
          onMouseEnter={(e) =>
            handleEnter(e, link.displayName, link.value ?? 0, "in")
          }
          onMouseMove={handleMove}
          onMouseLeave={handleLeave}
          aria-label={`${link.displayName}: ${formatOwnerCount(link.value ?? 0)} investors joined`}
        />
      ))}

      {/* Hub rectangle (wider than d3-sankey node for visual balance) */}
      {hubNode && (
        <rect
          x={hubX}
          y={hubY}
          width={HUB_RENDER_W}
          height={hubH}
          rx={4}
          fill={`url(#${uid}-hub)`}
        />
      )}

      {/* Hub labels */}
      {hubNode && (
        <>
          <text
            x={hubCx}
            y={hubCy - (netLabel ? 20 : 10)}
            textAnchor="middle"
            fontSize={11}
            fontWeight={600}
            fontFamily="DM Sans, sans-serif"
            letterSpacing="0.03em"
            fill={C.hubText}
          >
            Investor
          </text>
          <text
            x={hubCx}
            y={hubCy - (netLabel ? 6 : -4)}
            textAnchor="middle"
            fontSize={11}
            fontWeight={600}
            fontFamily="DM Sans, sans-serif"
            letterSpacing="0.03em"
            fill={C.hubText}
          >
            Pool
          </text>
          <text
            x={hubCx}
            y={hubCy + 10}
            textAnchor="middle"
            fontSize={10}
            fontFamily="DM Sans, sans-serif"
            fill={C.hubSubtext}
          >
            {`-${formatOwnerCount(totalOut)}`}
          </text>
          <text
            x={hubCx}
            y={hubCy + 22}
            textAnchor="middle"
            fontSize={10}
            fontFamily="DM Sans, sans-serif"
            fill={C.hubSubtext}
          >
            {`+${formatOwnerCount(totalIn)}`}
          </text>
          {netLabel && (
            <text
              x={hubCx}
              y={hubCy + 38}
              textAnchor="middle"
              fontSize={10}
              fontWeight={600}
              fontFamily="DM Sans, sans-serif"
              fill={netColor}
            >
              {netLabel}
            </text>
          )}
        </>
      )}

      {/* Outflow column header */}
      {outNodes.length > 0 && (
        <text
          x={outNodes[0].x0! - LABEL_GAP}
          y={(outNodes[0].y0 ?? 0) - 14}
          textAnchor="end"
          fontSize={10}
          fontWeight={500}
          fontFamily="DM Sans, sans-serif"
          letterSpacing="0.04em"
          fill={C.outVal}
        >
          OUTFLOWS
        </text>
      )}

      {/* Inflow column header */}
      {inNodes.length > 0 && (
        <text
          x={inNodes[0].x1! + LABEL_GAP}
          y={(inNodes[0].y0 ?? 0) - 14}
          textAnchor="start"
          fontSize={10}
          fontWeight={500}
          fontFamily="DM Sans, sans-serif"
          letterSpacing="0.04em"
          fill={C.inVal}
        >
          INFLOWS
        </text>
      )}

      {/* One-sided info notes — centered in the empty column area */}
      {!data.out.length && (
        <text
          x={SANKEY_LAYOUT.MARGIN.left / 2}
          y={hubCy}
          textAnchor="middle"
          fontSize={11}
          fontFamily="DM Sans, sans-serif"
          fill={C.labelDim}
        >
          No net outflows
        </text>
      )}
      {!data.in.length && (
        <text
          x={SVG_W - SANKEY_LAYOUT.MARGIN.right / 2}
          y={hubCy}
          textAnchor="middle"
          fontSize={11}
          fontFamily="DM Sans, sans-serif"
          fill={C.labelDim}
        >
          No net inflows
        </text>
      )}

      {/* Outflow nodes + labels */}
      {outNodes.map((node: LayoutNode, i: number) => {
        const midY = (node.y0! + node.y1!) / 2;
        return (
          <g key={node.id}>
            <rect
              x={node.x0}
              y={node.y0}
              width={node.x1! - node.x0!}
              height={node.y1! - node.y0!}
              rx={NODE_R}
              fill={`url(#${uid}-gO${i})`}
            />
            <text
              x={node.x0! - LABEL_GAP}
              y={midY - 5}
              textAnchor="end"
              fontSize={11}
              fontFamily="DM Sans, sans-serif"
              fill={C.labelDim}
            >
              {truncateLabel(node.displayName)}
            </text>
            <text
              x={node.x0! - LABEL_GAP}
              y={midY + 8}
              textAnchor="end"
              fontSize={11}
              fontWeight={600}
              fontFamily="DM Sans, sans-serif"
              fill={C.outVal}
            >
              {`${node.pct.toFixed(1)}%`}
            </text>
          </g>
        );
      })}

      {/* Inflow nodes + labels */}
      {inNodes.map((node: LayoutNode, i: number) => {
        const midY = (node.y0! + node.y1!) / 2;
        return (
          <g key={node.id}>
            <rect
              x={node.x0}
              y={node.y0}
              width={node.x1! - node.x0!}
              height={node.y1! - node.y0!}
              rx={NODE_R}
              fill={`url(#${uid}-gI${i})`}
            />
            <text
              x={node.x1! + LABEL_GAP}
              y={midY - 5}
              textAnchor="start"
              fontSize={11}
              fontFamily="DM Sans, sans-serif"
              fill={C.labelDim}
            >
              {truncateLabel(node.displayName)}
            </text>
            <text
              x={node.x1! + LABEL_GAP}
              y={midY + 8}
              textAnchor="start"
              fontSize={11}
              fontWeight={600}
              fontFamily="DM Sans, sans-serif"
              fill={C.inVal}
            >
              {`+${node.pct.toFixed(1)}%`}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
