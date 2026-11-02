import {
  buildSeries,
  chartTheme,
  ChartState,
  fitGrid,
  MARGIN,
  SeriesOption,
  tooltipHtml,
  zoomWindow,
} from "@/lib/fund-clusters-chart";
import { familyColour } from "@/lib/fund-clusters";
import { FUNDS, METHOD } from "@/test-utils/fund-clusters";

const STATE: ChartState = {
  view: "all",
  layers: { territory: true, names: true, rings: true },
  isolate: null,
  pinned: null,
  dark: false,
};

interface PointDatum {
  value: [number, number];
  symbol: string;
  symbolSize: number;
  itemStyle: {
    color: string;
    opacity: number;
    borderColor: string;
    borderWidth: number;
  };
}

function series(state: Partial<ChartState> = {}): Record<string, SeriesOption> {
  const all = buildSeries(METHOD, FUNDS, { ...STATE, ...state });
  return Object.fromEntries(all.map((s) => [s.id, s]));
}

function points(state: Partial<ChartState> = {}): PointDatum[] {
  return series(state).points.data as PointDatum[];
}

// ── layout ───────────────────────────────────────────────────────────────────

describe("fitGrid", () => {
  const extent = { xMax: 1 - 2 * MARGIN, yMax: 0.5 - 2 * MARGIN }; // aspect 2:1

  it("fits the width when the box is tall", () => {
    expect(fitGrid(424, 1000, extent)).toEqual({
      left: 12,
      top: 400,
      width: 400,
      height: 200,
    });
  });

  it("fits the height when the box is wide", () => {
    expect(fitGrid(1000, 224, extent)).toEqual({
      left: 300,
      top: 12,
      width: 400,
      height: 200,
    });
  });
});

describe("zoomWindow", () => {
  it("centres on the fund and keeps the map aspect", () => {
    const extent = { xMax: 1 - 2 * MARGIN, yMax: 0.5 - 2 * MARGIN };
    const result = zoomWindow({ ...METHOD.points[0], x: 0.5, y: 0.25 }, extent);

    expect(result.y[0]).toBeCloseTo(0.16);
    expect(result.y[1]).toBeCloseTo(0.34);
    expect(result.x[1] - result.x[0]).toBeCloseTo(
      2 * (result.y[1] - result.y[0])
    );
  });
});

describe("chartTheme", () => {
  it("differs between light and dark", () => {
    expect(chartTheme(true).ink).not.toBe(chartTheme(false).ink);
  });
});

// ── series ───────────────────────────────────────────────────────────────────

describe("buildSeries", () => {
  it("draws territory, points, labels, similar rings and the pin", () => {
    expect(Object.keys(series())).toEqual([
      "territory",
      "points",
      "labels",
      "similar",
      "pin",
    ]);
  });

  it("draws one dot per fund in file order with family shapes", () => {
    const data = points();

    expect(data.map((d) => d.value)).toEqual(
      METHOD.points.map((p) => [p.x, p.y])
    );
    expect(data.map((d) => d.symbol)).toEqual([
      "circle",
      "circle",
      "diamond",
      "circle",
    ]);
  });

  it("rings disagreements in the verdict colour", () => {
    const data = points();

    expect(data[0].itemStyle.borderWidth).toBeLessThan(2);
    expect(data[1].itemStyle).toMatchObject({
      borderColor: "#c58300",
      borderWidth: 2,
    });
    expect(data[2].itemStyle).toMatchObject({
      borderColor: "#d0353a",
      borderWidth: 2,
    });
  });

  it("drops rings when the layer is off", () => {
    const data = points({ layers: { ...STATE.layers, rings: false } });

    expect(data.every((d) => d.itemStyle.borderWidth < 2)).toBe(true);
  });

  it("hides agreeing funds in the disagreements view", () => {
    const sizes = points({ view: "disagree" }).map((d) => d.symbolSize);

    expect(sizes[0]).toBe(0);
    expect(sizes[1]).toBeGreaterThan(0);
    expect(sizes[2]).toBeGreaterThan(0);
    expect(sizes[3]).toBe(0);
  });

  it("dims funds outside an isolated category", () => {
    const opacity = points({
      isolate: { type: "category", value: "Norden" },
    }).map((d) => d.itemStyle.opacity);

    expect(opacity).toEqual([0.12, 1, 0.12, 0.12]);
  });

  it("dims funds outside an isolated cluster", () => {
    const opacity = points({ isolate: { type: "cluster", value: 0 } }).map(
      (d) => d.itemStyle.opacity
    );

    expect(opacity).toEqual([1, 0.12, 1, 0.12]);
  });

  it("drops the territory when the layer is off", () => {
    const off = series({ layers: { ...STATE.layers, territory: false } });

    expect(series().territory.data).toHaveLength(1);
    expect(off.territory.data).toHaveLength(0);
  });

  it("draws territory as one path per part with oriented rings", () => {
    const renderItem = series().territory.renderItem as (
      params: { dataIndex: number },
      api: { coord: (p: [number, number]) => [number, number] }
    ) => { shape: { pathData: string }; style: { fill: string } };

    const shape = renderItem(
      { dataIndex: 0 },
      { coord: ([x, y]) => [x * 100, y * 100] }
    );

    expect(shape.shape.pathData).toBe(
      "M0.0 0.0L30.0 0.0L30.0 30.0L0.0 30.0L0.0 0.0Z"
    );
    expect(shape.style.fill).toBe(
      familyColour("usa", 0, false, chartTheme(false).territoryAlpha)
    );
  });

  it("labels clusters largest first, or not at all", () => {
    const labels = series().labels.data as { name: string }[];

    expect(labels.map((l) => l.name)).toEqual(["USA, Mix bolag", "Sverige"]);
    expect(
      series({ layers: { ...STATE.layers, names: false } }).labels.data
    ).toEqual([]);
  });

  it("marks the pinned fund and its similar funds", () => {
    const s = series({ pinned: "SE0000000001" });

    expect(s.pin.data).toEqual([[0.1, 0.2]]);
    expect(s.similar.data).toEqual([
      [0.15, 0.25],
      [0.8, 0.5],
    ]);
  });

  it("marks nothing when no fund is pinned", () => {
    expect(series().pin.data).toEqual([]);
    expect(series().similar.data).toEqual([]);
  });
});

// ── tooltip ──────────────────────────────────────────────────────────────────

describe("tooltipHtml", () => {
  const [usaFund, , techFund] = FUNDS;

  it("shows name, category, cluster, verdict and allocations", () => {
    const html = tooltipHtml(
      METHOD.points[2],
      techFund,
      "USA, Mix bolag",
      false
    );

    expect(html).toContain("<strong>Example Tech Fund</strong>");
    expect(html).toContain("Category: Branschfond, Ny teknik");
    expect(html).toContain("Cluster: USA, Mix bolag");
    expect(html).toContain("Different family");
    expect(html).toContain("#d0353a");
    expect(html).toContain("Few close neighbours");
    expect(html).toContain("Sectors: Teknik 90.0%");
  });

  it("says when a fund is not in a cluster", () => {
    const html = tooltipHtml(METHOD.points[3], FUNDS[3], null, false);

    expect(html).toContain("Not in a cluster");
  });

  it("escapes text from the data", () => {
    const html = tooltipHtml(
      METHOD.points[0],
      { ...usaFund, name: '<img src=x onerror="alert(1)">' },
      "A & B",
      false
    );

    expect(html).not.toContain("<img");
    expect(html).toContain("&lt;img");
    expect(html).toContain("A &amp; B");
  });
});
