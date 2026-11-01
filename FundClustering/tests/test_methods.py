import numpy as np

from fund_clustering.load import FundData
from fund_clustering.methods import METHODS, allocation_matrix
from fund_clustering.methods.aitchison import aitchison_features
from fund_clustering.methods.cosine import cosine_features
from fund_clustering.methods.hellinger import hellinger_features


def test_methods_include_cosine_as_first() -> None:
    first = next(iter(METHODS.values()))

    assert first.id == "cosine"
    assert first.features is cosine_features


def test_methods_cosine_then_aitchison_then_hellinger() -> None:
    assert list(METHODS) == ["cosine", "aitchison", "hellinger"]
    assert METHODS["aitchison"].features is aitchison_features
    assert METHODS["hellinger"].features is hellinger_features


def test_methods_keyed_by_own_id() -> None:
    assert all(key == method.id for key, method in METHODS.items())


def test_allocation_matrix_countries_then_sectors_as_fractions(data: FundData) -> None:
    # SE0000000001: sverige 110, usa -10, finans 50, teknik 50
    expected = [1.1, -0.1, 0.5, 0.5]

    matrix = allocation_matrix(data)

    np.testing.assert_allclose(matrix[0], expected)
