"use client";

import { useEffect, useState } from "react";
import {
  fetchOwnershipFlow,
  fetchOwnershipPeriods,
  OwnershipFlowResponse,
  OwnershipPeriodsResponse,
  SelectedPeriod,
  TooltipState,
} from "@/lib/ownership-flow";
import { PeriodSelector } from "./period-selector";
import { SankeyCard } from "./sankey-card";
import { SankeyTooltip } from "./sankey-tooltip";

export function OwnershipFlowPage() {
  const [periods, setPeriods] = useState<OwnershipPeriodsResponse | null>(null);
  const [selected, setSelected] = useState<SelectedPeriod | null>(null);
  const [flowData, setFlowData] = useState<OwnershipFlowResponse | null>(null);
  const [periodsError, setPeriodsError] = useState<string | null>(null);
  const [flowError, setFlowError] = useState<string | null>(null);
  const [isLoadingPeriods, setIsLoadingPeriods] = useState(true);
  const [isLoadingFlow, setIsLoadingFlow] = useState(false);
  const [tooltip, setTooltip] = useState<TooltipState>({
    visible: false,
    x: 0,
    y: 0,
    name: "",
    value: 0,
    side: "out",
  });

  // Effect 1: load periods on mount, auto-select most recent weekly period
  useEffect(() => {
    setIsLoadingPeriods(true);
    fetchOwnershipPeriods()
      .then((data) => {
        setPeriods(data);
        // Default to "1 month" — weekly periods often have sparse data
        if (data.monthly.length > 0) {
          setSelected({
            group: "monthly",
            index: 0,
            period: data.monthly[0],
          });
        } else if (data.weekly.length > 0) {
          setSelected({
            group: "weekly",
            index: data.weekly.length - 1,
            period: data.weekly[data.weekly.length - 1],
          });
        }
      })
      .catch((err: unknown) =>
        setPeriodsError(
          err instanceof Error ? err.message : "Failed to load periods"
        )
      )
      .finally(() => setIsLoadingPeriods(false));
  }, []);

  // Effect 2: fetch flow data whenever selected period changes
  useEffect(() => {
    if (!selected) return;
    const controller = new AbortController();
    async function load() {
      setIsLoadingFlow(true);
      setFlowError(null);
      try {
        const data = await fetchOwnershipFlow(
          selected.period.from,
          selected.period.to
        );
        if (!controller.signal.aborted) setFlowData(data);
      } catch (err: unknown) {
        if (!controller.signal.aborted)
          setFlowError(
            err instanceof Error ? err.message : "Failed to load flow data"
          );
      } finally {
        if (!controller.signal.aborted) setIsLoadingFlow(false);
      }
    }
    void load();
    return () => controller.abort();
  }, [selected]);

  return (
    <main className="mx-auto max-w-5xl px-6 py-12">
      {/* Page header */}
      <div className="mb-8">
        <h1 className="font-serif text-3xl font-medium tracking-tight">
          Ownership Flow
        </h1>
        <p className="text-muted-foreground mt-2 text-sm">
          Investor movement across funds based on weekly ownership changes.
        </p>
      </div>

      {/* Period selector */}
      {periodsError ? (
        <div className="bg-destructive/10 text-destructive mb-6 rounded-lg px-4 py-3 text-sm">
          Could not load time periods: {periodsError}
        </div>
      ) : isLoadingPeriods ? (
        <div className="bg-muted mb-6 h-12 animate-pulse rounded-lg" />
      ) : periods && selected ? (
        <PeriodSelector
          periods={periods}
          selected={selected}
          onChange={setSelected}
          disabled={isLoadingFlow}
        />
      ) : null}

      {/* Flow error */}
      {flowError && (
        <div className="bg-destructive/10 text-destructive mb-6 rounded-lg px-4 py-3 text-sm">
          Could not load flow data: {flowError}
        </div>
      )}

      {/* Charts */}
      <div className="flex flex-col gap-6">
        <SankeyCard
          title="Category overview"
          subtitle="Investor movement grouped by fund category"
          data={flowData?.cat}
          svgHeight={300}
          isLoading={isLoadingFlow || isLoadingPeriods}
          periodLabel={flowData?.periodLabel}
          onTooltipChange={setTooltip}
        />
        <SankeyCard
          title="Top funds"
          subtitle="Top 10 funds gaining and losing investors"
          data={flowData?.fund}
          svgHeight={520}
          isLoading={isLoadingFlow || isLoadingPeriods}
          periodLabel={flowData?.periodLabel}
          onTooltipChange={setTooltip}
        />
      </div>

      {/* Shared tooltip — one instance for both charts */}
      <SankeyTooltip state={tooltip} />
    </main>
  );
}
