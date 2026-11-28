import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
SNAPSHOT_TOOL = REPOSITORY_ROOT / "Tools" / "CodeReview" / "review_snapshot.py"


class ReviewSnapshotCliTests(unittest.TestCase):
    def setUp(self):
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.temp_root = Path(self.temporary_directory.name)
        self.repository = self.temp_root / "repo"
        self.repository.mkdir()
        self.git("init", "--quiet")
        self.git("config", "user.email", "review-test@example.invalid")
        self.git("config", "user.name", "Review Test")
        (self.repository / "tracked.txt").write_text("baseline\n", encoding="utf-8")
        (self.repository / ".gitignore").write_text(
            "ignored/\nfoo\\*\n",
            encoding="utf-8",
        )
        (self.repository / "subdir").mkdir()
        (self.repository / "subdir" / "nested.txt").write_text("nested baseline\n", encoding="utf-8")
        self.git("add", "tracked.txt", ".gitignore", "subdir/nested.txt")
        self.git("commit", "--quiet", "-m", "baseline")
        self.base = self.git("rev-parse", "HEAD").stdout.strip()

    def tearDown(self):
        self.temporary_directory.cleanup()

    def git(self, *arguments):
        return subprocess.run(
            ["git", *arguments],
            cwd=self.repository,
            check=True,
            capture_output=True,
            text=True,
        )

    def run_tool(self, *arguments):
        return subprocess.run(
            [sys.executable, str(SNAPSHOT_TOOL), *arguments],
            cwd=self.repository,
            capture_output=True,
            text=True,
            check=False,
        )

    def capture(self, output, *extra_arguments):
        return self.run_tool(
            "capture",
            "--base",
            self.base,
            "--tracked",
            "tracked.txt",
            "--output",
            str(output),
            *extra_arguments,
        )

    def test_capture_records_combined_tracked_and_explicit_untracked_patch(self):
        tracked = self.repository / "tracked.txt"
        tracked.write_text("staged change\n", encoding="utf-8")
        self.git("add", "tracked.txt")
        tracked.write_text("final working tree content\n", encoding="utf-8")
        (self.repository / "new.txt").write_text("explicit addition\n", encoding="utf-8")
        output = self.temp_root / "snapshot"

        result = self.capture(output, "--untracked", "new.txt")

        self.assertEqual(result.returncode, 0, result.stderr)
        manifest = json.loads((output / "manifest.json").read_text(encoding="utf-8"))
        patch = (output / "patch.diff").read_text(encoding="utf-8")
        self.assertEqual(manifest["base_commit"], self.base)
        self.assertEqual(manifest["tracked_paths"], ["tracked.txt"])
        self.assertEqual(manifest["untracked_paths"], ["new.txt"])
        self.assertEqual(patch.count("diff --git a/tracked.txt"), 1)
        self.assertIn("final working tree content", patch)
        self.assertIn("explicit addition", patch)
        self.assertTrue((output / "review-record.md").is_file())
        self.assertIn("sha256", manifest["patch"])

    def test_capture_rejects_empty_patch_without_creating_output(self):
        output = self.temp_root / "empty-snapshot"

        result = self.capture(output)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("empty", result.stderr.lower())
        self.assertFalse(output.exists())

    def test_capture_does_not_include_unselected_untracked_files(self):
        (self.repository / "tracked.txt").write_text("selected change\n", encoding="utf-8")
        (self.repository / "unselected.txt").write_text("outside review scope\n", encoding="utf-8")
        output = self.temp_root / "scoped-snapshot"

        result = self.capture(output)

        self.assertEqual(result.returncode, 0, result.stderr)
        patch = (output / "patch.diff").read_text(encoding="utf-8")
        manifest = json.loads((output / "manifest.json").read_text(encoding="utf-8"))
        self.assertNotIn("outside review scope", patch)
        self.assertEqual(manifest["untracked_paths"], [])

    def test_capture_rejects_ignored_untracked_path(self):
        (self.repository / "ignored").mkdir()
        (self.repository / "ignored" / "data.txt").write_text("ignored\n", encoding="utf-8")
        output = self.temp_root / "ignored-snapshot"

        result = self.capture(output, "--untracked", "ignored/data.txt")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("untracked", result.stderr.lower())
        self.assertFalse(output.exists())

    def test_capture_rejects_tracked_directory_as_scope(self):
        output = self.temp_root / "directory-snapshot"

        result = self.run_tool(
            "capture",
            "--base",
            self.base,
            "--tracked",
            "subdir",
            "--output",
            str(output),
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("directory", result.stderr.lower())
        self.assertFalse(output.exists())

    def test_capture_rejects_file_replaced_by_directory_with_staged_descendants(self):
        tracked = self.repository / "tracked.txt"
        self.git("rm", "--cached", "--quiet", "tracked.txt")
        tracked.unlink()
        tracked.mkdir()
        (tracked / "child.txt").write_text("staged descendant\n", encoding="utf-8")
        self.git("add", "tracked.txt/child.txt")
        output = self.temp_root / "file-to-directory-snapshot"

        result = self.capture(output)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("directory", result.stderr.lower())
        self.assertFalse(output.exists())

    def test_capture_does_not_match_untracked_paths_as_globs(self):
        (self.repository / "foo*").write_text("ignored file\n", encoding="utf-8")
        (self.repository / "foo-other").write_text("untracked glob match\n", encoding="utf-8")
        output = self.temp_root / "glob-snapshot"

        result = self.capture(output, "--untracked", "foo*")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("untracked", result.stderr.lower())
        self.assertFalse(output.exists())

    def test_capture_includes_deletions_of_tracked_files(self):
        (self.repository / "tracked.txt").unlink()
        output = self.temp_root / "deletion-snapshot"

        result = self.capture(output)

        self.assertEqual(result.returncode, 0, result.stderr)
        patch = (output / "patch.diff").read_text(encoding="utf-8")
        self.assertIn("deleted file mode", patch)

    def test_capture_refuses_to_overwrite_an_existing_snapshot(self):
        (self.repository / "tracked.txt").write_text("change\n", encoding="utf-8")
        output = self.temp_root / "existing-snapshot"
        output.mkdir()
        sentinel = output / "keep.txt"
        sentinel.write_text("untouched\n", encoding="utf-8")

        result = self.capture(output)

        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(sentinel.read_text(encoding="utf-8"), "untouched\n")

    def test_capture_carries_forward_prior_adjudications(self):
        (self.repository / "tracked.txt").write_text("changed\n", encoding="utf-8")
        previous_record = self.temp_root / "previous-review-record.md"
        previous_record.write_text(
            "# Review adjudication log\n\nF-001 — rejected: no spec evidence.\n",
            encoding="utf-8",
        )

        result = self.capture(
            self.temp_root / "next-snapshot",
            "--prior-record",
            str(previous_record),
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        record = (self.temp_root / "next-snapshot" / "review-record.md").read_text(encoding="utf-8")
        self.assertIn("F-001 — rejected: no spec evidence.", record)
        self.assertIn("## Snapshot ", record)

    def test_capture_and_verify_include_tracked_deletion_and_untracked_replacement(self):
        tracked = self.repository / "tracked.txt"
        self.git("rm", "--cached", "--quiet", "tracked.txt")
        tracked.write_text("replacement content\n", encoding="utf-8")
        output = self.temp_root / "replacement-snapshot"

        captured = self.capture(output, "--untracked", "tracked.txt")

        self.assertEqual(captured.returncode, 0, captured.stderr)
        patch = (output / "patch.diff").read_text(encoding="utf-8")
        self.assertIn("deleted file mode", patch)
        self.assertIn("replacement content", patch)
        verified = self.run_tool("verify", str(output))
        self.assertEqual(verified.returncode, 0, verified.stderr)

        tracked.write_text("changed replacement content\n", encoding="utf-8")
        stale = self.run_tool("verify", str(output))
        self.assertNotEqual(stale.returncode, 0)
        self.assertIn("patch changed", stale.stderr.lower())

    def test_verify_detects_index_blob_change_with_same_status_and_worktree_patch(self):
        tracked = self.repository / "tracked.txt"
        tracked.write_text("first staged content\n", encoding="utf-8")
        self.git("add", "tracked.txt")
        tracked.write_text("unchanged worktree content\n", encoding="utf-8")
        output = self.temp_root / "index-snapshot"
        captured = self.capture(output)
        self.assertEqual(captured.returncode, 0, captured.stderr)

        tracked.write_text("second staged content\n", encoding="utf-8")
        self.git("add", "tracked.txt")
        tracked.write_text("unchanged worktree content\n", encoding="utf-8")

        result = self.run_tool("verify", str(output))

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("index content changed", result.stderr.lower())

    def test_verify_detects_content_change_after_capture(self):
        (self.repository / "tracked.txt").write_text("first change\n", encoding="utf-8")
        output = self.temp_root / "snapshot"
        captured = self.capture(output)
        self.assertEqual(captured.returncode, 0, captured.stderr)

        (self.repository / "tracked.txt").write_text("review changed this file\n", encoding="utf-8")
        result = self.run_tool("verify", str(output))

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("changed", result.stderr.lower())

    def test_verify_detects_staging_state_change_even_when_patch_is_same(self):
        tracked = self.repository / "tracked.txt"
        tracked.write_text("same final content\n", encoding="utf-8")
        output = self.temp_root / "snapshot"
        captured = self.capture(output)
        self.assertEqual(captured.returncode, 0, captured.stderr)

        self.git("add", "tracked.txt")
        result = self.run_tool("verify", str(output))

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("status", result.stderr.lower())

    def test_verify_succeeds_when_selected_patch_and_status_are_unchanged(self):
        (self.repository / "tracked.txt").write_text("reviewed change\n", encoding="utf-8")
        output = self.temp_root / "snapshot"
        captured = self.capture(output)
        self.assertEqual(captured.returncode, 0, captured.stderr)

        result = self.run_tool("verify", str(output))

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("unchanged", result.stdout.lower())

    def test_verify_requires_the_adjudication_record(self):
        (self.repository / "tracked.txt").write_text("reviewed change\n", encoding="utf-8")
        output = self.temp_root / "record-snapshot"
        captured = self.capture(output)
        self.assertEqual(captured.returncode, 0, captured.stderr)
        (output / "review-record.md").unlink()

        result = self.run_tool("verify", str(output))

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("record", result.stderr.lower())


if __name__ == "__main__":
    unittest.main()
