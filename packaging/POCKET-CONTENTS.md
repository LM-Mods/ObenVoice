# Pocket package contents and gate

Expected root: `ObenseuerTTS/engine-addons/pocket`.

The private reference contains `manifest.json`, `engine.py`, Pocket
source/package files, `model.safetensors`, `tokenizer.model`,
`voices/bill_boerst.safetensors`, extra Python dependencies and licences.

Recorded provenance:

| File | Recorded upstream | SHA-256 |
|---|---|---|
| `model.safetensors` | `kyutai/pocket-tts`, revision `db04f0e653473e5c7333c69686ac92b7460b0c1d` | `AEA704A2989277C1A700ABC16FB1442CB6BAA2FA9AD32251EF7A5F91E4EB7D58` |
| `tokenizer.model` | `kyutai/pocket-tts-without-voice-cloning`, revision `08ffc58775e62aad9409511b9591c9af626270f3` | `D461765AE179566678C93091C5FA6F2984C31BBE990BF1AA62D92C64D91BC3F6` |
| `bill_boerst.safetensors` | Kyutai Bill/`voice-zero` data through the no-cloning repository, revision `a7056f505a4a8ca72d8b5fb1440071d6f1a35739` | `A050DE1080452E84698AAEA1E989AC9EDCFF1D11B4B865530AF924BDA79DDA97` |

Pocket TTS code v4.0.1 is MIT. The model repository declares CC BY 4.0
but is gated and also presents acceptable-use conditions. The Kyutai voice
repository declares its `voice-zero` collection CC0. These declarations do
not establish that a file obtained through the model gate may be rehosted on
GitHub or Nexus.

**Publication remains blocked.** Before preparing any public package, obtain
an authenticated comparison of every file against the pinned revisions and
clear evidence that the gated access terms permit external redistribution.
If that evidence cannot be obtained, leave Pocket unpublished. Do not rename,
replace or rehost the restricted files as a workaround.
