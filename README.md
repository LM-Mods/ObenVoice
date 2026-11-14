# ObenVoice

ObenVoice adds fully local, offline text-to-speech to dialogue in
**Obenseuer**. It speaks NPC dialogue and barks, can optionally speak confirmed
player responses, and assigns stable voices to characters without sending
dialogue to an online service.

- **[Download ObenVoice Core](https://github.com/LM-Mods/ObenVoice/releases/download/v1.0.3-beta.1/ObenVoice-Core-v1.0.3-beta.1.zip)**
- **[All Add-on Downloads](docs/ADDON-DOWNLOADS.md)**

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
- Rowan and Mara dedicated player-only voices, plus player access to compatible
  installed voices through F9
- Automatic NPC dialogue and bark speech
- Optional confirmed player-response speech
- JSON casting or persistent gender-aware Random Voices with editable saved
  per-character voice, speed and pitch assignments
- Bundled 292-record game-registry database, optional NPC presets and per-NPC
  overrides; the database includes game test/placeholder and review entries
- Remembered automatic voices for known and unknown speakers
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

BepInEx and the compiled Obenseuer Modding Framework / ObenseuerCore 0.12.1
release must already be installed separately. Do not install the framework's
GitHub source-code ZIP as a game mod. ObenVoice v1.0.3-beta.1 uses one shared
DLL. The owner reports gameplay approval on BepInEx 6 and BepInEx 5.4.23.6.

## Download and installation

1. Download **ObenVoice Core** from GitHub Releases.
2. Extract the ZIP directly into the main Obenseuer game directory—the folder
   containing `Obenseuer.exe`.
3. Allow Windows to merge the included folders.
4. Launch Obenseuer normally.

Core includes Kokoro, six normal default voices, the Rowan and Mara player-only
voices and the required bundled Python runtime. Players do not need to install
Python, provide API keys or use a cloud speech service.

## Controls and guides

- **F8** — main ObenVoice settings and customisation menu
- **F9** — player voice controls

The F8 status row shows the current backend state plus the selected and running
engines. The four tabs are **General**, **NPC Voices**, **Pronunciation** and
**Advanced**. Dialogue controls are on General; engine diagnostics,
maintenance, reset and help are on Advanced.

### NPC voices and local data

NPC Voices shows installed normal voice sources, the **NPC Casting** selector
and **Overrides...**. Existing castings load their JSON assignments. **Random
Voices** reuses an assignment from
`ObenVoice/UserData/npc-voice-overrides.json` or generates one gender-matched
assignment, including subtle speed and pitch variation, and saves it there for
future conversations and restarts. Saved assignments remain editable through
the existing Overrides editor. Rowan and Mara are player-only and cannot enter
NPC casting, Overrides, Random Voices or fallback pools.

Unknown-speaker choices can be remembered in the same overrides file as
automatic records. Unknown-speaker candidates are logged separately in
`ObenVoice/Logs/unknown-speakers.tsv`. Pronunciation and exclusion rules, user
settings and diagnostic files are also stored locally. ObenVoice does not send
dialogue or these settings to an online service.

Shared character data and editable casting configurations are loaded from
`ObenVoice/Characters`. Voice assets and pack manifests use
`ObenVoice/VoicePacks`. Existing installations using the older
`BepInEx/plugins/ObenseuerDialogueTTS/characters.json` and
`ObenseuerTTS/voice-packs/kokoro` locations remain supported. New personal
rules and Overrides use `ObenVoice/UserData`; new diagnostics use
`ObenVoice/Logs`. If an existing legacy user-data group is found and the new
group is empty, Core continues using the legacy group without copying or
overwriting it.

Core supplies the 292-record **Default** casting. British Voices supplies
**Brit Expanded** and **Brit Pack Only**; Stalburg Voices Vol. 1 supplies
**SV1 Expanded** and **SV1 Pack Only**. These configurations retain their
stable IDs, and personal Overrides always take priority. The registry includes
game test/placeholder entries and records flagged for review; 292 does not mean
292 ordinary, encounterable or fully verified NPCs.

Restart the game after adding or removing an engine add-on, voice pack or NPC
preset. Use **Restart Voice Engine** after changing the selected engine, and
**Reload Pronunciation Rules** after editing rule files outside the game.

Legacy manual character-name strings in the BepInEx configuration remain
supported as a fallback but are not automatically migrated into the NPC
database or `npc-voice-overrides.json`.

If a voice is missing or incorrect, first check the selected preset and the
NPC's stored override. Advanced can flag recent dialogue for a local diagnostic
log. Malformed override JSON is preserved rather than overwritten; repair or
replace it, then reopen the editor and use **Reload file**.

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

Bug reports and contributions are welcome. Future development remains at the
project owner's discretion.

## Licensing and source credits

Original ObenVoice code and documentation are offered under the
[MIT licence](LICENSE):

```text
Copyright (c) 2026 LM-Mods
```

MIT does not relicense bundled third-party engines, libraries, models, voices
or data. The game-scanned character database compilation is separately offered
under CC BY-SA 4.0 to the extent applicable. The optional British Emma/George
pack is third-party Apache-2.0 material from a pinned Kokoro-82M revision.
Rowan and Mara are procedurally generated derivatives of the documented
Stalburg/Kokoro style-tensor chain and retain Apache-2.0 attribution,
modification notices and inherited acknowledgements.

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
- 11 November 2026: v1.0.2-beta.1 and the v1.0.1 optional packs passed final
  package, privacy and supported-loader checks.
- 14 November 2026: the owner approved the combined Random Voices and dedicated
  player-voice candidate for v1.0.3-beta.1 release preparation.

ObenVoice was developed independently for Obenseuer. The included backend is
not copied from Universal TTS Mod or another mod. Original checkpoints, hashes
and test records are preserved privately as supporting evidence. These dates
are not represented as Git history from before this repository was initialised.

AI-generated code can contain mistakes. This beta is provided for community
testing and should not be treated as an official or safety-critical product.

## Internal compatibility names

The project retains tested identifiers for loader and configuration
compatibility, including `ObenseuerDialogueTTS.dll` and plugin ID
`com.kokoro.tts.obenseuer`. New installations use the consolidated
`ObenVoice` folder; the old `ObenseuerTTS` runtime and saved-data paths remain
readable for existing installations.
