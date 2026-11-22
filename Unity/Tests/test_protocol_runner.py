import importlib.util
from pathlib import Path
import socket
import subprocess
import tempfile
import unittest
from unittest import mock
import xml.etree.ElementTree as ET


SCRIPT = Path(__file__).resolve().parent / "run_protocol_playmode_tests.py"
SPEC = importlib.util.spec_from_file_location("protocol_runner", SCRIPT)
runner = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(runner)
CHECK_REALM_PORT = runner.check_realm_port


class ProtocolRunnerTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.results = Path(self.directory.name) / "results"
        self.editor = Path(self.directory.name) / "Unity"
        self.editor.touch()
        self.patches = [
            mock.patch.object(runner, "RESULTS", self.results),
            mock.patch.object(runner, "project_version", return_value="6000.3.6f1"),
            mock.patch.object(runner, "check_realm_port"),
            mock.patch("sys.argv", ["runner", "--editor", str(self.editor), "--timeout", "1"]),
        ]
        for patch in self.patches:
            patch.start()
            self.addCleanup(patch.stop)

    def write_report(self, missing=None, skipped=None):
        count = str(len(runner.REQUIRED_TESTS) - (missing is not None))
        root = ET.Element("test-run", result="Passed", total=count, passed=count, failed="0", skipped="0", inconclusive="0")
        for name in sorted(runner.REQUIRED_TESTS - ({missing} if missing else set())):
            ET.SubElement(root, "test-case", fullname=name, result="Skipped" if name == skipped else "Passed")
        ET.ElementTree(root).write(self.results / "results.xml")

    def test_success_requires_valid_report_and_log(self):
        def run(command, **kwargs):
            if "-version" in command:
                return subprocess.CompletedProcess(command, 0, stdout="6000.3.6f1\n")
            self.write_report()
            (self.results / "editor.log").touch()
            return subprocess.CompletedProcess(command, 0)

        with mock.patch.object(runner.subprocess, "run", side_effect=run):
            runner.main()

    def test_missing_or_skipped_tests_are_not_success(self):
        self.results.mkdir()
        name = next(iter(runner.REQUIRED_TESTS))
        for missing, skipped in ((name, None), (None, name)):
            self.write_report(missing, skipped)
            with self.assertRaises(ValueError):
                runner.validate_report(self.results / "results.xml")

    def test_version_failure_removes_stale_report(self):
        self.results.mkdir()
        self.write_report()
        version = subprocess.CompletedProcess([], 0, stdout="wrong-version")
        with mock.patch.object(runner.subprocess, "run", return_value=version):
            with self.assertRaisesRegex(RuntimeError, "version mismatch"):
                runner.main()
        self.assertFalse((self.results / "results.xml").exists())

    def test_invalid_arguments_remove_stale_report(self):
        for arguments in (["runner"], ["runner", "--editor", str(self.editor), "--timeout", "0"]):
            with self.subTest(arguments=arguments):
                self.results.mkdir(exist_ok=True)
                self.write_report()
                with mock.patch("sys.argv", arguments), self.assertRaises(SystemExit):
                    runner.main()
                self.assertFalse((self.results / "results.xml").exists())

    def test_missing_report_nonzero_exit_and_timeout_fail(self):
        version = subprocess.CompletedProcess([], 0, stdout="6000.3.6f1")
        for result, exception in (
            (subprocess.CompletedProcess([], 0), FileNotFoundError),
            (subprocess.CompletedProcess([], 2), RuntimeError),
            (subprocess.TimeoutExpired("Unity", 1), subprocess.TimeoutExpired),
        ):
            with self.subTest(result=result), mock.patch.object(runner.subprocess, "run", side_effect=[version, result]):
                with self.assertRaises(exception):
                    runner.main()

    def test_concurrent_runner_cannot_acquire_lock(self):
        self.results.mkdir()
        with runner.exclusive_run():
            with self.assertRaises(OSError):
                with runner.exclusive_run():
                    self.fail("A second runner acquired the lock")

    def test_occupied_port_fails_without_closing_owner_or_using_stale_report(self):
        self.results.mkdir()
        self.write_report()
        version = subprocess.CompletedProcess([], 0, stdout="6000.3.6f1")
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as owner:
            owner.bind(("127.0.0.1", 30300))
            with mock.patch.object(runner.subprocess, "run", return_value=version), \
                    mock.patch.object(runner, "check_realm_port", side_effect=CHECK_REALM_PORT):
                with self.assertRaisesRegex(RuntimeError, "port .* unavailable"):
                    runner.main()
            self.assertEqual(owner.getsockname()[1], 30300)
        self.assertFalse((self.results / "results.xml").exists())


if __name__ == "__main__":
    unittest.main()
