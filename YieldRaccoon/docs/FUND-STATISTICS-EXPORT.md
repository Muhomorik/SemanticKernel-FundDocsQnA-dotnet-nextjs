# Fund Statistics CSV Export

Compute summary statistics from daily NAV data and export as CSV for exploratory data analysis with Claude.

> **For AI agents reading these CSVs:** see [FUND-STATISTICS-EXPORT-AGENT-GUIDE.md](FUND-STATISTICS-EXPORT-AGENT-GUIDE.md) — concise schema reference designed for inclusion in agent context.

![Statistics export window](IMG-STATISTICS-EXPORT.png)

## What it does

A single Export click writes **four CSVs** into the same folder, all sharing one ISO-week filename tag (`YieldRaccoon_*_{family}_{iso_week}.csv`):

| File | What it holds | Granularity |
| ------ | ------ | ------ |
| `YieldRaccoon_summary_{family}_{iso_week}.csv` | Per-bucket bi-weekly history (return, volatility, Sharpe, drawdown, skew, etc.) | ~26 rows per fund per year |
| `YieldRaccoon_snapshot_{family}_{iso_week}.csv` | Per-fund rolling 12-week + 1-year metrics anchored at the latest NAV date | 1 row per fund |
| `YieldRaccoon_metadata_{family}_{iso_week}.csv` | Static fund identity (name, fee, category, owners, …) | 1 row per fund |
| `YieldRaccoon_allocations_{family}_{iso_week}.csv` | Latest country + sector portfolio allocations (wide format) | 1 row per fund, ~50–100 columns |

The source database is **read-only** — nothing is modified. Output is UTF-8, RFC 4180 compliant.

**Key points:**

- `{family}` is `all` when no company filter is set, or the lower-cased company name otherwise
- `{iso_week}` is the ISO 8601 week designation (e.g., `2026-W18`) — re-running the same week overwrites the same files (immutability invariant)
- Only funds with `Buyable = 1` are included; min-owners filter and optional company filter apply identically across all three files

## How to use

### Opening the window

In the main application window, click **Statistics export** from the toolbar/menu. This opens the Statistics Export dialog.

### Options

| Option | Description | Default |
| -------- | ------------- | --------- |
| **Window size** | How many calendar days each time window spans. NAV history is sliced into back-to-back chunks of this size. | 2 weeks (14 days) |
| **Lookback** | How far back in time to include NAV data. Only data from this many days ago to today is processed. | 1 year (365 days) |
| **Min owners** | Minimum number of fund owners required. Funds with fewer owners are excluded. | 100 |
| **Company filter** | Optional. If set, only exports funds from this company (case-insensitive match on `CompanyName`). Leave blank for all companies. | Empty (all) |
| **Summary output path** | Where to save the per-bucket history CSV. Click **Browse** to pick a location. | `YieldRaccoon_summary_all_{iso_week}.csv` |
| **Snapshot output path** | Where to save the per-fund rolling-horizon CSV. | `YieldRaccoon_snapshot_all_{iso_week}.csv` |
| **Metadata output path** | Where to save the metadata companion CSV. | `YieldRaccoon_metadata_all_{iso_week}.csv` |
| **Allocations output path** | Where to save the country + sector allocations companion CSV. | `YieldRaccoon_allocations_all_{iso_week}.csv` |

### Available window sizes

| Label | Days | Typical rows per fund (6 months of data) |
| ------- | ------ | ------------------------------------------ |
| 1 week | 7 | ~26 |
| 2 weeks | 14 | ~13 |
| 3 weeks | 21 | ~9 |
| 1 month | 30 | ~6 |
| 3 months | 90 | ~2 |

### Available lookback periods

| Label | Days | Description |
| ------- | ------ | ------------- |
| 1 month | 30 | Only the last month of NAV data |
| 2 months | 60 | Last 2 months |
| 3 months | 90 | Last 3 months |
| 6 months | 180 | Last 6 months |
| 1 year | 365 | Last 12 months (default) |

### Running the export

1. Select your preferred window size and lookback period
2. Optionally set company filter and minimum owners
3. Choose an output path
4. Click **Export**
5. Wait for the progress indicator to complete
6. The status bar shows how many rows were written

## Summary CSV — column glossary

Each row represents one fund in one bi-weekly bucket. The four `_2w_*` columns name their horizon explicitly to disambiguate from snapshot.csv's `_12w` / `_1y` counterparts.

| Column | Description | Formula / Notes |
| -------- | ------------- | ----------------- |
| `isin` | Fund ISIN identifier | e.g., `SE0000000001` |
| `name` | Fund display name | e.g., `Example Index Fund` |
| `period_start` | First date of the window | `YYYY-MM-DD` |
| `period_end` | Last date of the window | `YYYY-MM-DD` |
| `first_nav`, `last_nav`, `nav_high`, `nav_low` | NAV bookends + extremes | absolute values |
| `return_2w_pct` | Total return over the bucket | `(last_nav / first_nav - 1) * 100` |
| `ann_volatility_2w_pct` | Annualized volatility | `std(daily_returns) * sqrt(252) * 100` |
| `max_drawdown_2w_pct` | Maximum peak-to-trough decline within the bucket | non-positive |
| `current_drawdown_pct` | Distance from period high at window end | `(last_nav - nav_high) / nav_high * 100` |
| `sharpe_2w` | Risk-adjusted return (risk-free rate = 0) | `NaN` when `ann_volatility_2w_pct < 0.01` |
| `best_day_pct`, `worst_day_pct` | Best/worst single-day return in the bucket | `%` |
| `pct_positive_days` | Percentage of days with positive return | `positive_days / total_days * 100` |
| `skewness` | Daily-return distribution asymmetry | Negative = left-skewed (tail risk) |

Trailing partial buckets spanning fewer than 7 days are dropped — they contaminate downstream "X of N positive windows" counting.

## Snapshot CSV — column glossary

One row per fund, anchored at `as_of_date` (= the most recent NAV date in the database, identical on every row).

| Column | Description |
| -------- | ------------- |
| `isin` | Fund ISIN identifier |
| `as_of_date` | Evaluation date (`YYYY-MM-DD`) — same on every row in the file |
| `return_12w_compound_pct` | Compound return over trailing 84 days |
| `ann_volatility_12w_pct` | Annualized volatility over trailing 84 days |
| `sharpe_12w` | Risk-adjusted return at 12-week horizon (`NaN` when vol < 0.01) |
| `max_drawdown_12w_pct` | Worst peak-to-trough decline within trailing 84 days |
| `return_1y_compound_pct`, `ann_volatility_1y_pct`, `sharpe_1y`, `max_drawdown_1y_pct` | Same four metrics over trailing 365 days |

Funds with shorter history than the horizon get `NaN` for that horizon's four columns. `NaN` always means "insufficient data" — never zero-fill.

## How to use with Claude

The CSV output is designed to fit within Claude's context window. With 1,400 funds and 2-week windows over 3 months, you get ~8,400 rows (~85K tokens) -- well within limits.

### Step 1: Upload and explore

Upload the CSV file to Claude (claude.ai) and use one of these prompts. Claude's analysis tool runs Python with matplotlib/seaborn, so asking for charts will produce real visualizations.

**Initial exploration with charts:**

```plaintext
Here's a CSV of fund summary statistics. Each row represents one fund in one 2-week time window.
Give me an overview:
- How many unique funds and time periods?
- Distribution of total returns, volatility, and Sharpe ratios
- Any obvious outliers or clusters?
- Which funds have the most extreme drawdowns?

Create charts:
1. Histogram of return_2w_pct across all rows
2. Scatter plot: ann_volatility_2w_pct (x) vs return_2w_pct (y), color by Sharpe ratio
3. Box plot of return_2w_pct grouped by period_start (to see market-wide trends)
```

**Clustering analysis with visualization:**

```plaintext
Analyze this fund statistics CSV and identify natural clusters:
- Group funds by risk/return profile (volatility vs total return)
- Identify "steady growers" (high Sharpe, low volatility) vs "volatile performers"
- Which funds show consistently negative skewness (tail risk)?

Create charts:
1. Scatter plot of mean ann_volatility_2w_pct vs mean return_2w_pct per fund, colored by cluster label
2. Heatmap of cluster centroids across all 13 statistics
3. Distribution plot (violin or box) of Sharpe ratios per cluster
```

### Step 2: Drill down

Once you spot interesting patterns, ask Claude to dig deeper:

**Trend analysis across windows:**

```plaintext
For the top 10 funds by average Sharpe ratio:
- Show how their volatility and return changed across time windows
- Did any fund's risk profile shift dramatically between periods?
- Which funds were most consistent vs most variable?

Create a multi-line chart showing return_2w_pct over time (period_start on x-axis)
for these top 10 funds, one line per fund.
```

**Drawdown investigation:**

```plaintext
Find all fund-period combinations where max_drawdown_2w_pct < -15%.
For each:
- What was the total return in the same period?
- Did the fund recover (current_drawdown_pct closer to 0)?
- How does the drawdown compare to the fund's typical volatility?

Create a scatter plot: max_drawdown_2w_pct (x) vs return_2w_pct (y) for these
severe drawdown periods, with point size proportional to ann_volatility_2w_pct.
```

**Company comparison:**

```plaintext
Compare funds from [Company A] vs [Company B]:
- Average return, volatility, and Sharpe across all periods
- Which company's funds have better risk-adjusted returns?
- Any significant differences in skewness or drawdown patterns?

Create charts:
1. Side-by-side box plots of return_2w_pct, ann_volatility_2w_pct, and sharpe_2w per company
2. Radar/spider chart comparing mean statistics for each company
```

### Step 3: Visualize the full landscape

**Risk-return map:**

```plaintext
Create a comprehensive risk-return map:
1. For each fund, compute mean return_2w_pct and mean ann_volatility_2w_pct across all windows
2. Scatter plot with volatility on x-axis, return on y-axis
3. Color points by mean Sharpe ratio (diverging colormap: red for negative, green for positive)
4. Size points by number of time windows (more data = bigger dot)
5. Add quadrant labels: "Low Risk/High Return", "High Risk/High Return", etc.
6. Annotate the top 5 and bottom 5 funds by Sharpe ratio with their names
```

**Market regime timeline:**

```plaintext
Create a timeline chart showing market regimes:
1. For each period_start, compute the median return_2w_pct across all funds
2. Plot as a bar chart (green for positive, red for negative periods)
3. Add a secondary y-axis showing median ann_volatility_2w_pct as a line
4. Highlight periods where median return < -5% as "stress periods"
```

**Drawdown heatmap:**

```plaintext
Create a heatmap with:
- Y-axis: fund names (sorted by average max_drawdown_2w_pct)
- X-axis: period_start dates
- Color: max_drawdown_2w_pct (darker = worse drawdown)
- This shows which funds suffered during which periods at a glance
Limit to top 50 funds by number of owners if there are too many.
```

### Step 4: Advanced analysis

**Regime detection:**

```plaintext
Looking at all funds across time windows:
- Are there periods where most funds had negative returns simultaneously (market stress)?
- Identify "regime changes" where volatility spiked across many funds
- Which funds were most resilient during high-volatility periods?

Create a chart showing the percentage of funds with negative return_2w_pct
per period, with a horizontal line at 50% to mark "majority negative" periods.
```

**Portfolio construction hints:**

```plaintext
If I wanted to build a low-volatility portfolio from these funds:
- Which 5-10 funds have the best Sharpe ratios with ann_volatility_2w_pct < 10%?
- Are there funds that tend to be up when others are down (diversification)?
- What's the trade-off between return and drawdown protection?

Create an efficient frontier scatter plot: for each fund show mean ann_volatility_2w_pct
vs mean return_2w_pct, and highlight the Pareto-optimal funds (best return
for given risk level) with a connecting line.
```

## Example CSV output

```csv
isin,name,period_start,period_end,first_nav,last_nav,nav_high,nav_low,return_2w_pct,ann_volatility_2w_pct,max_drawdown_2w_pct,current_drawdown_pct,sharpe_2w,best_day_pct,worst_day_pct,pct_positive_days,skewness
SE0000000001,Example Index Fund,2026-01-05,2026-01-16,100.0000,102.3000,103.0000,99.5000,2.3000,11.2000,-1.5000,-0.6796,0.8500,1.2000,-0.9000,60.0000,0.1200
SE0000000001,Example Index Fund,2026-01-19,2026-01-30,102.3000,103.5000,104.0000,101.8000,1.1730,8.5000,-0.5000,-0.4808,0.9200,0.8000,-0.6000,55.5556,-0.0500
SE0000000002,Example Fund,2026-01-05,2026-01-16,50.0000,49.2000,50.5000,48.8000,-1.6000,15.3000,-3.2000,-2.5743,-0.5500,1.0000,-2.1000,44.4444,-0.3000
```

## Token budget reference

Rows depend on both the **lookback period** and the **window size**. The table below assumes 1,400 funds:

| Lookback | Window size | Rows per fund | Total rows | Approx. tokens |
| ---------- | ------------- | --------------- | ------------ | ---------------- |
| 3 months | 2 weeks | ~6 | ~8,400 | ~85K |
| 6 months | 2 weeks | ~13 | ~18,200 | ~180K |
| 6 months | 1 month | ~6 | ~8,400 | ~85K |
| 1 year | 2 weeks | ~26 | ~36,400 | ~365K (too large) |
| 1 year | 1 month | ~12 | ~16,800 | ~170K |

**Recommendation:** Start with **6 months lookback + 2 weeks window** or **3 months + 2 weeks** for a good balance between coverage and token budget. If the CSV is too large for a single Claude conversation, use the company filter to export subsets.

## Metadata companion file

The metadata file holds one row per qualifying fund with static profile attributes — useful for joining with summary or snapshot rows via `isin`.

**Default filename:** `YieldRaccoon_metadata_{family}_{iso_week}.csv` (family = `all` or sanitized lower-cased company name)

**Filters applied:** Same as the summary export — `Buyable = 1`, optional company name, minimum number of owners.

### Metadata columns (17)

| Column | Description | Type |
| -------- | ------------- | ------ |
| `isin` | Fund ISIN identifier | Text |
| `name` | Fund display name | Text |
| `company_name` | Fund management company | Text |
| `currency_code` | ISO 4217 currency code (SEK, EUR, etc.) | Text |
| `category` | Fund category classification | Text |
| `fund_type` | e.g., Equity Fund, Bond Fund | Text |
| `is_index_fund` | Whether fund is index-tracking — **literal string** `"true"` / `"false"`, not a JSON bool | Text |
| `managed_type` | ACTIVE or PASSIVE | Text |
| `total_fee` | Total expense ratio in **percent points** (e.g. `1.25` = 1.25 %) — not a `0.0125` decimal fraction | Number |
| `management_fee` | Annual management fee in **percent points** (e.g. `1.50` = 1.50 %) | Number |
| `risk` | SRRI/SRI risk indicator (1–7) | Integer |
| `rating` | Star rating (1–5) | Integer |
| `sharpe_ratio` | Static fund-house-published Sharpe (not the computed `sharpe_2w` from summary) | Number |
| `standard_deviation` | Annualized volatility in percent points | Number |
| `recommended_holding_period` | Upper-case enum literal — `ONE_YEAR`, `THREE_YEAR`, `FIVE_YEAR`, `TEN_YEAR`, … | Text |
| `capital` | Total assets under management (in fund's reporting currency) | Number |
| `number_of_owners` | Number of unique investors | Integer |

### Example metadata CSV

```csv
isin,name,company_name,currency_code,category,fund_type,is_index_fund,managed_type,total_fee,management_fee,risk,rating,sharpe_ratio,standard_deviation,recommended_holding_period,capital,number_of_owners
SE0000000001,Example Index Fund,Example Asset Mgmt,SEK,Equity,Equity Fund,true,PASSIVE,0,0,4,3,1.25,12.5,FIVE_YEAR,1000000,50000
SE0000000002,Active Equity,Some Asset Mgmt,SEK,Equity,Equity Fund,false,ACTIVE,2.17,1.50,5,3,0.78,15.6,THREE_YEAR,3500000,12500
```

> Note: in the metadata file, `sharpe_ratio` is the static fund-house-published Sharpe number from the FundProfiles table (not a computed value). Don't confuse it with summary's `sharpe_2w` or snapshot's `sharpe_12w` / `sharpe_1y`.

## Allocations companion file

The allocations file holds one row per qualifying fund in **wide format** — country and sector portfolio allocations exposed as one column per category. Designed to drop straight into pandas/sklearn for clustering.

**Default filename:** `YieldRaccoon_allocations_{family}_{iso_week}.csv` (family = `all` or sanitized lower-cased company name)

**Filters applied:** Same as the other exports (`Buyable = 1`, optional company name, minimum number of owners) **plus** an additional rule: a fund is excluded if it has **no rows in either** the `FundCountryAllocations` or the `FundSectorAllocations` table. This means funds whose portfolio page hasn't been crawled yet are silently dropped — `0`-fill is reserved for "fund holds none of this category", not "haven't checked".

### Schema

| Block | Columns | Notes |
| ------- | --------- | ------- |
| Identity | `isin`, `name` | Always present |
| Countries | `country_<sanitized>` × N | One column per row in the `Countries` lookup table |
| Sectors | `sector_<sanitized>` × M | One column per row in the `Sectors` lookup table |

**Column ordering:** `isin`, `name`, then country columns alphabetically by sanitized suffix, then sector columns alphabetically by sanitized suffix.

**Cell values:** decimal percentages 0–100. Missing allocations are emitted as the literal `0` (the source page only lists non-zero entries — absence unambiguously means zero).

### Column-name sanitization

Country/sector display names are folded to ASCII-only column suffixes via:

1. Unicode normalize (FormD) and strip combining marks: `Hälsovård` → `Halsovard`, `Råvaror` → `Ravaror`, `Élève` → `Eleve`.
2. Lowercase: `Halsovard` → `halsovard`.
3. Replace whitespace + non-alphanumeric runs with single `_`: `North America` → `north_america`, `Telecom & Media` → `telecom_media`.
4. Trim trailing `_`.

If two source names sanitize to the same suffix (e.g. `Curaçao` and `Curacao` both → `country_curacao`), the export **throws** with both names in the message — resolve in the source data and re-run.

### Important caveats for consumers

- **The column set is not stable across exports.** When a new country or sector appears in subsequent crawls, it shows up as a new column in the next export. Always read the header row and select columns by prefix:
  ```python
  import pandas as pd
  df = pd.read_csv('YieldRaccoon_allocations_all_2026-W19.csv')
  country_cols = [c for c in df.columns if c.startswith('country_')]
  sector_cols  = [c for c in df.columns if c.startswith('sector_')]
  X = df[country_cols + sector_cols].to_numpy()  # ready for sklearn
  ```
- **`0` ≠ `NaN`.** A `0` cell means the fund verifiably holds none of that category (the source page lists only non-zero allocations). Funds where the data isn't known are absent from the file entirely.
- **Per-fund row totals usually sum to ~100** (per kind), but not always — the source occasionally reports cash positions that aren't broken out into sectors, leaving sector totals slightly under 100. Don't assume strict normalization.
- **Original Swedish display names are not preserved in column headers** — see sanitization rules above. If you need the source labels, query the SQLite `Countries` / `Sectors` tables directly.

### Example allocations CSV

```csv
isin,name,country_storbritannien,country_sverige,country_usa,sector_industri,sector_teknik
SE0000000001,Example Index Fund,5.20,12.30,45.20,18.40,30.10
SE0000000002,Tech Focus,0,0,82.00,0,95.50
SE0000000003,UK Bond,68.00,0,4.50,0,0
```
