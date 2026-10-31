"""Load one weekly YieldRaccoon export: allocations joined with metadata."""

import re
from dataclasses import dataclass
from pathlib import Path

import pandas as pd

COUNTRY_PREFIX = "country_"
SECTOR_PREFIX = "sector_"
EQUITY_FUND = "EQUITY_FUND"

# Metadata columns carried into the fund table (name comes from allocations).
METADATA_COLUMNS = ["isin", "category", "fund_type", "company_name", "is_index_fund"]

_ALLOCATIONS_PATTERN = re.compile(r"YieldRaccoon_allocations_all_(\d{4}-W\d{2})\.csv")


@dataclass(frozen=True)
class FundData:
    """Equity funds for one week, with their allocation columns.

    Allocation values are kept exactly as delivered (percent, short positions
    included). Column lists are discovered from the file, since the export adds
    countries as they appear.
    """

    week: str
    funds: pd.DataFrame
    country_columns: list[str]
    sector_columns: list[str]

    @property
    def allocation_columns(self) -> list[str]:
        return self.country_columns + self.sector_columns


def latest_week(data_dir: Path | str) -> str:
    """Return the newest week (e.g. ``2026-W44``) with an allocations export."""
    weeks = [
        match.group(1)
        for path in Path(data_dir).glob("YieldRaccoon_allocations_all_*.csv")
        if (match := _ALLOCATIONS_PATTERN.fullmatch(path.name))
    ]
    if not weeks:
        raise FileNotFoundError(f"No allocations export found in {data_dir}")
    return max(weeks)


def load_week(data_dir: Path | str, week: str) -> FundData:
    """Load allocations + metadata for ``week`` and keep equity funds only."""
    data_dir = Path(data_dir)
    allocations = pd.read_csv(data_dir / f"YieldRaccoon_allocations_all_{week}.csv")
    metadata = pd.read_csv(data_dir / f"YieldRaccoon_metadata_all_{week}.csv")

    funds = allocations.merge(
        metadata[METADATA_COLUMNS], on="isin", how="inner", validate="one_to_one"
    )
    funds = funds[funds["fund_type"] == EQUITY_FUND]
    funds = funds.sort_values("isin").reset_index(drop=True)

    return FundData(
        week=week,
        funds=funds,
        country_columns=[c for c in funds.columns if c.startswith(COUNTRY_PREFIX)],
        sector_columns=[c for c in funds.columns if c.startswith(SECTOR_PREFIX)],
    )
