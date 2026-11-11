# Kokoro voice-pack format

Install each pack beneath:

```text
ObenVoice/VoicePacks/<descriptive-pack-folder>/
```

`manifest.json` uses manifest version 1 and retains the existing fields `id`,
`name`, `engine` and `voices`. A pack may also provide an optional short label:

```json
{
  "manifestVersion": 1,
  "id": "example-pack",
  "name": "Example Voices",
  "shortCode": "EX",
  "engine": "kokoro",
  "voices": [
    {
      "id": "af_example",
      "displayName": "Example",
      "gender": "female",
      "file": "voices/af_example.pt"
    }
  ]
}
```

Existing packs under `ObenseuerTTS/voice-packs/kokoro/<pack-id>` remain
supported. Older manifests without `shortCode` remain valid. Voice and pack identifiers
must be stable; display names and short codes are presentation metadata only.

## Optional NPC presets

Shared casting configurations belong under `ObenVoice/Characters` and include
their owning `packId`. Preset IDs are qualified by that pack ID, so `expanded`
in `example-pack` becomes the stable selection ID `example-pack/expanded`.
Existing in-pack `presets` folders remain supported for upgrades.

```json
{
  "presetVersion": 1,
  "packId": "example-pack",
  "id": "expanded",
  "name": "Example - Expanded",
  "description": "Optional description",
  "provisional": false,
  "fallbackMode": "active",
  "assignments": [
    {
      "internalName": "example_npc",
      "displayName": "Example NPC",
      "voice": "af_example"
    }
  ]
}
```

`fallbackMode` is `active` for the user's selected automatic pool or `pack` to
prefer the preset owner's voices for unassigned NPCs. Assignments may identify
an NPC by verified `gameCharacterId`, canonical `internalName`, or unambiguous
`displayName`. Optional `speed` and `pitch` use the same ranges as NPC Voice
Overrides.

Presets never replace `npc-voice-overrides.json`. A voice-only pack with no
casting configuration remains fully supported.
