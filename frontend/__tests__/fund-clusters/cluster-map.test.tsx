import { createRef } from "react";
import { render, waitFor } from "@testing-library/react";
import {
  ClusterMap,
  ClusterMapHandle,
  ClusterMapProps,
} from "@/components/fund-clusters/cluster-map";
import { FUNDS, METHOD } from "@/test-utils/fund-clusters";

type ClickHandler = (params: { seriesId: string; dataIndex: number }) => void;

const chart = {
  setOption: jest.fn(),
  clear: jest.fn(),
  resize: jest.fn(),
  dispose: jest.fn(),
  dispatchAction: jest.fn(),
  on: jest.fn(),
};
const init = jest.fn<typeof chart, unknown[]>(() => chart);

jest.mock("echarts", () => ({ init: (...args: unknown[]) => init(...args) }));

beforeAll(() => {
  global.ResizeObserver = class {
    observe() {}
    disconnect() {}
  } as unknown as typeof ResizeObserver;
});

beforeEach(() => {
  jest.clearAllMocks();
});

function props(overrides: Partial<ClusterMapProps> = {}): ClusterMapProps {
  return {
    doc: METHOD,
    funds: FUNDS,
    view: "all",
    layers: { territory: true, names: true, rings: true },
    isolate: null,
    pinned: null,
    onPin: jest.fn(),
    ...overrides,
  };
}

function lastSeriesIds(): string[] {
  const calls = chart.setOption.mock.calls.filter(
    ([option]) => option.series !== undefined
  );
  const option = calls[calls.length - 1][0];
  return option.series.map((s: { id: string }) => s.id);
}

function clickHandler(): ClickHandler {
  const call = chart.on.mock.calls.find(([event]) => event === "click");
  return call[1];
}

describe("ClusterMap", () => {
  it("creates the chart and draws all series", async () => {
    render(<ClusterMap {...props()} />);

    await waitFor(() => expect(init).toHaveBeenCalled());
    await waitFor(() =>
      expect(lastSeriesIds()).toEqual([
        "territory",
        "points",
        "labels",
        "similar",
        "pin",
      ])
    );
  });

  it("pins a fund clicked on the map", async () => {
    const onPin = jest.fn();
    render(<ClusterMap {...props({ onPin })} />);
    await waitFor(() => expect(chart.on).toHaveBeenCalled());

    clickHandler()({ seriesId: "points", dataIndex: 2 });
    clickHandler()({ seriesId: "labels", dataIndex: 0 });

    expect(onPin).toHaveBeenCalledTimes(1);
    expect(onPin).toHaveBeenCalledWith("SE0000000003");
  });

  it("redraws when the pinned fund changes", async () => {
    const { rerender } = render(<ClusterMap {...props()} />);
    await waitFor(() => expect(chart.setOption).toHaveBeenCalled());
    const before = chart.setOption.mock.calls.length;

    rerender(<ClusterMap {...props({ pinned: "SE0000000001" })} />);

    await waitFor(() =>
      expect(chart.setOption.mock.calls.length).toBeGreaterThan(before)
    );
  });

  it("zooms to a fund and back out through its handle", async () => {
    const ref = createRef<ClusterMapHandle>();
    render(<ClusterMap {...props({ ref })} />);
    await waitFor(() => expect(init).toHaveBeenCalled());

    ref.current!.zoomTo("SE0000000002");
    ref.current!.resetZoom();

    const [zoom, reset] = chart.dispatchAction.mock.calls.map(([a]) => a);
    expect(zoom.type).toBe("dataZoom");
    expect(zoom.batch[1].startValue).toBeCloseTo(0.5 - 0.09);
    expect(reset.batch).toEqual([
      { dataZoomIndex: 0, start: 0, end: 100 },
      { dataZoomIndex: 1, start: 0, end: 100 },
    ]);
  });

  it("disposes the chart on unmount", async () => {
    const { unmount } = render(<ClusterMap {...props()} />);
    await waitFor(() => expect(init).toHaveBeenCalled());

    unmount();

    expect(chart.dispose).toHaveBeenCalled();
  });
});
