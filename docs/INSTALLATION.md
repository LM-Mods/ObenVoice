# Installation, upgrade and removal

ObenVoice release downloads are not stored in this source repository. Use the Core ZIP attached to a published release.

## Requirements

- Windows and Obenseuer from Steam
- BepInEx 6.0.0-be.788, or the supported BepInEx 5 installation
- Obenseuer Modding Framework / ObenseuerCore 0.12.1

BepInEx and Obenseuer Modding Framework must be installed separately before
installing ObenVoice.

## Install

1. Close Obenseuer and any ObenVoice backend process.
2. Open the Obenseuer game folder through Steam.
3. Extract the Core ZIP directly into the main game folder containing
   `Obenseuer.exe`, allowing Windows to merge `BepInEx` and `ObenVoice`.
4. Do not replace BepInEx or ObenseuerCore with files from unrelated packages.
5. Start the game normally. The packaged backend may take a short time to load.

Core includes Kokoro, six default voices and a bundled Python 3.12 runtime.
Players do not install Python separately and do not need an API key or cloud
speech service. ObenVoice launches only its packaged runtime and communicates
with it over the local loopback interface; it does not install Python
system-wide.

Optional voice packs are separate ZIPs. Extract a pack into the same main game
folder after Core, then restart the game so its manifest, voices and casting
configurations are discovered.

## Upgrade safely

Back up these user-managed files before upgrading:

- `ObenVoice\UserData\npc-voice-overrides.json`
- `ObenVoice\UserData\pronunciation-replacements.txt`
- `ObenVoice\UserData\speech-exclusions.txt`
- the same three files at the old `ObenseuerTTS` root, if present
- `BepInEx\config\com.kokoro.tts.obenseuer.cfg`
- `BepInEx\config\ObenseuerDialogueTTS\speaker_assignments.json`
- any installed folders under `ObenVoice\engine-addons` and `ObenVoice\VoicePacks`

Never overwrite existing pronunciation or exclusion files with example/default files. The plugin creates defaults only when those files are absent.
Installing a release over an existing copy does not require deleting settings,
Overrides, logs, models or other voice packs. Back them up first and remove only
files specifically identified by a release note.

## Remove

With the game closed, remove only:

- `BepInEx\plugins\ObenseuerDialogueTTS`
- `ObenVoice`
- optionally, the ObenVoice configuration and assignment files listed above

Do not remove BepInEx or ObenseuerCore when removing ObenVoice.

For the consolidated clean-install tree, legacy precedence and exact manual
migration/cleanup steps, see [Folder layout and migration](FOLDER-LAYOUT-AND-MIGRATION.md).
