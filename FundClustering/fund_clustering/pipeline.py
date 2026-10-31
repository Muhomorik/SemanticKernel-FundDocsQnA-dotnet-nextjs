"""Run one clustering method end to end; shared by main.py and the notebook."""

from collections.abc import Callable
from dataclasses import dataclass

import numpy as np
import pandas as pd
from shapely.geometry.base import BaseGeometry

from fund_clustering.cluster import ClusterResult, cluster_funds
from fund_clustering.features import cosine_features
from fund_clustering.load import FundData
from fund_clustering.naming import describe_clusters, fund_verdicts
from fund_clustering.neighbours import Neighbours, isolated_flags, nearest_neighbours
from fund_clustering.territory import territory_grid, territory_polygons


@dataclass(frozen=True)
class Method:
    """A way of turning allocations into feature vectors."""

    id: str
    name: str
    description: str
    features: Callable[[FundData], np.ndarray]


@dataclass(frozen=True)
class MethodRun:
    """Everything one method produces, rows in ``FundData.funds`` order."""

    method: Method
    result: ClusterResult
    clusters: pd.DataFrame
    verdicts: np.ndarray
    neighbours: Neighbours
    isolated: np.ndarray
    territories: dict[int, BaseGeometry]


# First entry is the default method on the map.
METHODS = {
    "cosine": Method(
        id="cosine",
        name="Cosine",
        description=(
            "Each fund vector scaled to length 1, so distance compares the mix "
            "of countries and sectors. Short positions used as reported. No weights."
        ),
        features=cosine_features,
    ),
}


def run_method(data: FundData, method: Method) -> MethodRun:
    """Cluster, name, find neighbours and draw territories for one method.

    Args:
        data: Funds loaded by ``load_week``.
        method: Which feature transform to use, e.g. ``METHODS["cosine"]``.

    Returns:
        All results for the method, ready for ``export.method_document``.
    """
    features = method.features(data)
    result = cluster_funds(features)
    categories = data.funds["category"]
    clusters = describe_clusters(categories, result.labels)
    neighbours = nearest_neighbours(features)
    grid = territory_grid(result.xy, result.labels)
    return MethodRun(
        method=method,
        result=result,
        clusters=clusters,
        verdicts=fund_verdicts(categories, result.labels, clusters),
        neighbours=neighbours,
        isolated=isolated_flags(neighbours),
        territories=territory_polygons(grid),
    )
