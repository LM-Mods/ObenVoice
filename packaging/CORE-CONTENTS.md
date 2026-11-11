# Core package contents

- Path-sanitised, revalidated `ObenseuerDialogueTTS.dll` and `ObenVoice/Characters` JSON files
  and its CC BY-SA 4.0 attribution/modification notice
- `ObenVoice/backend` Python source
- Packaged Python runtime and native dependencies
- Kokoro model cache and bundled default voices
- Launcher files and `ObenVoice/Docs`
- Runtime dependency inventory, licences and notices
- No `__pycache__`, `.pyc`, PDB, private build paths, logs or user data
- Public installation, usage, troubleshooting and limitations documents

Do not package live config, Overrides, pronunciation/exclusion rules, logs,
`tts_ready.txt`, `__pycache__`, engine add-ons or external voice packs. Preserve
existing user data on upgrade.
