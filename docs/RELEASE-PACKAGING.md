# Release packaging record

The v1.0.1-beta.1 Core archive was published on 9 November 2026. The original
v1.0.0-beta.1 assets and staging baseline remain untouched.

## Published Core asset

- `ObenVoice-v1.0.1-beta.1-Core.zip`

## Core

Baseline: a separate copy of the protected 4.1.1 Core package with the 4.1.2
DLL and current public documentation overlaid. The asset contains character
data, backend, one offline Python runtime, one Kokoro model/cache, launcher and
complete runtime notices.

The embedded Python runtime is required to run Kokoro, phonemization, audio
output and their native dependencies without requiring a system-wide Python
installation. It is local to ObenVoice and must not contain caches, logs or
user-specific configuration.

The public documents, licence/privacy review, BepInEx 6 gameplay test and
BepInEx 5 compatibility-reference build are complete. The packaging script
performs the final file, archive-integrity and checksum checks.

After assembling the core in a staging directory, regenerate its bundled Python
inventory and collected package licence files without downloading anything:

```powershell
python tools\generate_runtime_inventory.py C:\path\to\staged-core
```

The published archive reused the verified candidate bytes after the BepInEx 6
gameplay check. For future candidate validation:

```powershell
powershell -ExecutionPolicy Bypass -File tools\package-core-release.ps1 `
  -ReleaseCandidate -LicenseApproved
```

For a validation-only run without creating an archive:

```powershell
powershell -ExecutionPolicy Bypass -File tools\package-core-release.ps1 -ValidateOnly -ReleaseCandidate
```

## British voices

The preserved internal reference contains unmodified Emma and George weights,
hashes, manifest, attribution and Apache-2.0 text. Both files match Kokoro-82M
revision `f3ff3571791e39611d31c381e3a41a3af07b4987`. Keep that pinned revision,
the file hashes and attribution alongside the download.
