# Usage

## F8 settings

F8 opens the main settings window:

- **General:** selected/running engine, Apply control, startup behaviour and hotkeys.
- **Voices:** Kokoro fallback voice and automatic NPC speed/tone ranges.
- **Dialogue:** NPC dialogue, barks, speaker names and original-audio muting.
- **Voice packs:** installed Kokoro voice-pack controls.
- **Pronunciation:** pronunciation replacements and exact speech exclusions.
- **Data & status:** current record counts and the everyday unknown-speaker prompt option.
- **Advanced:** diagnostics, manual engine restart, database/rule reloads and reset controls.

Settings save automatically. After selecting another engine, use **Apply** beside the Voice Engine list. **Restart Voice Engine** remains available in Advanced for a manual restart. Pronunciation and exclusion edits apply without an engine restart.

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
Doorphone => Bzzzz!
```

Use `*` for any speaker or any text. Exclusions are checked before unknown-speaker prompts and speech queueing.

ObenVoice also suppresses the confirmed non-speech expressions `...` (including the Unicode ellipsis equivalent) and `AAAAH!`. Normal sentences containing ellipses and other exclamations are unaffected.

## Dialogue diagnostics

Press **End** by default to append the four most recent dialogue outcomes to `ObenseuerTTS\dialogue-diagnostics.log`. A short top-left confirmation reports the number saved. The log is bounded and remains separate from `tts_server.log` in the same folder.

Unknown speakers are deduplicated in `ObenseuerTTS\unknown-speakers.tsv` for later manual character-database review. The popup supports stable Male, Female, Random, selected-voice, default and ignore choices; remembered choices continue to use the existing assignment file.
