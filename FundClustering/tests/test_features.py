from pathlib import Path

import numpy as np
import pytest

from fund_clustering.features import allocation_matrix, cosine_features
from fund_clustering.load import FundData, load_week
from tests.conftest import WEEK


@pytest.fixture
def data(data_dir: Path) -> FundData:
    return load_week(data_dir, WEEK)


def test_allocation_matrix_countries_then_sectors_as_fractions(data: FundData) -> None:
    matrix = allocation_matrix(data)

    # SE0000000001: sverige 110, usa -10, finans 50, teknik 50
    np.testing.assert_allclose(matrix[0], [1.1, -0.1, 0.5, 0.5])


def test_cosine_features_rows_have_unit_length(data: FundData) -> None:
    features = cosine_features(data)

    np.testing.assert_allclose(np.linalg.norm(features, axis=1), 1.0)


def test_cosine_features_direction_preserved(data: FundData) -> None:
    features = cosine_features(data)
    matrix = allocation_matrix(data)

    # Scaling back by the original length restores the original vector.
    lengths = np.linalg.norm(matrix, axis=1, keepdims=True)
    np.testing.assert_allclose(features * lengths, matrix)


def test_cosine_features_short_position_stays_negative(data: FundData) -> None:
    features = cosine_features(data)

    assert features[0, 1] < 0


def test_cosine_features_zero_vector_raises(data: FundData) -> None:
    data.funds.loc[0, data.allocation_columns] = 0.0

    with pytest.raises(ValueError):
        cosine_features(data)
