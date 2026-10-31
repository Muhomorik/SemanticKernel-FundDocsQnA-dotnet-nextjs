import numpy as np
import pandas as pd
import pytest

from fund_clustering.naming import (
    AGREE,
    ASIA,
    DIFFERENT,
    EMERGING,
    GLOBAL,
    NEIGHBOURING,
    NONE,
    NORDIC,
    OTHER,
    SECTOR,
    category_family,
    describe_clusters,
    fund_verdicts,
)


@pytest.mark.parametrize(
    ("category", "family"),
    [
        ("Sverige", NORDIC),
        ("Sverige, Små-/medelstora bolag", NORDIC),
        ("Global & Sverige", NORDIC),
        ("Global, Mix bolag", GLOBAL),
        ("Tillväxtmarknader ex Kina", EMERGING),
        ("Kina & närliggande", ASIA),
        ("Asien & Australien ex Japan", ASIA),
        ("Branschfond, Ny teknik", SECTOR),
        ("Lång/kort", OTHER),
    ],
)
def test_category_family_known_category_mapped(category: str, family: str) -> None:
    result = category_family(category)

    assert result == family


def test_category_family_unknown_category_warns_and_falls_back_to_other() -> None:
    category = "Mars, Kolonier"

    with pytest.warns(UserWarning, match="Mars"):
        family = category_family(category)

    assert family == OTHER


def test_describe_clusters_named_after_most_common_category() -> None:
    categories = pd.Series(["Sverige", "Sverige", "Norden", "Japan"])
    labels = np.array([0, 0, 0, -1])

    clusters = describe_clusters(categories, labels)

    assert list(clusters.index) == [0]
    assert clusters.at[0, "name"] == "Sverige"
    assert clusters.at[0, "size"] == 3
    assert clusters.at[0, "purity"] == pytest.approx(2 / 3)
    assert clusters.at[0, "category_counts"] == {"Sverige": 2, "Norden": 1}


def test_describe_clusters_low_purity_named_mixed() -> None:
    categories = pd.Series(["Japan", "Japan", "Kina", "Kina", "Indien"])
    labels = np.zeros(5, dtype=int)

    clusters = describe_clusters(categories, labels)

    assert clusters.at[0, "name"] == "Mixed · Japan / Kina"


def test_describe_clusters_same_name_suffixed_largest_first() -> None:
    categories = pd.Series(["Sverige"] * 5)
    labels = np.array([0, 1, 1, 1, 2])

    clusters = describe_clusters(categories, labels)

    assert clusters["name"].to_dict() == {
        0: "Sverige · B",
        1: "Sverige · A",
        2: "Sverige · C",
    }


@pytest.mark.parametrize(
    ("category", "verdict"),
    [
        ("Sverige", AGREE),
        ("Norden", NEIGHBOURING),
        ("Japan", DIFFERENT),
    ],
)
def test_fund_verdicts_regional_cluster(category: str, verdict: str) -> None:
    categories = pd.Series(["Sverige", "Sverige", category])
    labels = np.array([0, 0, 0])
    clusters = describe_clusters(categories, labels)

    verdicts = fund_verdicts(categories, labels, clusters)

    assert verdicts[2] == verdict


def test_fund_verdicts_different_sector_industries_are_different() -> None:
    categories = pd.Series(
        ["Branschfond, Ny teknik", "Branschfond, Ny teknik", "Branschfond, Finans"]
    )
    labels = np.array([0, 0, 0])
    clusters = describe_clusters(categories, labels)

    verdicts = fund_verdicts(categories, labels, clusters)

    assert verdicts[2] == DIFFERENT


def test_fund_verdicts_not_in_cluster_is_none() -> None:
    categories = pd.Series(["Sverige", "Sverige", "Sverige"])
    labels = np.array([0, 0, -1])
    clusters = describe_clusters(categories, labels)

    verdicts = fund_verdicts(categories, labels, clusters)

    assert list(verdicts) == [AGREE, AGREE, NONE]
