# ObenVoice Core v1.0.2-beta.1

ObenVoice adds fully local, offline speech to Obenseuer dialogue. This release
adds the 292-character identity database and Default casting, NPC presets and
Overrides, consolidated `ObenVoice` folders, deterministic voice-pack loading,
and the updated F8 NPC voice controls.

## Install

Install BepInEx 5 or 6 and the compiled Obenseuer Modding Framework /
ObenseuerCore 0.12.1 release first. Extract
`ObenVoice-Core-v1.0.2-beta.1.zip` into the folder containing
`Obenseuer.exe`. Do not use the framework's GitHub source-code ZIP as the
installed framework.

Back up personal Overrides, pronunciation rules, exclusions and BepInEx
settings before upgrading. The release contains no personal settings or logs.

The shared DLL passed gameplay testing on BepInEx 6 and BepInEx 5.4.23.6.
The backend and speech remain local; there is no telemetry or cloud speech
service.

## Integrity

- ZIP SHA-256: `01FF75304D64CC3DEB606E2DC8BD0122EAD76C849171899347816051AA3C3FBE`
- Shared DLL SHA-256: `27CE12D012A048EDF89E44821D5FCC75F86BEB929AE7F1C9FD8A616D268FC230`

See the [licensing audit](LICENSING-AUDIT.md),
[third-party notices](../THIRD-PARTY-NOTICES.md) and
[installation guide](INSTALLATION.md). ObenVoice is an unofficial community
project and is not affiliated with or endorsed by Loiste Interactive or the
third-party projects it integrates. The code was generated using OpenAI Codex
under human-led design, testing and release decisions.

The frozen gameplay-tested candidate retains an earlier draft audit document
inside the archive. This repository's audit and release asset record supersede
that non-executable document; the DLL and package bytes are unchanged.
