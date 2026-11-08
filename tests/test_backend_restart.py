import os
import socket
import subprocess
import sys
import tempfile
from pathlib import Path


SOURCE_ROOT = Path(__file__).resolve().parents[1]
BACKEND_ROOT = SOURCE_ROOT / "src"
SERVER_PATH = BACKEND_ROOT / "backend" / "tts_server.py"
STATUS_PATH = BACKEND_ROOT / "tts_ready.txt"


def wait_for_backend(port, timeout=8.0):
    import time

    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            return socket.create_connection(("127.0.0.1", port), timeout=0.25)
        except OSError:
            time.sleep(0.05)
    raise RuntimeError("mock backend did not open its socket")


def run_cycle(port, log_dir, speak_first):
    environment = os.environ.copy()
    environment["TTS_ENGINE"] = "mock"
    environment["TTS_LOG_DIR"] = str(log_dir)
    environment["TTS_ROOT"] = str(BACKEND_ROOT)
    process = subprocess.Popen(
        [sys.executable, str(SERVER_PATH), "a", "af_heart", "1.0", "--port", str(port)],
        env=environment,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    try:
        connection = wait_for_backend(port)
        with connection:
            if speak_first:
                connection.sendall(b"This request should be cancelled.\nSTOP\n")
            connection.sendall(b"SHUTDOWN\n")
        exit_code = process.wait(timeout=8.0)
        assert exit_code == 0, "backend cycle exit code was " + str(exit_code)
    finally:
        if process.poll() is None:
            process.kill()
            process.wait(timeout=2.0)


def verify_busy_port_failure_isolated(port, log_dir):
    environment = os.environ.copy()
    environment["TTS_ENGINE"] = "mock"
    environment["TTS_LOG_DIR"] = str(log_dir)
    environment["TTS_ROOT"] = str(BACKEND_ROOT)
    command = [sys.executable, str(SERVER_PATH), "a", "af_heart", "1.0", "--port", str(port)]
    owner = subprocess.Popen(command, env=environment, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
    contender = None
    try:
        try:
            connection = wait_for_backend(port)
        except RuntimeError as exc:
            stderr = owner.stderr.read() if owner.poll() is not None and owner.stderr is not None else ""
            log_path = Path(log_dir) / "tts_server.log"
            log = log_path.read_text(encoding="utf-8", errors="replace") if log_path.exists() else ""
            raise RuntimeError(
                f"mock backend owner failed (exit={owner.poll()}): {stderr.strip()} {log.strip()}"
            ) from exc
        contender = subprocess.Popen(command, env=environment, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        assert contender.wait(timeout=8.0) != 0, "second backend unexpectedly acquired an occupied port"
        assert owner.poll() is None, "failed second backend disturbed the original port owner"
        with connection:
            connection.sendall(b"SHUTDOWN\n")
        assert owner.wait(timeout=8.0) == 0, "original backend did not shut down cleanly"
    finally:
        for process in (contender, owner):
            if process is not None and process.poll() is None:
                process.kill()
                process.wait(timeout=2.0)


def main():
    port_socket = socket.socket()
    port_socket.bind(("127.0.0.1", 0))
    port = port_socket.getsockname()[1]
    port_socket.close()
    original_status = STATUS_PATH.read_bytes() if STATUS_PATH.exists() else None
    try:
        with tempfile.TemporaryDirectory(dir=SOURCE_ROOT / "tests") as temporary:
            log_dir = Path(temporary)
            verify_busy_port_failure_isolated(port, log_dir)
            run_cycle(port, log_dir, True)
            run_cycle(port, log_dir, False)
    finally:
        if original_status is None:
            STATUS_PATH.unlink(missing_ok=True)
        else:
            STATUS_PATH.write_bytes(original_status)
    print("mock backend stop/restart test passed on the same port")


if __name__ == "__main__":
    main()
