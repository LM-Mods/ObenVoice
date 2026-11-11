# Usage

## F8 settings

F8 opens the main settings window. Its five tabs are **General**, **Voice
Packs**, **NPC Voices**, **Pronunciation** and **Advanced**. Settings save
automatically.

### General

- Select an installed voice engine, then use **Apply** when the selected and
  running engines differ.
- Choose whether the local voice service starts with the game, closes on game
  exit and shows brief status messages.
- Enable or disable NPC conversations, barks and confirmed player responses.
- Control speaker-name prefixes and muting of original conversation or bark
  audio.
- Change the F8 and F9 hotkeys.

### Voice Packs

For Kokoro, **Default** uses the built-in voices, **All Voices** combines the
built-ins with installed packs, a named pack limits the automatic pools to
that pack, and **Custom** keeps individual female and male selections. Explicit
NPC Overrides and preset assignments may still request any compatible
installed voice. Restart the game after installing or removing a pack.

Optional engines expose only the pack controls their add-on supports. Treat
third-party add-ons as untrusted until their source, dependencies, privacy
behaviour and licence have been reviewed.

### NPC Voices

Choose a discovered casting preset or open **Overrides...** to edit personal
per-NPC voice, speed and tone records. Resolution priority is personal
Override, selected preset, saved automatic voice, then a new automatic voice.
**Reset to Default** in the Overrides editor removes only that NPC's personal
record; it does not change a preset.

The optional **Automatic variation controls** expose separate speed-minimum,
speed-maximum, tone-minimum and tone-maximum rows. Their Reset buttons restore
the individual setting defaults. The unknown-speaker prompt can also be
enabled or disabled here.

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

F9 opens the player-response voice, speed and tone controls. Player dialogue is optional and speaks only the response the player confirms.

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
