# ObenVoice Core v1.0.3-beta.1

ObenVoice v1.0.3-beta.1 completes the simplified NPC casting system and adds
two dedicated player voices. This is a complete replacement Core package, not
a patch.

## Highlights

- Simplified four-tab F8 interface with automatic installed-voice discovery.
- JSON-controlled NPC Casting plus editable personal Overrides.
- Persistent **Random Voices**: a missing NPC receives one gender-matched
  voice with subtle speed and pitch variation, saved locally and reused.
- Rowan and Mara added to F9 as dedicated player-only voices.
- Compatible normal installed voices remain available to the player.
- Player-only voices are excluded from NPC casting, Overrides and fallback
  pools.
- Existing Core settings, voice packs, casting JSON and personal Overrides
  remain compatible.

## Installation

1. Close Obenseuer and any ObenVoice backend process.
2. Back up `ObenVoice/UserData` and
   `BepInEx/config/com.kokoro.tts.obenseuer.cfg` if upgrading.
3. Extract `ObenVoice-Core-v1.0.3-beta.1.zip` into the folder containing
   `Obenseuer.exe`, allowing Windows to merge the included folders.
4. Start the game normally.

The archive includes the required local Python runtime, Kokoro backend, model,
six normal Core voices, Rowan, Mara and the shared BepInEx 5/6 plugin DLL. No
system Python, cloud service or API key is required. Optional British and
Stalburg voice packs remain separate and do not need a version change.

## Compatibility and testing

- Obenseuer Modding Framework / ObenseuerCore 0.12.1.
- One shared plugin DLL for BepInEx 5 and BepInEx 6.
- The project owner reports gameplay approval of the combined candidate on
  BepInEx 5.4.23.6 and BepInEx 6.
- Focused build, casting, voice-discovery and packaged Kokoro tensor-loading
  checks passed during release preparation.

Known limitation: unidentified `???` dialogue speakers can share one
remembered assignment when the game does not expose a distinct usable identity.

## Privacy, source and licences

Speech and settings remain local. The package contains no telemetry, personal
Overrides, saved assignments, logs or user configuration. Rowan and Mara are
procedural derivatives of the documented Stalburg/Kokoro style-tensor chain;
Apache-2.0, modification notices, hashes and inherited acknowledgements are
included. Their original generation program was not supplied, so exact
regeneration and independent training are not claimed.

- [Source and full documentation](https://github.com/LM-Mods/ObenVoice)
- [Third-party notices](https://github.com/LM-Mods/ObenVoice/blob/main/THIRD-PARTY-NOTICES.md)
- [Licensing audit](https://github.com/LM-Mods/ObenVoice/blob/main/docs/LICENSING-AUDIT.md)
- [GPL/LGPL source-access record](https://github.com/LM-Mods/ObenVoice/blob/main/docs/ESPEAK-NG-REDISTRIBUTION.md)

ObenVoice code was generated using OpenAI Codex under human-led design,
direction, selection, gameplay testing and release decisions. This beta is
unofficial and is not guaranteed compatible with future game versions.

This is the final planned update for now. Future development remains at the
project owner's discretion.
