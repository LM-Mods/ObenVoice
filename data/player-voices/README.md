# Rowan and Mara player voices

Rowan and Mara are Kokoro-compatible player-only style tensors for ObenVoice.
They are available through F9 and are excluded from NPC casting, Overrides,
fallback and Random Voices selection.

They are procedurally generated derivatives, not independently trained voice
models and not audio clones. The generation notes describe offline statistical
style-space sampling conditioned on the 20 previously accepted Stalburg voice
tensors. Those tensors are themselves documented derivatives of Apache-2.0
Kokoro-82M voice tensors.

The user approved both voices by gameplay listening on 14 November 2026. This
is subjective quality approval, not proof of acoustic uniqueness or identity.

Redistribution must retain:

- `APACHE-2.0-LICENSE.txt`
- `NOTICE.txt`
- `PROVENANCE.json`

The exact player-generation program was not included with the candidate
archive, so the outputs are not represented as independently reproducible.
See `PROVENANCE.json` for hashes and the complete recorded source chain.
