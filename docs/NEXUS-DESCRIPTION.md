# ObenVoice — Give Obenseuer a Voice!

ObenVoice adds local, offline text-to-speech to Obenseuer dialogue. It speaks
NPC dialogue and barks, can optionally speak confirmed player responses, and
keeps stable local voice assignments without sending dialogue to an online
service.

**Built for personal use. Shared with everyone. Expanded by the community.**

## Development & Transparency

ObenVoice is an independent, open-source community project, originally
developed for personal use in late October 2026 and shared freely with
everyone.

The code was generated using OpenAI Codex under human-led design, direction
and gameplay testing. For Nexus Mods classification, select **AI-Generated
Content**, because AI produced the majority of the code. This disclosure is in
the main description intentionally.

Development history, source code, third-party licences, attribution and the
redistribution audit are publicly available in the GitHub repository.

## Compatibility

- Windows with Obenseuer installed through Steam
- BepInEx 5 or BepInEx 6, installed separately
- Obenseuer Modding Framework / ObenseuerCore 0.12.1

The release DLL passed gameplay testing on BepInEx 5 and BepInEx
6.0.0-be.788. BepInEx and Obenseuer Modding Framework must already be
installed separately.

## Offline operation

Core speech generation runs locally using the packaged Python runtime and
speech dependencies. The plugin communicates with its owned backend over the
local loopback interface. It does not require a cloud speech service.

## Features and customisation

- Fully offline Kokoro dialogue speech with six included voices.
- Automatic NPC dialogue and optional confirmed player responses.
- Full F8 interface for engine status and restart, dialogue behaviour, voice
  pools, voice packs, pronunciation rules and diagnostics.
- F9 quick controls for player voice, speed and tone.
- Stable per-character voice, speed and pitch assignments.
- NPC dialogue, barks and optional confirmed player-response speech.
- Exact speech exclusions and editable pronunciation replacements.
- Expandable engine-add-on API and removable Kokoro voice packs.
- Gameplay-tested shared plugin for both supported BepInEx loaders.

## Installation

1. Install BepInEx 5 or 6 and Obenseuer Modding Framework / ObenseuerCore
   0.12.1 separately.
2. Download ObenVoice Core.
3. Extract the ZIP directly into the main Obenseuer directory containing
   `Obenseuer.exe` and allow Windows to merge the folders.
4. Launch Obenseuer normally.

Core includes Kokoro, its default voices and the required private Python
runtime. No separate Python installation, API key or cloud speech account is
needed.

## Credits, Licensing & Source Code

- Source code and development history: https://github.com/LM-Mods/ObenVoice
- Licensing and redistribution audit: https://github.com/LM-Mods/ObenVoice/blob/main/docs/LICENSING-AUDIT.md
- Third-party notices: https://github.com/LM-Mods/ObenVoice/blob/main/THIRD-PARTY-NOTICES.md

The original ObenVoice code is offered under MIT. Kokoro, Python, eSpeak NG,
phonemizer and all other third-party engines, models and dependencies retain
their own licences. The optional British voice pack has its own Apache-2.0
attribution.

ObenVoice is unofficial and independently developed. It is not affiliated
with or endorsed by Loiste Interactive, the Obenseuer developers, or the
third-party projects it uses.
