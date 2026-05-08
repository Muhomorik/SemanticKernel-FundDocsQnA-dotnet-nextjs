# Fund Statistics Export — Agent Guide

Concise schema reference for AI agents reading the three weekly CSV exports. Optimized for inclusion in agent context — read top-down, stop when you have what you need.

## Overview

YieldRaccoon emits three CSVs per ISO week, all sharing one `{family}_{iso_week}` filename suffix:

| Kind | Granularity | Holds |
|---|---|---|
| `summary` | many rows per fund (~26/year) | per-bucket bi-weekly history |
| `snapshot` | one row per fund | rolling 12-week + 1-year metrics, anchored at the latest NAV date |
| `metadata` | one row per fund | static identity (name, fee, category, owners, …) |

Join all three on `isin`. Same week's bundle joins by simple filename glob `YieldRaccoon_*_{family}_{iso_week}.csv`.

## Filename grammar

`YieldRaccoon_{kind}_{family}_{iso_week}.csv`

| Segment | Values |
|---|---|
| `kind` | `summary`, `snapshot`, `metadata` |
| `family` | `all` (no company filter) or sanitized lower-cased company name |
| `iso_week` | ISO 8601 week — `YYYY-Www`, e.g. `2026-W18`. Uses ISO week-year, so 2027-01-01 may belong to `2026-W53`. |

Example: `YieldRaccoon_snapshot_all_2026-W18.csv`.

> **Casing note:** the filename `family` segment is **lower-cased** (e.g. `schroder`), but the metadata `company_name` column **preserves original case** (`Schroder`). To match a fund by company, lowercase one side or compare case-insensitively — never assume direct equality.

## Per-file schemas

### summary.csv (17 columns)

| Column | Type | Unit | Meaning |
|---|---|---|---|
| `isin` | string | — | Fund identifier (join key) |
| `name` | string | — | Display name |
| `period_start`, `period_end` | date | YYYY-MM-DD | Bucket bounds (~14 days) |
| `first_nav`, `last_nav`, `nav_high`, `nav_low` | number | abs | NAV bookends + extremes |
| `return_2w_pct` | number | percent | Compound return over the bucket |
| `ann_volatility_2w_pct` | number | percent (annualized) | std(daily log-returns) × √252 × 100 |
| `max_drawdown_2w_pct` | number | percent (≤0) | Worst peak-to-trough within the bucket |
| `current_drawdown_pct` | number | percent (≤0) | Distance from period high at `period_end` |
| `sharpe_2w` | number | ratio | Risk-adjusted return (rf=0). `NaN` when vol < 0.01% |
| `best_day_pct`, `worst_day_pct` | number | percent | Largest single-day gain / loss |
| `pct_positive_days` | number | percent | Share of trading days with positive return |
| `skewness` | number | dimensionless | Daily-return distribution asymmetry; negative = tail risk |

### snapshot.csv (10 columns)

| Column | Type | Unit | Meaning |
|---|---|---|---|
| `isin` | string | — | Fund identifier (join key) |
| `as_of_date` | date | YYYY-MM-DD | Evaluation date — **identical on every row in the file** |
| `return_12w_compound_pct` | number | percent | Compound return over trailing 84 days: `(nav_last / nav_first − 1) × 100` |
| `ann_volatility_12w_pct` | number | percent (annualized) | `std(daily_simple_returns) × √252 × 100` (sample stddev, n−1 denominator) |
| `sharpe_12w` | number | ratio | Risk-adjusted return at 12w (rf=0). `NaN` when vol < 0.01% |
| `max_drawdown_12w_pct` | number | percent (≤0) | Worst trailing-12w peak-to-trough |
| `return_1y_compound_pct`, `ann_volatility_1y_pct`, `sharpe_1y`, `max_drawdown_1y_pct` | number | as above | Same four metrics over trailing 365 days |

**Sharpe formula (for replication):**

```text
daily_returns = nav[i] / nav[i-1] − 1        # simple returns, NOT log returns
sharpe_H = mean(daily_returns) × √252 / std(daily_returns)        # rf = 0
```

Equivalent to `(mean × 252) / (std × √252)`. Both `mean` and `std` use the same daily-return series; std is sample (Bessel-corrected). For small daily moves, simple ≈ log returns; if you replicate with `np.log(nav).diff()` you'll see ~basis-point differences.

### metadata.csv (17 columns)

| Column | Type | Meaning |
|---|---|---|
| `isin`, `name` | string | Identity — always populated |
| `company_name`, `currency_code`, `category`, `fund_type` | string | Identity — usually populated; can be empty for new/incomplete funds |
| `is_index_fund` | string `"true"` / `"false"` / empty | Literal lower-case strings, **not** JSON booleans — parse with `df['is_index_fund'].map({'true': True, 'false': False})` |
| `managed_type` | string | `ACTIVE` / `PASSIVE` |
| `total_fee`, `management_fee` | number | **Percent points**, not decimal fraction. `1.25` means 1.25 % expense ratio (not 125 %, not 0.0125). |
| `risk` | int (1–7) | SRRI/SRI risk indicator — typically populated for buyable funds |
| `rating` | int (1–5) or empty | Star rating — **frequently empty** for newer funds and some specialty categories |
| `sharpe_ratio`, `standard_deviation` | number | **Static** fund-house-published values — *not* the computed `sharpe_2w` from summary. `standard_deviation` is also percent points. |
| `recommended_holding_period` | string (enum) | Upper-case enum literal — e.g. `ONE_YEAR`, `THREE_YEAR`, `FIVE_YEAR`, `TEN_YEAR`. Not human-formatted. |
| `capital` | number | AUM in fund's reporting currency |
| `number_of_owners` | int | Investor count — minimum-owners filter at export time guarantees this is populated |

> **Nullability:** every column except `isin`, `name`, and `number_of_owners` (the last two are filter-guarded at export time) can be empty. Don't assume non-null on `rating`, `sharpe_ratio`, `standard_deviation`, fees, or category strings — handle empty cells defensively.

## Example data rows

`summary.csv`:

```
isin,name,period_start,period_end,first_nav,last_nav,nav_high,nav_low,return_2w_pct,ann_volatility_2w_pct,max_drawdown_2w_pct,current_drawdown_pct,sharpe_2w,best_day_pct,worst_day_pct,pct_positive_days,skewness
SE0000000001,Avanza Zero,2026-01-05,2026-01-16,100.0000,102.3000,103.0000,99.5000,2.3000,11.2000,-1.5000,-0.6796,0.8500,1.2000,-0.9000,60.0000,0.1200
SE0000000002,Bond Money Market,2026-01-05,2026-01-16,50.0000,50.0010,50.0011,49.9999,0.0020,0.0050,-0.0024,-0.0020,NaN,0.0008,-0.0007,55.0000,0.0100
```

`snapshot.csv`:

```
isin,as_of_date,return_12w_compound_pct,ann_volatility_12w_pct,sharpe_12w,max_drawdown_12w_pct,return_1y_compound_pct,ann_volatility_1y_pct,sharpe_1y,max_drawdown_1y_pct
SE0000000001,2026-04-30,4.2300,11.5000,1.4500,-2.7000,18.6500,12.3000,1.5100,-7.4000
SE0000000002,2026-04-30,0.0500,0.0080,NaN,-0.0010,NaN,NaN,NaN,NaN
```

`metadata.csv`:

```
isin,name,company_name,currency_code,category,fund_type,is_index_fund,managed_type,total_fee,management_fee,risk,rating,sharpe_ratio,standard_deviation,recommended_holding_period,capital,number_of_owners
SE0000000001,Avanza Zero,Avanza Fonder,SEK,Equity,Equity Fund,true,PASSIVE,0,0,4,3,1.25,12.5,FIVE_YEAR,1000000,50000
SE0000000002,Active Equity,Some Asset Mgmt,SEK,Equity,Equity Fund,false,ACTIVE,2.17,1.50,5,3,0.78,15.6,THREE_YEAR,3500000,12500
```

Note the fee columns: `total_fee=2.17` means 2.17 % expense ratio (percent points, not a `0.0217` decimal fraction).

## Pitfalls

- **`NaN` always means insufficient data or volatility-guard suppression.** Never zero-fill, never drop the row outright (drop only when filtering by a NaN column).
- **`sharpe_*` columns can be `NaN`** when the corresponding `ann_volatility_*` < 0.01 %. This is intentional — guards against bond-fund Sharpe explosions on near-constant NAV.
- **Threshold `0.01` is on the percent scale**, not the raw decimal scale. Vol of 0.005 (% annualized) trips the guard; vol of 0.5 (= 0.5 %) does not.
- **summary `period_end` is monotonic per ISIN** — required for "last N positive windows" logic.
- **summary trailing windows < 7 days are dropped**, so the latest `period_end` per ISIN may be a few days behind the snapshot's `as_of_date`.
- **snapshot `as_of_date` is identical on every row** — it is the latest NAV date in the producer's database, not "today". Use it as the anchor for any "as-of" reasoning.
- **`return_2w_pct` (summary) vs `return_12w_compound_pct` / `return_1y_compound_pct` (snapshot)** — different horizons, different files. Don't conflate.
- **metadata `sharpe_ratio` is static fund-house-published**, not a computed time-series Sharpe. Use summary/snapshot for live Sharpe.
- **metadata `total_fee` and `management_fee` are percent points, not decimals.** A value of `1.25` means 1.25 %. Don't multiply by 100. Same scale convention as summary/snapshot percent columns.
- **metadata `is_index_fund` is the literal string `"true"` / `"false"`** (or empty). Pandas reads it as object dtype; cast explicitly if you need a Python bool.
- **metadata `recommended_holding_period` is an upper-case enum string** (`ONE_YEAR`, `THREE_YEAR`, `FIVE_YEAR`, `TEN_YEAR`, …) — not a human-formatted "5 years". Map / parse it before display.
- **All CSVs are UTF-8, RFC 4180.** Pandas `read_csv(..., parse_dates=['period_start','period_end'])` (or `['as_of_date']`) handles them out of the box.

## Deeper docs

- [summary-csv-plan.md](summary-csv-plan.md) — full validation rules, edge cases, partial-bucket policy
- [snapshot-csv-plan.md](snapshot-csv-plan.md) — anchoring rule, insufficient-history thresholds, immutability invariant
- [FUND-STATISTICS-EXPORT.md](FUND-STATISTICS-EXPORT.md) — user-facing manual + Claude analysis prompts
