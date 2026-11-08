import json
from pathlib import Path

import numpy as np
import pandas as pd
import pytest

from fund_clustering.cluster import NOT_IN_CLUSTER
from fund_clustering.load import FundData
from fund_clustering.methods import METHODS
from fund_clustering.migration import (
    NOT_IN_CLUSTER_NAME,
    MethodWeek,
    allocation_drift,
    cluster_transitions,
    compare_weeks,
    load_method_week,
    match_clusters,
    mate_retention,
    neighbour_retention,
)


def method_week(
    labels: dict[str, int], neighbours: dict[str, list[str]] | None = None
) -> MethodWeek:
    points = pd.DataFrame(
        {"cluster": pd.Series(labels)},
    )
    points["cluster_name"] = [f"Cluster {label}" for label in points["cluster"]]
    return MethodWeek(week="2026-W01", points=points, neighbours=neighbours or {})


def fund_data(rows: dict[str, dict[str, float]]) -> FundData:
    funds = pd.DataFrame.from_dict(rows, orient="index").rename_axis("isin")
    funds = funds.reset_index()
    columns = [c for c in funds.columns if c != "isin"]
    return FundData(
        week="2026-W01",
        funds=funds,
        country_columns=[c for c in columns if c.startswith("country_")],
        sector_columns=[c for c in columns if c.startswith("sector_")],
    )


# ---------- load_method_week ----------


def test_load_method_week_points_named_by_cluster(tmp_path: Path) -> None:
    document = {
        "week": "2026-W40",
        "points": [
            {"isin": "SE0000000002", "cluster": 0, "verdict": "agree"},
            {"isin": "SE0000000001", "cluster": -1, "verdict": "none"},
        ],
        "clusters": [{"id": 0, "name": "Sverige"}],
        "neighbours": {
            "SE0000000001": [{"isin": "SE0000000002", "distance": 0.1}],
            "SE0000000002": [{"isin": "SE0000000001", "distance": 0.1}],
        },
    }
    (tmp_path / "cosine_2026-W40.json").write_text(
        json.dumps(document), encoding="utf-8"
    )

    week = load_method_week(tmp_path, "cosine", "2026-W40")

    assert week.week == "2026-W40"
    assert week.points.loc["SE0000000002", "cluster_name"] == "Sverige"
    assert week.points.loc["SE0000000001", "cluster_name"] == NOT_IN_CLUSTER_NAME
    assert week.neighbours["SE0000000001"] == ["SE0000000002"]


def test_load_method_week_missing_file_names_the_command(tmp_path: Path) -> None:
    with pytest.raises(FileNotFoundError, match="main.py .*--week 2026-W40"):
        load_method_week(tmp_path, "cosine", "2026-W40")


# ---------- match_clusters ----------


def test_match_clusters_renumbered_ids_still_match() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 1, "D": 1})
    after = pd.Series({"A": 7, "B": 7, "C": 3, "D": 3})

    matches = match_clusters(before, after)

    assert matches[["before_cluster", "after_cluster"]].values.tolist() == [
        [0, 7],
        [1, 3],
    ]
    assert matches["jaccard"].tolist() == [1.0, 1.0]


def test_match_clusters_split_leaves_one_half_unmatched() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 0, "D": 0, "E": 0, "F": 0})
    after = pd.Series({"A": 1, "B": 1, "C": 1, "D": 1, "E": 2, "F": 2})

    matches = match_clusters(before, after)

    assert matches[["before_cluster", "after_cluster"]].values.tolist() == [[0, 1]]


def test_match_clusters_small_overlap_not_matched() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 0, "D": 0, "E": 1})
    after = pd.Series({"A": 5, "B": 6, "C": 6, "D": 6, "E": 6})

    matches = match_clusters(before, after)

    # Cluster 0 shares 3 of 5 funds with 6 (0.6); cluster 1 only E with 6.
    assert matches[["before_cluster", "after_cluster"]].values.tolist() == [[0, 6]]


def test_match_clusters_not_in_cluster_ignored_and_new_funds_skipped() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": -1})
    after = pd.Series({"A": 4, "B": 4, "C": -1, "NEW": 4})

    matches = match_clusters(before, after)

    assert matches[["before_cluster", "after_cluster"]].values.tolist() == [[0, 4]]
    assert matches["jaccard"].tolist() == [1.0]


# ---------- cluster_transitions ----------


def transition(transitions: pd.DataFrame, side: str, cluster: int) -> pd.Series:
    row = transitions[(transitions["side"] == side) & (transitions["cluster"] == cluster)]
    assert len(row) == 1
    return row.iloc[0]


def test_cluster_transitions_renumbered_cluster_survived() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 0, "D": 0})
    after = pd.Series({"A": 9, "B": 9, "C": 9, "D": -1})

    transitions = cluster_transitions(before, after)

    row = transition(transitions, "before", 0)
    assert row["transition"] == "survived"
    assert row["partners"] == [9]
    assert row["shares"] == [0.75]
    assert transition(transitions, "after", 9)["transition"] == "continued"


def test_cluster_transitions_two_halves_split() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 0, "D": 0, "E": 0, "F": 0})
    after = pd.Series({"A": 1, "B": 1, "C": 1, "D": 2, "E": 2, "F": 2})

    transitions = cluster_transitions(before, after)

    row = transition(transitions, "before", 0)
    assert row["transition"] == "split"
    assert row["partners"] == [1, 2]
    assert row["shares"] == [0.5, 0.5]


def test_cluster_transitions_big_piece_and_small_piece_survived() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 0, "D": 0})
    after = pd.Series({"A": 1, "B": 1, "C": 1, "D": 2})

    transitions = cluster_transitions(before, after)

    # 3 of 4 went to 1, which holds nothing else: the cluster survived.
    assert transition(transitions, "before", 0)["transition"] == "survived"


def test_cluster_transitions_two_clusters_merged() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 1, "D": 1})
    after = pd.Series({"A": 5, "B": 5, "C": 5, "D": 5})

    transitions = cluster_transitions(before, after)

    row = transition(transitions, "after", 5)
    assert row["transition"] == "merged"
    assert row["partners"] == [0, 1]
    assert row["shares"] == [0.5, 0.5]


def test_cluster_transitions_small_cluster_absorbed_by_big_one() -> None:
    before = pd.Series(
        {"A": 0, "B": 0, "C": 1, "D": 1, "E": 1, "F": 1, "G": 1, "H": 1, "I": 1}
    )
    after = pd.Series({isin: 4 for isin in "ABCDEFGHI"})

    transitions = cluster_transitions(before, after)

    # Cluster 0 is only 2 of the 9 funds in 4: absorbed, not a survivor.
    row = transition(transitions, "before", 0)
    assert row["transition"] == "absorbed"
    assert row["partners"] == [4]
    assert transition(transitions, "before", 1)["transition"] == "survived"
    # 2/9 from cluster 0 is below MIN_SHARE: 4 simply continues cluster 1.
    assert transition(transitions, "after", 4)["transition"] == "continued"


def test_cluster_transitions_scattered_cluster_disappeared() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 0, "D": 0, "E": 0, "F": 0})
    after = pd.Series({"A": -1, "B": -1, "C": -1, "D": -1, "E": -1, "F": 3})

    transitions = cluster_transitions(before, after)

    row = transition(transitions, "before", 0)
    assert row["transition"] == "disappeared"
    assert row["partners"] == []


def test_cluster_transitions_new_cluster_from_unclustered_funds() -> None:
    before = pd.Series({"A": -1, "B": -1, "C": 0, "D": 0})
    after = pd.Series({"A": 2, "B": 2, "C": 1, "D": 1, "NEW": 2})

    transitions = cluster_transitions(before, after)

    row = transition(transitions, "after", 2)
    assert row["transition"] == "new"
    assert row["partners"] == []
    # Funds outside a cluster are not a cluster: no row for -1 on either side.
    assert NOT_IN_CLUSTER not in transitions["cluster"].tolist()


# ---------- mate_retention ----------


def test_mate_retention_share_of_cluster_mates_kept() -> None:
    before = pd.Series({"A": 0, "B": 0, "C": 0, "D": 1})
    after = pd.Series({"A": 5, "B": 5, "C": 6, "D": 5})

    retention = mate_retention(before, after)

    # A: mates {B, C} → {B, D}: 1 shared of 3 → 1/3.
    assert retention["A"] == pytest.approx(1 / 3)
    # C: mates {A, B} → {}: nothing kept.
    assert retention["C"] == 0.0


def test_mate_retention_not_in_cluster_is_nan() -> None:
    before = pd.Series({"A": -1, "B": 0, "C": 0})
    after = pd.Series({"A": 1, "B": 1, "C": -1})

    retention = mate_retention(before, after)

    # A and C are outside a cluster in one week; B is clustered in both.
    assert retention.isna().to_dict() == {"A": True, "B": False, "C": True}


# ---------- neighbour_retention ----------


def test_neighbour_retention_share_of_top_k_kept() -> None:
    before = {"A": ["B", "C", "D", "E"], "B": ["A"]}
    after = {"A": ["C", "B", "X", "Y"], "B": ["A"], "C": [], "D": [], "X": [], "Y": []}

    retention = neighbour_retention(before, after, k=3)

    # A's top 3 {B, C, D} → {C, B, X}: 2 of 3 kept.
    assert retention["A"] == pytest.approx(2 / 3)
    assert retention["B"] == 1.0


def test_neighbour_retention_gone_neighbours_not_counted() -> None:
    before = {"A": ["B", "GONE"], "B": ["A"]}
    after = {"A": ["B"], "B": ["A"]}

    retention = neighbour_retention(before, after, k=2)

    assert retention["A"] == 1.0


# ---------- allocation_drift ----------


def test_allocation_drift_identical_weeks_is_zero() -> None:
    rows = {
        "A": {"country_sverige": 100.0, "sector_teknik": 100.0},
        "B": {"country_sverige": 50.0, "sector_teknik": 100.0},
    }

    drift = allocation_drift(fund_data(rows), fund_data(rows), METHODS["cosine"])

    assert drift.to_dict() == {"A": 0.0, "B": 0.0}


def test_allocation_drift_new_column_aligned_before_features() -> None:
    before = fund_data(
        {"A": {"country_sverige": 60.0, "country_usa": 40.0, "sector_teknik": 100.0}}
    )
    after = fund_data(
        {
            "A": {
                "country_sverige": 60.0,
                "country_usa": 40.0,
                "country_japan": 0.0,
                "sector_teknik": 100.0,
            }
        }
    )

    # Aitchison centres by the column count, so an unaligned extra zero column
    # would show up as drift.
    drift = allocation_drift(before, after, METHODS["aitchison"])

    assert drift["A"] == pytest.approx(0.0)


def test_allocation_drift_only_shared_funds() -> None:
    before = fund_data({"A": {"country_sverige": 100.0, "sector_teknik": 100.0}})
    after = fund_data(
        {
            "A": {"country_sverige": 50.0, "country_usa": 50.0, "sector_teknik": 100.0},
            "NEW": {"country_sverige": 100.0, "country_usa": 0.0, "sector_teknik": 100.0},
        }
    )

    drift = allocation_drift(before, after, METHODS["hellinger"])

    assert drift.index.tolist() == ["A"]
    assert drift["A"] > 0


# ---------- compare_weeks ----------


def test_compare_weeks_each_change_class() -> None:
    before = method_week(
        {
            "S1": 0, "S2": 0, "S3": 0, "S4": 0,  # stay together
            "MOVED": 0, "RESHUFFLED": 0,
            "T1": 1, "T2": 1, "T3": 1,
            "JOINS": -1, "LEAVES": 1, "GONE": 1,
        }
    )
    after = method_week(
        {
            "S1": 8, "S2": 8, "S3": 8, "S4": 8,
            "MOVED": 9, "RESHUFFLED": 9,
            "T1": 9, "T2": 9, "T3": 9,
            "JOINS": 8, "LEAVES": -1, "NEW": 8,
        }
    )
    drift = pd.Series(
        {
            "S1": 0.01, "S2": 0.02, "S3": 0.01, "S4": 0.02,
            "MOVED": 0.5, "RESHUFFLED": 0.01,
            "T1": 0.01, "T2": 0.01, "T3": 0.01,
            "JOINS": 0.01, "LEAVES": 0.01,
        }
    )

    migration = compare_weeks(before, after, drift)

    change = migration.funds["change"]
    assert change["S1"] == "stayed"
    assert change["T1"] == "stayed"  # cluster 1 → 9 is a match
    assert change["MOVED"] == "moved"
    assert change["RESHUFFLED"] == "reshuffled"
    assert change["JOINS"] == "joined"
    assert change["LEAVES"] == "left"
    assert change["GONE"] == "gone"
    assert change["NEW"] == "new"
    assert migration.drift_threshold == pytest.approx(0.02)


def test_compare_weeks_columns_from_both_weeks() -> None:
    before = method_week({"A": 0, "B": 0}, {"A": ["B"], "B": ["A"]})
    after = method_week({"A": 3, "B": 3}, {"A": ["B"], "B": ["A"]})
    drift = pd.Series({"A": 0.0, "B": 0.0})

    funds = compare_weeks(before, after, drift).funds

    row = funds.loc["A"]
    assert (row["before_cluster"], row["after_cluster"]) == (0, 3)
    assert (row["before_name"], row["after_name"]) == ("Cluster 0", "Cluster 3")
    assert (row["mate_retention"], row["neighbour_retention"]) == (1.0, 1.0)
    assert row["drift"] == 0.0


def test_compare_weeks_new_fund_has_no_before_values() -> None:
    before = method_week({"A": 0})
    after = method_week({"A": 0, "NEW": 0})

    funds = compare_weeks(before, after, pd.Series({"A": 0.0})).funds

    assert np.isnan(funds.loc["NEW", "drift"])
    assert pd.isna(funds.loc["NEW", "before_cluster"])
    assert pd.isna(funds.loc["NEW", "before_name"])
