import numpy as np
import pytest
from shapely.geometry import Point

from fund_clustering.territory import (
    NO_TERRITORY,
    TerritoryGrid,
    territory_grid,
    territory_polygons,
)

# Two clusters: 0 on the left, 1 on the right; one unclustered fund (-1) between.
XY = np.array(
    [
        [0.10, 0.10], [0.12, 0.12], [0.10, 0.14],
        [0.90, 0.10], [0.88, 0.12], [0.90, 0.14],
        [0.50, 0.12],
    ]
)
LABELS = np.array([0, 0, 0, 1, 1, 1, -1])


def cell_label_at(grid: TerritoryGrid, x: float, y: float) -> int:
    col = int((x - grid.origin[0]) // grid.cell_size)
    row = int((y - grid.origin[1]) // grid.cell_size)
    return int(grid.labels[row, col])


def test_territory_grid_cells_near_cluster_take_its_label() -> None:
    grid = territory_grid(XY, LABELS, cells_wide=100, radius=0.1)

    left, right = cell_label_at(grid, 0.11, 0.12), cell_label_at(grid, 0.89, 0.12)

    assert (left, right) == (0, 1)


def test_territory_grid_cells_beyond_radius_are_empty() -> None:
    grid = territory_grid(XY, LABELS, cells_wide=100, radius=0.1)

    middle = cell_label_at(grid, 0.50, 0.12)

    assert middle == NO_TERRITORY


def test_territory_grid_width_in_cells_matches_request() -> None:
    grid = territory_grid(XY, LABELS, cells_wide=100, radius=0.1)

    rows, cols = grid.labels.shape

    assert cols == 100
    assert rows < cols  # map is much wider than tall


def test_territory_grid_closer_cluster_outvotes_farther_one() -> None:
    xy = np.array([[0.0, 0.0], [1.0, 0.0]])
    labels = np.array([0, 1])

    grid = territory_grid(xy, labels, cells_wide=50, radius=2.0)

    assert cell_label_at(grid, 0.2, 0.0) == 0
    assert cell_label_at(grid, 0.8, 0.0) == 1


def test_territory_grid_no_clustered_funds_gives_empty_grid() -> None:
    labels = np.full(len(XY), -1)

    grid = territory_grid(XY, labels, cells_wide=20, radius=0.1)

    assert (grid.labels == NO_TERRITORY).all()


def test_territory_polygons_one_shape_per_cluster_containing_its_funds() -> None:
    grid = territory_grid(XY, LABELS, cells_wide=100, radius=0.1)

    polygons = territory_polygons(grid)

    assert set(polygons) == {0, 1}
    for (x, y), label in zip(XY[:6], LABELS[:6]):
        assert polygons[label].contains(Point(x, y))


def test_territory_polygons_do_not_overlap() -> None:
    xy = np.array([[0.0, 0.0], [1.0, 0.0]])
    grid = territory_grid(xy, np.array([0, 1]), cells_wide=50, radius=2.0)

    polygons = territory_polygons(grid)

    assert polygons[0].intersection(polygons[1]).area == pytest.approx(0.0)


def test_territory_polygons_area_matches_cell_count() -> None:
    grid = TerritoryGrid(
        labels=np.array([[0, 0, -1], [1, 0, -1]]), origin=(0.0, 0.0), cell_size=0.5
    )

    polygons = territory_polygons(grid)

    assert polygons[0].area == pytest.approx(3 * 0.25)
    assert polygons[1].area == pytest.approx(0.25)
