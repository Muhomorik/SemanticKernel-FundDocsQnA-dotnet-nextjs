"""Aitchison method: compare the ratios between shares."""

import numpy as np

from fund_clustering.load import FundData
from fund_clustering.methods.base import Method, allocation_matrix

# Stand-in for a zero share so the logarithm is defined.
PSEUDO_COUNT = 0.005


def centred_log_ratio(shares: np.ndarray) -> np.ndarray:
    """Return the centred log-ratio of each row.

    Negative shares read as 0 and ``PSEUDO_COUNT`` is added before the log.

    Args:
        shares: Fractions, one row per fund.

    Returns:
        Log shares minus their row mean, same shape; each row sums to 0.
    """
    logs = np.log(np.clip(shares, 0.0, None) + PSEUDO_COUNT)
    return logs - logs.mean(axis=1, keepdims=True)


def aitchison_features(data: FundData) -> np.ndarray:
    """Centred log-ratio per block, country block followed by sector block.

    Euclidean distance between the results is the Aitchison distance, which
    compares the *ratios* between shares, so small positions count as much
    as large ones.

    Args:
        data: Funds loaded by ``load_week``.

    Returns:
        Array with the same shape and order as ``allocation_matrix``.
    """
    matrix = allocation_matrix(data)
    countries = len(data.country_columns)
    return np.hstack(
        [
            centred_log_ratio(matrix[:, :countries]),
            centred_log_ratio(matrix[:, countries:]),
        ]
    )


METHOD = Method(
    id="aitchison",
    name="Aitchison",
    description=(
        "Centred log-ratio of the country shares and of the sector shares, so "
        "distance compares ratios between holdings and small positions count. "
        "Short positions read as 0. No weights."
    ),
    features=aitchison_features,
)
