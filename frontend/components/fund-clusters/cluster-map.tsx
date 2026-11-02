"use client";

import {
  useEffect,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
  type Ref,
} from "react";
import type { ECharts } from "echarts";
import { useTheme } from "next-themes";
import {
  mapExtent,
  type Fund,
  type Isolate,
  type MapLayers,
  type MapView,
  type MethodFile,
} from "@/lib/fund-clusters";
import {
  buildSeries,
  chartTheme,
  fitGrid,
  MARGIN,
  tooltipHtml,
  zoomWindow,
} from "@/lib/fund-clusters-chart";

export interface ClusterMapHandle {
  resetZoom: () => void;
  zoomTo: (isin: string) => void;
}

export interface ClusterMapProps {
  doc: MethodFile;
  funds: Fund[];
  view: MapView;
  layers: MapLayers;
  isolate: Isolate;
  pinned: string | null;
  onPin: (isin: string) => void;
  ref?: Ref<ClusterMapHandle>;
}

export function ClusterMap({
  doc,
  funds,
  view,
  layers,
  isolate,
  pinned,
  onPin,
  ref,
}: ClusterMapProps) {
  const elementRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<ECharts | null>(null);
  const [ready, setReady] = useState(false);
  const dark = useTheme().resolvedTheme === "dark";

  const extent = useMemo(() => mapExtent(doc.points), [doc]);
  const fundsByIsin = useMemo(
    () => new Map(funds.map((f) => [f.isin, f])),
    [funds]
  );

  // Latest values for the long-lived chart callbacks
  const latest = useRef({ doc, fundsByIsin, extent, onPin, dark });
  useEffect(() => {
    latest.current = { doc, fundsByIsin, extent, onPin, dark };
  });

  // Create the chart once; ECharts is loaded on demand to keep it out of
  // the other pages and the static prerender.
  useEffect(() => {
    let disposed = false;
    let observer: ResizeObserver | undefined;

    void import("echarts").then((echarts) => {
      const element = elementRef.current;
      if (disposed || !element) return;
      const chart = echarts.init(element, null, { renderer: "canvas" });
      chartRef.current = chart;

      chart.on("click", (params) => {
        if (params.seriesId !== "points") return;
        const point = latest.current.doc.points[params.dataIndex];
        if (point) latest.current.onPin(point.isin);
      });

      observer = new ResizeObserver(() => {
        chart.resize();
        chart.setOption({
          grid: fitGrid(
            element.clientWidth,
            element.clientHeight,
            latest.current.extent
          ),
        });
      });
      observer.observe(element);
      setReady(true);
    });

    return () => {
      disposed = true;
      observer?.disconnect();
      chartRef.current?.dispose();
      chartRef.current = null;
    };
  }, []);

  // New method file: new axes, reset zoom
  useEffect(() => {
    const chart = chartRef.current;
    const element = elementRef.current;
    if (!ready || !chart || !element) return;
    chart.clear();
    chart.setOption({
      animation: false,
      xAxis: {
        type: "value",
        min: -MARGIN,
        max: extent.xMax + MARGIN,
        show: false,
      },
      yAxis: {
        type: "value",
        min: -MARGIN,
        max: extent.yMax + MARGIN,
        show: false,
      },
      grid: fitGrid(element.clientWidth, element.clientHeight, extent),
      dataZoom: [
        { type: "inside", xAxisIndex: 0, filterMode: "none" },
        { type: "inside", yAxisIndex: 0, filterMode: "none" },
      ],
      tooltip: {
        trigger: "item",
        confine: true,
        formatter: (params: { seriesId?: string; dataIndex: number }) => {
          if (params.seriesId !== "points") return "";
          const { doc, fundsByIsin, dark } = latest.current;
          const point = doc.points[params.dataIndex];
          const fund = point && fundsByIsin.get(point.isin);
          if (!fund) return "";
          const cluster = doc.clusters.find((c) => c.id === point.cluster);
          return tooltipHtml(point, fund, cluster?.name ?? null, dark);
        },
      },
    });
  }, [ready, doc, extent]);

  // Any state change: redraw the series
  useEffect(() => {
    const chart = chartRef.current;
    if (!ready || !chart) return;
    const theme = chartTheme(dark);
    chart.setOption({
      tooltip: {
        backgroundColor: theme.halo,
        borderColor: dark ? "#3a332e" : "#e6e0d8",
        textStyle: { color: theme.ink, fontSize: 12 },
      },
      series: buildSeries(doc, funds, { view, layers, isolate, pinned, dark }),
    });
  }, [ready, doc, funds, view, layers, isolate, pinned, dark]);

  useImperativeHandle(
    ref,
    () => ({
      resetZoom() {
        chartRef.current?.dispatchAction({
          type: "dataZoom",
          batch: [
            { dataZoomIndex: 0, start: 0, end: 100 },
            { dataZoomIndex: 1, start: 0, end: 100 },
          ],
        });
      },
      zoomTo(isin: string) {
        const point = doc.points.find((p) => p.isin === isin);
        if (!point) return;
        const window = zoomWindow(point, extent);
        chartRef.current?.dispatchAction({
          type: "dataZoom",
          batch: [
            {
              dataZoomIndex: 0,
              startValue: window.x[0],
              endValue: window.x[1],
            },
            {
              dataZoomIndex: 1,
              startValue: window.y[0],
              endValue: window.y[1],
            },
          ],
        });
      },
    }),
    [doc, extent]
  );

  return (
    <div
      ref={elementRef}
      role="img"
      aria-label="Map of funds coloured by category, over the cluster areas they were placed in"
      className="min-h-0 flex-1"
    />
  );
}
