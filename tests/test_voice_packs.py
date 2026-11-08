import importlib.util
import json
import os
import shutil
import tempfile
from pathlib import Path


SOURCE_ROOT = Path(__file__).resolve().parents[1]
ENGINE_PATH = SOURCE_ROOT / "src" / "backend" / "tts_engines.py"


def load_engines():
    spec = importlib.util.spec_from_file_location("tts_engines_voice_pack_test", ENGINE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    engines = load_engines()
    original_root = os.environ.get("TTS_ROOT")
    try:
        with tempfile.TemporaryDirectory(dir=SOURCE_ROOT / "tests") as temporary:
            root = Path(temporary)
            os.environ["TTS_ROOT"] = str(root)
            assert engines.discover_external_voices("kokoro") == {}
            assert list(engines.AVAILABLE_VOICES) == engines.get_available_voices()

            pack = root / "voice-packs" / "kokoro" / "test-pack"
            voices = pack / "voices"
            voices.mkdir(parents=True)
            (voices / "bf_test.pt").write_bytes(b"test")
            (voices / "bm_test.pt").write_bytes(b"test")
            (pack / "manifest.json").write_text(
                json.dumps(
                    {
                        "manifestVersion": 1,
                        "id": "test-pack",
                        "name": "Test Pack",
                        "engine": "kokoro",
                        "voices": [
                            {"id": "bf_test", "displayName": "Test Female", "gender": "female", "file": "voices/bf_test.pt"},
                            {"id": "bm_test", "displayName": "Test Male", "gender": "male", "file": "voices/bm_test.pt"},
                        ],
                    }
                ),
                encoding="utf-8",
            )

            found = engines.discover_external_voices("kokoro")
            assert set(found) == {"bf_test", "bm_test"}
            assert found["bf_test"]["gender"] == "female"
            assert found["bm_test"]["path"] == (voices / "bm_test.pt").resolve()
            assert engines.get_available_voices()[-2:] == ["bf_test", "bm_test"]

            shutil.rmtree(pack)
            assert engines.discover_external_voices("kokoro") == {}
            assert list(engines.AVAILABLE_VOICES) == engines.get_available_voices()
    finally:
        if original_root is None:
            os.environ.pop("TTS_ROOT", None)
        else:
            os.environ["TTS_ROOT"] = original_root

    print("external voice-pack discovery/removal tests passed")


if __name__ == "__main__":
    main()
