"use client";

import * as React from "react";
import { createPortal } from "react-dom";
import { formatOwnerCount, TooltipState } from "@/lib/ownership-flow";

interface SankeyTooltipProps {
  state: TooltipState;
}

export function SankeyTooltip({ state }: SankeyTooltipProps) {
  const [mounted, setMounted] = React.useState(false);

  React.useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) return null;

  const label =
    state.side === "out"
      ? `${formatOwnerCount(state.value)} investors left`
      : `${formatOwnerCount(state.value)} investors joined`;

  const content = (
    <div
      role="tooltip"
      style={{
        position: "fixed",
        left: state.x + 14,
        top: state.y - 10,
        pointerEvents: "none",
        zIndex: 9999,
        transition: "opacity 0.1s",
        opacity: state.visible ? 1 : 0,
      }}
      className="bg-background border-border rounded-lg border px-3 py-2 text-sm shadow-lg"
    >
      <p className="font-medium">{state.name}</p>
      <p className="text-muted-foreground">{label}</p>
    </div>
  );

  return createPortal(content, document.body);
}
