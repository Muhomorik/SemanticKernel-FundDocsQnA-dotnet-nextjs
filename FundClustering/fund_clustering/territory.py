"""Cluster territories: background areas behind the dots on the 2D map."""

import math
from dataclasses import dataclass

import numpy as np
import shapely
from shapely.geometry.base import BaseGeometry
from sklearn.neighbors import NearestNeighbors

from fund_clustering.cluster import NOT_IN_CLUSTER

NO_TERRITORY = -1
CELLS_WIDE = 220
VOTERS = 7
RADIUS = 0.05


@dataclass(frozen=True)
class TerritoryGrid:
    """Cluster id per grid cell; row 0 is the bottom (smallest y)."""

    labels: np.ndarray
    origin: tuple[float, float]
    cell_size: float


def territory_grid(
    xy: np.ndarray,
    labels: np.ndarray,
    cells_wide: int = CELLS_WIDE,
    voters: int = VOTERS,
    radius: float = RADIUS,
) -> TerritoryGrid:
    """Give each grid cell the cluster that its nearest funds belong to.

    Each cell centre takes up to ``voters`` nearest clustered funds within
    ``radius``; each votes for its cluster with weight ``1 / distance``.
    Unclustered funds never vote, and cells with no voter stay empty.

    Args:
        xy: Map coordinates per fund (from ``map_coordinates``).
        labels: Cluster id per fund; ``NOT_IN_CLUSTER`` funds are ignored.
        cells_wide: Grid width in cells; height follows the map's aspect.
        voters: Nearest funds consulted per cell.
        radius: Farthest a fund can be and still vote, in map units.

    Returns:
        The grid, padded by ``radius`` around all funds so edge dots get
        territory too.
    """
    x0, y0 = xy.min(axis=0) - radius
    x1, y1 = xy.max(axis=0) + radius
    cell_size = (x1 - x0) / cells_wide
    rows = math.ceil((y1 - y0) / cell_size)
    grid = np.full((rows, cells_wide), NO_TERRITORY)

    clustered = labels != NOT_IN_CLUSTER
    if clustered.any():
        centres_x = x0 + (np.arange(cells_wide) + 0.5) * cell_size
        centres_y = y0 + (np.arange(rows) + 0.5) * cell_size
        gx, gy = np.meshgrid(centres_x, centres_y)
        centres = np.column_stack([gx.ravel(), gy.ravel()])
        grid = _vote(centres, xy[clustered], labels[clustered], voters, radius)
        grid = grid.reshape(rows, cells_wide)

    return TerritoryGrid(
        labels=grid, origin=(float(x0), float(y0)), cell_size=cell_size
    )


def _vote(
    centres: np.ndarray,
    xy: np.ndarray,
    labels: np.ndarray,
    voters: int,
    radius: float,
) -> np.ndarray:
    search = NearestNeighbors(n_neighbors=min(voters, len(xy))).fit(xy)
    distances, nearest = search.kneighbors(centres)
    weights = np.where(distances <= radius, 1.0 / np.maximum(distances, 1e-9), 0.0)

    cluster_ids, positions = np.unique(labels, return_inverse=True)
    votes = np.zeros((len(centres), len(cluster_ids)))
    rows = np.arange(len(centres))
    for column in range(nearest.shape[1]):
        np.add.at(votes, (rows, positions[nearest[:, column]]), weights[:, column])

    winners = cluster_ids[votes.argmax(axis=1)]
    return np.where(votes.max(axis=1) > 0, winners, NO_TERRITORY)


def territory_polygons(grid: TerritoryGrid) -> dict[int, BaseGeometry]:
    """Merge each cluster's cells into one shape.

    Args:
        grid: Output of ``territory_grid``.

    Returns:
        Cluster id -> ``Polygon`` or ``MultiPolygon`` (a cluster's territory
        can be split), in map coordinates. Empty cells produce no shape.
    """
    # Cell edges computed once as origin + n * size, so the shared edge of two
    # adjacent cells is bit-identical and the union has no hairline gaps.
    row_count, col_count = grid.labels.shape
    xs = grid.origin[0] + np.arange(col_count + 1) * grid.cell_size
    ys = grid.origin[1] + np.arange(row_count + 1) * grid.cell_size

    polygons = {}
    for cluster_id in np.unique(grid.labels):
        if cluster_id == NO_TERRITORY:
            continue
        rows, cols = np.nonzero(grid.labels == cluster_id)
        cells = shapely.box(xs[cols], ys[rows], xs[cols + 1], ys[rows + 1])
        polygons[int(cluster_id)] = shapely.union_all(cells)
    return polygons
