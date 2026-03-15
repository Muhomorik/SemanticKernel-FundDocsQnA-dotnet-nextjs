import {
  buildSankeyGraph,
  computeSankeyLayout,
  FlowSide,
  formatOwnerCount,
  SANKEY_LAYOUT,
  truncateLabel,
} from "@/lib/ownership-flow";

// ── Helpers ─────────────────────────────────────────────────────────────────

function makeData(out: number[], inflow: number[]): FlowSide {
  return {
    out: out.map((v, i) => ({ name: `Out${i}`, value: v, pct: -1 })),
    in: inflow.map((v, i) => ({ name: `In${i}`, value: v, pct: 1 })),
  };
}

// ── buildSankeyGraph ────────────────────────────────────────────────────────

describe("buildSankeyGraph", () => {
  it("creates out nodes with 'out_' prefix", () => {
    const graph = buildSankeyGraph(makeData([100, 200], [50]));
    const outNodes = graph.nodes.filter((n) => n.side === "out");
    expect(outNodes).toHaveLength(2);
    expect(outNodes[0].id).toBe("out_Out0");
    expect(outNodes[1].id).toBe("out_Out1");
  });

  it("creates in nodes with 'in_' prefix", () => {
    const graph = buildSankeyGraph(makeData([100], [50, 75]));
    const inNodes = graph.nodes.filter((n) => n.side === "in");
    expect(inNodes).toHaveLength(2);
    expect(inNodes[0].id).toBe("in_In0");
    expect(inNodes[1].id).toBe("in_In1");
  });

  it("always creates a hub node", () => {
    const graph = buildSankeyGraph(makeData([100], [50]));
    const hub = graph.nodes.find((n) => n.side === "hub");
    expect(hub).toBeDefined();
    expect(hub!.id).toBe("hub");
  });

  it("creates out→hub links for each outflow", () => {
    const graph = buildSankeyGraph(makeData([100, 200], [50]));
    const outLinks = graph.links.filter((l) => l.side === "out");
    expect(outLinks).toHaveLength(2);
    outLinks.forEach((l) => expect(l.target).toBe("hub"));
  });

  it("creates hub→in links for each inflow", () => {
    const graph = buildSankeyGraph(makeData([100], [50, 75]));
    const inLinks = graph.links.filter((l) => l.side === "in");
    expect(inLinks).toHaveLength(2);
    inLinks.forEach((l) => expect(l.source).toBe("hub"));
  });

  it("link values match input values", () => {
    const graph = buildSankeyGraph(makeData([100, 200], [300]));
    const outLinks = graph.links.filter((l) => l.side === "out");
    expect(outLinks[0].value).toBe(100);
    expect(outLinks[1].value).toBe(200);
    const inLinks = graph.links.filter((l) => l.side === "in");
    expect(inLinks[0].value).toBe(300);
  });

  it("adds phantom inflow link when in is empty", () => {
    const graph = buildSankeyGraph(makeData([100], []));
    const phantomLinks = graph.links.filter((l) => l.side === "phantom");
    expect(phantomLinks).toHaveLength(1);
    expect(phantomLinks[0].target).toBe("in_phantom");
  });

  it("adds phantom outflow link when out is empty", () => {
    const graph = buildSankeyGraph(makeData([], [100]));
    const phantomLinks = graph.links.filter((l) => l.side === "phantom");
    expect(phantomLinks).toHaveLength(1);
    expect(phantomLinks[0].source).toBe("out_phantom");
  });

  it("adds two phantom links when both sides are empty", () => {
    const graph = buildSankeyGraph(makeData([], []));
    const phantomLinks = graph.links.filter((l) => l.side === "phantom");
    expect(phantomLinks).toHaveLength(2);
  });

  it("preserves displayName from original data", () => {
    const data: FlowSide = {
      out: [{ name: "Sverige", value: 100, pct: -0.5 }],
      in: [{ name: "Global", value: 200, pct: 1.2 }],
    };
    const graph = buildSankeyGraph(data);
    expect(graph.nodes.find((n) => n.id === "out_Sverige")!.displayName).toBe(
      "Sverige"
    );
    expect(graph.nodes.find((n) => n.id === "in_Global")!.displayName).toBe(
      "Global"
    );
  });
});

// ── computeSankeyLayout ─────────────────────────────────────────────────────

describe("computeSankeyLayout", () => {
  const SVG_H = 300;

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

  describe("node filtering", () => {
    it("outNodes excludes phantom nodes", () => {
      const layout = computeSankeyLayout(makeData([], [100]), SVG_H);
      expect(layout.outNodes).toHaveLength(0);
    });

    it("inNodes excludes phantom nodes", () => {
      const layout = computeSankeyLayout(makeData([100], []), SVG_H);
      expect(layout.inNodes).toHaveLength(0);
    });

    it("outNodes count matches data.out count", () => {
      const layout = computeSankeyLayout(
        makeData([100, 200, 300], [50]),
        SVG_H
      );
      expect(layout.outNodes).toHaveLength(3);
    });

    it("inNodes count matches data.in count", () => {
      const layout = computeSankeyLayout(makeData([50], [100, 200]), SVG_H);
      expect(layout.inNodes).toHaveLength(2);
    });

    it("hubNode is always present", () => {
      const layout = computeSankeyLayout(makeData([100], [100]), SVG_H);
      expect(layout.hubNode).toBeDefined();
    });
  });

  describe("node positions from d3-sankey", () => {
    it("out nodes have x0/x1/y0/y1 set", () => {
      const layout = computeSankeyLayout(makeData([100, 200], [100]), SVG_H);
      layout.outNodes.forEach((n) => {
        expect(n.x0).toBeDefined();
        expect(n.x1).toBeDefined();
        expect(n.y0).toBeDefined();
        expect(n.y1).toBeDefined();
      });
    });

    it("in nodes have x0/x1/y0/y1 set", () => {
      const layout = computeSankeyLayout(makeData([100], [100, 200]), SVG_H);
      layout.inNodes.forEach((n) => {
        expect(n.x0).toBeDefined();
        expect(n.x1).toBeDefined();
        expect(n.y0).toBeDefined();
        expect(n.y1).toBeDefined();
      });
    });

    it("node width equals configured NODE_W", () => {
      const layout = computeSankeyLayout(makeData([100], [100]), SVG_H);
      const node = layout.outNodes[0];
      expect(node.x1! - node.x0!).toBe(SANKEY_LAYOUT.NODE_W);
    });

    it("out nodes are positioned left of hub", () => {
      const layout = computeSankeyLayout(makeData([100], [100]), SVG_H);
      expect(layout.outNodes[0].x1!).toBeLessThan(layout.hubNode!.x0!);
    });

    it("in nodes are positioned right of hub", () => {
      const layout = computeSankeyLayout(makeData([100], [100]), SVG_H);
      expect(layout.inNodes[0].x0!).toBeGreaterThan(layout.hubNode!.x1!);
    });
  });

  describe("link filtering", () => {
    it("outLinks count matches data.out count", () => {
      const layout = computeSankeyLayout(
        makeData([100, 200, 300], [50]),
        SVG_H
      );
      expect(layout.outLinks).toHaveLength(3);
    });

    it("inLinks count matches data.in count", () => {
      const layout = computeSankeyLayout(makeData([50], [100, 200]), SVG_H);
      expect(layout.inLinks).toHaveLength(2);
    });

    it("links have width set by d3-sankey", () => {
      const layout = computeSankeyLayout(makeData([100], [100]), SVG_H);
      layout.outLinks.forEach((l) => {
        expect(l.width).toBeDefined();
        expect(l.width).toBeGreaterThan(0);
      });
    });
  });

  describe("linkOpacity", () => {
    it("returns LINK_OPACITY_FEW when max(out, in) <= threshold (6)", () => {
      const layout = computeSankeyLayout(
        makeData([100, 100, 100], [100, 100, 100]),
        SVG_H
      );
      expect(layout.linkOpacity).toBe(SANKEY_LAYOUT.LINK_OPACITY_FEW);
    });

    it("returns LINK_OPACITY_MANY when max(out, in) > threshold (7)", () => {
      const layout = computeSankeyLayout(
        makeData([100, 100, 100, 100, 100, 100, 100], [100]),
        SVG_H
      );
      expect(layout.linkOpacity).toBe(SANKEY_LAYOUT.LINK_OPACITY_MANY);
    });

    it("uses the larger side for threshold check", () => {
      const layout = computeSankeyLayout(
        makeData([100, 100, 100], [100, 100, 100, 100, 100, 100, 100]),
        SVG_H
      );
      expect(layout.linkOpacity).toBe(SANKEY_LAYOUT.LINK_OPACITY_MANY);
    });
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
