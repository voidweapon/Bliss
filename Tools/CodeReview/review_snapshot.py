#!/usr/bin/env python3
"""Capture and verify immutable worktree review snapshots."""

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import subprocess
import sys
import tempfile


MANIFEST_VERSION = 1
PATCH_FILE_NAME = "patch.diff"
MANIFEST_FILE_NAME = "manifest.json"
RECORD_FILE_NAME = "review-record.md"


class SnapshotError(Exception):
    """A review snapshot cannot be created or verified safely."""


def run_git(repository, arguments, allowed_return_codes=(0,)):
    result = subprocess.run(
        ["git", *arguments],
        cwd=repository,
        capture_output=True,
        check=False,
    )
    if result.returncode not in allowed_return_codes:
        detail = result.stderr.decode("utf-8", errors="replace").strip()
        raise SnapshotError(detail or f"git {' '.join(arguments)} failed with {result.returncode}")
    return result


def find_repository(start_directory):
    result = run_git(start_directory, ["rev-parse", "--show-toplevel"])
    return Path(os.fsdecode(result.stdout.strip())).resolve()


def resolve_commit(repository, revision):
    result = run_git(repository, ["rev-parse", "--verify", f"{revision}^{{commit}}"])
    return result.stdout.decode("ascii").strip()


def normalize_paths(paths, label):
    normalized = []
    for path in paths:
        candidate = PurePosixPath(path)
        if (
            not path
            or candidate.is_absolute()
            or "\\" in path
            or any(part in ("", ".", "..") for part in path.split("/"))
        ):
            raise SnapshotError(f"{label} path must be a normalized repository-relative file path: {path!r}")
        normalized.append(candidate.as_posix())
    if len(set(normalized)) != len(normalized):
        raise SnapshotError(f"{label} paths must not contain duplicates.")
    return sorted(normalized)


def validate_scope(repository, base_commit, tracked_paths, untracked_paths):
    if not tracked_paths and not untracked_paths:
        raise SnapshotError("Select at least one tracked or untracked file.")

    overlap = set(tracked_paths).intersection(untracked_paths)

    for path in tracked_paths:
        base_object = run_git(
            repository,
            ["cat-file", "-t", f"{base_commit}:{path}"],
            allowed_return_codes=(0, 1, 128),
        )
        index_entries = run_git(
            repository,
            ["--literal-pathspecs", "ls-files", "--stage", "-z", "--", path],
        ).stdout
        index_paths = {
            entry.split(b"\t", 1)[1]
            for entry in index_entries.split(b"\0")
            if b"\t" in entry
        }
        file_path = repository.joinpath(*PurePosixPath(path).parts)
        if file_path.is_dir() and not file_path.is_symlink():
            raise SnapshotError(
                f"Selected tracked path currently names a directory, not a file: {path}"
            )
        encoded_path = os.fsencode(path)
        if any(index_path.startswith(encoded_path + b"/") for index_path in index_paths):
            raise SnapshotError(
                f"Selected tracked path has indexed descendants and is not an exact file: {path}"
            )
        tracked_at_base = base_object.returncode == 0 and base_object.stdout.strip() == b"blob"
        tracked_in_index = os.fsencode(path) in index_paths
        if base_object.returncode == 0 and base_object.stdout.strip() == b"tree":
            raise SnapshotError(f"Selected tracked path must name a file, not a directory: {path}")
        if not tracked_at_base and not tracked_in_index:
            raise SnapshotError(
                f"Selected tracked path is not tracked at the base or in the index: {path}"
            )

    for path in overlap:
        base_object = run_git(
            repository,
            ["cat-file", "-t", f"{base_commit}:{path}"],
            allowed_return_codes=(0, 1, 128),
        )
        if base_object.returncode != 0 or base_object.stdout.strip() != b"blob":
            raise SnapshotError(
                f"A path may be selected as both tracked and untracked only for a tracked file replacement: {path}"
            )

    for path in untracked_paths:
        file_path = repository.joinpath(*PurePosixPath(path).parts)
        if not file_path.is_file() and not file_path.is_symlink():
            raise SnapshotError(f"Selected untracked path is not a file: {path}")
        status = run_git(
            repository,
            [
                "--literal-pathspecs",
                "status",
                "--porcelain=v1",
                "-z",
                "--untracked-files=all",
                "--",
                path,
            ],
        ).stdout
        exact_untracked_entry = b"?? " + os.fsencode(path)
        if exact_untracked_entry not in status.split(b"\0"):
            raise SnapshotError(
                f"Selected untracked path is not reported as untracked (it may be ignored): {path}"
            )


def build_patch(repository, base_commit, tracked_paths, untracked_paths):
    patch = b""
    if tracked_paths:
        patch = run_git(
            repository,
            [
                "--literal-pathspecs",
                "diff",
                "--binary",
                "--full-index",
                "--no-ext-diff",
                base_commit,
                "--",
                *tracked_paths,
            ],
        ).stdout

    for path in untracked_paths:
        result = run_git(
            repository,
            [
                "--literal-pathspecs",
                "diff",
                "--no-index",
                "--binary",
                "--full-index",
                "--no-ext-diff",
                "--",
                "/dev/null",
                path,
            ],
            allowed_return_codes=(0, 1),
        )
        if patch and not patch.endswith(b"\n"):
            patch += b"\n"
        patch += result.stdout
    return patch


def selected_status(repository, tracked_paths, untracked_paths):
    paths = sorted(set(tracked_paths + untracked_paths))
    if not paths:
        return b""
    return run_git(
        repository,
        [
            "--literal-pathspecs",
            "status",
            "--porcelain=v1",
            "--untracked-files=all",
            "--",
            *paths,
        ],
    ).stdout


def selected_index_entries(repository, tracked_paths):
    if not tracked_paths:
        return b""
    return run_git(
        repository,
        ["--literal-pathspecs", "ls-files", "--stage", "-z", "--", *tracked_paths],
    ).stdout


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def review_record(snapshot_hash, manifest, prior_record=None):
    if prior_record is None:
        content = (
            "# Review adjudication log\n\n"
            "Keep this file with the review artifacts and pass it as `--prior-record` "
            "for the next snapshot. Reuse stable finding IDs when a finding recurs. "
            "Record dispositions with the cited rule/spec, evidence, and state; "
            "do not reopen a ruled finding without new evidence.\n"
        )
    else:
        content = prior_record.rstrip() + "\n"

    scope = manifest["tracked_paths"] + manifest["untracked_paths"]
    content += (
        f"\n## Snapshot {snapshot_hash[:12]}\n\n"
        f"- Base commit: `{manifest['base_commit']}`\n"
        f"- Captured HEAD: `{manifest['captured_head']}`\n"
        f"- Patch SHA-256: `{manifest['patch']['sha256']}`\n"
        f"- Status SHA-256: `{manifest['selected_status']['sha256']}`\n"
        f"- Scope: {', '.join(f'`{path}`' for path in scope)}\n\n"
        "### Findings and dispositions\n\n"
        "For each finding record a stable ID, Standards/Spec axis, file and lines, "
        "quoted rule or requirement, concise finding, ruling (`valid`, `duplicate`, "
        "`not an issue`, or `deferred`), evidence/rationale, and current state "
        "(`open`, `fixed`, `accepted`, or `deferred`). Link a recurring finding to "
        "its earlier ID and explain any changed ruling.\n"
    )
    return content


def capture(arguments):
    invocation_directory = Path.cwd().resolve()
    repository = find_repository(invocation_directory)
    base_commit = resolve_commit(repository, arguments.base)
    captured_head = resolve_commit(repository, "HEAD")
    tracked_paths = normalize_paths(arguments.tracked, "Tracked")
    untracked_paths = normalize_paths(arguments.untracked, "Untracked")
    validate_scope(repository, base_commit, tracked_paths, untracked_paths)

    patch = build_patch(repository, base_commit, tracked_paths, untracked_paths)
    if not patch:
        raise SnapshotError("Selected patch is empty; no review snapshot was created.")

    status = selected_status(repository, tracked_paths, untracked_paths)
    index_entries = selected_index_entries(repository, tracked_paths)
    prior_record = None
    if arguments.prior_record:
        prior_path = Path(arguments.prior_record).expanduser().resolve()
        if not prior_path.is_file():
            raise SnapshotError(f"Prior review record does not exist: {prior_path}")
        prior_record = prior_path.read_text(encoding="utf-8")
        if not prior_record.startswith("# Review adjudication log"):
            raise SnapshotError("Prior review record must begin with '# Review adjudication log'.")

    manifest = {
        "version": MANIFEST_VERSION,
        "created_at_utc": datetime.now(timezone.utc).isoformat(),
        "repository": str(repository),
        "base_commit": base_commit,
        "captured_head": captured_head,
        "tracked_paths": tracked_paths,
        "untracked_paths": untracked_paths,
        "patch": {
            "file": PATCH_FILE_NAME,
            "sha256": sha256(patch),
            "bytes": len(patch),
        },
        "selected_status": {
            "sha256": sha256(status),
            "text": status.decode("utf-8", errors="replace"),
        },
        "selected_index": {
            "sha256": sha256(index_entries),
        },
    }

    output = Path(arguments.output).expanduser().absolute()
    if output.exists() or output.is_symlink():
        raise SnapshotError(f"Output already exists; refusing to overwrite: {output}")
    output.parent.mkdir(parents=True, exist_ok=True)

    try:
        with tempfile.TemporaryDirectory(prefix=".review-snapshot-", dir=output.parent) as temp_directory:
            staging = Path(temp_directory) / "snapshot"
            staging.mkdir()
            (staging / PATCH_FILE_NAME).write_bytes(patch)
            (staging / MANIFEST_FILE_NAME).write_text(
                json.dumps(manifest, indent=2, ensure_ascii=False) + "\n",
                encoding="utf-8",
            )
            (staging / RECORD_FILE_NAME).write_text(
                review_record(manifest["patch"]["sha256"], manifest, prior_record),
                encoding="utf-8",
            )
            os.replace(staging, output)
    except OSError as error:
        raise SnapshotError(f"Could not write snapshot to {output}: {error}") from error

    print(f"Snapshot created: {output}")
    print(f"Base commit: {base_commit}")
    print(f"Selected patch SHA-256: {manifest['patch']['sha256']}")
    print(f"Selected worktree status SHA-256: {manifest['selected_status']['sha256']}")
    print(f"Scope: {', '.join(tracked_paths + untracked_paths)}")


def verify(arguments):
    snapshot = Path(arguments.snapshot).expanduser().resolve()
    manifest_path = snapshot / MANIFEST_FILE_NAME
    patch_path = snapshot / PATCH_FILE_NAME
    if not manifest_path.is_file() or not patch_path.is_file():
        raise SnapshotError(f"Snapshot must contain {MANIFEST_FILE_NAME} and {PATCH_FILE_NAME}.")

    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise SnapshotError(f"Could not read snapshot manifest: {error}") from error

    if not isinstance(manifest, dict):
        raise SnapshotError("Snapshot manifest must contain a JSON object.")
    if manifest.get("version") != MANIFEST_VERSION:
        raise SnapshotError(f"Unsupported snapshot manifest version: {manifest.get('version')!r}")

    try:
        expected_patch_hash = manifest["patch"]["sha256"]
        captured_head = manifest["captured_head"]
        base_commit = manifest["base_commit"]
        tracked_paths = normalize_paths(manifest["tracked_paths"], "Tracked")
        untracked_paths = normalize_paths(manifest["untracked_paths"], "Untracked")
        expected_status_hash = manifest["selected_status"]["sha256"]
        expected_index_hash = manifest["selected_index"]["sha256"]
    except (KeyError, TypeError) as error:
        raise SnapshotError(f"Snapshot manifest is missing required fields: {error}") from error

    try:
        stored_patch = patch_path.read_bytes()
    except OSError as error:
        raise SnapshotError(f"Could not read snapshot patch: {error}") from error
    if sha256(stored_patch) != expected_patch_hash:
        raise SnapshotError("Snapshot patch artifact has changed since capture.")
    record_path = snapshot / RECORD_FILE_NAME
    if not record_path.is_file():
        raise SnapshotError(f"Snapshot is missing its adjudication record: {RECORD_FILE_NAME}")
    try:
        record_content = record_path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError) as error:
        raise SnapshotError(f"Could not read adjudication record: {error}") from error
    if not record_content.startswith("# Review adjudication log"):
        raise SnapshotError("Adjudication record is empty or has an invalid header.")

    repository = find_repository(Path.cwd().resolve())
    current_head = resolve_commit(repository, "HEAD")
    differences = []
    if current_head != captured_head:
        differences.append("HEAD commit changed")

    validate_scope(repository, base_commit, tracked_paths, untracked_paths)
    current_status = selected_status(repository, tracked_paths, untracked_paths)
    if sha256(current_status) != expected_status_hash:
        differences.append("selected worktree status changed")
    current_index_entries = selected_index_entries(repository, tracked_paths)
    if sha256(current_index_entries) != expected_index_hash:
        differences.append("selected index content changed")

    current_patch = build_patch(
        repository,
        base_commit,
        tracked_paths,
        untracked_paths,
    )
    if sha256(current_patch) != expected_patch_hash:
        differences.append("selected patch changed")

    if differences:
        raise SnapshotError("Snapshot is stale: " + "; ".join(differences) + ".")
    print(f"Snapshot unchanged: {expected_patch_hash}")


def make_parser():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)

    capture_parser = commands.add_parser("capture", help="capture a selected review patch and worktree state")
    capture_parser.add_argument("--base", required=True, help="fixed commit/ref for the review diff")
    capture_parser.add_argument("--tracked", action="append", default=[], metavar="PATH")
    capture_parser.add_argument("--untracked", action="append", default=[], metavar="PATH")
    capture_parser.add_argument("--output", required=True, help="new directory for patch, manifest, and review record")
    capture_parser.add_argument("--prior-record", help="previous review-record.md to carry forward")
    capture_parser.set_defaults(handler=capture)

    verify_parser = commands.add_parser("verify", help="verify the worktree still matches a captured snapshot")
    verify_parser.add_argument("snapshot", help="snapshot directory created by capture")
    verify_parser.set_defaults(handler=verify)
    return parser


def main(argv=None):
    parser = make_parser()
    arguments = parser.parse_args(argv)
    try:
        arguments.handler(arguments)
    except SnapshotError as error:
        print(f"review snapshot: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
