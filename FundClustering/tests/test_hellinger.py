import numpy as np

from fund_clustering.load import FundData
from fund_clustering.methods import allocation_matrix
from fund_clustering.methods.hellinger import (
    COUNTRY_WEIGHT,
    SECTOR_WEIGHT,
    hellinger_features,
)


def test_hellinger_weights_sixty_forty() -> None:
    assert (COUNTRY_WEIGHT, SECTOR_WEIGHT) == (0.6, 0.4)


def test_hellinger_features_same_shape_as_allocation_matrix(data: FundData) -> None:
    features = hellinger_features(data)

    assert features.shape == allocation_matrix(data).shape


def test_hellinger_features_square_root_of_weighted_shares(data: FundData) -> None:
    # SE0000000001: sverige 110, usa -10 (read as 0), finans 50, teknik 50
    expected = np.sqrt([COUNTRY_WEIGHT, 0.0, SECTOR_WEIGHT / 2, SECTOR_WEIGHT / 2])

    features = hellinger_features(data)

    np.testing.assert_allclose(features[0], expected)


def test_hellinger_features_block_squares_sum_to_weight(data: FundData) -> None:
    countries = len(data.country_columns)

    features = hellinger_features(data)

    np.testing.assert_allclose((features[:, :countries] ** 2).sum(axis=1), COUNTRY_WEIGHT)
    np.testing.assert_allclose((features[:, countries:] ** 2).sum(axis=1), SECTOR_WEIGHT)


def test_hellinger_features_block_renormalised(data: FundData) -> None:
    full = hellinger_features(data)[0]
    data.funds.loc[0, ["sector_finans", "sector_teknik"]] = [20.0, 20.0]

    partial = hellinger_features(data)[0]

    np.testing.assert_allclose(partial, full)


def test_hellinger_features_empty_block_stays_zero(data: FundData) -> None:
    countries = len(data.country_columns)
    data.funds.loc[0, ["sector_finans", "sector_teknik"]] = [0.0, -5.0]

    features = hellinger_features(data)[0]

    np.testing.assert_allclose(features[countries:], 0.0)
    np.testing.assert_allclose(features[:countries], [np.sqrt(COUNTRY_WEIGHT), 0.0])
