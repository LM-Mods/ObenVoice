# Folder layout and migration

## Clean installation

Copy Core into the game folder so the DLL remains under BepInEx and all other
ObenVoice files use this tree:

```text
BepInEx/plugins/ObenseuerDialogueTTS/ObenseuerDialogueTTS.dll
ObenVoice/
  backend/
  runtime/
  model-cache/
  cache/                 generated
  Characters/
  VoicePacks/
  engine-addons/
  UserData/              private local files
  Logs/                  generated private diagnostics
  Docs/
  run_backend.bat
```

`BepInEx/config` remains managed by BepInEx. Model-cache contents retain their
existing Hugging Face directory structure. `tts_ready.txt`, `__pycache__`,
logs and cache output are generated and are not installation content.

## Existing installations

New paths take priority. Core falls back to a complete legacy
`ObenseuerTTS/backend` plus `runtime` installation when the new backend is not
complete. For user data, Core selects one whole group: if any supported file
exists in `ObenVoice/UserData`, that group is used; otherwise an existing
legacy group at the `ObenseuerTTS` root is used. Logs follow the same grouped
rule. This prevents two active configurations being merged unpredictably.

Before changing an existing installation, back up:

- `npc-voice-overrides.json`, `pronunciation-replacements.txt` and
  `speech-exclusions.txt` from the active user-data location;
- any diagnostics you want to retain;
- `BepInEx/config/com.kokoro.tts.obenseuer.cfg` if present;
- optional voice packs and engine add-ons that are not in the new folders.

To migrate personal data, close the game and backend, create
`ObenVoice/UserData`, then copy the three user-data files there. Copy wanted
logs separately to `ObenVoice/Logs`. Do not merge different versions of the
same JSON file.

## Old files that may be removed manually

Only after the clean installation has started successfully and backups are
verified, these ObenVoice-specific legacy items may be removed:

- `ObenseuerTTS` after wanted runtime/model/add-on/user data is migrated;
- old `characters.json` and `CHARACTER-DATA-LICENSE.md` beside the plugin DLL;
- generated `backend/__pycache__` and `tts_ready.txt`;
- old in-pack preset folders after the matching central presets work;
- superseded ObenVoice documentation or launchers previously loose in the game root.

Never remove `Obenseuer.exe`, `Obenseuer_Data`, BepInEx core files,
ObenseuerCore, unrelated mods/configuration, Steam files, or unrelated game
documentation. Cleanup is deliberately manual; Core never moves, deletes or
overwrites user data, models, runtimes or logs.
