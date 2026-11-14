"""Offline speech engine for Obenseuer Dialogue TTS.

Independently implemented for Obenseuer Dialogue TTS using Kokoro's documented
Python API. This file is original project code and is not derived from
Universal TTS Mod.
"""

import os
import sys
import logging
import json
import importlib.util
import re
import threading
import time
from pathlib import Path


SAMPLE_RATE = 24000
DEFAULT_VOICE = "af_heart"
AVAILABLE_VOICES = (
    "af_heart",
    "af_bella",
    "af_sarah",
    "am_adam",
    "am_michael",
    "am_onyx",
)
VOICE_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]{0,63}$")
PACK_ID_PATTERN = re.compile(r"^[a-z0-9][a-z0-9_-]{0,63}$")


def _voice_pack_root():
    """Legacy voice-pack root retained for existing installations and tests."""
    tts_root = Path(os.environ.get("TTS_ROOT", Path(__file__).resolve().parent.parent)).resolve()
    return (tts_root / "voice-packs").resolve()


def _voice_pack_roots(engine_id):
    tts_root = Path(os.environ.get("TTS_ROOT", Path(__file__).resolve().parent.parent)).resolve()
    obenvoice_root = Path(os.environ.get("OBENVOICE_ROOT", tts_root.parent / "ObenVoice")).resolve()
    legacy_root = Path(os.environ.get("OBENVOICE_LEGACY_ROOT", tts_root)).resolve()
    primary = (obenvoice_root / "VoicePacks").resolve()
    legacy = (legacy_root / "voice-packs" / engine_id).resolve()
    roots = []
    for root in (primary, legacy):
        if root not in roots:
            roots.append(root)
    return roots


def _player_voice_root():
    tts_root = Path(os.environ.get("TTS_ROOT", Path(__file__).resolve().parent.parent)).resolve()
    obenvoice_root = Path(os.environ.get("OBENVOICE_ROOT", tts_root.parent / "ObenVoice")).resolve()
    return (obenvoice_root / "PlayerVoices").resolve()


def discover_external_voices(engine_name="kokoro"):
    """Return validated external voices for one engine, keyed by voice id."""
    engine_id = (engine_name or "").strip().lower()
    if not PACK_ID_PATTERN.fullmatch(engine_id):
        return {}
    discovered = {}
    seen_pack_ids = set()
    for engine_root in _voice_pack_roots(engine_id):
        if not engine_root.is_dir():
            continue
        for pack_root in sorted(
            (path for path in engine_root.iterdir() if path.is_dir()),
            key=lambda path: (path.name.lower(), path.name),
        ):
            manifest_path = pack_root / "manifest.json"
            if not manifest_path.is_file():
                continue
            try:
                manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
                pack_id = str(manifest.get("id", "")).strip().lower()
                manifest_engine = str(manifest.get("engine", "")).strip().lower()
                if manifest.get("manifestVersion") != 1 or not PACK_ID_PATTERN.fullmatch(pack_id) or manifest_engine != engine_id:
                    raise ValueError("manifest version, id, or engine is invalid")
                if pack_id in seen_pack_ids:
                    logging.warning("Ignoring duplicate Kokoro voice-pack id: %s", pack_id)
                    continue
                seen_pack_ids.add(pack_id)
                voices = manifest.get("voices", [])
                if not isinstance(voices, list):
                    raise ValueError("voices must be a list")
                for item in voices:
                    if not isinstance(item, dict):
                        continue
                    voice_id = str(item.get("id", "")).strip().lower()
                    gender = str(item.get("gender", "")).strip().lower()
                    relative_file = str(item.get("file", "")).strip()
                    if not VOICE_ID_PATTERN.fullmatch(voice_id) or gender not in ("male", "female"):
                        continue
                    voice_path = (pack_root / relative_file).resolve()
                    if pack_root.resolve() not in voice_path.parents or voice_path.suffix.lower() != ".pt" or not voice_path.is_file():
                        continue
                    if voice_id in AVAILABLE_VOICES or voice_id in discovered:
                        logging.warning("Ignoring duplicate external Kokoro voice id: %s", voice_id)
                        continue
                    discovered[voice_id] = {
                        "path": voice_path,
                        "gender": gender,
                        "display_name": str(item.get("displayName", voice_id)).strip() or voice_id,
                        "pack_id": pack_id,
                        "pack_name": str(manifest.get("name", pack_id)).strip() or pack_id,
                    }
            except Exception as exc:
                logging.warning("Ignoring invalid voice-pack manifest %s: %s", manifest_path, exc)
    return discovered


def discover_player_voices(engine_name="kokoro"):
    """Return validated Core player-only voices, keyed by voice id."""
    engine_id = (engine_name or "").strip().lower()
    if not PACK_ID_PATTERN.fullmatch(engine_id):
        return {}
    root = _player_voice_root()
    manifest_path = root / "manifest.json"
    if not manifest_path.is_file():
        return {}
    discovered = {}
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        pack_id = str(manifest.get("id", "")).strip().lower()
        manifest_engine = str(manifest.get("engine", "")).strip().lower()
        role = str(manifest.get("role", "")).strip().lower()
        if (
            manifest.get("manifestVersion") != 1
            or not PACK_ID_PATTERN.fullmatch(pack_id)
            or manifest_engine != engine_id
            or role != "player"
        ):
            raise ValueError("manifest version, id, engine, or role is invalid")
        voices = manifest.get("voices", [])
        if not isinstance(voices, list):
            raise ValueError("voices must be a list")
        for item in voices:
            if not isinstance(item, dict):
                continue
            voice_id = str(item.get("id", "")).strip().lower()
            gender = str(item.get("gender", "")).strip().lower()
            relative_file = str(item.get("file", "")).strip()
            voice_path = (root / relative_file).resolve()
            if not VOICE_ID_PATTERN.fullmatch(voice_id) or gender not in ("male", "female"):
                continue
            if root not in voice_path.parents or voice_path.suffix.lower() != ".pt" or not voice_path.is_file():
                continue
            if voice_id in AVAILABLE_VOICES or voice_id in discovered:
                logging.warning("Ignoring duplicate player voice id: %s", voice_id)
                continue
            discovered[voice_id] = {
                "path": voice_path,
                "gender": gender,
                "display_name": str(item.get("displayName", voice_id)).strip() or voice_id,
                "pack_id": pack_id,
                "pack_name": str(manifest.get("name", pack_id)).strip() or pack_id,
                "role": "player",
            }
    except Exception as exc:
        logging.warning("Ignoring invalid player-voice manifest %s: %s", manifest_path, exc)
    return discovered


def discover_file_voices(engine_name="kokoro"):
    discovered = discover_player_voices(engine_name)
    for voice_id, details in discover_external_voices(engine_name).items():
        if voice_id in discovered:
            logging.warning("Ignoring normal voice-pack id reserved by a Core player voice: %s", voice_id)
            continue
        discovered[voice_id] = details
    return discovered


def get_available_voices():
    return list(AVAILABLE_VOICES) + sorted(discover_file_voices("kokoro"))


class KokoroEngine:
    """Generate and play English dialogue with the bundled Kokoro model."""

    def __init__(self, lang="a"):
        from kokoro import KPipeline

        self.engine_id = "kokoro"
        self._pipeline = KPipeline(lang_code=lang or "a", repo_id="hexgrad/Kokoro-82M")
        self._external_voices = discover_file_voices("kokoro")
        self.available_voices = list(AVAILABLE_VOICES) + sorted(self._external_voices)
        self._voice_cache = {}
        self._cancel_lock = threading.Lock()
        self._generation = 0

    def _resolve_voice(self, voice):
        cache_key = voice or DEFAULT_VOICE
        cached = self._voice_cache.get(cache_key)
        if cached is not None:
            return cached
        resolved = []
        for voice_id in (part.strip() for part in (voice or DEFAULT_VOICE).split(",")):
            normalized = voice_id.lower()
            if normalized in AVAILABLE_VOICES:
                resolved.append(normalized)
            elif normalized in self._external_voices:
                resolved.append(str(self._external_voices[normalized]["path"]))
            elif voice_id:
                logging.warning("Unavailable Kokoro voice '%s'; using %s", voice_id, DEFAULT_VOICE)
        result = ",".join(resolved) if resolved else DEFAULT_VOICE
        if len(self._voice_cache) >= 128:
            self._voice_cache.clear()
        self._voice_cache[cache_key] = result
        return result

    def _ticket(self):
        with self._cancel_lock:
            return self._generation

    def _cancelled(self, ticket):
        with self._cancel_lock:
            return ticket != self._generation

    def stop(self):
        with self._cancel_lock:
            self._generation += 1
        try:
            import sounddevice as sound

            sound.stop()
        except Exception:
            pass

    @staticmethod
    def _pitch_shift(samples, factor):
        import numpy as np

        if len(samples) < 2 or abs(factor - 1.0) < 0.0001:
            return np.asarray(samples, dtype=np.float32)
        target_count = max(1, int(round(len(samples) / factor)))
        source_axis = np.arange(len(samples), dtype=np.float64)
        target_axis = np.linspace(0.0, len(samples) - 1, target_count)
        return np.interp(target_axis, source_axis, samples).astype(np.float32)

    def synthesize(self, text, voice=None, speed=1.0, pitch_semitones=0.0, cancel_ticket=None):
        import numpy as np

        selected_voice = self._resolve_voice(voice)
        pitch_factor = 2.0 ** (float(pitch_semitones) / 12.0)
        synthesis_speed = max(0.25, min(4.0, float(speed) / pitch_factor))
        chunks = []
        for result in self._pipeline(
            text,
            voice=selected_voice,
            speed=synthesis_speed,
            split_pattern=r"(?<=[.!?])\s+",
        ):
            if cancel_ticket is not None and self._cancelled(cancel_ticket):
                return None
            audio = result.audio if hasattr(result, "audio") else result[2]
            if audio is None:
                continue
            if hasattr(audio, "detach"):
                audio = audio.detach().cpu().numpy()
            samples = self._pitch_shift(np.asarray(audio, dtype=np.float32), pitch_factor)
            if samples.size:
                chunks.append(samples)
        if not chunks:
            raise RuntimeError("Kokoro produced no audio for the supplied dialogue")
        return chunks[0] if len(chunks) == 1 else np.concatenate(chunks)

    def speak(self, text, voice=None, speed=1.0, pitch_semitones=0.0):
        ticket = self._ticket()
        audio = self.synthesize(text, voice, speed, pitch_semitones, ticket)
        if audio is None or self._cancelled(ticket):
            return
        import sounddevice as sound

        sound.play(audio, SAMPLE_RATE, blocking=True)

    def speak_to_file(self, text, output_path, voice=None, speed=1.0, pitch_semitones=0.0):
        import soundfile

        audio = self.synthesize(text, voice, speed, pitch_semitones)
        destination = Path(output_path)
        destination.parent.mkdir(parents=True, exist_ok=True)
        soundfile.write(str(destination), audio, SAMPLE_RATE)
        return str(destination)

    def close(self):
        self.stop()


class MockEngine:
    """Fast engine used only for protocol and shutdown diagnostics."""

    def __init__(self, lang="a"):
        self.engine_id = "mock"
        self._stopped = threading.Event()

    def speak(self, text, voice=None, speed=1.0, pitch_semitones=0.0):
        self._stopped.clear()
        deadline = time.monotonic() + min(0.2, max(0.01, len(text) / 1000.0))
        while time.monotonic() < deadline and not self._stopped.wait(0.01):
            pass

    def stop(self):
        self._stopped.set()

    def close(self):
        self.stop()


def _load_addon_engine(lang, engine_name):
    """Load one explicitly selected local engine add-on.

    An add-on is a folder under engine-addons whose folder name and manifest id
    match. Its engine.py must expose create_engine(lang, addon_root, tts_root).
    """
    if not re.fullmatch(r"[a-z0-9][a-z0-9_-]{0,31}", engine_name):
        raise RuntimeError("Invalid TTS engine add-on id")

    tts_root = Path(os.environ.get("TTS_ROOT", Path(__file__).resolve().parent.parent)).resolve()
    obenvoice_root = Path(os.environ.get("OBENVOICE_ROOT", tts_root)).resolve()
    legacy_root = Path(os.environ.get("OBENVOICE_LEGACY_ROOT", tts_root)).resolve()
    addon_root = None
    for addons_root in ((obenvoice_root / "engine-addons").resolve(), (legacy_root / "engine-addons").resolve()):
        candidate = (addons_root / engine_name).resolve()
        if candidate.parent == addons_root and candidate.is_dir():
            addon_root = candidate
            break
    if addon_root is None:
        raise RuntimeError("TTS engine add-on folder was not found")

    manifest_path = addon_root / "manifest.json"
    entry_path = addon_root / "engine.py"
    with manifest_path.open("r", encoding="utf-8") as handle:
        manifest = json.load(handle)
    if not isinstance(manifest, dict) or manifest.get("id") != engine_name:
        raise RuntimeError("TTS engine add-on manifest id does not match its folder")
    if manifest.get("api_version") != 1:
        raise RuntimeError("Unsupported TTS engine add-on API version")
    if not entry_path.is_file():
        raise RuntimeError("TTS engine add-on has no engine.py")

    module_name = "_obenseuer_tts_addon_" + engine_name
    spec = importlib.util.spec_from_file_location(module_name, str(entry_path))
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load TTS engine add-on module")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    factory = getattr(module, "create_engine", None)
    if not callable(factory):
        raise RuntimeError("TTS engine add-on has no create_engine factory")
    engine = factory(lang=lang, addon_root=addon_root, tts_root=tts_root)
    for method_name in ("speak", "stop", "close"):
        if not callable(getattr(engine, method_name, None)):
            raise RuntimeError("TTS engine add-on is missing " + method_name)
    if not getattr(engine, "engine_id", None):
        try:
            engine.engine_id = engine_name
        except Exception:
            pass
    return engine


def get_tts_engine(lang="a", engine_name="kokoro"):
    selected = (engine_name or "kokoro").strip().lower()
    if selected == "mock":
        return MockEngine(lang)
    if selected == "kokoro":
        return KokoroEngine(lang)
    try:
        return _load_addon_engine(lang, selected)
    except Exception:
        # Optional engines must never take down the frozen Kokoro path.
        logging.exception("Optional TTS engine '%s' could not initialise; falling back to Kokoro", selected)
        return KokoroEngine(lang)

