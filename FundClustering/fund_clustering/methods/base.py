"""What every clustering method shares: the ``Method`` strategy and raw input."""

from collections.abc import Callable
from dataclasses import dataclass

import numpy as np

from fund_clustering.load import FundData


@dataclass(frozen=True)
class Method:
    """A way of turning allocations into feature vectors.

    Methods differ only in ``features``; clustering, naming and layout are
    shared. Euclidean distance between feature rows must be the method's
    distance, because UMAP and the neighbour search use Euclidean distance.
    """

    id: str
    name: str
    description: str
    features: Callable[[FundData], np.ndarray]


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
