# Release packaging plan

Release assets were created locally and verified on 8 November 2026 for review
before any upload. Their sizes and SHA-256 values are recorded in
[RELEASE-ASSETS-v1.0.0-beta.1.md](RELEASE-ASSETS-v1.0.0-beta.1.md).

## Planned assets

- `ObenVoice-v1.0.0-beta.1-Core.zip`
- `ObenVoice-v1.0.0-beta.1-British-Voices.zip`

## Core

Baseline: the protected 4.1.1 `Ready-to-Install-Core` checkpoint. The eventual asset contains the tested DLL, character data, backend, offline Python runtime, Kokoro model/cache, launcher and complete runtime notices.

The embedded Python runtime is required to run Kokoro, phonemization, audio
output and their native dependencies without requiring a system-wide Python
installation. It is local to ObenVoice and must not contain caches, logs or
user-specific configuration.

The public documents, licence/privacy review and BepInEx 5/6 gameplay tests are
complete. The packaging script performs the final file, archive-integrity and
checksum checks.

After assembling the core in a staging directory, regenerate its bundled Python
inventory and collected package licence files without downloading anything:

```powershell
python tools\generate_runtime_inventory.py C:\path\to\staged-core
```

With the approved licence and completed gameplay confirmations, create and
verify the final Core asset with:

```powershell
powershell -ExecutionPolicy Bypass -File tools\package-core-release.ps1 `
  -LicenseApproved -BepInEx5GameplayPassed -BepInEx6GameplayPassed
```

For a validation-only run without creating an archive:

```powershell
powershell -ExecutionPolicy Bypass -File tools\package-core-release.ps1 -ValidateOnly
```

## Pocket

The preserved internal reference archive was independently recalculated as
SHA-256 `14AEE71D5F2F990A0E021B3987547BF7087F9F9349C391B8B1C2E685DDFC83E9`.

It contains engine API version 1, Pocket code, model/tokenizer, fixed voice state, SciPy, einops, SentencePiece and collected licences. Publication remains blocked until the model, tokenizer and `bill_boerst.safetensors` provenance is tied to an identifiable upstream release and copyright holder. This does not block Core.

## British voices

The preserved internal reference contains unmodified Emma and George weights,
hashes, manifest, attribution and Apache-2.0 text. Both files match Kokoro-82M
revision `f3ff3571791e39611d31c381e3a41a3af07b4987`. Keep that pinned revision,
the file hashes and attribution alongside the download.
