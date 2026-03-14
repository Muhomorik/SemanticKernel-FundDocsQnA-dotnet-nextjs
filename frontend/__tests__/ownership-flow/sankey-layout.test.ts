import {
  buildLinkPath,
  computeSankeyLayout,
  FlowSide,
  formatOwnerCount,
  SANKEY_CONSTANTS,
  truncateLabel,
} from "@/lib/ownership-flow";

const {
  PAD_T,
  PAD_B,
  MIN_NODE_H,
  NODE_GAP,
  LINK_OPACITY_FEW,
  LINK_OPACITY_MANY,
} = SANKEY_CONSTANTS;

// ── Helpers ─────────────────────────────────────────────────────────────────

function makeData(out: number[], inflow: number[]): FlowSide {
  return {
    out: out.map((v, i) => ({ name: `Out${i}`, value: v, pct: -1 })),
    in: inflow.map((v, i) => ({ name: `In${i}`, value: v, pct: 1 })),
  };
}

// ── computeSankeyLayout ─────────────────────────────────────────────────────

describe("computeSankeyLayout", () => {
  const SVG_H = 300;
  const availH = SVG_H - PAD_T - PAD_B;

  describe("totals", () => {
    it("computes totalOut as sum of out values", () => {
      const layout = computeSankeyLayout(
        makeData([100, 200, 300], [50]),
        SVG_H
      );
      expect(layout.totalOut).toBe(600);
    });

    it("computes totalIn as sum of in values", () => {
      const layout = computeSankeyLayout(makeData([50], [100, 150]), SVG_H);
      expect(layout.totalIn).toBe(250);
    });

    it("computes net as totalIn - totalOut", () => {
      const layout = computeSankeyLayout(makeData([200], [500]), SVG_H);
      expect(layout.net).toBe(300);
    });
  });

  describe("node positioning", () => {
    it("first out node starts at PAD_T", () => {
      const layout = computeSankeyLayout(makeData([100, 200], [100]), SVG_H);
      expect(layout.outNodes[0].y).toBe(PAD_T);
    });

    it("first in node starts at PAD_T", () => {
      const layout = computeSankeyLayout(makeData([100], [100, 200]), SVG_H);
      expect(layout.inNodes[0].y).toBe(PAD_T);
    });

    it("node heights are proportional to values", () => {
      const layout = computeSankeyLayout(makeData([100, 300], [100]), SVG_H);
      // Node 1 is 3x the value of node 0 → should be ~3x the height (ignoring min)
      const ratio = layout.outNodes[1].h / layout.outNodes[0].h;
      expect(ratio).toBeCloseTo(3, 1);
    });

    it("respects MIN_NODE_H for tiny values", () => {
      const layout = computeSankeyLayout(makeData([1, 10000], [100]), SVG_H);
      expect(layout.outNodes[0].h).toBeGreaterThanOrEqual(MIN_NODE_H);
    });

    it("consecutive nodes are separated by NODE_GAP", () => {
      const layout = computeSankeyLayout(
        makeData([100, 100, 100], [100]),
        SVG_H
      );
      const gap =
        layout.outNodes[1].y - (layout.outNodes[0].y + layout.outNodes[0].h);
      expect(gap).toBeCloseTo(NODE_GAP, 5);
    });

    it("returns empty arrays for empty data", () => {
      const layout = computeSankeyLayout(makeData([], []), SVG_H);
      expect(layout.outNodes).toHaveLength(0);
      expect(layout.inNodes).toHaveLength(0);
    });

    it("handles single node on each side", () => {
      const layout = computeSankeyLayout(makeData([500], [500]), SVG_H);
      expect(layout.outNodes).toHaveLength(1);
      expect(layout.inNodes).toHaveLength(1);
    });
  });

  describe("hub", () => {
    it("hubY equals PAD_T", () => {
      const layout = computeSankeyLayout(makeData([100], [100]), SVG_H);
      expect(layout.hubY).toBe(PAD_T);
    });

    it("hubH equals availH", () => {
      const layout = computeSankeyLayout(makeData([100], [100]), SVG_H);
      expect(layout.hubH).toBe(availH);
    });
  });

  describe("hub slices", () => {
    it("outHubSlices count matches out nodes count", () => {
      const layout = computeSankeyLayout(
        makeData([100, 200, 300], [100]),
        SVG_H
      );
      expect(layout.outHubSlices).toHaveLength(3);
    });

    it("inHubSlices count matches in nodes count", () => {
      const layout = computeSankeyLayout(makeData([100], [100, 200]), SVG_H);
      expect(layout.inHubSlices).toHaveLength(2);
    });

    it("outHubSlices span the full hub height", () => {
      const layout = computeSankeyLayout(
        makeData([100, 200, 300], [100]),
        SVG_H
      );
      const last = layout.outHubSlices[layout.outHubSlices.length - 1];
      expect(last.y2).toBeCloseTo(PAD_T + availH, 1);
    });

    it("inHubSlices span the full hub height", () => {
      const layout = computeSankeyLayout(
        makeData([100], [100, 200, 300]),
        SVG_H
      );
      const last = layout.inHubSlices[layout.inHubSlices.length - 1];
      expect(last.y2).toBeCloseTo(PAD_T + availH, 1);
    });

    it("consecutive slices are contiguous (no gap)", () => {
      const layout = computeSankeyLayout(makeData([100, 200], [100]), SVG_H);
      expect(layout.outHubSlices[1].y1).toBeCloseTo(
        layout.outHubSlices[0].y2,
        5
      );
    });

    it("returns empty slices for empty data", () => {
      const layout = computeSankeyLayout(makeData([], []), SVG_H);
      expect(layout.outHubSlices).toHaveLength(0);
      expect(layout.inHubSlices).toHaveLength(0);
    });
  });

  describe("linkOpacity", () => {
    it("returns LINK_OPACITY_FEW when max(out, in) <= threshold (6)", () => {
      const layout = computeSankeyLayout(
        makeData([100, 100, 100], [100, 100, 100]),
        SVG_H
      );
      expect(layout.linkOpacity).toBe(LINK_OPACITY_FEW);
    });

    it("returns LINK_OPACITY_MANY when max(out, in) > threshold (7)", () => {
      const layout = computeSankeyLayout(
        makeData([100, 100, 100, 100, 100, 100, 100], [100]),
        SVG_H
      );
      expect(layout.linkOpacity).toBe(LINK_OPACITY_MANY);
    });

    it("uses the larger side for threshold check", () => {
      // 3 out, 7 in → max is 7 → MANY
      const layout = computeSankeyLayout(
        makeData([100, 100, 100], [100, 100, 100, 100, 100, 100, 100]),
        SVG_H
      );
      expect(layout.linkOpacity).toBe(LINK_OPACITY_MANY);
    });
  });
});

// ── buildLinkPath ────────────────────────────────────────────────────────────

describe("buildLinkPath", () => {
  it("returns a string starting with M", () => {
    const path = buildLinkPath(0, 10, 50, 100, 20, 60);
    expect(path).toMatch(/^M/);
  });

  it("returns a string containing C (cubic bezier)", () => {
    const path = buildLinkPath(0, 10, 50, 100, 20, 60);
    expect(path).toContain("C");
  });

  it("returns a string containing L (lineto)", () => {
    const path = buildLinkPath(0, 10, 50, 100, 20, 60);
    expect(path).toContain("L");
  });

  it("returns a string ending with Z (closepath)", () => {
    const path = buildLinkPath(0, 10, 50, 100, 20, 60);
    expect(path.trim()).toMatch(/Z$/);
  });

  it("first control point x is at 55% of dx from source", () => {
    // sx=0, tx=200 → dx=200, cp=0.55*200=110
    const path = buildLinkPath(0, 10, 50, 200, 20, 60);
    // Match "C110" allowing for floating-point suffix like "C110.000000001,"
    expect(path).toMatch(/C110(\.\d+)?,/);
  });
});

// ── formatOwnerCount ─────────────────────────────────────────────────────────

describe("formatOwnerCount", () => {
  it("formats 0 as '0'", () => {
    expect(formatOwnerCount(0)).toBe("0");
  });

  it("formats 1234 with comma separator", () => {
    expect(formatOwnerCount(1234)).toBe("1,234");
  });

  it("formats 1000000", () => {
    expect(formatOwnerCount(1000000)).toBe("1,000,000");
  });

  it("formats small numbers without separator", () => {
    expect(formatOwnerCount(42)).toBe("42");
  });
});

// ── truncateLabel ─────────────────────────────────────────────────────────────

describe("truncateLabel", () => {
  it("returns string unchanged if within limit", () => {
    expect(truncateLabel("Short name")).toBe("Short name");
  });

  it("truncates at default 32 chars and appends ellipsis", () => {
    const long = "A".repeat(40);
    const result = truncateLabel(long);
    expect(result).toHaveLength(32);
    expect(result.endsWith("…")).toBe(true);
  });

  it("respects custom max", () => {
    const result = truncateLabel("Hello World", 8);
    expect(result).toHaveLength(8);
    expect(result.endsWith("…")).toBe(true);
  });

  it("does not truncate string exactly at limit", () => {
    const exact = "A".repeat(32);
    expect(truncateLabel(exact)).toBe(exact);
  });

  it("handles empty string", () => {
    expect(truncateLabel("")).toBe("");
  });
});
