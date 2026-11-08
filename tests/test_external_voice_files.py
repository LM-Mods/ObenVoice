import argparse
import importlib.util
import os
from pathlib import Path


SOURCE_ROOT = Path(__file__).resolve().parents[1]
ENGINE_PATH = SOURCE_ROOT / "src" / "backend" / "tts_engines.py"


def load_engines():
    spec = importlib.util.spec_from_file_location("tts_engines_external_file_test", ENGINE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("pack_tts_root", type=Path)
    args = parser.parse_args()
    os.environ["TTS_ROOT"] = str(args.pack_tts_root.resolve())

    engines = load_engines()
    discovered = engines.discover_external_voices("kokoro")
    assert set(discovered) == {"bf_emma", "bm_george"}

    import torch

    for voice_id in ("bf_emma", "bm_george"):
        data = torch.load(discovered[voice_id]["path"], weights_only=True)
        assert data is not None

    engine = engines.KokoroEngine("a")
    try:
        samples = engine.synthesize("Emma is ready.", "bf_emma")
        assert len(samples) > 0
        samples = engine.synthesize("George is ready.", "bm_george")
        assert len(samples) > 0
        samples = engine.synthesize("Agnes is still working.", "af_heart")
        assert len(samples) > 0
    finally:
        engine.close()

    print("Emma, George, and bundled Kokoro/eSpeak synthesis tests passed")


if __name__ == "__main__":
    main()
