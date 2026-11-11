# Troubleshooting

## Backend does not become ready

- Wait for model loading to finish.
- Confirm another program is not using the configured backend port (default 5050).
- Confirm the Core runtime and model-cache folders were installed completely.
- Check `ObenVoice\Logs\tts_server.log` and the BepInEx log.

## Engine change does not apply

Choose the installed engine in F8 and press **Apply** beside the Voice Engine list. Use Advanced → **Restart Voice Engine** for a manual restart. ObenVoice refuses to terminate a backend process it did not start.

## Optional engine or voice is missing

- Engine add-ons belong under `ObenVoice\engine-addons\<id>`.
- Kokoro voice packs belong under `ObenVoice\VoicePacks\<descriptive-pack-folder>`; the older `ObenseuerTTS\voice-packs\kokoro\<pack-id>` path remains supported.
- Add-ons and packs are discovered at game startup.

## Preserve diagnostics safely

`ObenVoice\Logs\dialogue-diagnostics.log` and `ObenVoice\Logs\unknown-speakers.tsv` may contain dialogue and speaker names. Review them before sharing and do not include configuration or speaker-assignment files in public bug reports unless deliberately sanitized. Existing installations may still use the corresponding files under `ObenseuerTTS` until manually migrated.
