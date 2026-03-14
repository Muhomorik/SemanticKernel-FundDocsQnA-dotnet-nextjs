"use client";

import { OwnershipPeriodsResponse, SelectedPeriod } from "@/lib/ownership-flow";

interface PeriodSelectorProps {
  periods: OwnershipPeriodsResponse;
  selected: SelectedPeriod;
  onChange: (period: SelectedPeriod) => void;
  disabled?: boolean;
}

const ACTIVE_STYLE: React.CSSProperties = {
  background:
    "linear-gradient(to right, oklch(0.65 0.1 45 / 0.15), oklch(0.65 0.1 45 / 0.1))",
  borderColor: "oklch(0.65 0.1 45 / 0.4)",
  color: "oklch(0.65 0.1 45)",
};

export function PeriodSelector({
  periods,
  selected,
  onChange,
  disabled = false,
}: PeriodSelectorProps) {
  return (
    <div className="bg-card border-border/50 mb-6 flex flex-wrap items-center gap-2 rounded-lg border p-1.5">
      <span className="text-muted-foreground px-2 text-xs font-medium">
        Weekly
      </span>
      {periods.weekly.map((period, i) => {
        const isActive = selected.group === "weekly" && selected.index === i;
        return (
          <button
            key={period.from}
            aria-pressed={isActive}
            disabled={disabled}
            onClick={() => onChange({ group: "weekly", index: i, period })}
            className="rounded-md border border-transparent px-3 py-1 text-sm font-medium transition-colors duration-150 disabled:pointer-events-none disabled:opacity-60"
            style={isActive ? ACTIVE_STYLE : undefined}
          >
            {period.label}
          </button>
        );
      })}

      <span
        aria-hidden="true"
        className="bg-border mx-1 h-5 w-px flex-shrink-0"
      />

      <span className="text-muted-foreground px-2 text-xs font-medium">
        Monthly
      </span>
      {periods.monthly.map((period, i) => {
        const isActive = selected.group === "monthly" && selected.index === i;
        return (
          <button
            key={period.from}
            aria-pressed={isActive}
            disabled={disabled}
            onClick={() => onChange({ group: "monthly", index: i, period })}
            className="rounded-md border border-transparent px-3 py-1 text-sm font-medium transition-colors duration-150 disabled:pointer-events-none disabled:opacity-60"
            style={isActive ? ACTIVE_STYLE : undefined}
          >
            {period.label}
          </button>
        );
      })}
    </div>
  );
}
