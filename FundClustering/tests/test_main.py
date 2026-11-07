import json
from pathlib import Path

import pytest

from main import PROJECT_DIR, main, parse_args
from tests.conftest import write_week
from tests.test_pipeline import grouped_funds

METADATA_COLUMNS = [
    "isin", "name", "category", "fund_type", "company_name", "is_index_fund"
]


def write_grouped_week(data_dir: Path, week: str) -> None:
    data = grouped_funds()
    write_week(
        data_dir,
        week,
        data.funds[["isin", "name", *data.allocation_columns]].to_dict("records"),
        data.funds[METADATA_COLUMNS].to_dict("records"),
    )


def test_parse_args_defaults_relative_to_project() -> None:
    args = parse_args([])

    assert args.input == PROJECT_DIR / "data"
    assert args.output == PROJECT_DIR / "output"
    assert args.week is None


def test_parse_args_custom_values() -> None:
    args = parse_args(["--input", "in", "--output", "out", "--week", "2026-W01"])

    assert (args.input, args.output, args.week) == (Path("in"), Path("out"), "2026-W01")


def test_main_missing_input_returns_error(
    tmp_path: Path, capsys: pytest.CaptureFixture[str]
) -> None:
    exit_code = main(["--input", str(tmp_path / "missing"), "--output", str(tmp_path)])

    assert exit_code == 1
    assert "No allocations export" in capsys.readouterr().err


@pytest.mark.slow
def test_main_latest_week_written_to_output(tmp_path: Path) -> None:
    input_dir, output_dir = tmp_path / "in", tmp_path / "out"
    input_dir.mkdir()
    write_grouped_week(input_dir, "2026-W01")
    write_grouped_week(input_dir, "2026-W02")

    exit_code = main(["--input", str(input_dir), "--output", str(output_dir)])

    assert exit_code == 0
    assert sorted(p.name for p in output_dir.iterdir()) == [
        "aitchison_2026-W02.json",
        "cosine_2026-W02.json",
        "funds_2026-W02.json",
        "hellinger_2026-W02.json",
        "methods_2026-W02.json",
    ]
    manifest = json.loads(
        (output_dir / "methods_2026-W02.json").read_text(encoding="utf-8")
    )
    assert manifest["week"] == "2026-W02"
