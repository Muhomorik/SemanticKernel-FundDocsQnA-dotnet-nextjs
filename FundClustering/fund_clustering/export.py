"""Turn pipeline results into the JSON files the backend serves to the chart.

Everything the chart shows is precomputed here, so the frontend only draws.
Keys are camelCase for the TypeScript frontend; numbers are rounded to keep
files small.
"""

import json
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.metrics import adjusted_rand_score, normalized_mutual_info_score

from fund_clustering.cluster import NOT_IN_CLUSTER
from fund_clustering.load import FundData
from fund_clustering.naming import AGREE, DIFFERENT, NEIGHBOURING, category_family
from fund_clustering.pipeline import MethodRun

DECIMALS = 4
TOP_ALLOCATIONS = 4


def _round(value: float) -> float:
    return round(float(value), DECIMALS)


def top_allocations(
    row: pd.Series, columns: list[str], count: int = TOP_ALLOCATIONS
) -> list[list]:
    """Largest allocations for one fund, as ``[[name, percent], ...]``.

    Args:
        row: One fund's row from ``FundData.funds``.
        columns: Allocation columns to consider, e.g. the country columns.
        count: How many to keep.

    Returns:
        Largest first; zero allocations dropped; the ``country_`` / ``sector_``
        prefix removed. Values as delivered, rounded to 2 decimals.
    """
    values = [
        (column.split("_", 1)[1], float(row[column]))
        for column in columns
        if row[column] != 0
    ]
    values.sort(key=lambda item: item[1], reverse=True)
    return [[name, round(value, 2)] for name, value in values[:count]]


def funds_document(data: FundData) -> dict:
    """Per-fund display data shared by every method.

    ``shade`` is the category's position (alphabetical) within its family, so
    the chart can give region variants different shades of the family colour.

    Args:
        data: Funds loaded by ``load_week``.

    Returns:
        ``{"week": ..., "funds": [...]}`` in ``data.funds`` order.
    """
    # str() for the type checker: pandas types unique() values as Any | Timestamp.
    categories = sorted({str(category) for category in data.funds["category"]})
    families = {category: category_family(category) for category in categories}
    shades = {}
    for category in categories:
        family = families[category]
        shades[category] = sum(1 for c in shades if families[c] == family)

    funds = [
        {
            "isin": row["isin"],
            "name": row["name"],
            "company": row["company_name"],
            "category": row["category"],
            "family": families[row["category"]],
            "shade": shades[row["category"]],
            "isIndex": bool(row["is_index_fund"]),
            "topCountries": top_allocations(row, data.country_columns),
            "topSectors": top_allocations(row, data.sector_columns),
        }
        for _, row in data.funds.iterrows()
    ]
    return {"week": data.week, "funds": funds}


def method_document(data: FundData, run: MethodRun) -> dict:
    """Everything the chart needs for one method.

    Args:
        data: Funds loaded by ``load_week``.
        run: Output of ``pipeline.run_method`` for the same funds.

    Returns:
        Method description, summary, points, clusters, categories, territory
        polygons and the nearest neighbours per ISIN.
    """
    isins = data.funds["isin"].tolist()
    labels = run.result.labels
    xy = run.result.xy
    clusters = run.clusters.sort_index()

    return {
        "week": data.week,
        "method": run.method.id,
        "name": run.method.name,
        "description": run.method.description,
        "summary": _summary(data, run),
        "points": [
            {
                "isin": isin,
                "x": _round(x),
                "y": _round(y),
                "cluster": int(label),
                "verdict": str(verdict),
                "isolated": bool(isolated),
            }
            for isin, (x, y), label, verdict, isolated in zip(
                isins, xy, labels, run.verdicts, run.isolated
            )
        ],
        "clusters": [
            {
                "id": cluster_id,
                "name": cluster["name"],
                "size": int(cluster["size"]),
                "purity": _round(cluster["purity"]),
                "leadingCategory": cluster["leading_category"],
                "labelX": _round(np.median(xy[labels == cluster_id, 0])),
                "labelY": _round(np.median(xy[labels == cluster_id, 1])),
                "categories": {
                    category: int(count)
                    for category, count in cluster["category_counts"].items()
                },
            }
            # Ids from the index as plain ints: iterrows() types labels as Hashable.
            for cluster_id, (_, cluster) in zip(
                clusters.index.astype(int).tolist(), clusters.iterrows()
            )
        ],
        "categories": _categories(data, run),
        "territory": [
            {
                "cluster": int(cluster_id),
                "rings": [
                    [[_round(x), _round(y)] for x, y in ring.coords]
                    for ring in [polygon.exterior, *polygon.interiors]
                ],
            }
            for cluster_id, shape in sorted(run.territories.items())
            for polygon in getattr(shape, "geoms", [shape])
        ],
        "neighbours": {
            isin: [
                {"isin": isins[index], "distance": _round(distance)}
                for index, distance in zip(indices, distances)
            ]
            for isin, indices, distances in zip(
                isins, run.neighbours.indices, run.neighbours.distances
            )
        },
    }


def _summary(data: FundData, run: MethodRun) -> dict:
    clustered = run.result.labels != NOT_IN_CLUSTER
    count = int(clustered.sum())
    verdicts = run.verdicts[clustered]
    categories = data.funds["category"].to_numpy()[clustered]
    labels = run.result.labels[clustered]

    def share(verdict: str) -> float:
        return _round(np.mean(verdicts == verdict)) if count else 0.0

    return {
        "funds": len(data.funds),
        "clusters": run.result.cluster_count,
        "notInCluster": run.result.not_in_cluster_count,
        "agree": share(AGREE),
        "neighbouring": share(NEIGHBOURING),
        "different": share(DIFFERENT),
        "ari": _round(adjusted_rand_score(categories, labels)) if count else None,
        "nmi": _round(normalized_mutual_info_score(categories, labels))
        if count
        else None,
    }


def _categories(data: FundData, run: MethodRun) -> list[dict]:
    frame = pd.DataFrame(
        {
            "category": data.funds["category"].to_numpy(),
            "clustered": run.result.labels != NOT_IN_CLUSTER,
            "agree": run.verdicts == AGREE,
        }
    )
    rows = []
    # groupby sorts by category; str() because pandas types group keys as any scalar.
    for key, group in frame.groupby("category"):
        category = str(key)
        clustered = int(group["clustered"].sum())
        rows.append(
            {
                "category": category,
                "family": category_family(category),
                "funds": len(group),
                "agreeShare": _round(group["agree"].sum() / clustered)
                if clustered
                else None,
            }
        )
    return rows


def write_documents(
    output_dir: Path | str, data: FundData, runs: list[MethodRun]
) -> list[Path]:
    """Write ``methods_<week>.json``, ``funds_<week>.json`` and one
    ``<method>_<week>.json`` per run, so several weeks can share a folder.

    Args:
        output_dir: Folder to write into; created if missing.
        data: Funds loaded by ``load_week``.
        runs: Method results; the first one is the default on the map.

    Returns:
        Paths of the files written.
    """
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    def file_name(base: str) -> str:
        return f"{base}_{data.week}.json"

    documents = {file_name("funds"): funds_document(data)}
    for run in runs:
        documents[file_name(run.method.id)] = method_document(data, run)
    documents[file_name("methods")] = {
        "week": data.week,
        "default": runs[0].method.id,
        "methods": [
            {
                "id": run.method.id,
                "name": run.method.name,
                "file": file_name(run.method.id),
            }
            for run in runs
        ],
    }

    written = []
    for file_name, document in documents.items():
        path = output_dir / file_name
        path.write_text(
            json.dumps(document, ensure_ascii=False, separators=(",", ":")),
            encoding="utf-8",
        )
        written.append(path)
    return written
