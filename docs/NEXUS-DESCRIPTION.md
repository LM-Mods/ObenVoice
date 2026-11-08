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

- Windows and Obenseuer on Steam
- BepInEx 5: gameplay-tested with the accepted 4.1.1 baseline
- BepInEx 6.0.0-be.788: gameplay-tested with the accepted 4.1.1 baseline
- Obenseuer Modding Framework / ObenseuerCore 0.12.1

The path-sanitised public-release DLL passed gameplay testing on both loaders.

## Offline operation

Core speech generation runs locally using the packaged Python runtime and
speech dependencies. The plugin communicates with its owned backend over the
local loopback interface. It does not require a cloud speech service.

## Features and customisation

- Full F8 interface for engine status and restart, dialogue behaviour, voice
  pools, voice packs, pronunciation rules and diagnostics.
- F9 quick controls for player voice, speed and tone.
- Stable per-character voice, speed and pitch assignments.
- NPC dialogue, barks and optional confirmed player-response speech.
- Exact speech exclusions and editable pronunciation replacements.
- Expandable engine-add-on API and removable Kokoro voice packs.
- Gameplay-tested shared plugin for both supported BepInEx loaders.

## Credits, Licensing & Source Code

- Source code and development history: https://github.com/LM-Mods/ObenVoice
- Licensing and redistribution audit: https://github.com/LM-Mods/ObenVoice/blob/main/docs/LICENSING-AUDIT.md
- Third-party notices: https://github.com/LM-Mods/ObenVoice/blob/main/THIRD-PARTY-NOTICES.md

The original ObenVoice code is offered under MIT. Kokoro, Python, eSpeak NG,
phonemizer and all other third-party engines, models and dependencies retain
their own licences. The optional British voice pack has its own Apache-2.0
attribution. Pocket TTS is not part of the Core download and remains
unpublished pending separate redistribution clearance.

ObenVoice is unofficial and independently developed. It is not affiliated
with or endorsed by Loiste Interactive, the Obenseuer developers, or the
third-party projects it uses.
