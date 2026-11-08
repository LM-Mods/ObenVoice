import importlib.util
import json
import os
import tempfile
from pathlib import Path


SOURCE_ROOT = Path(__file__).resolve().parents[1]
CORE_PATH = SOURCE_ROOT / "src" / "backend" / "tts_engines.py"


def load_core():
    spec = importlib.util.spec_from_file_location("tts_engines_under_test", CORE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class KokoroSentinel:
    def __init__(self, lang):
        self.lang = lang

    def speak(self, *args, **kwargs):
        pass

    def stop(self):
        pass

    def close(self):
        pass


def main():
    core = load_core()
    core.KokoroEngine = KokoroSentinel

    with tempfile.TemporaryDirectory(dir=SOURCE_ROOT / "tests") as temporary:
        root = Path(temporary)
        os.environ["TTS_ROOT"] = str(root)

        fallback = core.get_tts_engine("a", "not-installed")
        assert isinstance(fallback, KokoroSentinel)

        addon = root / "engine-addons" / "sample"
        addon.mkdir(parents=True)
        (addon / "manifest.json").write_text(
            json.dumps({"id": "sample", "api_version": 1}), encoding="utf-8"
        )
        (addon / "engine.py").write_text(
            "class Engine:\n"
            "    def speak(self, *args, **kwargs): pass\n"
            "    def stop(self): pass\n"
            "    def close(self): pass\n"
            "def create_engine(lang, addon_root, tts_root):\n"
            "    result = Engine()\n"
            "    result.lang = lang\n"
            "    result.addon_root = addon_root\n"
            "    result.tts_root = tts_root\n"
            "    return result\n",
            encoding="utf-8",
        )

        loaded = core.get_tts_engine("a", "sample")
        assert loaded.__class__.__name__ == "Engine"
        assert loaded.lang == "a"
        assert loaded.addon_root == addon.resolve()
        assert loaded.tts_root == root.resolve()

        (addon / "manifest.json").write_text(
            json.dumps({"id": "wrong", "api_version": 1}), encoding="utf-8"
        )
        fallback = core.get_tts_engine("a", "sample")
        assert isinstance(fallback, KokoroSentinel)

    print("engine add-on loader tests passed")


if __name__ == "__main__":
    main()
