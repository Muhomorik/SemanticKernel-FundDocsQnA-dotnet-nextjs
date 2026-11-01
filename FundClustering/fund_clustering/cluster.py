"""Cluster fund feature vectors and project them onto a 2D map."""

from dataclasses import dataclass

import numpy as np
import umap
from sklearn.cluster import HDBSCAN

RANDOM_STATE = 42
NOT_IN_CLUSTER = -1


@dataclass(frozen=True)
class ClusterResult:
    """Cluster label and map position per fund, rows in feature order."""

    labels: np.ndarray
    xy: np.ndarray

    @property
    def cluster_count(self) -> int:
        return len(set(self.labels) - {NOT_IN_CLUSTER})

    @property
    def not_in_cluster_count(self) -> int:
        return int(np.sum(self.labels == NOT_IN_CLUSTER))


def cluster_embedding(features: np.ndarray) -> np.ndarray:
    """Reduce features to 10D with dense packing, ready for HDBSCAN.

    ``min_dist=0`` packs similar funds tightly so density-based clustering can
    find groups; HDBSCAN on the raw ~100D vectors labels most funds as noise.

    Args:
        features: One row per fund, e.g. from ``cosine_features``.

    Returns:
        Array of shape ``(n_funds, 10)``.
    """
    reducer = umap.UMAP(
        n_components=10,
        n_neighbors=15,
        min_dist=0.0,
        random_state=RANDOM_STATE,
        n_jobs=1,  # random_state forces single-threaded; say so to skip the warning
    )
    return reducer.fit_transform(features)


def cluster_labels(embedding: np.ndarray) -> np.ndarray:
    """Find dense groups; HDBSCAN decides how many.

    Args:
        embedding: Output of ``cluster_embedding``.

    Returns:
        Cluster id per fund, ``NOT_IN_CLUSTER`` (-1) for funds outside any group.
    """
    return HDBSCAN(min_cluster_size=5, min_samples=3, copy=True).fit_predict(embedding)


def map_coordinates(features: np.ndarray) -> np.ndarray:
    """Project features to 2D for display, spread out so dots stay readable.

    Args:
        features: One row per fund, e.g. from ``cosine_features``.

    Returns:
        Array of shape ``(n_funds, 2)`` scaled by ``scale_to_unit_square``.
    """
    reducer = umap.UMAP(
        n_components=2,
        n_neighbors=40,
        min_dist=0.9,
        spread=1.2,
        random_state=RANDOM_STATE,
        n_jobs=1,
    )
    return scale_to_unit_square(reducer.fit_transform(features))


def scale_to_unit_square(xy: np.ndarray) -> np.ndarray:
    """Shift to the origin and scale so the longer axis spans 0..1.

    Both axes use the same factor, so distances on the map keep their shape.

    Args:
        xy: Points of shape ``(n, 2)``.

    Returns:
        Scaled points; the shorter axis spans ``0..height/width`` (or vice versa).
    """
    shifted = xy - xy.min(axis=0)
    longest = shifted.max()
    return shifted / longest if longest > 0 else shifted


def cluster_funds(features: np.ndarray) -> ClusterResult:
    """Run the full clustering: 10D UMAP + HDBSCAN, and a separate 2D map.

    Args:
        features: One row per fund, e.g. from ``cosine_features``.

    Returns:
        Labels and map coordinates, rows in ``features`` order.
    """
    labels = cluster_labels(cluster_embedding(features))
    return ClusterResult(labels=labels, xy=map_coordinates(features))
