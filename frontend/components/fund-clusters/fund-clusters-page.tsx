"use client";

import { useEffect, useRef, useState } from "react";
import {
  fetchClusterFunds,
  fetchClusterMethod,
  fetchClusterMethods,
  Fund,
  Isolate,
  MapLayers,
  MapView,
  MethodFile,
  MethodsManifest,
  SidebarTab,
} from "@/lib/fund-clusters";
import { DemoBanner } from "../demo-banner";
import { ClusterLegend } from "./cluster-legend";
import { ClusterMap, ClusterMapHandle } from "./cluster-map";
import { ClusterSidebar } from "./cluster-sidebar";
import { ClusterStats } from "./cluster-stats";
import { ClusterToolbar } from "./cluster-toolbar";

function message(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback;
}

export function FundClustersPage() {
  const [manifest, setManifest] = useState<MethodsManifest | null>(null);
  const [funds, setFunds] = useState<Fund[] | null>(null);
  const [methodId, setMethodId] = useState<string | null>(null);
  const [doc, setDoc] = useState<MethodFile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoadingMethod, setIsLoadingMethod] = useState(false);

  const [view, setView] = useState<MapView>("all");
  const [layers, setLayers] = useState<MapLayers>({
    territory: true,
    names: true,
    rings: true,
  });
  const [isolate, setIsolate] = useState<Isolate>(null);
  const [pinned, setPinned] = useState<string | null>(null);
  const [tab, setTab] = useState<SidebarTab>("categories");
  const mapRef = useRef<ClusterMapHandle>(null);

  // Effect 1: manifest + funds once, then open the default method
  useEffect(() => {
    const controller = new AbortController();
    Promise.all([
      fetchClusterMethods(controller.signal),
      fetchClusterFunds(controller.signal),
    ])
      .then(([m, f]) => {
        setManifest(m);
        setFunds(f.funds);
        setMethodId(m.default);
      })
      .catch((err: unknown) => {
        if (!controller.signal.aborted)
          setError(message(err, "Failed to load fund clusters"));
      });
    return () => controller.abort();
  }, []);

  // Effect 2: the selected method's file
  useEffect(() => {
    if (!methodId) return;
    const controller = new AbortController();
    async function load(id: string) {
      setIsLoadingMethod(true);
      try {
        const data = await fetchClusterMethod(id, controller.signal);
        if (controller.signal.aborted) return;
        setDoc(data);
        // Cluster ids differ per method; ISINs don't, so the pin survives.
        setIsolate(null);
        setError(null);
      } catch (err: unknown) {
        if (!controller.signal.aborted)
          setError(message(err, "Failed to load clustering method"));
      } finally {
        if (!controller.signal.aborted) setIsLoadingMethod(false);
      }
    }
    void load(methodId);
    return () => controller.abort();
  }, [methodId]);

  function pinAndZoom(isin: string | null) {
    setPinned(isin);
    if (isin) mapRef.current?.zoomTo(isin);
  }

  const ready = manifest && funds && doc && methodId;

  return (
    <main className="mx-auto max-w-7xl px-4 py-12 sm:px-6">
      <div className="mx-auto max-w-5xl">
        <DemoBanner />
      </div>

      <div className="mt-6 mb-6 flex flex-wrap items-end justify-between gap-x-10 gap-y-4">
        <div className="max-w-xl">
          <h1 className="font-serif text-3xl font-medium tracking-tight">
            Fund Clusters
          </h1>
          <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
            Equity funds grouped by what they actually hold. Dot colour is the
            fund&apos;s own category; the shaded area is the cluster its country
            and sector mix puts it in.
          </p>
          {doc && (
            <p className="text-muted-foreground/80 mt-1 text-xs tabular-nums">
              Week {doc.week}, {doc.summary.funds} equity funds,{" "}
              {doc.summary.clusters} clusters
            </p>
          )}
        </div>
        {doc && <ClusterStats doc={doc} />}
      </div>

      {error && (
        <div
          role="alert"
          className="bg-destructive/10 text-destructive mb-6 rounded-lg px-4 py-3 text-sm"
        >
          Could not load the cluster map: {error}
        </div>
      )}

      {!ready && !error && (
        <div className="space-y-3" aria-busy="true">
          <div className="bg-muted h-12 animate-pulse rounded-lg" />
          <div className="bg-muted h-[60vh] animate-pulse rounded-lg" />
        </div>
      )}

      {ready && (
        <div className="flex flex-col gap-3">
          <ClusterToolbar
            methods={manifest.methods}
            methodId={methodId}
            onMethodChange={setMethodId}
            funds={funds}
            onPickFund={pinAndZoom}
            view={view}
            onViewChange={setView}
            layers={layers}
            onLayersChange={setLayers}
            onResetZoom={() => mapRef.current?.resetZoom()}
            disabled={isLoadingMethod}
          />

          <div className="grid grid-cols-1 items-start gap-3 lg:grid-cols-[minmax(0,1fr)_340px]">
            <div
              className={`bg-card border-border/50 flex h-[min(78vh,820px)] min-h-[420px] flex-col overflow-hidden rounded-lg border transition-opacity ${isLoadingMethod ? "opacity-60" : ""}`}
            >
              <ClusterMap
                ref={mapRef}
                doc={doc}
                funds={funds}
                view={view}
                layers={layers}
                isolate={isolate}
                pinned={pinned}
                onPin={setPinned}
              />
              <ClusterLegend />
            </div>
            <ClusterSidebar
              doc={doc}
              funds={funds}
              tab={tab}
              onTabChange={setTab}
              isolate={isolate}
              onIsolate={setIsolate}
              pinned={pinned}
              onPin={pinAndZoom}
            />
          </div>

          <p className="text-muted-foreground/60 text-center text-[11px] leading-relaxed">
            Scroll to zoom, drag to pan, click a fund to see its most similar
            funds. Distances compare holdings within one method only.
          </p>
        </div>
      )}
    </main>
  );
}
