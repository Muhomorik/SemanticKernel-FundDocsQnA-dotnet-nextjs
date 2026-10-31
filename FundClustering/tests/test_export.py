import json
from pathlib import Path

import numpy as np
import pandas as pd
import pytest
from shapely.geometry import MultiPolygon, box

from fund_clustering.cluster import ClusterResult
from fund_clustering.export import (
    funds_document,
    method_document,
    top_allocations,
    write_documents,
)
from fund_clustering.features import cosine_features
from fund_clustering.load import FundData
from fund_clustering.naming import describe_clusters, fund_verdicts
from fund_clustering.neighbours import Neighbours
from fund_clustering.pipeline import Method, MethodRun

COUNTRIES = ["country_sverige", "country_usa"]
SECTORS = ["sector_finans", "sector_teknik"]


@pytest.fixture
def data() -> FundData:
    funds = pd.DataFrame(
        {
            "isin": ["SE0000000001", "SE0000000002", "SE0000000003", "SE0000000004"],
            "name": ["Example A", "Example B", "Example C", "Example D"],
            "category": ["Sverige", "Sverige", "Norden", "Japan"],
            "fund_type": ["EQUITY_FUND"] * 4,
            "company_name": ["AcmeCo"] * 4,
            "is_index_fund": [True, False, False, False],
            "country_sverige": [100.0, 110.0, 60.0, 0.0],
            "country_usa": [0.0, -10.0, 40.0, 100.0],
            "sector_finans": [70.0, 50.0, 0.0, 30.0],
            "sector_teknik": [30.0, 50.0, 100.0, 70.0],
        }
    )
    return FundData(
        week="2026-W01", funds=funds, country_columns=COUNTRIES, sector_columns=SECTORS
    )


@pytest.fixture
def run(data: FundData) -> MethodRun:
    labels = np.array([0, 0, 0, -1])
    clusters = describe_clusters(data.funds["category"], labels)
    return MethodRun(
        method=Method("cosine", "Cosine", "Unit-length vectors.", cosine_features),
        result=ClusterResult(
            labels=labels,
            xy=np.array([[0.1, 0.1], [0.3, 0.1], [0.2, 0.3], [0.9, 0.912345]]),
        ),
        clusters=clusters,
        verdicts=fund_verdicts(data.funds["category"], labels, clusters),
        neighbours=Neighbours(
            indices=np.array([[1, 2], [0, 2], [0, 1], [2, 0]]),
            distances=np.array([[0.1, 0.2], [0.1, 0.3], [0.2, 0.3], [0.9, 1.234567]]),
        ),
        isolated=np.array([False, False, False, True]),
        territories={0: box(0.0, 0.0, 0.5, 0.5)},
    )


def test_top_allocations_largest_first_zeros_dropped_prefix_stripped() -> None:
    columns = ["country_sverige", "country_usa", "country_japan"]
    row = pd.Series(dict(zip(columns, [60.0, 40.0, 0.0])))

    top = top_allocations(row, columns)

    assert top == [["sverige", 60.0], ["usa", 40.0]]


def test_top_allocations_limited_to_count_and_short_positions_kept() -> None:
    row = pd.Series({"country_a": 50.0, "country_b": 70.0, "country_c": -20.0})

    top = top_allocations(row, ["country_a", "country_b", "country_c"], count=2)

    assert top == [["b", 70.0], ["a", 50.0]]


def test_funds_document_one_entry_per_fund_with_display_fields(data: FundData) -> None:
    document = funds_document(data)

    assert document["week"] == "2026-W01"
    assert document["funds"][0] == {
        "isin": "SE0000000001",
        "name": "Example A",
        "company": "AcmeCo",
        "category": "Sverige",
        "family": "nordic",
        "shade": 1,
        "isIndex": True,
        "topCountries": [["sverige", 100.0]],
        "topSectors": [["finans", 70.0], ["teknik", 30.0]],
    }


def test_funds_document_shade_is_alphabetical_within_family(data: FundData) -> None:
    document = funds_document(data)

    shades = {fund["category"]: fund["shade"] for fund in document["funds"]}
    assert shades == {"Norden": 0, "Sverige": 1, "Japan": 0}


def test_method_document_points_rounded_and_json_native(
    data: FundData, run: MethodRun
) -> None:
    document = method_document(data, run)

    assert document["points"][3] == {
        "isin": "SE0000000004",
        "x": 0.9,
        "y": 0.9123,
        "cluster": -1,
        "verdict": "none",
        "isolated": True,
    }


def test_method_document_cluster_named_with_label_at_member_median(
    data: FundData, run: MethodRun
) -> None:
    document = method_document(data, run)

    assert document["clusters"] == [
        {
            "id": 0,
            "name": "Sverige",
            "size": 3,
            "purity": 0.6667,
            "leadingCategory": "Sverige",
            "labelX": 0.2,
            "labelY": 0.1,
            "categories": {"Sverige": 2, "Norden": 1},
        }
    ]


def test_method_document_category_agree_share_among_clustered_funds(
    data: FundData, run: MethodRun
) -> None:
    document = method_document(data, run)

    assert document["categories"] == [
        {"category": "Japan", "family": "asia", "funds": 1, "agreeShare": None},
        {"category": "Norden", "family": "nordic", "funds": 1, "agreeShare": 0.0},
        {"category": "Sverige", "family": "nordic", "funds": 2, "agreeShare": 1.0},
    ]


def test_method_document_summary_counts_and_shares(
    data: FundData, run: MethodRun
) -> None:
    document = method_document(data, run)

    summary = document["summary"]
    assert summary["clusters"] == 1
    assert summary["notInCluster"] == 1
    assert summary["agree"] == pytest.approx(0.6667)
    assert summary["neighbouring"] == pytest.approx(0.3333)
    assert summary["different"] == 0.0
    assert set(summary) >= {"ari", "nmi"}


def test_method_document_describes_method(data: FundData, run: MethodRun) -> None:
    document = method_document(data, run)

    assert (document["method"], document["name"], document["week"]) == (
        "cosine",
        "Cosine",
        "2026-W01",
    )
    assert document["description"] == "Unit-length vectors."


def test_method_document_territory_as_closed_rings(
    data: FundData, run: MethodRun
) -> None:
    run.territories[0] = MultiPolygon(
        [box(0.0, 0.0, 0.1, 0.1), box(0.5, 0.5, 0.6, 0.6)]
    )

    document = method_document(data, run)

    territory = document["territory"]
    assert [part["cluster"] for part in territory] == [0, 0]
    ring = territory[0]["rings"][0]
    assert ring[0] == ring[-1]
    corners = sorted(map(tuple, ring[:-1]))
    assert corners == [(0.0, 0.0), (0.0, 0.1), (0.1, 0.0), (0.1, 0.1)]


def test_method_document_neighbours_keyed_by_isin(
    data: FundData, run: MethodRun
) -> None:
    document = method_document(data, run)

    assert document["neighbours"]["SE0000000004"] == [
        {"isin": "SE0000000003", "distance": 0.9},
        {"isin": "SE0000000001", "distance": 1.2346},
    ]


def test_write_documents_writes_manifest_funds_and_method_files(
    tmp_path: Path, data: FundData, run: MethodRun
) -> None:
    written = write_documents(tmp_path, data, [run])

    assert sorted(path.name for path in written) == [
        "cosine.json",
        "funds.json",
        "methods.json",
    ]
    manifest = json.loads((tmp_path / "methods.json").read_text(encoding="utf-8"))
    assert manifest == {
        "week": "2026-W01",
        "default": "cosine",
        "methods": [{"id": "cosine", "name": "Cosine", "file": "cosine.json"}],
    }
    method = json.loads((tmp_path / "cosine.json").read_text(encoding="utf-8"))
    assert method["points"][0]["isin"] == "SE0000000001"
