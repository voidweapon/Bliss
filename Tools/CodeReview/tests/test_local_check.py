import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
LOCAL_CHECK = REPOSITORY_ROOT / "Tools" / "CodeReview" / "check-local.sh"


class LocalCheckEntryTests(unittest.TestCase):
    def setUp(self):
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.temp_root = Path(self.temporary_directory.name)
        code_review = self.temp_root / "Tools" / "CodeReview"
        code_review.mkdir(parents=True)
        shutil.copy2(LOCAL_CHECK, code_review / "check-local.sh")
        shutil.copy2(REPOSITORY_ROOT / "Tools" / "CodeReview" / "review_snapshot.py", code_review)
        test_directory = code_review / "tests"
        test_directory.mkdir()
        shutil.copy2(
            REPOSITORY_ROOT / "Tools" / "CodeReview" / "tests" / "test_review_snapshot.py",
            test_directory,
        )
        self.local_check = code_review / "check-local.sh"

        package_lock = self.temp_root / "Unity" / "Packages" / "packages-lock.json"
        package_lock.parent.mkdir(parents=True)
        package_lock.write_text(
            json.dumps({"dependencies": {"com.unity.mathematics": {"version": "1.3.3"}}}),
            encoding="utf-8",
        )
        self.mathematics_package = (
            self.temp_root
            / "Unity"
            / "Library"
            / "PackageCache"
            / "com.unity.mathematics@test-cache"
        )
        (self.mathematics_package / "Unity.Mathematics").mkdir(parents=True)
        (self.mathematics_package / "package.json").write_text(
            json.dumps({"version": "1.3.3"}),
            encoding="utf-8",
        )
        (self.mathematics_package / "Unity.Mathematics" / "Unity.Mathematics.asmdef").write_text(
            "{}\n",
            encoding="utf-8",
        )

        self.fake_bin = self.temp_root / "bin"
        self.fake_bin.mkdir()
        self.dotnet_log = self.temp_root / "dotnet-arguments.txt"
        fake_dotnet = self.fake_bin / "dotnet"
        fake_dotnet.write_text(
            "#!/bin/sh\n"
            "printf '%s\\n' \"$*\" >> \"$DOTNET_LOG\"\n"
            "if [ -n \"${FAIL_ON_MATCH:-}\" ] && "
            "printf '%s' \"$*\" | grep -Fq \"$FAIL_ON_MATCH\"; then exit 7; fi\n",
            encoding="utf-8",
        )
        fake_dotnet.chmod(0o755)
        self.environment = os.environ.copy()
        self.environment["PATH"] = f"{self.fake_bin}{os.pathsep}{self.environment['PATH']}"
        self.environment["DOTNET_LOG"] = str(self.dotnet_log)

    def tearDown(self):
        self.temporary_directory.cleanup()

    def run_check(self, fail_on_match=None):
        environment = self.environment.copy()
        if fail_on_match is not None:
            environment["FAIL_ON_MATCH"] = fail_on_match
        return subprocess.run(
            ["bash", str(self.local_check)],
            cwd=self.temp_root,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_local_entry_runs_checks_in_ci_order(self):
        result = self.run_check()

        self.assertEqual(result.returncode, 0, result.stderr)
        commands = self.dotnet_log.read_text(encoding="utf-8").splitlines()
        self.assertEqual(len(commands), 4)
        self.assertIn("build Server/Server.sln", commands[0])
        self.assertIn("Bliss.ProjectRule.Test.csproj", commands[1])
        self.assertIn("Bliss.UnitTest.csproj", commands[2])
        self.assertIn("Bliss.GameplayIntegrationTest.csproj", commands[3])
        for index in (0, 2, 3):
            self.assertIn("UnityMathematicsPackagePath=", commands[index])
            self.assertIn(str(self.mathematics_package), commands[index])

    def test_local_entry_stops_after_first_failed_ci_check(self):
        result = self.run_check("Bliss.ProjectRule.Test.csproj")

        self.assertNotEqual(result.returncode, 0)
        commands = self.dotnet_log.read_text(encoding="utf-8").splitlines()
        self.assertEqual(len(commands), 2)
        self.assertIn("Bliss.ProjectRule.Test.csproj", commands[-1])

    def test_local_entry_fails_if_cached_package_does_not_match_lockfile(self):
        package_manifest = self.mathematics_package / "package.json"
        package_manifest.write_text(json.dumps({"version": "1.2.0"}), encoding="utf-8")

        result = self.run_check()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Unity.Mathematics 1.3.3", result.stderr)
        self.assertFalse(self.dotnet_log.exists())


if __name__ == "__main__":
    unittest.main()
