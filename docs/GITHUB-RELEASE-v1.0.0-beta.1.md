# ObenVoice v1.0.0-beta.1

First public beta of ObenVoice, an independent local text-to-speech mod for
Obenseuer.

## Core highlights

- Offline Kokoro speech with six bundled voices.
- NPC dialogue, barks and optional confirmed player-response speech.
- Full F8 settings UI and compact F9 player-voice controls.
- Stable character voice, speed and pitch assignments.
- Pronunciation editor, exact speech exclusions and removable voice packs.
- Owned local backend with restart, cancellation and clean shutdown handling.
- BepInEx 5 and BepInEx 6 support.
- Expandable engine-add-on and Kokoro voice-pack interfaces.

## Development transparency

ObenVoice began as a personal project in late October 2026 and is shared
freely as an independent open-source community project. Its code was generated
using OpenAI Codex under human-led design, direction, selection, gameplay
testing and release decisions.

## Downloads

- `ObenVoice-v1.0.0-beta.1-Core.zip` — complete required installation.
- British Emma/George voice pack — optional Apache-2.0 Kokoro voices.

Pocket TTS is not included and remains unpublished pending separate model
redistribution clearance.

## Installation

Extract Core into the Obenseuer game directory so its `BepInEx` and
`ObenseuerTTS` folders merge with the corresponding game folders. Back up
custom data before upgrading. Existing pronunciation rules, speech exclusions,
configuration and saved voice assignments should be preserved.

## Source and licensing

- Repository: https://github.com/LM-Mods/ObenVoice
- Licensing audit: https://github.com/LM-Mods/ObenVoice/blob/main/docs/LICENSING-AUDIT.md
- Third-party notices: https://github.com/LM-Mods/ObenVoice/blob/main/THIRD-PARTY-NOTICES.md

Original ObenVoice material is released under MIT. Third-party
engines, dependencies, models, voices and the CC BY-SA character database
retain their own licences. ObenVoice is unofficial and is not affiliated with
or endorsed by Loiste Interactive.

The sanitised release DLL passed focused gameplay testing on BepInEx 5 and
BepInEx 6. Very short hesitation sounds can occasionally sound imperfect or
be skipped; this is a known nonblocking beta limitation.
