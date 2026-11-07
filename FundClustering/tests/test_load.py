from pathlib import Path

import pytest

from fund_clustering.load import available_weeks, latest_week, load_week
from tests.conftest import WEEK, write_week


def test_load_week_non_equity_funds_are_dropped(data_dir: Path) -> None:
    data = load_week(data_dir, WEEK)

    assert list(data.funds["isin"]) == ["SE0000000001", "SE0000000002"]


def test_load_week_columns_discovered_from_file(data_dir: Path) -> None:
    data = load_week(data_dir, WEEK)

    assert data.country_columns == ["country_sverige", "country_usa"]
    assert data.sector_columns == ["sector_finans", "sector_teknik"]
    assert data.allocation_columns == data.country_columns + data.sector_columns


def test_load_week_short_positions_kept_as_delivered(data_dir: Path) -> None:
    data = load_week(data_dir, WEEK)

    fund = data.funds.set_index("isin").loc["SE0000000001"]
    assert fund["country_sverige"] == 110.0
    assert fund["country_usa"] == -10.0


def test_load_week_name_from_allocations_and_only_selected_metadata(
    data_dir: Path,
) -> None:
    data = load_week(data_dir, WEEK)

    assert data.funds.loc[0, "name"] == "Example Long Short Fund"
    assert data.funds.loc[0, "company_name"] == "AcmeCo"
    assert "total_fee" not in data.funds.columns


def test_load_week_missing_week_raises(data_dir: Path) -> None:
    missing_week = "2026-W02"

    with pytest.raises(FileNotFoundError):
        load_week(data_dir, missing_week)


def test_latest_week_several_weeks_returns_newest(data_dir: Path) -> None:
    write_week(data_dir, "2026-W10", [{"isin": "X"}], [{"isin": "X"}])
    write_week(data_dir, "2025-W52", [{"isin": "X"}], [{"isin": "X"}])

    week = latest_week(data_dir)

    assert week == "2026-W10"


def test_latest_week_empty_folder_raises(tmp_path: Path) -> None:
    with pytest.raises(FileNotFoundError):
        latest_week(tmp_path)


def test_available_weeks_several_weeks_sorted_oldest_first(data_dir: Path) -> None:
    write_week(data_dir, "2026-W10", [{"isin": "X"}], [{"isin": "X"}])
    write_week(data_dir, "2025-W52", [{"isin": "X"}], [{"isin": "X"}])

    weeks = available_weeks(data_dir)

    assert weeks == ["2025-W52", WEEK, "2026-W10"]


def test_available_weeks_other_files_ignored(tmp_path: Path) -> None:
    (tmp_path / "YieldRaccoon_snapshot_all_2026-W01.csv").touch()
    (tmp_path / "YieldRaccoon_allocations_all_latest.csv").touch()

    weeks = available_weeks(tmp_path)

    assert weeks == []
