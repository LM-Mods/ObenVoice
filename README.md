# ObenVoice

ObenVoice adds fully local, offline text-to-speech to dialogue in
**Obenseuer**. It speaks NPC dialogue and barks, can optionally speak confirmed
player responses, and assigns stable voices to characters without sending
dialogue to an online service.

## Development & Transparency

ObenVoice is an independent, open-source community project, originally built
for personal use in late October 2026 and shared freely with everyone.

**The code was generated using OpenAI Codex under human-led design, direction,
selection, gameplay testing and release decisions.**

Development history, source code, third-party licences, attribution and the
redistribution audit are available in this repository:

- [Third-party notices](THIRD-PARTY-NOTICES.md)
- [Public licensing audit](docs/LICENSING-AUDIT.md)
- [Source and licence boundaries](docs/SOURCE-AND-LICENSES.md)

Source: https://github.com/LM-Mods/ObenVoice

## Features

- Offline Kokoro speech engine with six included voices
- Automatic NPC dialogue and bark speech
- Optional confirmed player-response speech
- Stable per-character voice, speed and pitch selection
- Full F8 settings and customisation interface
- F9 quick player-voice controls
- In-game pronunciation replacement editor
- Exact speaker/text speech exclusions
- Removable Kokoro voice packs
- Expandable community engine-add-on and voice-pack interfaces
- Owned backend startup, reconnect, restart and clean shutdown

## Compatibility and requirements

- Windows with Obenseuer installed through Steam
- BepInEx 5 or BepInEx 6
- Obenseuer Modding Framework / ObenseuerCore 0.12.1

BepInEx and Obenseuer Modding Framework must already be installed separately.
ObenVoice uses one shared DLL for both loaders. The original public DLL passed
gameplay testing on BepInEx 5 and BepInEx 6.0.0-be.788; routine development
gameplay testing now uses BepInEx 6, with BepInEx 5 checked by a separate
reference build unless a release record says otherwise.

## Download and installation

1. Download **ObenVoice Core** from GitHub Releases.
2. Extract the ZIP directly into the main Obenseuer game directory—the folder
   containing `Obenseuer.exe`.
3. Allow Windows to merge the included folders.
4. Launch Obenseuer normally.

Core includes Kokoro, six default voices and the required private Python
runtime. Players do not need to install Python, provide API keys or use a cloud
speech service.

## Controls and guides

- **F8** — main ObenVoice settings and customisation menu
- **F9** — player voice controls

More information:

- [Detailed installation, upgrades and removal](docs/INSTALLATION.md)
- [Using F8, F9, pronunciation and exclusions](docs/USAGE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Known limitations](docs/KNOWN-LIMITATIONS.md)
- [Building from source](docs/BUILDING.md)

## Community add-ons and contributions

ObenVoice includes documented generic interfaces for independently developed
TTS engine add-ons and Kokoro voice packs. Package authors are responsible for
the source, safety, compatibility and licensing of their own components.

- [Engine add-ons and voice packs](docs/ADDONS.md)
- [Engine add-on example](examples/engine-addon/README.md)
- [Voice-pack example](examples/voice-pack/README.md)
- [Contribution guide](CONTRIBUTING.md)

Bug reports, focused fixes, forks, documentation improvements and community
add-ons are welcome.

## Licensing and source credits

Original ObenVoice code and documentation are offered under the
[MIT licence](LICENSE):

```text
Copyright (c) 2026 LM-Mods
```

MIT does not relicense bundled third-party engines, libraries, models, voices
or data. The adapted character database is separately licensed under CC BY-SA
4.0. The optional British Emma/George pack is third-party Apache-2.0 material
from a pinned Kokoro-82M revision.

See [LICENSING.md](docs/LICENSING.md),
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and the
[public licensing audit](docs/LICENSING-AUDIT.md) before redistributing a
binary package.

ObenVoice is unofficial. It is not affiliated with or endorsed by Loiste
Interactive, Obenseuer, BepInEx, Obenseuer Modding Framework, OpenAI or the
third-party speech projects it uses.

## Development provenance

- Late October 2026: personal project and initial implementation began.
- 29 October 2026: working local TTS versions were documented.
- 2 November 2026: preserved development checkpoints existed.
- 4 November 2026: the British voice-pack checkpoint was preserved.
- 7 November 2026: internal build 4.1.1 was frozen after gameplay testing.
- 8 November 2026: sanitised BepInEx 5/6 release testing and packaging passed.

ObenVoice was developed independently for Obenseuer. The included backend is
not copied from Universal TTS Mod or another mod. Original checkpoints, hashes
and test records are preserved privately as supporting evidence. These dates
are not represented as Git history from before this repository was initialised.

AI-generated code can contain mistakes. This beta is provided for community
testing and should not be treated as an official or safety-critical product.

## Internal compatibility names

The initial public beta retains tested legacy identifiers for upgrade and
configuration compatibility, including `ObenseuerDialogueTTS.dll`, plugin ID
`com.kokoro.tts.obenseuer`, runtime folder `ObenseuerTTS`, and existing saved
configuration paths.
