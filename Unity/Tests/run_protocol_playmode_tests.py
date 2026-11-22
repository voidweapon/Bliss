#!/usr/bin/env python3
"""Run the isolated client protocol PlayMode tests with a specified Unity Editor."""

import argparse
import contextlib
import os
from pathlib import Path
import socket
import subprocess
import sys
import xml.etree.ElementTree as ET


PROJECT = Path(__file__).resolve().parent.parent
RESULTS = PROJECT / "TestResults" / "ProtocolPlayMode"
REQUIRED_TESTS = {
    "ProtocolLoginTests.LoginSucceedsThroughRealmAndGate",
    "ProtocolLoginTests.GateRejectionDoesNotLogIn",
    "ProtocolLoginTests.ExistingWorldIsPreserved",
    "ProtocolLoginTests.OccupiedRealmPortFailsWithoutClosingOwner",
    "ProtocolLoginTests.SuspendedRunCanBeCleanedUp",
    "ProtocolLoginTests.DestroyingCleanupObjectReleasesSuspendedRun",
}


@contextlib.contextmanager
def exclusive_run():
    lock_path = RESULTS / "run.lock"
    with lock_path.open("a+b") as lock:
        lock.seek(0, os.SEEK_END)
        if lock.tell() == 0:
            lock.write(b"0")
            lock.flush()
        lock.seek(0)
        if os.name == "nt":
            import msvcrt

            msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        else:
            import fcntl

            fcntl.flock(lock.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        try:
            yield
        finally:
            if os.name == "nt":
                lock.seek(0)
                msvcrt.locking(lock.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(lock.fileno(), fcntl.LOCK_UN)


def project_version():
    for line in (PROJECT / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8").splitlines():
        if line.startswith("m_EditorVersion:"):
            return line.split(":", 1)[1].strip()
    raise ValueError("ProjectVersion.txt does not contain m_EditorVersion")


def check_realm_port():
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as probe:
        if hasattr(socket, "SO_EXCLUSIVEADDRUSE"):
            probe.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
        try:
            probe.bind(("127.0.0.1", 30300))
        except OSError as error:
            raise RuntimeError(f"Realm UDP port 127.0.0.1:30300 is unavailable: {error}") from error


def validate_report(path):
    root = ET.parse(path).getroot()
    if root.tag != "test-run" or root.get("result") != "Passed":
        raise ValueError("NUnit report does not contain a passed test run")
    cases = root.findall(".//test-case")
    if (len(cases) < len(REQUIRED_TESTS)
            or int(root.get("total", "0")) != len(cases)
            or int(root.get("passed", "0")) != len(cases)
            or any(int(root.get(key, "0")) != 0 for key in ("failed", "skipped", "inconclusive"))):
        raise ValueError("NUnit report has an incomplete or inconsistent test count")
    by_name = {case.get("fullname"): case for case in cases}
    if len(by_name) != len(cases) or not REQUIRED_TESTS.issubset(by_name):
        raise ValueError("NUnit report is missing required protocol tests or contains duplicate names")
    if any(case.get("result") != "Passed" for case in cases):
        raise ValueError("NUnit report contains a failed or skipped test")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--editor", required=True, help="Path to the Unity Editor executable")
    parser.add_argument("--timeout", type=int, default=300, help="Test timeout in seconds (default: 300)")
    if "-h" in sys.argv[1:] or "--help" in sys.argv[1:]:
        parser.parse_args()
    RESULTS.mkdir(parents=True, exist_ok=True)
    with exclusive_run():
        report = RESULTS / "results.xml"
        log = RESULTS / "editor.log"
        report.unlink(missing_ok=True)
        log.unlink(missing_ok=True)
        args = parser.parse_args()
        if args.timeout <= 0:
            parser.error("--timeout must be positive")
        editor = Path(args.editor).expanduser().resolve(strict=True)
        expected = project_version()
        version = subprocess.run([str(editor), "-version"], capture_output=True, text=True, timeout=30, check=True)
        if version.stdout.strip() != expected:
            raise RuntimeError(f"Editor version mismatch: expected {expected}, got {version.stdout.strip()!r}")
        check_realm_port()
        command = [
            str(editor), "-batchmode", "-projectPath", str(PROJECT),
            "-runTests", "-testPlatform", "PlayMode", "-testFilter", "ProtocolLoginTests",
            "-testResults", str(report), "-logFile", str(log), "--burst-disable-compilation",
        ]
        result = subprocess.run(command, timeout=args.timeout)
        if result.returncode != 0:
            raise RuntimeError(f"Unity exited with code {result.returncode}; see {log}")
        validate_report(report)
        if not log.is_file():
            raise RuntimeError(f"Unity did not create its Editor log: {log}")
        print(f"Protocol PlayMode tests passed. Report: {report}; log: {log}")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, RuntimeError, ET.ParseError, subprocess.SubprocessError) as error:
        print(f"Protocol PlayMode tests failed: {error}", file=sys.stderr)
        sys.exit(1)
