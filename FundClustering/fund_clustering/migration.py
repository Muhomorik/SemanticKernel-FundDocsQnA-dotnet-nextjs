"""Compare one method's clusters between two hand-picked weeks.

Cluster ids are renumbered on every run, so clusters are matched by the funds
they share, not by id. Each fund then gets a change class, and allocation
drift tells a real change in holdings from clustering noise.
"""

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import pandas as pd
from scipy.optimize import linear_sum_assignment

from fund_clustering.cluster import NOT_IN_CLUSTER
from fund_clustering.load import FundData
from fund_clustering.methods import Method

NOT_IN_CLUSTER_NAME = "Not in a cluster"

# Smallest member overlap (Jaccard) for two clusters to count as the same one.
MIN_MATCH = 0.3

# A fund that changed cluster "moved" when its drift is above this quantile of
# the drift of funds that stayed, i.e. more than the normal weekly wobble.
DRIFT_QUANTILE = 0.95

NEIGHBOURS_COMPARED = 20


@dataclass(frozen=True)
class MethodWeek:
    """One method's result for one week, read back from ``<method>_<week>.json``.

    ``points`` is indexed by ``isin`` with columns ``cluster`` and
    ``cluster_name``; ``neighbours`` maps each ISIN to its neighbours, closest
    first.
    """

    week: str
    points: pd.DataFrame
    neighbours: dict[str, list[str]]


@dataclass(frozen=True)
class Migration:
    """How funds and clusters changed between two weeks.

    ``matches``: one row per matched cluster pair (``before_cluster``,
    ``after_cluster``, ``jaccard``). ``funds``: one row per ISIN in either week,
    see ``compare_weeks``. ``drift_threshold``: drift above which a fund that
    changed cluster counts as ``moved``.
    """

    matches: pd.DataFrame
    funds: pd.DataFrame
    drift_threshold: float


def load_method_week(output_dir: Path | str, method_id: str, week: str) -> MethodWeek:
    """Read ``<method_id>_<week>.json`` written by ``main.py``.

    Raises:
        FileNotFoundError: The week was not run; the message names the command.
    """
    path = Path(output_dir) / f"{method_id}_{week}.json"
    if not path.exists():
        raise FileNotFoundError(
            f"{path} not found. Run: python main.py --input <data dir> "
            f"--output {output_dir} --week {week}"
        )
    document = json.loads(path.read_text(encoding="utf-8"))

    names = {cluster["id"]: cluster["name"] for cluster in document["clusters"]}
    points = pd.DataFrame(document["points"]).set_index("isin")
    points["cluster_name"] = [
        names.get(label, NOT_IN_CLUSTER_NAME) for label in points["cluster"]
    ]
    neighbours = {
        isin: [neighbour["isin"] for neighbour in entries]
        for isin, entries in document["neighbours"].items()
    }
    return MethodWeek(week=document["week"], points=points, neighbours=neighbours)


def _members(labels: pd.Series) -> dict[int, set[str]]:
    """ISINs per cluster id, funds outside a cluster left out."""
    clustered = labels[labels != NOT_IN_CLUSTER]
    return {
        int(label): set(group.index)
        for label, group in clustered.groupby(clustered)
    }


def _jaccard(a: set[str], b: set[str]) -> float:
    union = a | b
    return len(a & b) / len(union) if union else 1.0


def match_clusters(before: pd.Series, after: pd.Series) -> pd.DataFrame:
    """Pair clusters of two weeks by the funds they share.

    Only funds present in both weeks count. Each cluster is matched at most
    once (best total overlap), and only when the Jaccard overlap is at least
    ``MIN_MATCH``. Unmatched clusters ended (before) or are new (after),
    which includes splits and merges.

    Args:
        before, after: Cluster id per ISIN.

    Returns:
        Columns ``before_cluster``, ``after_cluster``, ``jaccard``, sorted by
        ``before_cluster``.
    """
    shared = before.index.intersection(after.index)
    before_members = _members(before[shared])
    after_members = _members(after[shared])
    before_ids, after_ids = sorted(before_members), sorted(after_members)

    overlap = np.array(
        [
            [_jaccard(before_members[b], after_members[a]) for a in after_ids]
            for b in before_ids
        ]
    ).reshape(len(before_ids), len(after_ids))
    rows, columns = linear_sum_assignment(overlap, maximize=True)

    matches = pd.DataFrame(
        {
            "before_cluster": [before_ids[r] for r in rows],
            "after_cluster": [after_ids[c] for c in columns],
            "jaccard": overlap[rows, columns],
        }
    )
    matches = matches[matches["jaccard"] >= MIN_MATCH]
    return matches.sort_values("before_cluster").reset_index(drop=True)


def mate_retention(before: pd.Series, after: pd.Series) -> pd.Series:
    """Per fund, Jaccard overlap of its cluster-mates in the two weeks.

    Label-free: needs no cluster matching. Only funds present in both weeks are
    compared, and only those funds count as mates. NaN when the fund is outside
    a cluster in either week.

    Args:
        before, after: Cluster id per ISIN.

    Returns:
        0..1 per shared ISIN, sorted by ISIN.
    """
    shared = before.index.intersection(after.index).sort_values()
    before, after = before[shared], after[shared]
    before_members, after_members = _members(before), _members(after)

    def retention(isin: str) -> float:
        b, a = int(before[isin]), int(after[isin])
        if NOT_IN_CLUSTER in (b, a):
            return np.nan
        return _jaccard(before_members[b] - {isin}, after_members[a] - {isin})

    return pd.Series([retention(isin) for isin in shared], index=shared, dtype=float)


def neighbour_retention(
    before: dict[str, list[str]],
    after: dict[str, list[str]],
    k: int = NEIGHBOURS_COMPARED,
) -> pd.Series:
    """Per fund, share of its top-``k`` neighbours still in its top-``k``.

    Neighbours that are gone from the later week can't be kept, so they're
    left out of the count.

    Returns:
        0..1 per ISIN present in both weeks (NaN with nothing to compare),
        sorted by ISIN.
    """
    shared = sorted(before.keys() & after.keys())

    def retention(isin: str) -> float:
        earlier = [n for n in before[isin][:k] if n in after]
        if not earlier:
            return np.nan
        return len(set(earlier) & set(after[isin][:k])) / len(earlier)

    return pd.Series(
        [retention(isin) for isin in shared], index=pd.Index(shared), dtype=float
    )


def _aligned(data: FundData, countries: list[str], sectors: list[str], isins) -> FundData:
    funds = (
        data.funds.set_index("isin")
        .loc[isins]
        .reindex(columns=countries + sectors, fill_value=0.0)
        .fillna(0.0)
        .reset_index()
    )
    return FundData(
        week=data.week, funds=funds, country_columns=countries, sector_columns=sectors
    )


def allocation_drift(before: FundData, after: FundData, method: Method) -> pd.Series:
    """Per fund, how far its own allocation moved between the weeks.

    Both weeks are first aligned to the same columns (a column missing in one
    week reads as 0) and to the funds present in both, so the method sees the
    same layout twice. That matters for Aitchison, whose centring depends on
    the column count.

    Returns:
        The method's distance between the fund's two feature vectors, per
        shared ISIN, sorted by ISIN.
    """
    def union(first: list[str], second: list[str]) -> list[str]:
        return first + [column for column in second if column not in first]

    countries = union(before.country_columns, after.country_columns)
    sectors = union(before.sector_columns, after.sector_columns)
    isins = sorted(set(before.funds["isin"]) & set(after.funds["isin"]))

    before_features = method.features(_aligned(before, countries, sectors, isins))
    after_features = method.features(_aligned(after, countries, sectors, isins))
    distances = np.linalg.norm(after_features - before_features, axis=1)
    return pd.Series(distances, index=pd.Index(isins), dtype=float)


def _change(before: object, after: object, matched: object) -> str:
    if pd.isna(before):
        return "new"
    if pd.isna(after):
        return "gone"
    if before == NOT_IN_CLUSTER and after == NOT_IN_CLUSTER:
        return "stayed"
    if before == NOT_IN_CLUSTER:
        return "joined"
    if after == NOT_IN_CLUSTER:
        return "left"
    return "stayed" if matched == after else "changed"


def compare_weeks(before: MethodWeek, after: MethodWeek, drift: pd.Series) -> Migration:
    """Match the clusters of two weeks and classify every fund.

    Change classes: ``stayed`` (still in the matched cluster, or outside a
    cluster both weeks), ``moved`` (other cluster, drift above the threshold:
    the holdings changed), ``reshuffled`` (other cluster, normal drift: the
    clustering moved, not the fund), ``joined`` / ``left`` (into / out of a
    cluster), ``new`` / ``gone`` (fund in one week's data only).

    Args:
        before, after: The two weeks, e.g. from ``load_method_week``.
        drift: Output of ``allocation_drift`` for the same weeks and method.

    Returns:
        Cluster matches, one row per fund (indexed by ISIN, with
        ``before_cluster``, ``after_cluster``, ``before_name``, ``after_name``,
        ``mate_retention``, ``neighbour_retention``, ``drift``, ``change``)
        and the drift threshold used.
    """
    before_labels = before.points["cluster"]
    after_labels = after.points["cluster"]
    matches = match_clusters(before_labels, after_labels)
    matched_to = dict(zip(matches["before_cluster"], matches["after_cluster"]))

    funds = pd.DataFrame(
        {
            "before_cluster": before_labels.astype("Int64"),
            "after_cluster": after_labels.astype("Int64"),
            "before_name": before.points["cluster_name"],
            "after_name": after.points["cluster_name"],
        }
    ).sort_index()
    funds["mate_retention"] = mate_retention(before_labels, after_labels)
    funds["neighbour_retention"] = neighbour_retention(
        before.neighbours, after.neighbours
    )
    funds["drift"] = drift.reindex(funds.index).astype(float)
    funds["change"] = [
        _change(b, a, matched_to.get(int(b)) if not pd.isna(b) else None)
        for b, a in zip(funds["before_cluster"], funds["after_cluster"])
    ]

    stayed = funds["change"] == "stayed"
    clustered = funds["after_cluster"] != NOT_IN_CLUSTER
    reference = funds.loc[stayed & clustered, "drift"].dropna()
    if reference.empty:
        reference = funds["drift"].dropna()
    threshold = float(reference.quantile(DRIFT_QUANTILE)) if len(reference) else np.nan

    changed = funds["change"] == "changed"
    funds.loc[changed, "change"] = np.where(
        funds.loc[changed, "drift"] > threshold, "moved", "reshuffled"
    )
    return Migration(matches=matches, funds=funds, drift_threshold=threshold)
