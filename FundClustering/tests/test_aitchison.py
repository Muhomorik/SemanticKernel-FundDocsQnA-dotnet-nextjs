import numpy as np

from fund_clustering.load import FundData
from fund_clustering.methods import allocation_matrix
from fund_clustering.methods.aitchison import PSEUDO_COUNT, aitchison_features


def test_aitchison_features_same_shape_as_allocation_matrix(data: FundData) -> None:
    features = aitchison_features(data)

    assert features.shape == allocation_matrix(data).shape


def test_aitchison_features_each_block_sums_to_zero(data: FundData) -> None:
    countries = len(data.country_columns)

    features = aitchison_features(data)

    np.testing.assert_allclose(features[:, :countries].sum(axis=1), 0.0, atol=1e-12)
    np.testing.assert_allclose(features[:, countries:].sum(axis=1), 0.0, atol=1e-12)


def test_aitchison_features_centred_log_ratio_per_block(data: FundData) -> None:
    # SE0000000001: sverige 110, usa -10 (read as 0), finans 50, teknik 50
    logs = np.log([1.1 + PSEUDO_COUNT, PSEUDO_COUNT])
    half_gap = (logs[0] - logs[1]) / 2
    expected = [half_gap, -half_gap, 0.0, 0.0]

    features = aitchison_features(data)

    np.testing.assert_allclose(features[0], expected)


def test_aitchison_features_short_position_same_as_zero(data: FundData) -> None:
    shorted = aitchison_features(data)[0]
    data.funds.loc[0, "country_usa"] = 0.0

    zeroed = aitchison_features(data)[0]

    np.testing.assert_allclose(shorted, zeroed)
