# Fund Statistics CSV Export

Compute summary statistics from daily NAV data and export as CSV for exploratory data analysis with Claude.

![Statistics export window](IMG-STATISTICS-EXPORT.png)

## What it does

The Statistics Export feature reads your fund database (read-only), slices each fund's NAV history (limited by lookback period) into non-overlapping time windows, computes 13 summary statistics for each window, and writes the results to a CSV file.

**Key points:**

- Each fund produces **multiple rows** -- one per time window
- Only funds with `Buyable = 1` are included
- The source database is **never modified** (read-only access)
- Output is a standard CSV file (UTF-8, RFC 4180 compliant)
- A **metadata companion file** is also generated (see below)

## How to use

### Opening the window

In the main application window, click **Statistics export** from the toolbar/menu. This opens the Statistics Export dialog.

### Options

| Option | Description | Default |
| -------- | ------------- | --------- |
| **Window size** | How many calendar days each time window spans. NAV history is sliced into back-to-back chunks of this size. | 2 weeks (14 days) |
| **Lookback** | How far back in time to include NAV data. Only data from this many days ago to today is processed. | 6 months (180 days) |
| **Min owners** | Minimum number of fund owners required. Funds with fewer owners are excluded. | 100 |
| **Company filter** | Optional. If set, only exports funds from this company (case-insensitive match on `CompanyName`). Leave blank for all companies. | Empty (all) |
| **Output path** | Where to save the CSV file. Click **Browse** to pick a location. | `YieldRaccoon_stats_2weeks_6months.csv` |
| **Metadata output path** | Where to save the metadata companion CSV. Click **Browse** to pick a location. | `YieldRaccoon_metadata.csv` |

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
| 6 months | 180 | Last 6 months (default) |
| 1 year | 365 | Last 12 months |

### Running the export

1. Select your preferred window size and lookback period
2. Optionally set company filter and minimum owners
3. Choose an output path
4. Click **Export**
5. Wait for the progress indicator to complete
6. The status bar shows how many rows were written

## Statistics glossary

Each row in the CSV represents one fund in one time window. Here's what each column means:

| Column | Description | Formula / Notes |
| -------- | ------------- | ----------------- |
| `isin` | Fund ISIN identifier | e.g., `SE0000000001` |
| `name` | Fund display name | e.g., `Avanza Zero` |
| `period_start` | First date of the window | `YYYY-MM-DD` |
| `period_end` | Last date of the window | `YYYY-MM-DD` |
| `first_nav` | NAV on the first day of the window | Opening price |
| `last_nav` | NAV on the last day of the window | Closing price |
| `nav_high` | Highest NAV in the window | Peak price |
| `nav_low` | Lowest NAV in the window | Trough price |
| `total_return_pct` | Total return over the window | `(last_nav / first_nav - 1) * 100` |
| `ann_volatility` | Annualized volatility | `std(daily_returns) * sqrt(252) * 100` |
| `max_drawdown_pct` | Maximum peak-to-trough decline | Worst cumulative loss from any peak |
| `current_drawdown_pct` | Distance from period high at window end | `(last_nav - nav_high) / nav_high * 100` |
| `sharpe_ratio` | Risk-adjusted return (risk-free rate = 0) | `ann_return / ann_volatility` |
| `best_day_pct` | Best single-day return | Largest daily gain in `%` |
| `worst_day_pct` | Worst single-day return | Largest daily loss in `%` |
| `pct_positive_days` | Percentage of days with positive return | `positive_days / total_days * 100` |
| `skewness` | Return distribution asymmetry | Negative = left-skewed (tail risk), Positive = right-skewed |

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
1. Histogram of total_return_pct across all rows
2. Scatter plot: ann_volatility (x) vs total_return_pct (y), color by Sharpe ratio
3. Box plot of total_return_pct grouped by period_start (to see market-wide trends)
```

**Clustering analysis with visualization:**

```plaintext
Analyze this fund statistics CSV and identify natural clusters:
- Group funds by risk/return profile (volatility vs total return)
- Identify "steady growers" (high Sharpe, low volatility) vs "volatile performers"
- Which funds show consistently negative skewness (tail risk)?

Create charts:
1. Scatter plot of mean ann_volatility vs mean total_return_pct per fund, colored by cluster label
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

Create a multi-line chart showing total_return_pct over time (period_start on x-axis)
for these top 10 funds, one line per fund.
```

**Drawdown investigation:**

```plaintext
Find all fund-period combinations where max_drawdown_pct < -15%.
For each:
- What was the total return in the same period?
- Did the fund recover (current_drawdown_pct closer to 0)?
- How does the drawdown compare to the fund's typical volatility?

Create a scatter plot: max_drawdown_pct (x) vs total_return_pct (y) for these
severe drawdown periods, with point size proportional to ann_volatility.
```

**Company comparison:**

```plaintext
Compare funds from [Company A] vs [Company B]:
- Average return, volatility, and Sharpe across all periods
- Which company's funds have better risk-adjusted returns?
- Any significant differences in skewness or drawdown patterns?

Create charts:
1. Side-by-side box plots of total_return_pct, ann_volatility, and sharpe_ratio per company
2. Radar/spider chart comparing mean statistics for each company
```

### Step 3: Visualize the full landscape

**Risk-return map:**

```plaintext
Create a comprehensive risk-return map:
1. For each fund, compute mean total_return_pct and mean ann_volatility across all windows
2. Scatter plot with volatility on x-axis, return on y-axis
3. Color points by mean Sharpe ratio (diverging colormap: red for negative, green for positive)
4. Size points by number of time windows (more data = bigger dot)
5. Add quadrant labels: "Low Risk/High Return", "High Risk/High Return", etc.
6. Annotate the top 5 and bottom 5 funds by Sharpe ratio with their names
```

**Market regime timeline:**

```plaintext
Create a timeline chart showing market regimes:
1. For each period_start, compute the median total_return_pct across all funds
2. Plot as a bar chart (green for positive, red for negative periods)
3. Add a secondary y-axis showing median ann_volatility as a line
4. Highlight periods where median return < -5% as "stress periods"
```

**Drawdown heatmap:**

```plaintext
Create a heatmap with:
- Y-axis: fund names (sorted by average max_drawdown_pct)
- X-axis: period_start dates
- Color: max_drawdown_pct (darker = worse drawdown)
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

Create a chart showing the percentage of funds with negative total_return_pct
per period, with a horizontal line at 50% to mark "majority negative" periods.
```

**Portfolio construction hints:**

```plaintext
If I wanted to build a low-volatility portfolio from these funds:
- Which 5-10 funds have the best Sharpe ratios with ann_volatility < 10%?
- Are there funds that tend to be up when others are down (diversification)?
- What's the trade-off between return and drawdown protection?

Create an efficient frontier scatter plot: for each fund show mean ann_volatility
vs mean total_return_pct, and highlight the Pareto-optimal funds (best return
for given risk level) with a connecting line.
```

## Example CSV output

```csv
isin,name,period_start,period_end,first_nav,last_nav,nav_high,nav_low,total_return_pct,ann_volatility,max_drawdown_pct,current_drawdown_pct,sharpe_ratio,best_day_pct,worst_day_pct,pct_positive_days,skewness
SE0000000001,Avanza Zero,2026-01-05,2026-01-16,100.0000,102.3000,103.0000,99.5000,2.3000,11.2000,-1.5000,-0.6796,0.8500,1.2000,-0.9000,60.0000,0.1200
SE0000000001,Avanza Zero,2026-01-19,2026-01-30,102.3000,103.5000,104.0000,101.8000,1.1730,8.5000,-0.5000,-0.4808,0.9200,0.8000,-0.6000,55.5556,-0.0500
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

When you click **Export**, a second CSV file is generated alongside the statistics CSV. This metadata file contains one row per qualifying fund with static profile attributes — useful for joining with the statistics data during analysis.

**Default filename:** `YieldRaccoon_metadata.csv` (or `YieldRaccoon_metadata_{company}.csv` when a company filter is set)

**Filters applied:** Same as the statistics export — `Buyable = 1`, company name (if set), minimum number of owners.

### Metadata columns (17)

| Column | Description | Type |
| -------- | ------------- | ------ |
| `isin` | Fund ISIN identifier | Text |
| `name` | Fund display name | Text |
| `company_name` | Fund management company | Text |
| `currency_code` | ISO 4217 currency code (SEK, EUR, etc.) | Text |
| `category` | Fund category classification | Text |
| `fund_type` | e.g., Equity Fund, Bond Fund | Text |
| `is_index_fund` | Whether fund is index-tracking | `true`/`false`/empty |
| `managed_type` | ACTIVE or PASSIVE | Text |
| `total_fee` | Total expense ratio (decimal, e.g., 0.0125) | Decimal |
| `management_fee` | Annual management fee (decimal) | Decimal |
| `risk` | SRRI/SRI risk indicator (1-7) | Integer |
| `rating` | Star rating (1-5) | Integer |
| `sharpe_ratio` | Risk-adjusted return metric | Decimal |
| `standard_deviation` | Annualized volatility | Decimal |
| `recommended_holding_period` | Investor holding guidance | Text |
| `capital` | Total assets under management | Decimal |
| `number_of_owners` | Number of unique investors | Integer |

### Example metadata CSV

```csv
isin,name,company_name,currency_code,category,fund_type,is_index_fund,managed_type,total_fee,management_fee,risk,rating,sharpe_ratio,standard_deviation,recommended_holding_period,capital,number_of_owners
SE0000000001,Avanza Zero,Avanza Fonder,SEK,Equity,Equity Fund,true,PASSIVE,0,0,4,3,1.25,12.5,5 years,1000000,50000
```
