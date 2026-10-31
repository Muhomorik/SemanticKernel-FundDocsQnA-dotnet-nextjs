"""Most similar funds per fund, and which funds sit far from all others."""

from dataclasses import dataclass

import numpy as np
from sklearn.metrics import pairwise_distances

NEIGHBOUR_COUNT = 20
ISOLATION_RANK = 5
ISOLATED_SHARE = 0.05


@dataclass(frozen=True)
class Neighbours:
    """Nearest other funds per fund, closest first, rows in feature order."""

    indices: np.ndarray
    distances: np.ndarray


def nearest_neighbours(features: np.ndarray, k: int = NEIGHBOUR_COUNT) -> Neighbours:
    """Find each fund's ``k`` most similar other funds.

    Distances are Euclidean in the method's own feature space (not on the 2D
    map, which distorts distances). For unit-length cosine features this ranks
    funds exactly like cosine distance.

    The full distance matrix is computed so a fund is never listed as its own
    neighbour, even when share classes have identical allocations. That is
    fine for a few thousand funds.

    Args:
        features: One row per fund, e.g. from ``cosine_features``.
        k: Neighbours per fund; capped at ``n_funds - 1``.

    Returns:
        Indices and distances of shape ``(n_funds, k)``. Ties keep feature
        order, so results are stable between runs.
    """
    distances = pairwise_distances(features)
    np.fill_diagonal(distances, np.inf)

    k = min(k, len(features) - 1)
    order = np.argsort(distances, axis=1, kind="stable")[:, :k]
    return Neighbours(
        indices=order, distances=np.take_along_axis(distances, order, axis=1)
    )


def isolated_flags(
    neighbours: Neighbours,
    rank: int = ISOLATION_RANK,
    share: float = ISOLATED_SHARE,
) -> np.ndarray:
    """Flag funds whose ``rank``-th nearest neighbour is unusually far away.

    Args:
        neighbours: Output of ``nearest_neighbours`` with at least ``rank``
            neighbours per fund.
        rank: Which neighbour's distance to use (5 = fifth closest).
        share: Fraction of funds to flag, farthest first.

    Returns:
        Boolean per fund; ``True`` when that distance is above the
        ``1 - share`` quantile. Funds tied at the cut-off are not flagged, so
        equal distances everywhere flag nobody.
    """
    distance = neighbours.distances[:, rank - 1]
    return distance > np.quantile(distance, 1 - share)
