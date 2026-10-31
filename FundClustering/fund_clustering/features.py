"""Turn fund allocations into feature vectors for clustering."""

import numpy as np

from fund_clustering.load import FundData


def allocation_matrix(data: FundData) -> np.ndarray:
    """Return country + sector shares as fractions, one row per fund.

    Columns are all country columns followed by all sector columns, unweighted.
    Values are used as delivered, so short positions stay negative.

    Args:
        data: Funds loaded by ``load_week``.

    Returns:
        Array of shape ``(n_funds, n_countries + n_sectors)``, rows in
        ``data.funds`` order.
    """
    return data.funds[data.allocation_columns].to_numpy(dtype=float) / 100.0


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
