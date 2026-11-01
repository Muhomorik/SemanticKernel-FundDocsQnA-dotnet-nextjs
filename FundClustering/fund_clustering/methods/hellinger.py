"""Hellinger method: compare square-rooted shares, countries weighted 60/40."""

import numpy as np

from fund_clustering.load import FundData
from fund_clustering.methods.base import Method, allocation_matrix

# Share of the squared distance that each block contributes.
COUNTRY_WEIGHT = 0.6
SECTOR_WEIGHT = 0.4


def square_root_shares(shares: np.ndarray) -> np.ndarray:
    """Return the square root of each row's shares after renormalising to 1.

    Negative shares read as 0. A row with nothing left stays all zeros.

    Args:
        shares: Fractions, one row per fund.

    Returns:
        Same shape; the squares of each non-empty row sum to 1.
    """
    clipped = np.clip(shares, 0.0, None)
    totals = clipped.sum(axis=1, keepdims=True)
    normalised = np.divide(
        clipped, totals, out=np.zeros_like(clipped), where=totals > 0
    )
    return np.sqrt(normalised)


def hellinger_features(data: FundData) -> np.ndarray:
    """Square-rooted shares per block, country block followed by sector block.

    Euclidean distance between the results is the Hellinger distance. The
    square root lifts small positions a little without the log's blow-up.
    Blocks are scaled by the square root of their weight, so countries make up
    60% of the squared distance and sectors 40%.

    Args:
        data: Funds loaded by ``load_week``.

    Returns:
        Array with the same shape and order as ``allocation_matrix``.
    """
    matrix = allocation_matrix(data)
    countries = len(data.country_columns)
    return np.hstack(
        [
            np.sqrt(COUNTRY_WEIGHT) * square_root_shares(matrix[:, :countries]),
            np.sqrt(SECTOR_WEIGHT) * square_root_shares(matrix[:, countries:]),
        ]
    )


METHOD = Method(
    id="hellinger",
    name="Hellinger 60/40",
    description=(
        "Square root of the country shares and of the sector shares, each "
        "renormalised to 100%, so small positions count a little more. "
        "Countries weigh 60% and sectors 40%. Short positions read as 0."
    ),
    features=hellinger_features,
)
