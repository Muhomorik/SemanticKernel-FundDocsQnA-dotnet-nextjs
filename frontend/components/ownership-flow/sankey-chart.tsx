"use client";

import { useId, useMemo } from "react";
import { useTheme } from "next-themes";
import {
  buildLinkPath,
  computeSankeyLayout,
  FlowSide,
  formatOwnerCount,
  SANKEY_CONSTANTS,
  TooltipState,
  truncateLabel,
} from "@/lib/ownership-flow";
import { SankeyEmpty } from "./sankey-empty";

const {
  LEFT_NODE_X,
  LEFT_LABEL_END,
  HUB_X,
  HUB_W,
  NODE_W,
  NODE_R,
  RIGHT_NODE_X,
  RIGHT_LABEL_START,
} = SANKEY_CONSTANTS;

// ── Theme-aware colors ───────────────────────────────────────────────────────

const C_SHARED = {
  outNode: "oklch(0.58 0.18 25)",
  outNodeEnd: "oklch(0.52 0.16 25)",
  outLink0: "oklch(0.58 0.18 25)",
  outLink1: "oklch(0.55 0.08 45)",
  inNode: "oklch(0.58 0.13 155)",
  inNodeEnd: "oklch(0.52 0.11 155)",
  inLink0: "oklch(0.55 0.08 45)",
  inLink1: "oklch(0.58 0.13 155)",
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
  onTooltipChange: (updater: TooltipState | ((prev: TooltipState) => TooltipState)) => void;
}

export function SankeyChart({ data, svgHeight, onTooltipChange }: SankeyChartProps) {
  const uid = useId();
  const { resolvedTheme } = useTheme();
  const C = { ...C_SHARED, ...(resolvedTheme === "dark" ? C_DARK : C_LIGHT) };

  const layout = useMemo(
    () => computeSankeyLayout(data, svgHeight),
    [data, svgHeight],
  );

  if (!data.out.length && !data.in.length) return <SankeyEmpty variant="none" />;
  if (!data.out.length) return <SankeyEmpty variant="only-in" />;
  if (!data.in.length) return <SankeyEmpty variant="only-out" />;

  const { outNodes, inNodes, outHubSlices, inHubSlices, totalOut, totalIn, net, hubY, hubH, linkOpacity } = layout;

  const hubRight = HUB_X + HUB_W;
  const netLabel = net > 0 ? `▲ +${formatOwnerCount(net)}` : net < 0 ? `▼ ${formatOwnerCount(net)}` : null;
  const netColor = net > 0 ? C.inVal : C.outVal;

  function handleEnter(e: React.MouseEvent, name: string, value: number, side: "out" | "in") {
    onTooltipChange({ visible: true, x: e.clientX, y: e.clientY, name, value, side });
  }

  function handleMove(e: React.MouseEvent) {
    onTooltipChange((prev) => ({ ...prev, x: e.clientX, y: e.clientY }));
  }

  function handleLeave() {
    onTooltipChange((prev) => ({ ...prev, visible: false }));
  }

  return (
    <svg
      viewBox={`0 0 1200 ${svgHeight}`}
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
          <linearGradient key={i} id={`${uid}-gO${i}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={C.outNode} />
            <stop offset="100%" stopColor={C.outNodeEnd} />
          </linearGradient>
        ))}

        {/* In node gradients */}
        {inNodes.map((_, i) => (
          <linearGradient key={i} id={`${uid}-gI${i}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={C.inNode} />
            <stop offset="100%" stopColor={C.inNodeEnd} />
          </linearGradient>
        ))}

        {/* Out link gradients */}
        {outNodes.map((_, i) => (
          <linearGradient key={i} id={`${uid}-lgO${i}`} x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor={C.outLink0} stopOpacity={linkOpacity} />
            <stop offset="100%" stopColor={C.outLink1} stopOpacity={linkOpacity} />
          </linearGradient>
        ))}

        {/* In link gradients */}
        {inNodes.map((_, i) => (
          <linearGradient key={i} id={`${uid}-lgI${i}`} x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor={C.inLink0} stopOpacity={linkOpacity} />
            <stop offset="100%" stopColor={C.inLink1} stopOpacity={linkOpacity} />
          </linearGradient>
        ))}
      </defs>

      {/* Outflow links (node → hub) */}
      {outNodes.map((node, i) => {
        const slice = outHubSlices[i];
        const nodeMidY = node.y + node.h / 2;
        const path = buildLinkPath(
          LEFT_NODE_X + NODE_W, node.y, node.y + node.h,
          HUB_X, slice.y1, slice.y2,
        );
        return (
          <path
            key={i}
            d={path}
            fill={`url(#${uid}-lgO${i})`}
            className="cursor-pointer transition-opacity hover:opacity-100"
            style={{ opacity: linkOpacity }}
            onMouseEnter={(e) => handleEnter(e, node.name, node.value, "out")}
            onMouseMove={handleMove}
            onMouseLeave={handleLeave}
            aria-label={`${node.name}: ${formatOwnerCount(node.value)} investors left`}
          />
        );
        void nodeMidY;
      })}

      {/* Inflow links (hub → node) */}
      {inNodes.map((node, i) => {
        const slice = inHubSlices[i];
        const path = buildLinkPath(
          hubRight, slice.y1, slice.y2,
          RIGHT_NODE_X, node.y, node.y + node.h,
        );
        return (
          <path
            key={i}
            d={path}
            fill={`url(#${uid}-lgI${i})`}
            className="cursor-pointer transition-opacity hover:opacity-100"
            style={{ opacity: linkOpacity }}
            onMouseEnter={(e) => handleEnter(e, node.name, node.value, "in")}
            onMouseMove={handleMove}
            onMouseLeave={handleLeave}
            aria-label={`${node.name}: ${formatOwnerCount(node.value)} investors joined`}
          />
        );
      })}

      {/* Hub rectangle */}
      <rect
        x={HUB_X}
        y={hubY}
        width={HUB_W}
        height={hubH}
        rx={4}
        fill={`url(#${uid}-hub)`}
      />

      {/* Hub labels */}
      <text
        x={HUB_X + HUB_W / 2}
        y={hubY + hubH / 2 - (netLabel ? 20 : 10)}
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
        x={HUB_X + HUB_W / 2}
        y={hubY + hubH / 2 - (netLabel ? 6 : -4)}
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
        x={HUB_X + HUB_W / 2}
        y={hubY + hubH / 2 + 10}
        textAnchor="middle"
        fontSize={10}
        fontFamily="DM Sans, sans-serif"
        fill={C.hubSubtext}
      >
        {`-${formatOwnerCount(totalOut)}`}
      </text>
      <text
        x={HUB_X + HUB_W / 2}
        y={hubY + hubH / 2 + 22}
        textAnchor="middle"
        fontSize={10}
        fontFamily="DM Sans, sans-serif"
        fill={C.hubSubtext}
      >
        {`+${formatOwnerCount(totalIn)}`}
      </text>
      {netLabel && (
        <text
          x={HUB_X + HUB_W / 2}
          y={hubY + hubH / 2 + 38}
          textAnchor="middle"
          fontSize={10}
          fontWeight={600}
          fontFamily="DM Sans, sans-serif"
          fill={netColor}
        >
          {netLabel}
        </text>
      )}

      {/* Outflow column header */}
      <text
        x={LEFT_NODE_X - 6}
        y={hubY - 14}
        textAnchor="end"
        fontSize={10}
        fontWeight={500}
        fontFamily="DM Sans, sans-serif"
        letterSpacing="0.04em"
        fill={C.outVal}
      >
        OUTFLOWS
      </text>

      {/* Inflow column header */}
      <text
        x={RIGHT_NODE_X + NODE_W + 6}
        y={hubY - 14}
        textAnchor="start"
        fontSize={10}
        fontWeight={500}
        fontFamily="DM Sans, sans-serif"
        letterSpacing="0.04em"
        fill={C.inVal}
      >
        INFLOWS
      </text>

      {/* Outflow nodes + labels */}
      {outNodes.map((node, i) => {
        const midY = node.y + node.h / 2;
        return (
          <g key={i}>
            <rect
              x={LEFT_NODE_X}
              y={node.y}
              width={NODE_W}
              height={node.h}
              rx={NODE_R}
              fill={`url(#${uid}-gO${i})`}
            />
            <text
              x={LEFT_LABEL_END}
              y={midY - 5}
              textAnchor="end"
              fontSize={11}
              fontFamily="DM Sans, sans-serif"
              fill={C.labelDim}
            >
              {truncateLabel(node.name)}
            </text>
            <text
              x={LEFT_LABEL_END}
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
      {inNodes.map((node, i) => {
        const midY = node.y + node.h / 2;
        return (
          <g key={i}>
            <rect
              x={RIGHT_NODE_X}
              y={node.y}
              width={NODE_W}
              height={node.h}
              rx={NODE_R}
              fill={`url(#${uid}-gI${i})`}
            />
            <text
              x={RIGHT_LABEL_START}
              y={midY - 5}
              textAnchor="start"
              fontSize={11}
              fontFamily="DM Sans, sans-serif"
              fill={C.labelDim}
            >
              {truncateLabel(node.name)}
            </text>
            <text
              x={RIGHT_LABEL_START}
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
