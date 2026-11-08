# Troubleshooting

## Backend does not become ready

- Wait for model loading to finish.
- Confirm another program is not using the configured backend port (default 5050).
- Confirm the Core runtime and model-cache folders were installed completely.
- Check `ObenseuerTTS\tts_server.log` and the BepInEx log.

## Engine change does not apply

Choose the installed engine in F8 and press **Restart Voice Engine**. ObenVoice refuses to terminate a backend process it did not start.

## Optional engine or voice is missing

- Engine add-ons belong under `ObenseuerTTS\engine-addons\<id>`.
- Kokoro voice packs belong under `ObenseuerTTS\voice-packs\kokoro\<pack-id>`.
- Add-ons and packs are discovered at game startup.

## Preserve diagnostics safely

Logs may contain dialogue and speaker names. Review them before sharing and do not include configuration or speaker-assignment files in public bug reports unless deliberately sanitized.
