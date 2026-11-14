# Usage

## F8 settings

F8 opens the main settings window. Its four tabs are **General**, **NPC
Voices**, **Pronunciation** and **Advanced**. Settings save automatically.

### General

- Select an installed voice engine, then use **Apply** when the selected and
  running engines differ.
- Choose whether the local voice service starts with the game, closes on game
  exit and shows brief status messages.
- Enable or disable NPC conversations, barks and confirmed player responses.
- Control speaker-name prefixes and muting of original conversation or bark
  audio.
- Change the F8 and F9 hotkeys.

### NPC Voices

The installed-voices summary is automatic; voice packs no longer require a
separate source selector. Choose a discovered JSON configuration from **NPC
Casting**, choose **Random Voices**, or open **Overrides...** to edit personal
per-NPC voice, speed and pitch records.

Normal casting configurations keep their established assignments, with a
personal Override taking priority. In Random Voices mode, an existing valid
Override is reused. If none exists, ObenVoice selects an installed
NPC-compatible voice using the character's known gender, creates subtle speed
and pitch variation, saves the assignment immediately to
`ObenVoice\UserData\npc-voice-overrides.json`, and reuses it in later
conversations and game sessions. Random means assign once, not on every line.

Rowan and Mara never appear in NPC candidate pools, casting assignments or the
Overrides voice selector. Restart the game after installing or removing a
voice pack or casting configuration.

### Pronunciation

The left editor manages pronunciation replacements. The right editor manages
exact speech exclusions. Add, update or delete individual rules, or restore
the relevant defaults. Changes are saved and applied without restarting the
engine.

### Advanced

- **Diagnostics:** enable continuous detailed logging when troubleshooting,
  change the diagnostic flag hotkey (End by default), or clear the bounded
  diagnostic log.
- **Maintenance:** use **Restart Voice Engine** for a manual restart or
  **Reload Pronunciation Rules** after editing rule files outside the game.
- **Reset Settings to Defaults:** resets ordinary settings while preserving
  Overrides, pronunciation rules and exclusions.
- **Help / Documentation:** opens the public ObenVoice README.

## F9 player voice

F9 opens the player-response voice, speed and tone controls. Player dialogue
is optional and speaks only the response the player confirms. Rowan (male) and
Mara (female) are dedicated player voices; compatible normal installed voices
remain selectable for the player as well.

## Pronunciation replacements

Rules use:

```text
Original Text => Say Instead
```

Matching is case-insensitive and word-boundary aware. Changes apply to subsequent speech without changing the subtitle displayed by the game.

## Speech exclusions

Exclusions use exact normalized speaker/text matching:

```text
* => Bzzzz!
```

Use `*` for any speaker or any text. Exclusions are checked before unknown-speaker prompts and speech queueing.

The three global exact-match defaults are `Any speaker => Bzzzz!`,
`Any speaker => ...` and `Any speaker => AAAAH!`. They are not limited to
Doorphone or another particular NPC. Normal sentences containing ellipses or
other exclamations are unaffected. Upgrades add these defaults once without
replacing custom rules; removing one afterward is respected.

## Dialogue diagnostics

Press **End** by default to append the four most recent dialogue outcomes to `ObenVoice\Logs\dialogue-diagnostics.log`. A short top-left confirmation reports the number saved. The log is bounded and remains separate from `tts_server.log` in the same folder.

Unknown speakers are deduplicated in `ObenVoice\Logs\unknown-speakers.tsv` for later manual character-database review. The popup supports stable Male, Female, Random, selected-voice, default and ignore choices; remembered choices use the local Overrides file.
