# ObenVoice

ObenVoice adds local, offline text-to-speech to dialogue in **Obenseuer**. It can speak NPC dialogue and barks, optionally speak confirmed player responses, assign stable voices to characters, and run without sending dialogue to an online service.

## Development & Transparency

ObenVoice is an independent, open-source community project, originally
developed for personal use in late October 2026 and shared freely with
everyone.

**The code was generated using OpenAI Codex under human-led design, direction,
selection, gameplay testing and release decisions.**

The public source, development provenance, third-party licences and
redistribution findings are available in this repository. See the
[third-party notices](THIRD-PARTY-NOTICES.md) and
[public licensing audit](docs/LICENSING-AUDIT.md).

Planned public repository: https://github.com/LM-Mods/ObenVoice

The first planned public release is **v1.0.0-beta.1**, based on the gameplay-tested internal build **4.1.1**.

## Current compatibility

- Windows
- Obenseuer on Steam
- BepInEx 6.0.0-be.788: gameplay-tested with build 4.1.1
- Obenseuer Modding Framework / ObenseuerCore 0.12.1
- BepInEx 5: gameplay-tested with build 4.1.1

The path-sanitised public-release DLL is compiled from the same 4.1.1 source
and passed focused gameplay testing on both supported loaders.

ObenVoice is an unofficial community mod. It is not affiliated with Loiste Interactive, Obenseuer, BepInEx, Obenseuer Modding Framework, OpenAI, or the third-party speech projects it uses.

## Features

- Offline Kokoro speech with six bundled voices
- Stable per-character voice, speed and pitch selection
- Optional player-response speech and bark speech
- Optional Pocket engine add-on
- Removable Kokoro voice packs
- F8 settings with engine restart, voices, dialogue, packs, pronunciation and status
- F9 quick player-voice controls
- In-game pronunciation replacement editor
- Exact speaker/text speech exclusions
- Owned backend startup, reconnect, restart and clean shutdown

## Installation and use

The source repository does not contain the large Python runtime, models or release DLL. Install from a GitHub Release asset when one is published.

Core packages a private Python 3.12 runtime because Kokoro, phonemization,
audio output and their native libraries are Python components and cannot be
assumed to exist on a player's machine. ObenVoice launches only that local
runtime and communicates with it over loopback; it does not install Python
system-wide or use a cloud speech API.

- [Installation, upgrades and removal](docs/INSTALLATION.md)
- [Using F8, F9, pronunciation and exclusions](docs/USAGE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Building from source](docs/BUILDING.md)
- [Engine add-ons and voice packs](docs/ADDONS.md)
- [Known limitations](docs/KNOWN-LIMITATIONS.md)

## Internal compatibility names

The public project is named ObenVoice. To preserve upgrades and tested behaviour, the initial release retains legacy internal identifiers including:

- `ObenseuerDialogueTTS.dll`
- plugin ID `com.kokoro.tts.obenseuer`
- runtime folder `ObenseuerTTS`
- existing configuration keys and saved-assignment paths

## Development provenance

- Late October 2026: personal project and initial implementation began.
- 29 October 2026: working local TTS versions were documented.
- 2 November 2026: preserved development checkpoints existed.
- 4 November 2026: the British voice-pack checkpoint was preserved.
- 7 November 2026: internal build 4.1.1 was frozen after gameplay testing.
- 8 November 2026: BepInEx 5/6 verification and public-release preparation.

ObenVoice was developed independently for Obenseuer. The included backend is
not copied from Universal TTS Mod or another mod. The dates above are supported
by private checkpoints and test records; they are not represented as Git
history from before this repository was initialised.

AI-generated code can contain mistakes. This beta is provided for community
testing and should not be treated as an official or safety-critical product.

## Licensing

Original ObenVoice code and documentation are offered under the
[MIT licence](LICENSE), with the public identity **LM-Mods** in the copyright
notice. Forks, modifications, community contributions and independently
developed add-ons are welcome under their applicable licences.

MIT does not relicense bundled third-party engines, libraries, models, voices
or data. The adapted character database is separately licensed under CC BY-SA
4.0. Optional add-ons can have additional conditions:

- The British Emma/George voice pack is Apache-2.0 material from a pinned
  Kokoro-82M revision.
- Pocket TTS remains unpublished while its gated model redistribution terms
  and exact-file provenance are unresolved.

Read [LICENSING.md](docs/LICENSING.md),
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and the
[public licensing audit](docs/LICENSING-AUDIT.md) before redistributing a
binary package.

The concise [source and licence-boundary record](docs/SOURCE-AND-LICENSES.md)
explains what is original, adapted, referenced and separately licensed.

## Community contributions

Bug reports, focused fixes, documentation improvements, forks and independently
developed add-ons are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md)
before submitting material, particularly the provenance and third-party
licensing requirements.
