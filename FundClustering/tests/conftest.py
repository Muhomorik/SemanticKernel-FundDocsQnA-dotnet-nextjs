"""Shared fixtures: tiny synthetic weekly exports written to a temp folder."""

from pathlib import Path

import pandas as pd
import pytest

from fund_clustering.load import FundData, load_week

WEEK = "2026-W01"


def write_week(
    data_dir: Path, week: str, allocations: list[dict], metadata: list[dict]
) -> None:
    pd.DataFrame(allocations).to_csv(
        data_dir / f"YieldRaccoon_allocations_all_{week}.csv", index=False
    )
    pd.DataFrame(metadata).to_csv(
        data_dir / f"YieldRaccoon_metadata_all_{week}.csv", index=False
    )


@pytest.fixture
def data_dir(tmp_path: Path) -> Path:
    """One week with two equity funds (one holding a short) and one bond fund."""
    allocations = [
        {"isin": "SE0000000002", "name": "Example Index Fund",
         "country_sverige": 100.0, "country_usa": 0.0,
         "sector_finans": 60.0, "sector_teknik": 40.0},
        {"isin": "SE0000000001", "name": "Example Long Short Fund",
         "country_sverige": 110.0, "country_usa": -10.0,
         "sector_finans": 50.0, "sector_teknik": 50.0},
        {"isin": "SE0000000003", "name": "Example Bond Fund",
         "country_sverige": 50.0, "country_usa": 50.0,
         "sector_finans": 100.0, "sector_teknik": 0.0},
    ]
    metadata = [
        {"isin": "SE0000000001", "name": "Metadata Name", "category": "Sverige",
         "fund_type": "EQUITY_FUND", "company_name": "AcmeCo", "is_index_fund": False,
         "total_fee": 1.2},
        {"isin": "SE0000000002", "name": "Metadata Name", "category": "Sverige",
         "fund_type": "EQUITY_FUND", "company_name": "AcmeCo", "is_index_fund": True,
         "total_fee": 0.2},
        {"isin": "SE0000000003", "name": "Metadata Name", "category": "Räntefond",
         "fund_type": "INTEREST_FUND", "company_name": "AcmeCo", "is_index_fund": False,
         "total_fee": 0.4},
    ]
    write_week(tmp_path, WEEK, allocations, metadata)
    return tmp_path


@pytest.fixture
def data(data_dir: Path) -> FundData:
    return load_week(data_dir, WEEK)
