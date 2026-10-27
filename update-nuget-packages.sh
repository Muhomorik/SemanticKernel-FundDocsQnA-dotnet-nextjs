#!/usr/bin/env bash
# Updates every outdated NuGet package (latest stable) in the given solutions/projects.
# Usage: ./update-nuget-packages.sh PdfTextExtractor/PdfTextExtractor.sln YieldRaccoon/YieldRaccoon.sln ...
# Prerelease-only packages (e.g. SK Ollama connector) show "Not found at the sources" and are skipped — bump those manually.
set -euo pipefail

for target in "$@"; do
  project=""
  dotnet list "$target" package --outdated 2>/dev/null | tr -d '\r' | while read -r line; do
    if [[ "$line" =~ ^Project\ \`([^\`]+)\` ]]; then
      name="${BASH_REMATCH[1]}"
      project=$(find "$(dirname "$target")" -name "$name.csproj" -not -path "*/obj/*" | head -n1)
      echo "== $project"
    elif [[ "$line" =~ ^\>\ ([^ ]+)\ +([^ ]+)\ +([^ ]+)\ +([^ ]+) ]]; then
      pkg="${BASH_REMATCH[1]}"; latest="${BASH_REMATCH[4]}"
      [[ "$latest" == "Not" ]] && { echo "   skip $pkg (prerelease only)"; continue; }
      echo "   $pkg -> $latest"
      dotnet add "$project" package "$pkg" --version "$latest" --no-restore >/dev/null
    fi
  done
done
