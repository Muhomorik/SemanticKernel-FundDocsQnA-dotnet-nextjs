"""Regenerate all fund cluster map data files from one weekly export.

Usage:
    python main.py [--input DIR] [--output DIR] [--week 2026-W44]

Runs every method in ``fund_clustering.methods.METHODS`` and writes
``methods.json``, ``funds.json`` and one ``{method}.json`` per method.
"""

import argparse
import sys
from pathlib import Path

from fund_clustering.export import write_documents
from fund_clustering.load import latest_week, load_week
from fund_clustering.methods import METHODS
from fund_clustering.pipeline import run_method

PROJECT_DIR = Path(__file__).resolve().parent


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    """Read command-line options; defaults are relative to this project folder."""
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--input",
        type=Path,
        default=PROJECT_DIR / "data",
        help="folder with YieldRaccoon_*_all_<week>.csv exports (default: data/)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=PROJECT_DIR / "output",
        help="folder to write the JSON files to (default: output/)",
    )
    parser.add_argument(
        "--week",
        help="ISO week to process, e.g. 2026-W44 (default: newest in --input)",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    """Run the pipeline and write the files.

    Args:
        argv: Command-line arguments; ``None`` reads ``sys.argv``.

    Returns:
        Process exit code: 0 on success, 1 when input data is missing.
    """
    args = parse_args(argv)
    try:
        week = args.week or latest_week(args.input)
        data = load_week(args.input, week)
    except FileNotFoundError as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    print(f"Week {week}: {len(data.funds)} equity funds from {args.input}")
    runs = []
    for method in METHODS.values():
        run = run_method(data, method)
        result = run.result
        print(
            f"  {method.name}: {result.cluster_count} clusters, "
            f"{result.not_in_cluster_count} not in a cluster"
        )
        runs.append(run)

    for path in write_documents(args.output, data, runs):
        print(f"  wrote {path} ({path.stat().st_size / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
