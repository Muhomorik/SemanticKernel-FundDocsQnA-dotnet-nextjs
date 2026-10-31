---
tags: [yieldraccoon, clustering, fund-similarity]
created: 2026-10-31
status: in-progress
---

# Fund clustering for similarity search

## Goal
Find **similar funds** for search. Existing categories (e.g. "Branschfond, Ny teknik") are too generic. Cluster funds on their **country + sector allocations**, then compare clusters with the existing categories visually on a scatter map.

Artifact: **Fund Cluster Map** — https://claude.ai/artifact/NgFRv4smEwQFqsB7LxchRU

## Constraints (agreed)
- Source data comes from an external system; **never modify it**. Use values as delivered (including short positions / negative weights).
- Use **categories verbatim** (no merging, e.g. "Sverige" ≠ "Sverige, Små-/medelstora bolag").
- **No weights** between country and sector blocks.
- **No fixed number of clusters** / no slider; the algorithm decides.
- **One clustering** on country + sector together (not separate country and sector clusterings).
- Scope is allocations only: **no extra features** like volatility here (separate task).

## Data
- `YieldRaccoon_allocations_all_2026-W28.csv`: 831 funds × (`isin`, `name`, 89 `country_*`, 11 `sector_*`). Percent; each block sums to 100.
- `YieldRaccoon_metadata_all_2026-W28.csv`: 1006 funds; uses `category`, `fund_type`, `company_name`, `is_index_fund`.
- Join on `isin`; keep `fund_type == EQUITY_FUND`: **770 funds**, **82 categories**.
- 22 funds have small negative weights (short positions); 17 of them are equity funds.

## Final pipeline
1. **Vector per fund**: all country + sector columns concatenated, divided by 100.
2. **Method coordinates** (three precomputed, switchable):
   - **Cosine (default)**: scale each fund vector to length 1 (`V / ||V||`). Uses negatives as reported. No weights.
   - **Aitchison**: centred log-ratio per block, `clr(p) = log(p+0.005) - mean(log(p+0.005))`, country and sector blocks concatenated. Zeros need the 0.005 stand-in; negatives read as 0 inside the calculation.
   - **Hellinger 60/40** (first version, kept for comparison): clip negatives to 0, renormalise each block, square root, multiply country by √0.6 and sector by √0.4.
3. **Clustering**: UMAP to 10 dimensions (`n_neighbors=15, min_dist=0.0, random_state=42`), then **HDBSCAN** (`min_cluster_size=5, min_samples=3`). HDBSCAN decides the number of clusters; funds outside any dense group get label −1 = "Not in a cluster".
4. **Display map**: separate 2D UMAP per method (`n_neighbors=40, min_dist=0.9, spread=1.2, random_state=42`), scaled to [0,1] keeping aspect ratio.
5. **Isolated flag**: distance to the 5th nearest neighbour; top 5% flagged.
6. **Scores**: ARI and NMI between clusters and categories, on clustered funds only.

Libraries: pandas, numpy, scikit-learn (HDBSCAN, metrics), umap-learn.

## Cluster naming and match rules
- Cluster name = most common category among its funds. If purity < 50%: "Mixed · A / B". Several clusters led by the same category get suffixes · A, · B (by size).
- Category **families**: Nordics (Sverige, Norden, Norge, Finland, Global & Sverige), Europe (Europa, Euroland, Tyskland, Italien, Spanien, Storbritannien, Östeuropa ex Ryssland), USA, Global, Emerging markets (Tillväxtmarknader*, Latinamerika, Brasilien, Afrika och Mellanöstern, Indien), Asia & Japan (Japan, Kina*, Hongkong, Taiwan, Korea, Asien*, ASEAN, Indonesien, Vietnam), Sector funds (Branschfond, *), Other (Övriga aktiefonder, Lång/kort). The family is the text before the first comma.
- Fund status: **agree** (own category = cluster name), **neighbouring** (same regional family, amber ring), **different family** (red ring; any two sector industries count as different), **not in a cluster** (no ring, not counted).

## Chart (artifact)
- Scatter, one dot per fund. **Dot colour = existing category** (hue per family, shades per region variant). Shape: circle = regional, diamond = sector fund, square = other.
- **Background = cluster territory**: grid (~220 cells wide) over the map; each cell takes the weighted vote of its 7 nearest funds' clusters within a radius; unclustered funds don't vote; thin lines between clusters.
- Controls: method selector (Cosine / Aitchison / Hellinger 60/40), show all or disagreements only, toggles for background, cluster names, rings; search by **name or ISIN** with a results list (Enter / click zooms and pins).
- Header stats: % agree, neighbouring, other family, no cluster, ARI, NMI.
- Side panel tabs: Categories (agree % per category, click to isolate), Clusters (composition, click to isolate), Disagreements (click to zoom).
- Tooltip: name, company, ISIN, category, cluster (size, purity), verdict, top countries and sectors as reported.
- Zoom (wheel), pan (drag), reset; light and dark themes.

## Results (single run, seed 42; stability from 5 seeds)
| | Cosine | Aitchison | Hellinger 60/40 |
|---|---|---|---|
| Clusters | 51 (47–58 across runs) | 32 (32–43) | 44 (40–47) |
| Not in a cluster | 64 | 35 | 43 |
| Median cluster size | 11 | 19 | 11 |
| Stability (ARI between runs) | 0.64 | 0.53 | 0.83 |
| Mean purity | 0.72 | 0.65 | 0.72 |
| NMI vs categories | 0.78 | 0.74 | 0.76 |
| Energy test pair together | 5/5 | 4/5 | 5/5 |
| Weights | none | none | 60/40 |

**Conclusion so far**: Cosine is the best method without weights. Stability is still too low to rely on.

## Test cases
- **LU0256331488** Schroder ISF Global Energy (Energi) should match **LU0612318385** BGF Natural Resources (Råvaror): same commodity geography (USA/Canada/UK), BGF ~40% energy. Category search can't link them; clustering does.
- **SE0015837778** Go Blockchain Fund A (Ny teknik): 95% USA, tech 38% / finance 38%. Allocations make it look like a US index fund. **SE0018042806** SEB Blockchain is 74% tech, so the two are not alike in allocation data; the shared theme isn't visible in country/sector data.

## Tried and rejected
- **Ward + cluster slider (6–60)**: user wants no fixed cluster count.
- **Silhouette-chosen cut**: gave 6 clusters for Aitchison (score flat 0.20–0.24 across 6–60), worse than categories for search.
- **Average linkage with cosine / Jensen-Shannon / Aitchison**: one giant cluster (250–340 funds) plus singletons.
- **HDBSCAN on raw coordinates**: ~40% of funds labelled noise.
- **Merging categories** (region only, Fastighetsbolag merged): replaced by verbatim categories.
- **Country/sector weights, separate clusterings, volatility feature**: ruled out by constraints.
- Leiden community detection on a 15-NN graph: 12–15 groups, too coarse.

## Findings
- Data has ~6–15 clearly separated groups at the top level; finer structure is a continuum, so density-based clustering (HDBSCAN) fits better than cutting a tree.
- Swedish large caps and small/mid caps separate well through sector mix (finance-heavy vs healthcare/tech/real estate).
- Size/style variants elsewhere (Europa Småbolag, Global Tillväxtbolag…) mostly don't separate on allocations.

## Next steps
- [ ] **Consensus clustering**: run UMAP + HDBSCAN with ~50 seeds, build a co-occurrence matrix, cluster that. Gives stable clusters and a "grouped together in X% of runs" confidence per fund pair.
- [ ] Test **Hellinger without weights** (square root only) for stability.
- [ ] **Search view**: for a selected fund, show its cluster peers ranked by distance, lines to the nearest funds, and "why similar".
- [ ] Collect 10–20 known similar / not-similar fund pairs to score search accuracy.
