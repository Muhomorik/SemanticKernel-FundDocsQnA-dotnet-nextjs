import numpy as np
import pytest

from fund_clustering.cluster import (
    NOT_IN_CLUSTER,
    ClusterResult,
    cluster_funds,
    cluster_labels,
    scale_to_unit_square,
)


def make_groups(group_count: int, size: int, dims: int = 8) -> np.ndarray:
    """Well-separated groups of unit vectors, each around its own axis."""
    rng = np.random.default_rng(0)
    rows = []
    for group in range(group_count):
        centre = np.zeros(dims)
        centre[group] = 1.0
        rows.append(centre + rng.normal(scale=0.02, size=(size, dims)))
    points = np.vstack(rows)
    return points / np.linalg.norm(points, axis=1, keepdims=True)


def test_scale_to_unit_square_longer_axis_spans_zero_to_one() -> None:
    xy = np.array([[10.0, 5.0], [14.0, 7.0], [12.0, 6.0]])

    scaled = scale_to_unit_square(xy)

    np.testing.assert_allclose(scaled, [[0.0, 0.0], [1.0, 0.5], [0.5, 0.25]])


def test_scale_to_unit_square_single_point_stays_at_origin() -> None:
    xy = np.array([[3.0, 4.0]])

    scaled = scale_to_unit_square(xy)

    np.testing.assert_allclose(scaled, [[0.0, 0.0]])


def test_cluster_labels_separated_groups_found() -> None:
    points = make_groups(group_count=3, size=10, dims=3)

    labels = cluster_labels(points)

    assert len(set(labels) - {NOT_IN_CLUSTER}) == 3
    assert len(set(labels[:10])) == 1


def test_cluster_result_counts_exclude_noise() -> None:
    result = ClusterResult(labels=np.array([0, 0, 1, -1, -1]), xy=np.zeros((5, 2)))

    counts = (result.cluster_count, result.not_in_cluster_count)

    assert counts == (2, 2)


@pytest.mark.slow
def test_cluster_funds_separated_groups_clustered_and_mapped() -> None:
    features = make_groups(group_count=3, size=20)

    result = cluster_funds(features)

    assert result.cluster_count == 3
    assert result.not_in_cluster_count == 0
    assert result.xy.shape == (60, 2)
    assert result.xy.min() == 0.0
    assert result.xy.max() == pytest.approx(1.0)
