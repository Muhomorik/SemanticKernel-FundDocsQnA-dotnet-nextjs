import numpy as np

from fund_clustering.neighbours import Neighbours, isolated_flags, nearest_neighbours


def test_nearest_neighbours_closest_first_and_self_excluded() -> None:
    features = np.array([[0.0], [1.0], [3.0], [7.0]])

    neighbours = nearest_neighbours(features, k=3)

    assert list(neighbours.indices[0]) == [1, 2, 3]
    np.testing.assert_allclose(neighbours.distances[0], [1.0, 3.0, 7.0])


def test_nearest_neighbours_identical_funds_never_list_themselves() -> None:
    features = np.array([[1.0, 0.0], [1.0, 0.0], [0.0, 1.0]])

    neighbours = nearest_neighbours(features, k=2)

    assert list(neighbours.indices[0]) == [1, 2]
    assert list(neighbours.indices[1]) == [0, 2]
    assert neighbours.distances[0, 0] == 0.0


def test_nearest_neighbours_k_capped_at_other_fund_count() -> None:
    features = np.array([[0.0], [1.0], [2.0]])

    neighbours = nearest_neighbours(features, k=20)

    assert neighbours.indices.shape == (3, 2)


def test_nearest_neighbours_ties_keep_feature_order() -> None:
    features = np.array([[0.0], [1.0], [-1.0]])

    neighbours = nearest_neighbours(features, k=2)

    assert list(neighbours.indices[0]) == [1, 2]


def test_isolated_flags_far_fund_flagged() -> None:
    grid = np.array([[x, y] for x in range(5) for y in range(4)], dtype=float)
    features = np.vstack([grid, [[100.0, 100.0]]])
    neighbours = nearest_neighbours(features)

    flags = isolated_flags(neighbours, rank=5, share=0.05)

    assert flags[-1]
    assert flags.sum() == 1


def test_isolated_flags_uses_requested_rank() -> None:
    neighbours = Neighbours(
        indices=np.zeros((4, 2), dtype=int),
        distances=np.array([[1.0, 1.0], [1.0, 2.0], [1.0, 3.0], [9.0, 4.0]]),
    )

    flags = isolated_flags(neighbours, rank=2, share=0.25)

    assert list(flags) == [False, False, False, True]
