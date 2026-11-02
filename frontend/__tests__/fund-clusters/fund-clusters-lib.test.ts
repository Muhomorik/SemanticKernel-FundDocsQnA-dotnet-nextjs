import {
  allocationLabel,
  categoryShades,
  ClusterPoint,
  disagreements,
  familyColour,
  formatShare,
  Fund,
  isDisagreement,
  mapExtent,
  orientRings,
  Ring,
  searchFunds,
  signedArea,
  symbolFor,
  verdictColour,
} from "@/lib/fund-clusters";

// ── Fixtures ─────────────────────────────────────────────────────────────────

function fund(overrides: Partial<Fund>): Fund {
  return {
    isin: "SE0000000001",
    name: "Example Index Fund",
    company: "Example Asset Mgmt",
    category: "USA, Mix bolag",
    family: "usa",
    shade: 0,
    isIndex: true,
    topCountries: [["usa", 99.2]],
    topSectors: [["teknik", 37.3]],
    ...overrides,
  };
}

function point(overrides: Partial<ClusterPoint>): ClusterPoint {
  return {
    isin: "SE0000000001",
    x: 0.5,
    y: 0.5,
    cluster: 0,
    verdict: "agree",
    isolated: false,
    ...overrides,
  };
}

// Counter-clockwise unit square (positive area)
const CCW: Ring = [
  [0, 0],
  [1, 0],
  [1, 1],
  [0, 1],
  [0, 0],
];
const CW: Ring = [...CCW].reverse();

// ── familyColour ─────────────────────────────────────────────────────────────

describe("familyColour", () => {
  it("uses the family hue", () => {
    expect(familyColour("usa", 1, false)).toMatch(/^hsla\(268, 62%/);
  });

  it("shifts hue and lightness by shade so region variants differ", () => {
    expect(familyColour("nordic", 0, false)).not.toEqual(
      familyColour("nordic", 1, false)
    );
  });

  it("is lighter in dark mode", () => {
    const light = familyColour("europe", 0, false);
    const dark = familyColour("europe", 0, true);
    expect(light).toContain("38%");
    expect(dark).toContain("52%");
  });

  it("is grey for the other family", () => {
    expect(familyColour("other", 0, false)).toMatch(/, 0%,/);
  });

  it("falls back to other for an unknown family", () => {
    expect(familyColour("mystery", 0, false)).toEqual(
      familyColour("other", 0, false)
    );
  });

  it("applies alpha", () => {
    expect(familyColour("usa", 0, false, 0.2)).toMatch(/, 0\.2\)$/);
  });
});

// ── symbolFor ────────────────────────────────────────────────────────────────

describe("symbolFor", () => {
  it.each([
    ["sector", "diamond"],
    ["other", "rect"],
    ["nordic", "circle"],
    ["usa", "circle"],
  ] as const)("%s → %s", (family, symbol) => {
    expect(symbolFor(family)).toBe(symbol);
  });
});

// ── verdicts ─────────────────────────────────────────────────────────────────

describe("isDisagreement", () => {
  it.each([
    ["agree", false],
    ["neighbouring", true],
    ["different", true],
    ["none", false],
  ] as const)("%s → %s", (verdict, expected) => {
    expect(isDisagreement(point({ verdict }))).toBe(expected);
  });
});

describe("disagreements", () => {
  it("keeps only disagreements, different family first", () => {
    const result = disagreements([
      point({ isin: "A", verdict: "neighbouring" }),
      point({ isin: "B", verdict: "agree" }),
      point({ isin: "C", verdict: "different" }),
      point({ isin: "D", verdict: "none" }),
      point({ isin: "E", verdict: "different" }),
    ]);
    expect(result.map((p) => p.isin)).toEqual(["C", "E", "A"]);
  });
});

// ── geometry ─────────────────────────────────────────────────────────────────

describe("signedArea", () => {
  it("is positive for counter-clockwise rings", () => {
    expect(signedArea(CCW)).toBeCloseTo(1);
  });

  it("is negative for clockwise rings", () => {
    expect(signedArea(CW)).toBeCloseTo(-1);
  });
});

describe("orientRings", () => {
  it("makes the outer ring counter-clockwise and holes clockwise", () => {
    const result = orientRings([CW, CCW]);
    expect(signedArea(result[0])).toBeGreaterThan(0);
    expect(signedArea(result[1])).toBeLessThan(0);
  });

  it("leaves correctly oriented rings untouched", () => {
    const result = orientRings([CCW, CW]);
    expect(result[0]).toBe(CCW);
    expect(result[1]).toBe(CW);
  });

  it("does not mutate the input", () => {
    const input: Ring = [...CW];
    orientRings([input]);
    expect(input).toEqual(CW);
  });
});

describe("mapExtent", () => {
  it("returns the largest x and y", () => {
    expect(
      mapExtent([point({ x: 0.2, y: 0.6 }), point({ x: 1, y: 0.3 })])
    ).toEqual({ xMax: 1, yMax: 0.6 });
  });
});

// ── search ───────────────────────────────────────────────────────────────────

describe("searchFunds", () => {
  const funds = [
    fund({ isin: "SE0000000001", name: "Example Index Fund" }),
    fund({ isin: "SE0000000002", name: "Example Bond Fund" }),
    fund({ isin: "LU0000000003", name: "AcmeCo Global" }),
  ];

  it("matches name case-insensitively", () => {
    expect(searchFunds(funds, "bond").map((f) => f.isin)).toEqual([
      "SE0000000002",
    ]);
  });

  it("matches ISIN", () => {
    expect(searchFunds(funds, "lu00").map((f) => f.name)).toEqual([
      "AcmeCo Global",
    ]);
  });

  it("needs at least two characters", () => {
    expect(searchFunds(funds, " e ")).toEqual([]);
  });

  it("limits the number of results", () => {
    expect(searchFunds(funds, "example", 1)).toHaveLength(1);
  });
});

// ── categories ───────────────────────────────────────────────────────────────

describe("categoryShades", () => {
  it("maps each category to its family and shade", () => {
    const result = categoryShades([
      fund({ category: "Sverige", family: "nordic", shade: 3 }),
      fund({ category: "Sverige", family: "nordic", shade: 3 }),
      fund({ category: "USA, Mix bolag", family: "usa", shade: 1 }),
    ]);
    expect(result.get("Sverige")).toEqual({ family: "nordic", shade: 3 });
    expect(result.get("USA, Mix bolag")).toEqual({ family: "usa", shade: 1 });
    expect(result.size).toBe(2);
  });
});

// ── formatting ───────────────────────────────────────────────────────────────

describe("formatShare", () => {
  it("rounds to a whole percent", () => {
    expect(formatShare(0.6514)).toBe("65%");
  });

  it("shows a dash for null", () => {
    expect(formatShare(null)).toBe("–");
  });
});

describe("allocationLabel", () => {
  it("replaces underscores and capitalises", () => {
    expect(allocationLabel("konsument_cyklisk")).toBe("Konsument cyklisk");
  });
});

describe("verdictColour", () => {
  it("is amber for neighbouring and red for different family", () => {
    expect(verdictColour("neighbouring", false)).toBe("#c58300");
    expect(verdictColour("different", false)).toBe("#d0353a");
  });

  it("is lighter in dark mode", () => {
    expect(verdictColour("different", true)).toBe("#ec5b5f");
  });

  it("is null when no ring is drawn", () => {
    expect(verdictColour("agree", false)).toBeNull();
    expect(verdictColour("none", true)).toBeNull();
  });
});
