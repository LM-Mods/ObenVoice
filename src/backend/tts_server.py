"""Local socket service for Obenseuer Dialogue TTS.

Independently implemented specifically for Obenseuer Dialogue TTS. This file
is original project code and is not derived from Universal TTS Mod.
"""

import argparse
import logging
from logging.handlers import RotatingFileHandler
import os
import queue
import secrets
import signal
import socket
import sys
import threading
from dataclasses import dataclass
from pathlib import Path

BACKEND_DIR = Path(__file__).resolve().parent
if str(BACKEND_DIR) not in sys.path:
    sys.path.insert(0, str(BACKEND_DIR))

from text_cleaner import TextCleaner
from tts_engines import DEFAULT_VOICE, get_available_voices, get_tts_engine
from tts_protocol import SET_DEBUG, SET_LANG, SET_PITCH, SET_SPEED, SET_VOICE, SHUTDOWN, STOP, parse_message


ROOT = BACKEND_DIR.parent
STATUS_PATH = ROOT / "tts_ready.txt"
LOG_PATH = Path(os.environ.get("TTS_LOG_DIR", str(ROOT))) / "tts_server.log"


@dataclass(frozen=True)
class SpeechRequest:
    text: str
    voice: str
    speed: float
    pitch: float


class Backend:
    def __init__(self, engine, voice, speed):
        self.engine = engine
        self.voice = voice
        self.speed = speed
        self.pitch = 0.0
        self.lang = "a"
        self.debug = os.environ.get("TTS_DEBUG", "0") == "1"
        self.pending = queue.Queue(maxsize=16)
        self.lock = threading.Lock()
        self.stopping = threading.Event()
        self.worker = threading.Thread(target=self._speech_loop, name="obenseuer-tts-worker", daemon=True)

    def start(self):
        self.worker.start()

    def _speech_loop(self):
        while not self.stopping.is_set():
            try:
                request = self.pending.get(timeout=0.25)
            except queue.Empty:
                continue
            try:
                self.engine.speak(request.text, request.voice, request.speed, request.pitch)
            except Exception:
                logging.exception("Speech request failed")
            finally:
                self.pending.task_done()

    def _empty_pending(self):
        while True:
            try:
                self.pending.get_nowait()
                self.pending.task_done()
            except queue.Empty:
                return

    def stop_speech(self):
        self._empty_pending()
        self.engine.stop()

    @staticmethod
    def _number(value, low, high):
        parsed = float(value)
        if not low <= parsed <= high:
            raise ValueError("value outside supported range")
        return parsed

    def accept(self, raw_line):
        message = parse_message(raw_line)
        if message is None:
            return
        if message.kind == STOP:
            self.stop_speech()
            return
        with self.lock:
            try:
                if message.kind == SET_VOICE:
                    if message.value:
                        self.voice = message.value
                    return
                if message.kind == SET_SPEED:
                    self.speed = self._number(message.value, 0.25, 4.0)
                    return
                if message.kind == SET_PITCH:
                    self.pitch = self._number(message.value, -12.0, 12.0)
                    return
                if message.kind == SET_DEBUG:
                    self.debug = message.value in ("1", "true", "True")
                    return
                if message.kind == SET_LANG:
                    self.lang = message.value or self.lang
                    return
            except (TypeError, ValueError):
                logging.warning("Ignored invalid %s value", message.kind)
                return
            text = TextCleaner.clean(message.value)
            if not text:
                return
            request = SpeechRequest(text, self.voice, self.speed, self.pitch)
        try:
            self.pending.put_nowait(request)
        except queue.Full:
            try:
                self.pending.get_nowait()
                self.pending.task_done()
            except queue.Empty:
                pass
            self.pending.put_nowait(request)
        if self.debug:
            logging.info("Queued dialogue: voice=%s speed=%.3f pitch=%.2f", request.voice, request.speed, request.pitch)

    def close(self):
        self.stopping.set()
        self.stop_speech()
        self.worker.join(timeout=2.0)
        self.engine.close()


def _arguments():
    parser = argparse.ArgumentParser(description="Obenseuer Dialogue TTS local backend")
    parser.add_argument("lang", nargs="?", default="a")
    parser.add_argument("voice", nargs="?", default=DEFAULT_VOICE)
    parser.add_argument("speed", nargs="?", type=float, default=1.0)
    parser.add_argument("--port", type=int, default=int(os.environ.get("TTS_PORT", "5050")))
    return parser.parse_args()


def _write_status(value):
    STATUS_PATH.write_text(value, encoding="utf-8")


def _serve_client(connection, backend, shutdown):
    with connection:
        reader = connection.makefile("r", encoding="utf-8", newline="\n")
        with reader:
            for line in reader:
                message = parse_message(line)
                if message is not None and message.kind == SHUTDOWN:
                    logging.info("Shutdown requested by the plugin client")
                    shutdown.set()
                    return
                backend.accept(line)


def main():
    args = _arguments()
    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    file_handler = RotatingFileHandler(LOG_PATH, maxBytes=2 * 1024 * 1024, backupCount=1, encoding="utf-8")
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
        handlers=(file_handler, logging.StreamHandler(sys.stdout)),
    )
    shutdown = threading.Event()

    def request_shutdown(_signum=None, _frame=None):
        shutdown.set()

    for signal_name in ("SIGINT", "SIGTERM", "SIGBREAK"):
        if hasattr(signal, signal_name):
            signal.signal(getattr(signal, signal_name), request_shutdown)

    server = None
    backend = None
    try:
        engine_name = os.environ.get("TTS_ENGINE", "kokoro").strip().lower()
        _write_status("LOADING|" + engine_name)
        engine = get_tts_engine(args.lang, engine_name)
        running_engine = getattr(engine, "engine_id", engine_name)
        backend = Backend(engine, args.voice or DEFAULT_VOICE, args.speed)
        backend.start()
        server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        if hasattr(socket, "SO_EXCLUSIVEADDRUSE"):
            server.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
        server.bind(("127.0.0.1", args.port))
        server.listen(4)
        server.settimeout(0.5)
        token = secrets.token_hex(8)
        available_voices = getattr(engine, "available_voices", None)
        if available_voices is None:
            available_voices = get_available_voices() if running_engine == "kokoro" else []
        _write_status("READY|" + token + "|" + running_engine + "|" + ",".join(available_voices))
        logging.info("Obenseuer Dialogue TTS ready on 127.0.0.1:%d using %s", args.port, running_engine)
        while not shutdown.is_set():
            try:
                connection, _address = server.accept()
            except socket.timeout:
                continue
            thread = threading.Thread(target=_serve_client, args=(connection, backend, shutdown), daemon=True)
            thread.start()
        return 0
    except Exception as exc:
        logging.exception("Backend startup failed")
        _write_status("ERROR|" + str(exc).replace("\r", " ").replace("\n", " "))
        return 1
    finally:
        logging.info("TTS backend shutdown cleanup started")
        if server is not None:
            server.close()
        if backend is not None:
            backend.close()


if __name__ == "__main__":
    raise SystemExit(main())

