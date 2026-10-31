"""Name clusters after their categories and judge each fund's placement."""

import warnings

import numpy as np
import pandas as pd

from fund_clustering.cluster import NOT_IN_CLUSTER

NORDIC = "nordic"
EUROPE = "europe"
USA = "usa"
GLOBAL = "global"
EMERGING = "em"
ASIA = "asia"
SECTOR = "sector"
OTHER = "other"

FAMILY_NAMES = {
    NORDIC: "Nordics",
    EUROPE: "Europe",
    USA: "USA",
    GLOBAL: "Global",
    EMERGING: "Emerging markets",
    ASIA: "Asia & Japan",
    SECTOR: "Sector funds",
    OTHER: "Other",
}

# Families where two different categories still count as "neighbouring".
# Sector funds are excluded: two different industries are different families.
REGIONAL_FAMILIES = {NORDIC, EUROPE, USA, GLOBAL, EMERGING, ASIA}

# Category prefix (text before the first comma) -> family.
_EXACT_PREFIXES = {
    "Sverige": NORDIC,
    "Norden": NORDIC,
    "Norge": NORDIC,
    "Finland": NORDIC,
    "Global & Sverige": NORDIC,
    "Europa": EUROPE,
    "Euroland": EUROPE,
    "Tyskland": EUROPE,
    "Italien": EUROPE,
    "Spanien": EUROPE,
    "Storbritannien": EUROPE,
    "Östeuropa ex Ryssland": EUROPE,
    "USA": USA,
    "Global": GLOBAL,
    "Latinamerika": EMERGING,
    "Brasilien": EMERGING,
    "Afrika och Mellanöstern": EMERGING,
    "Indien": EMERGING,
    "Japan": ASIA,
    "Hongkong": ASIA,
    "Taiwan": ASIA,
    "Korea": ASIA,
    "ASEAN": ASIA,
    "Indonesien": ASIA,
    "Vietnam": ASIA,
    "Branschfond": SECTOR,
    "Övriga aktiefonder": OTHER,
    "Lång/kort": OTHER,
}

# Prefixes matched by "starts with", for regions with several variants.
_STARTS_WITH = [
    ("Tillväxtmarknader", EMERGING),
    ("Kina", ASIA),
    ("Asien", ASIA),
]

MIXED_PURITY = 0.5

AGREE = "agree"
NEIGHBOURING = "neighbouring"
DIFFERENT = "different"
NONE = "none"


def category_family(category: str) -> str:
    """Return the family id for a category, e.g. ``"Sverige, Små-/medelstora bolag"``.

    Args:
        category: Category exactly as delivered.

    Returns:
        One of the family ids in ``FAMILY_NAMES``; unknown categories fall back
        to ``OTHER`` with a warning, so a new category never stops a run.
    """
    prefix = category.split(",")[0].strip()
    if prefix in _EXACT_PREFIXES:
        return _EXACT_PREFIXES[prefix]
    for start, family in _STARTS_WITH:
        if prefix.startswith(start):
            return family
    warnings.warn(f"Unknown category {category!r}, using family {OTHER!r}")
    return OTHER


def describe_clusters(categories: pd.Series, labels: np.ndarray) -> pd.DataFrame:
    """Name each cluster and summarise its category mix.

    The name is the most common category; under 50% purity it becomes
    ``"Mixed · A / B"`` (top two categories). Clusters that end up with the same
    name get ``" · A"``, ``" · B"`` suffixes, largest cluster first.

    Args:
        categories: Category per fund, in the same order as ``labels``.
        labels: Cluster id per fund; ``NOT_IN_CLUSTER`` rows are ignored.

    Returns:
        One row per cluster, indexed by cluster id, with columns ``name``,
        ``size``, ``purity``, ``leading_category`` and ``category_counts``
        (category -> fund count, most common first).
    """
    frame = pd.DataFrame({"category": np.asarray(categories), "cluster": labels})
    frame = frame[frame["cluster"] != NOT_IN_CLUSTER]

    rows = []
    for cluster_id, group in frame.groupby("cluster"):
        counts = group["category"].value_counts()
        # Ties: most funds first, then alphabetical, so names are stable.
        counts = counts.sort_index().sort_values(ascending=False, kind="stable")
        leading, size = counts.index[0], len(group)
        purity = counts.iloc[0] / size
        if purity < MIXED_PURITY and len(counts) > 1:
            name = f"Mixed · {counts.index[0]} / {counts.index[1]}"
        else:
            name = leading
        rows.append(
            {
                "cluster": cluster_id,
                "name": name,
                "size": size,
                "purity": purity,
                "leading_category": leading,
                "category_counts": counts.to_dict(),
            }
        )

    clusters = pd.DataFrame(rows).set_index("cluster")
    clusters["name"] = _add_suffixes(clusters)
    return clusters


def _add_suffixes(clusters: pd.DataFrame) -> pd.Series:
    names = clusters["name"].copy()
    for name, group in clusters.groupby("name"):
        if len(group) == 1:
            continue
        ordered = group.reset_index().sort_values(
            ["size", "cluster"], ascending=[False, True]
        )
        for position, cluster_id in enumerate(ordered["cluster"]):
            names[cluster_id] = f"{name} · {chr(ord('A') + position)}"
    return names


def fund_verdicts(
    categories: pd.Series, labels: np.ndarray, clusters: pd.DataFrame
) -> np.ndarray:
    """Judge whether each fund's cluster matches its own category.

    Args:
        categories: Category per fund, in the same order as ``labels``.
        labels: Cluster id per fund.
        clusters: Output of ``describe_clusters``.

    Returns:
        Per fund: ``AGREE`` (own category leads the cluster), ``NEIGHBOURING``
        (same regional family), ``DIFFERENT`` (any other family, or two
        different sector industries) or ``NONE`` (not in a cluster).
    """
    verdicts = []
    for category, label in zip(np.asarray(categories), labels):
        if label == NOT_IN_CLUSTER:
            verdicts.append(NONE)
            continue
        leading = clusters.at[label, "leading_category"]
        family = category_family(category)
        if category == leading:
            verdicts.append(AGREE)
        elif family in REGIONAL_FAMILIES and family == category_family(leading):
            verdicts.append(NEIGHBOURING)
        else:
            verdicts.append(DIFFERENT)
    return np.array(verdicts)
