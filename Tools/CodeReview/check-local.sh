#!/usr/bin/env bash
set -euo pipefail
export PYTHONDONTWRITEBYTECODE=1

script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd -- "$script_directory/../.." && pwd)"
cd "$repository_root"

unity_mathematics_path="$(
    python3 - "$repository_root" <<'PY'
import json
from pathlib import Path
import sys

root = Path(sys.argv[1])
lock_path = root / "Unity" / "Packages" / "packages-lock.json"
lock = json.loads(lock_path.read_text(encoding="utf-8"))
version = lock["dependencies"]["com.unity.mathematics"]["version"]
package_cache = root / "Unity" / "Library" / "PackageCache"
matches = []
for package_path in package_cache.glob("com.unity.mathematics@*"):
    metadata_path = package_path / "package.json"
    if not metadata_path.is_file():
        continue
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    if (
        metadata.get("version") == version
        and (package_path / "Unity.Mathematics" / "Unity.Mathematics.asmdef").is_file()
    ):
        matches.append(package_path)

if len(matches) != 1:
    raise SystemExit(
        f"Expected one cached Unity.Mathematics {version} package with its runtime assembly; "
        f"found {len(matches)}. Restore the package matching packages-lock.json."
    )
print(matches[0])
PY
)"

printf '%s\n' 'Running review snapshot tooling tests...'
python3 -m unittest discover \
    -s Tools/CodeReview/tests \
    -p 'test_review_snapshot.py' \
    -v

printf '%s\n' 'Building the Server solution...'
dotnet build Server/Server.sln --configuration Debug \
    "-p:UnityMathematicsPackagePath=$unity_mathematics_path"

printf '%s\n' 'Running analyzer tests...'
dotnet test Bliss.ProjectRule/Bliss.ProjectRule.Test/Bliss.ProjectRule.Test.csproj \
    --configuration Debug

printf '%s\n' 'Running unit tests...'
dotnet test Bliss.UnitTest/Bliss.UnitTest/Bliss.UnitTest.csproj \
    --configuration Debug \
    "-p:UnityMathematicsPackagePath=$unity_mathematics_path"

printf '%s\n' 'Running GameplayIntegration tests...'
dotnet test Bliss.UnitTest/Bliss.GameplayIntegrationTest/Bliss.GameplayIntegrationTest.csproj \
    --configuration Debug \
    "-p:UnityMathematicsPackagePath=$unity_mathematics_path"
