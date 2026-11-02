import type { Fund, MethodFile, MethodsManifest } from "@/lib/fund-clusters";

export const MANIFEST: MethodsManifest = {
  week: "2026-W44",
  default: "cosine",
  methods: [
    { id: "cosine", name: "Cosine", file: "cosine.json" },
    { id: "aitchison", name: "Aitchison", file: "aitchison.json" },
  ],
};

export const FUNDS: Fund[] = [
  {
    isin: "SE0000000001",
    name: "Example Index Fund",
    company: "Example Asset Mgmt",
    category: "USA, Mix bolag",
    family: "usa",
    shade: 0,
    isIndex: true,
    topCountries: [
      ["usa", 99.19],
      ["schweiz", 0.35],
    ],
    topSectors: [
      ["teknik", 37.28],
      ["finans", 13.01],
    ],
  },
  {
    isin: "SE0000000002",
    name: "Example Nordic Fund",
    company: "AcmeCo",
    category: "Norden",
    family: "nordic",
    shade: 1,
    isIndex: false,
    topCountries: [["sverige", 70]],
    topSectors: [["industri", 35]],
  },
  {
    isin: "SE0000000003",
    name: "Example Tech Fund",
    company: "AcmeCo",
    category: "Branschfond, Ny teknik",
    family: "sector",
    shade: 0,
    isIndex: false,
    topCountries: [["usa", 80]],
    topSectors: [["teknik", 90]],
  },
  {
    isin: "SE0000000004",
    name: "Example Sverige Fund",
    company: "AcmeCo",
    category: "Sverige",
    family: "nordic",
    shade: 2,
    isIndex: false,
    topCountries: [["sverige", 95]],
    topSectors: [["finans", 40]],
  },
];

export const METHOD: MethodFile = {
  week: "2026-W44",
  method: "cosine",
  name: "Cosine",
  description: "Each fund vector scaled to length 1.",
  summary: {
    funds: 4,
    clusters: 2,
    notInCluster: 1,
    agree: 0.3333,
    neighbouring: 0.3333,
    different: 0.3334,
    ari: 0.3961,
    nmi: 0.7582,
  },
  points: [
    {
      isin: "SE0000000001",
      x: 0.1,
      y: 0.2,
      cluster: 0,
      verdict: "agree",
      isolated: false,
    },
    {
      isin: "SE0000000002",
      x: 0.8,
      y: 0.5,
      cluster: 1,
      verdict: "neighbouring",
      isolated: false,
    },
    {
      isin: "SE0000000003",
      x: 0.15,
      y: 0.25,
      cluster: 0,
      verdict: "different",
      isolated: true,
    },
    {
      isin: "SE0000000004",
      x: 1,
      y: 0.6,
      cluster: -1,
      verdict: "none",
      isolated: false,
    },
  ],
  clusters: [
    {
      id: 0,
      name: "USA, Mix bolag",
      size: 2,
      purity: 0.5,
      leadingCategory: "USA, Mix bolag",
      labelX: 0.12,
      labelY: 0.22,
      categories: { "USA, Mix bolag": 1, "Branschfond, Ny teknik": 1 },
    },
    {
      id: 1,
      name: "Sverige",
      size: 1,
      purity: 0,
      leadingCategory: "Sverige",
      labelX: 0.8,
      labelY: 0.5,
      categories: { Norden: 1 },
    },
  ],
  categories: [
    {
      category: "Branschfond, Ny teknik",
      family: "sector",
      funds: 1,
      agreeShare: 0,
    },
    { category: "Norden", family: "nordic", funds: 1, agreeShare: 0 },
    { category: "Sverige", family: "nordic", funds: 1, agreeShare: null },
    { category: "USA, Mix bolag", family: "usa", funds: 1, agreeShare: 1 },
  ],
  territory: [
    {
      cluster: 0,
      rings: [
        [
          [0, 0],
          [0.3, 0],
          [0.3, 0.3],
          [0, 0.3],
          [0, 0],
        ],
      ],
    },
  ],
  neighbours: {
    SE0000000001: [
      { isin: "SE0000000003", distance: 0.0312 },
      { isin: "SE0000000002", distance: 0.5 },
    ],
    SE0000000002: [{ isin: "SE0000000004", distance: 0.1 }],
    SE0000000003: [{ isin: "SE0000000001", distance: 0.0312 }],
    SE0000000004: [{ isin: "SE0000000002", distance: 0.1 }],
  },
};
