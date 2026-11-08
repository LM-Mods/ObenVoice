# Core package contents

- Path-sanitised, revalidated `ObenseuerDialogueTTS.dll`, `characters.json`
  and its CC BY-SA 4.0 attribution/modification notice
- `ObenseuerTTS/backend` Python source
- Packaged Python runtime and native dependencies
- Kokoro model cache and bundled default voices
- Launcher files
- Runtime dependency inventory, licences and notices
- No `__pycache__`, `.pyc`, PDB, private build paths, logs or user data
- Public installation, usage, troubleshooting and limitations documents

Do not package live config, speaker assignments, logs, status files, engine add-ons or external voice packs. Preserve existing pronunciation and exclusion files on upgrade.
