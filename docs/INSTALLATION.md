# Installation, upgrade and removal

ObenVoice release downloads are not stored in this source repository. Use the Core ZIP attached to a published release.

## Requirements

- Windows and Obenseuer from Steam
- BepInEx 6.0.0-be.788, or the supported BepInEx 5 installation
- Obenseuer Modding Framework / ObenseuerCore 0.12.1

## Install

1. Close Obenseuer and any ObenVoice backend process.
2. Open the Obenseuer game folder through Steam.
3. Copy the Core ZIP contents into the game folder, merging `BepInEx` and `ObenseuerTTS`.
4. Do not replace BepInEx or ObenseuerCore with files from unrelated packages.
5. Start the game normally. The packaged backend may take a short time to load.

## Upgrade safely

Back up these user-managed files before upgrading:

- `ObenseuerTTS\pronunciation-replacements.txt`
- `ObenseuerTTS\speech-exclusions.txt`
- `BepInEx\config\com.kokoro.tts.obenseuer.cfg`
- `BepInEx\config\ObenseuerDialogueTTS\speaker_assignments.json`
- any folders under `ObenseuerTTS\engine-addons` and `ObenseuerTTS\voice-packs`

Never overwrite existing pronunciation or exclusion files with example/default files. The plugin creates defaults only when those files are absent.

## Remove

With the game closed, remove only:

- `BepInEx\plugins\ObenseuerDialogueTTS`
- `ObenseuerTTS`
- optionally, the ObenVoice configuration and assignment files listed above

Do not remove BepInEx or ObenseuerCore when removing ObenVoice.
