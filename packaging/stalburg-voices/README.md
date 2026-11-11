# Stalburg Voices Vol. 1

**20 Kokoro-compatible NPC-inspired voices: 10 male and 10 female.**

This is the complete **voice-pack add-on** for ObenVoice, not an engine, runtime or Core update.
All 20 were listened to in game and accepted by the user on 10 November 2026.
The underlying `.pt` voice files are **byte-for-byte identical** to the working
`ObenVoice-NPC-Voice-Lab-20-v1-PRIVATE.zip` test pack. Only user-facing metadata,
documentation and attributions were prepared for this candidate.

## Installation

1. Close Obenseuer and the ObenVoice voice backend.
2. Place the pack folder at
   `ObenVoice/VoicePacks/Stalburg Voices Vol. 1/`.
3. Restart the game and choose voices through the existing F8/F9 voice selection.
4. To uninstall: exit the game, then remove only that individual voice-pack folder.

The internal folder and voice IDs deliberately **retain their lab identifiers** to
replace the exact tested pack without duplicates or breaking future NPC draft
references. The in-game visible pack name is `Stalburg Voices Vol. 1`.
If you already installed the earlier 20-voice lab pack, copy this folder to the
new path and remove the old pack only after testing; the voice IDs are preserved. Do not delete
other voice packs, Core files, engine files or user preferences.

Requires existing ObenVoice Core and a working Kokoro engine. The Core is not
included or changed. No NPC settings are changed automatically.

## Voices

Male (M01-M10): Brann, Eero, Kemal, Vasko, Kiro, Hakon, Niko, Veld, Kram, Otto.
Female (F01-F10): Aila, Nessa, Zora, Vika, Hilde, Oona, Tasha, Minna, Shade, Rima.

The names and design notes are inspired by Obenseuer NPCs and Stalburg's varied
population, NOT verified character casting or authentic regional accents. Some
source styles derive from non-English Kokoro voices and are used experimentally
with the existing English speech pipeline.

User feedback: all 20 were worth keeping. F09 Shade and F10 Rima stood out;
M04 Vasko's accent was liked; M06 Hakon sounded slightly feminine but suitable;
M09 Kram had a perceived Indian-like quality; M10 Otto sounded suited to
narration/game voiceover. These are subjective listening impressions, not an
accent-identification result.

## Optional provisional NPC presets

Version 1.0.1 provides two opt-in casting configurations
under `ObenVoice/Characters`:

- **SV1 - Expanded** contains a complete 292-character provisional casting and
  retains active-pool fallback.
- **SV1 - Pack Only** contains a complete 292-character provisional casting.

The existing provisional assignments are preserved for compatibility. Exact
matches to the game-scanned database carry game character IDs; older unmatched
name-based assignments are not promoted into the master character database.
Presets never replace `npc-voice-overrides.json` or the Core NPC identity
database. Selecting Default restores Core automatic behaviour without deleting
user data.
## Licensing and original code

These 20 styles are **modified derivatives of official Kokoro-82M voice tensors**;
they are not newly trained independent human voice models. Kokoro-82M lists
Apache License 2.0; see `APACHE-2.0-LICENSE.txt`, `NOTICE.txt` and
`PROVENANCE.json` for the upstream sources, changes, credits and file hashes.

`GENERATOR.py` is the original Python/Torch generation program for transparency.
It is optional developer-only source and is NOT run by the game. It generates
new tensors from upstream input assets, not from any user's private game data.
The release retains the main project's licensing and integrity records.

No user names, email addresses, absolute local paths, telemetry, online services,
recorded user speech or private audio are part of this add-on.
