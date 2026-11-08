import argparse
import shutil
import tempfile
from pathlib import Path


RUNTIME_FILES = (
    Path("BepInEx/plugins/ObenseuerDialogueTTS/ObenseuerDialogueTTS.dll"),
    Path("ObenseuerTTS/backend/tts_server.py"),
    Path("ObenseuerTTS/backend/tts_engines.py"),
)
PRONUNCIATION_FILE = Path("ObenseuerTTS/pronunciation-replacements.txt")


def install_and_rollback(patch_root, existing_pronunciation):
    test_root = Path(__file__).resolve().parent
    with tempfile.TemporaryDirectory(prefix="obenseuer-tts-patch-", dir=test_root) as temporary:
        game_root = Path(temporary) / "game"
        backup_root = Path(temporary) / "backup"
        original = {}
        for index, relative in enumerate(RUNTIME_FILES):
            target = game_root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            original[relative] = ("old-runtime-%d" % index).encode("ascii")
            target.write_bytes(original[relative])
        if existing_pronunciation:
            target = game_root / PRONUNCIATION_FILE
            target.parent.mkdir(parents=True, exist_ok=True)
            original[PRONUNCIATION_FILE] = b"customword => custom pronunciation\n"
            target.write_bytes(original[PRONUNCIATION_FILE])

        for relative, expected in original.items():
            backup = backup_root / relative
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(game_root / relative, backup)
            assert backup.read_bytes() == expected

        for relative in RUNTIME_FILES:
            source = patch_root / relative
            assert source.is_file(), "patch file missing: " + str(relative)
            target = game_root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
            assert target.read_bytes() == source.read_bytes()

        pronunciation_source = patch_root / PRONUNCIATION_FILE
        assert pronunciation_source.is_file(), "default pronunciation file missing"
        pronunciation_target = game_root / PRONUNCIATION_FILE
        if not pronunciation_target.exists():
            pronunciation_target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(pronunciation_source, pronunciation_target)
        if existing_pronunciation:
            assert pronunciation_target.read_bytes() == original[PRONUNCIATION_FILE]
        else:
            assert pronunciation_target.read_bytes() == pronunciation_source.read_bytes()

        for relative in RUNTIME_FILES + (PRONUNCIATION_FILE,):
            backup = backup_root / relative
            target = game_root / relative
            if backup.exists():
                shutil.copy2(backup, target)
            elif target.exists():
                target.unlink()

        assert not pronunciation_target.exists() if not existing_pronunciation else pronunciation_target.read_bytes() == original[PRONUNCIATION_FILE]
        for relative in RUNTIME_FILES:
            assert (game_root / relative).read_bytes() == original[relative]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("patch_root", type=Path)
    args = parser.parse_args()
    patch_root = args.patch_root.resolve()
    actual = sorted(path.relative_to(patch_root) for path in patch_root.rglob("*") if path.is_file())
    expected = sorted(RUNTIME_FILES + (PRONUNCIATION_FILE,))
    assert actual == expected, "combined patch must contain exactly the four expected runtime files"
    install_and_rollback(patch_root, existing_pronunciation=True)
    install_and_rollback(patch_root, existing_pronunciation=False)
    print("temporary patch install/rollback test passed with existing and new pronunciation files")


if __name__ == "__main__":
    main()
