import numpy as np
import pandas as pd
import pytest

from fund_clustering.features import cosine_features
from fund_clustering.load import FundData
from fund_clustering.pipeline import METHODS, run_method


def grouped_funds(group_count: int = 3, size: int = 20) -> FundData:
    """Funds in clearly separated allocation groups, one category per group."""
    rng = np.random.default_rng(0)
    countries = [f"country_c{i}" for i in range(4)]
    sectors = [f"sector_s{i}" for i in range(4)]
    rows = []
    for group in range(group_count):
        for member in range(size):
            country = np.full(4, 2.0) + rng.random(4)
            country[group] = 90.0
            sector = np.full(4, 2.0) + rng.random(4)
            sector[group] = 90.0
            rows.append(
                {
                    "isin": f"SE{group:02d}{member:08d}",
                    "name": f"Example {group}-{member}",
                    "category": ["Sverige", "Japan", "USA"][group],
                    "fund_type": "EQUITY_FUND",
                    "company_name": "AcmeCo",
                    "is_index_fund": False,
                    **dict(zip(countries, country / country.sum() * 100)),
                    **dict(zip(sectors, sector / sector.sum() * 100)),
                }
            )
    return FundData(
        week="2026-W01",
        funds=pd.DataFrame(rows),
        country_columns=countries,
        sector_columns=sectors,
    )


def test_methods_include_cosine_as_first() -> None:
    first = next(iter(METHODS.values()))

    assert first.id == "cosine"
    assert first.features is cosine_features


@pytest.mark.slow
def test_run_method_all_parts_cover_every_fund() -> None:
    data = grouped_funds()

    run = run_method(data, METHODS["cosine"])

    fund_count = len(data.funds)
    assert run.result.cluster_count == 3
    assert len(run.verdicts) == fund_count
    assert run.isolated.shape == (fund_count,)
    assert run.neighbours.indices.shape[0] == fund_count
    assert set(run.territories) == set(run.clusters.index)
    assert set(run.verdicts) == {"agree"}
