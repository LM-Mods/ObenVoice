# Usage

## F8 settings

F8 opens the main settings window:

- **General:** selected/running engine, owned backend restart, startup behaviour and hotkeys.
- **Voices:** Kokoro fallback voice and automatic NPC speed/tone ranges.
- **Dialogue:** NPC dialogue, barks, speaker names and original-audio muting.
- **Voice packs:** installed Kokoro voice-pack controls.
- **Pronunciation:** pronunciation replacements and exact speech exclusions.
- **Data & status:** character database, assignments, diagnostics and reload controls.

Settings save automatically. Restart Voice Engine is needed for engine changes, not for pronunciation or exclusion edits.

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
