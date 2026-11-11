# Engine add-ons and voice packs

Add-ons are trusted local code and data. Install only packages from sources you trust.

## Engine add-ons

Primary location: `ObenVoice\engine-addons\<engine-id>`. The legacy
`ObenseuerTTS\engine-addons\<engine-id>` location remains supported, but a
matching add-on ID in the primary location wins.

API version 1 requires `manifest.json` and `engine.py`. The manifest ID must exactly match the folder name. `engine.py` must expose `create_engine(lang, addon_root, tts_root)`. The returned object must provide `speak`, `stop` and `close`. See [the example](../examples/engine-addon/README.md).

## Kokoro voice packs

Primary location: `ObenVoice\VoicePacks\<descriptive-pack-folder>`.
The legacy `ObenseuerTTS\voice-packs\kokoro\<pack-id>` location remains supported.

Manifest version 1 supports an ID, display name, engine and voice entries. Kokoro voice files use `.pt`; paths must remain inside the pack. See [the example](../examples/voice-pack/README.md).

Package authors must supply their own model/voice licensing and attribution. The examples contain no third-party weights.
