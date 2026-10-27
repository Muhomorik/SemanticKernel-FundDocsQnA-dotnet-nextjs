"""NUnit 5 migration: Assert.ThrowsAsync/CatchAsync/DoesNotThrowAsync now return Task and must be awaited.

Adds `await` to each un-awaited call and turns the enclosing `public void X(` test into `public async Task X(`.
Usage: python nunit5-await-asserts.py <dir> [<dir> ...]
"""
import pathlib
import re
import sys

CALL = re.compile(r"(?<!await )\bAssert\.(ThrowsAsync|CatchAsync|DoesNotThrowAsync)\b")
VOID_METHOD = re.compile(r"^(\s*public\s+)void(\s+\w+\s*\()")
ANY_METHOD = re.compile(r"^\s*(public|private|protected|internal)\s+[\w<>\[\],? ]+\s+\w+\s*\(")

for root in sys.argv[1:]:
    for path in pathlib.Path(root).rglob("*.cs"):
        if "obj" in path.parts or "bin" in path.parts:
            continue
        with open(path, encoding="utf-8-sig", newline="") as f:  # newline="" keeps CRLF intact
            text = f.read()
        lines = text.splitlines(keepends=True)
        changed = 0
        for i, line in enumerate(lines):
            if not CALL.search(line):
                continue
            lines[i] = CALL.sub(lambda m: "await " + m.group(0), line)
            changed += 1
            for j in range(i - 1, -1, -1):
                if VOID_METHOD.match(lines[j]):
                    lines[j] = VOID_METHOD.sub(r"\1async Task\2", lines[j])
                    break
                if ANY_METHOD.match(lines[j]):
                    break  # already async Task (or not a void test) - leave as is
        if changed:
            had_bom = path.read_bytes().startswith(b"\xef\xbb\xbf")
            path.write_text("".join(lines), encoding="utf-8-sig" if had_bom else "utf-8", newline="")
            print(f"{path}: {changed} call(s)")
