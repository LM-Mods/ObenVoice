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
    original_obenvoice_root = os.environ.get("OBENVOICE_ROOT")
    original_legacy_root = os.environ.get("OBENVOICE_LEGACY_ROOT")
    try:
        with tempfile.TemporaryDirectory(dir=SOURCE_ROOT / "tests") as temporary:
            root = Path(temporary)
            os.environ["TTS_ROOT"] = str(root)
            os.environ["OBENVOICE_ROOT"] = str(root / "ObenVoice")
            os.environ["OBENVOICE_LEGACY_ROOT"] = str(root)
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

            primary_pack = root / "ObenVoice" / "VoicePacks" / "Friendly Pack Folder"
            primary_voices = primary_pack / "voices"
            primary_voices.mkdir(parents=True)
            (primary_voices / "af_primary.pt").write_bytes(b"primary")
            (primary_pack / "manifest.json").write_text(
                json.dumps(
                    {
                        "manifestVersion": 1,
                        "id": "primary-pack",
                        "name": "Primary Pack",
                        "engine": "kokoro",
                        "voices": [
                            {"id": "af_primary", "displayName": "Primary", "gender": "female", "file": "voices/af_primary.pt"}
                        ],
                    }
                ),
                encoding="utf-8",
            )
            found = engines.discover_external_voices("kokoro")
            assert set(found) == {"af_primary", "bf_test", "bm_test"}
            assert found["af_primary"]["pack_id"] == "primary-pack"

            player_root = root / "ObenVoice" / "PlayerVoices"
            player_root.mkdir(parents=True)
            (player_root / "af_player.pt").write_bytes(b"player")
            (player_root / "manifest.json").write_text(
                json.dumps(
                    {
                        "manifestVersion": 1,
                        "id": "core-player-voices",
                        "name": "Player Voices",
                        "engine": "kokoro",
                        "role": "player",
                        "voices": [
                            {"id": "af_player", "displayName": "Player", "gender": "female", "file": "af_player.pt"}
                        ],
                    }
                ),
                encoding="utf-8",
            )
            player_voices = engines.discover_player_voices("kokoro")
            assert set(player_voices) == {"af_player"}
            assert player_voices["af_player"]["role"] == "player"
            assert "af_player" in engines.get_available_voices()

            for pack_name in ("z-pack", "a-pack"):
                duplicate_pack = root / "voice-packs" / "kokoro" / pack_name
                duplicate_voices = duplicate_pack / "voices"
                duplicate_voices.mkdir(parents=True)
                (duplicate_voices / "af_duplicate.pt").write_bytes(pack_name.encode("ascii"))
                (duplicate_pack / "manifest.json").write_text(
                    json.dumps(
                        {
                            "manifestVersion": 1,
                            "id": pack_name,
                            "name": pack_name,
                            "engine": "kokoro",
                            "voices": [
                                {"id": "af_duplicate", "displayName": "Duplicate", "gender": "female", "file": "voices/af_duplicate.pt"}
                            ],
                        }
                    ),
                    encoding="utf-8",
                )
            found = engines.discover_external_voices("kokoro")
            assert found["af_duplicate"]["pack_id"] == "a-pack", "alphabetically first pack must win duplicate voice IDs"

            shutil.rmtree(pack)
            shutil.rmtree(root / "ObenVoice")
            shutil.rmtree(root / "voice-packs" / "kokoro" / "a-pack")
            shutil.rmtree(root / "voice-packs" / "kokoro" / "z-pack")
            assert engines.discover_external_voices("kokoro") == {}
            assert list(engines.AVAILABLE_VOICES) == engines.get_available_voices()
    finally:
        if original_root is None:
            os.environ.pop("TTS_ROOT", None)
        else:
            os.environ["TTS_ROOT"] = original_root
        if original_obenvoice_root is None:
            os.environ.pop("OBENVOICE_ROOT", None)
        else:
            os.environ["OBENVOICE_ROOT"] = original_obenvoice_root
        if original_legacy_root is None:
            os.environ.pop("OBENVOICE_LEGACY_ROOT", None)
        else:
            os.environ["OBENVOICE_LEGACY_ROOT"] = original_legacy_root

    print("external voice-pack discovery/removal tests passed")


if __name__ == "__main__":
    main()
