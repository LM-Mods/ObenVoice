import argparse
import importlib.util
import os
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("obenvoice_root", type=Path)
    args = parser.parse_args()
    root = args.obenvoice_root.resolve()
    os.environ["OBENVOICE_ROOT"] = str(root)
    os.environ["TTS_ROOT"] = str(root)

    engine_path = root / "backend" / "tts_engines.py"
    spec = importlib.util.spec_from_file_location("candidate_player_voice_engine", engine_path)
    engines = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(engines)

    discovered = engines.discover_player_voices("kokoro")
    expected = {"am_obv_player_rowan_test", "af_obv_player_mara_test"}
    assert set(discovered) == expected
    assert all(details["role"] == "player" for details in discovered.values())
    assert expected.issubset(set(engines.get_available_voices()))

    from kokoro import KPipeline

    pipeline = object.__new__(KPipeline)
    pipeline.voices = {}
    pipeline.repo_id = "hexgrad/Kokoro-82M"
    pipeline.lang_code = "a"
    for voice_id, details in sorted(discovered.items()):
        tensor = pipeline.load_single_voice(str(details["path"]))
        assert tuple(tensor.shape) == (510, 1, 256)
        print(f"{voice_id}: Kokoro tensor load passed")

    print("player-only discovery and Kokoro loading tests passed")


if __name__ == "__main__":
    main()
