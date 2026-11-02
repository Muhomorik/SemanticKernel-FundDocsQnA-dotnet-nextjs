"use client";

import { RotateCcw } from "lucide-react";
import { Fund, MapLayers, MapView, MethodEntry } from "@/lib/fund-clusters";
import { FundSearch } from "./fund-search";

export interface ClusterToolbarProps {
  methods: MethodEntry[];
  methodId: string;
  onMethodChange: (id: string) => void;
  funds: Fund[];
  onPickFund: (isin: string) => void;
  view: MapView;
  onViewChange: (view: MapView) => void;
  layers: MapLayers;
  onLayersChange: (layers: MapLayers) => void;
  onResetZoom: () => void;
  disabled?: boolean;
}

const VIEWS: { id: MapView; label: string }[] = [
  { id: "all", label: "All funds" },
  { id: "disagree", label: "Disagreements" },
];

const LAYERS: { key: keyof MapLayers; label: string }[] = [
  { key: "territory", label: "Cluster areas" },
  { key: "names", label: "Cluster names" },
  { key: "rings", label: "Disagreement rings" },
];

export function ClusterToolbar({
  methods,
  methodId,
  onMethodChange,
  funds,
  onPickFund,
  view,
  onViewChange,
  layers,
  onLayersChange,
  onResetZoom,
  disabled = false,
}: ClusterToolbarProps) {
  return (
    <div className="bg-card border-border/50 flex flex-wrap items-center gap-x-5 gap-y-3 rounded-lg border px-3 py-2.5">
      <label className="text-muted-foreground flex items-center gap-2 text-sm">
        Method
        <select
          value={methodId}
          disabled={disabled}
          onChange={(e) => onMethodChange(e.target.value)}
          className="bg-background border-border text-foreground focus-visible:ring-ring/40 rounded-md border px-2 py-1.5 text-sm outline-none focus-visible:ring-2 disabled:opacity-60"
        >
          {methods.map((m) => (
            <option key={m.id} value={m.id}>
              {m.name}
            </option>
          ))}
        </select>
      </label>

      <FundSearch funds={funds} onPick={onPickFund} />

      <div
        role="group"
        aria-label="Funds shown"
        className="border-border/60 inline-flex rounded-md border p-0.5"
      >
        {VIEWS.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            aria-pressed={view === id}
            onClick={() => onViewChange(id)}
            className="aria-pressed:bg-warm/12 aria-pressed:text-warm text-muted-foreground hover:text-foreground rounded px-3 py-1 text-sm font-medium transition-colors duration-150"
          >
            {label}
          </button>
        ))}
      </div>

      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        {LAYERS.map(({ key, label }) => (
          <label
            key={key}
            className="flex cursor-pointer items-center gap-1.5 text-sm"
          >
            <input
              type="checkbox"
              checked={layers[key]}
              onChange={(e) =>
                onLayersChange({ ...layers, [key]: e.target.checked })
              }
              className="accent-warm"
            />
            {label}
          </label>
        ))}
      </div>

      <button
        type="button"
        onClick={onResetZoom}
        className="text-muted-foreground hover:text-foreground ml-auto flex items-center gap-1.5 rounded-md px-2 py-1 text-sm transition-colors"
      >
        <RotateCcw className="h-3.5 w-3.5" aria-hidden="true" />
        Reset zoom
      </button>
    </div>
  );
}
