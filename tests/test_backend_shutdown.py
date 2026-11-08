import argparse
import os
import socket
import subprocess
import sys
import tempfile
import time
from pathlib import Path


SOURCE_ROOT = Path(__file__).resolve().parents[1]
BACKEND_ROOT = SOURCE_ROOT / "src"
SERVER_PATH = BACKEND_ROOT / "backend" / "tts_server.py"
STATUS_PATH = BACKEND_ROOT / "tts_ready.txt"


def wait_for_backend(port, timeout=8.0):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            return socket.create_connection(("127.0.0.1", port), timeout=0.25)
        except OSError:
            time.sleep(0.05)
    raise RuntimeError("mock backend did not open its socket")


def stop_owned_process(process):
    if process is None or process.poll() is not None:
        return
    process.terminate()
    try:
        process.wait(timeout=2.0)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=2.0)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--engine", choices=("mock", "kokoro"), default="mock")
    args = parser.parse_args()
    original_status = STATUS_PATH.read_bytes() if STATUS_PATH.exists() else None
    sentinel = None
    backend = None
    with tempfile.TemporaryDirectory(dir=SOURCE_ROOT / "tests") as log_dir:
        port_socket = socket.socket()
        port_socket.bind(("127.0.0.1", 0))
        port = port_socket.getsockname()[1]
        port_socket.close()

        environment = os.environ.copy()
        environment["TTS_ENGINE"] = args.engine
        environment["TTS_LOG_DIR"] = log_dir
        environment["TTS_ROOT"] = str(BACKEND_ROOT)
        environment["HF_HOME"] = str(BACKEND_ROOT / "model-cache")
        environment["HF_HUB_OFFLINE"] = "1"
        environment["TRANSFORMERS_OFFLINE"] = "1"

        try:
            sentinel = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(30)"])
            backend = subprocess.Popen(
                [sys.executable, str(SERVER_PATH), "a", "af_heart", "1.0", "--port", str(port)],
                env=environment,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )
            connection = wait_for_backend(port, timeout=90.0 if args.engine == "kokoro" else 8.0)
            with connection:
                connection.sendall(b"SHUTDOWN\n")

            exit_code = backend.wait(timeout=10.0)
            assert exit_code == 0, "backend shutdown exit code was " + str(exit_code)
            assert sentinel.poll() is None, "unrelated Python sentinel was terminated"
        finally:
            stop_owned_process(backend)
            stop_owned_process(sentinel)
            if original_status is None:
                STATUS_PATH.unlink(missing_ok=True)
            else:
                STATUS_PATH.write_bytes(original_status)

    print(args.engine + " owned backend shutdown test passed; unrelated Python process survived")


if __name__ == "__main__":
    main()
