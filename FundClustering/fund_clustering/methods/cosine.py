"""Cosine method: compare the mix of countries and sectors."""

import numpy as np

from fund_clustering.load import FundData
from fund_clustering.methods.base import Method, allocation_matrix


def cosine_features(data: FundData) -> np.ndarray:
    """Scale each fund's allocation vector to length 1.

    Euclidean distance between unit vectors ranks funds the same way as cosine
    distance, so the result compares allocation *mix* rather than size.

    Args:
        data: Funds loaded by ``load_week``.

    Returns:
        Unit-length rows, same shape and order as ``allocation_matrix``.

    Raises:
        ValueError: If a fund has no allocations at all (zero vector).
    """
    matrix = allocation_matrix(data)
    norms = np.linalg.norm(matrix, axis=1, keepdims=True)
    if np.any(norms == 0):
        raise ValueError("Fund with an all-zero allocation vector cannot be scaled")
    return matrix / norms


METHOD = Method(
    id="cosine",
    name="Cosine",
    description=(
        "Each fund vector scaled to length 1, so distance compares the mix "
        "of countries and sectors. Short positions used as reported. No weights."
    ),
    features=cosine_features,
)
