import numpy as np
import pytest

from fund_clustering.load import FundData
from fund_clustering.methods import allocation_matrix
from fund_clustering.methods.cosine import cosine_features


def test_cosine_features_rows_have_unit_length(data: FundData) -> None:
    features = cosine_features(data)

    np.testing.assert_allclose(np.linalg.norm(features, axis=1), 1.0)


def test_cosine_features_direction_preserved(data: FundData) -> None:
    matrix = allocation_matrix(data)
    lengths = np.linalg.norm(matrix, axis=1, keepdims=True)

    features = cosine_features(data)

    # Scaling back by the original length restores the original vector.
    np.testing.assert_allclose(features * lengths, matrix)


def test_cosine_features_short_position_stays_negative(data: FundData) -> None:
    features = cosine_features(data)

    assert features[0, 1] < 0


def test_cosine_features_zero_vector_raises(data: FundData) -> None:
    data.funds.loc[0, data.allocation_columns] = 0.0

    with pytest.raises(ValueError):
        cosine_features(data)
