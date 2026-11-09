# Development checkpoint

Updated: 9 November 2026

## Repository state

- Branch: `dev/f8-webui-diagnostics`
- Final small-fix implementation: `444036c` (`Make default speech exclusions global`)
- Release preparation: `84a0600` (`Prepare v1.0.1 beta release candidate`)
- Public baseline: `aa9f00d` on `main`
- Published public version: `v1.0.1-beta.1` (internal build 4.1.2)
- GitHub source, tag and Core release are published. No Nexus upload has been performed.

## Completed

- Retained exact matching and the editable F8 speech-exclusion system.
- Changed fresh-install/default-restoration exclusions to three global rules:
  `Bzzzz!`, `...` and `AAAAH!`.
- Kept the built-in defaults separate from the user-managed
  `ObenseuerTTS\speech-exclusions.txt`; existing user files are not rewritten.
- Matched the pronunciation and exclusion reset buttons at 230 × 25 pixels.
- Retained the completed F8/F9 layout, engine Apply behavior, stable unknown-
  speaker Random selection/tracking and Advanced dialogue diagnostics.
- Retained ObenseuerCore 0.12.1; framework refactoring remains deferred.
- Updated public metadata, changelog, GitHub/Nexus notes, licensing references
  and AI-development disclosure for the release candidate.
- Prepared a full Core candidate from a separate copy of the protected staging
  baseline. Internal test/release-history material is excluded.

## Verification

- All focused C# tests passed.
- Engine-add-on and external voice-pack Python tests passed.
- Mock backend owned shutdown and same-port restart passed with localhost
  socket permission.
- Default .NET Framework 4.7.2 release build passed with no warnings.
- BepInEx 5 compatibility-reference build passed with no warnings.
- Both builds produced the same DLL:
  `A67DF8D9AB82EF7225D23D2836D0AE4FE89B84C3B746F6D39A16BAC4002D5DB5`.
- Core candidate validation passed for 13,803 files. It contains one Python
  runtime, one Kokoro model, no tests and no user data.
- ZIP integrity and per-entry checks passed.

## Gameplay and publication status

- The user gameplay-tested the preceding feature build on BepInEx 6 and
  reported F8/F9, engine controls, diagnostics, unknown-speaker behavior and
  exclusions working.
- Final internal build 4.1.2 passed the BepInEx 6 gameplay check.
- The 4.1.2 BepInEx 5 result is build/reference compatibility only; no new
  gameplay pass has been performed.
- The Nexus page is publicly visible at version 1.0.0-beta.1 with one file;
  its current scan label is **Some suspicious files**. Review that scan and
  ticket `#277860` before upload.

## Local artifacts

- Development overlay:
  local `ObenVoice-Development-84a0600` overlay outside the repository
- Core candidate ZIP:
  `dist/ObenVoice-v1.0.1-beta.1-Core.zip`
- ZIP SHA-256:
  `B115229DC2610F1CA82AD5FD0C2FB95DC10DA9700F219CB96975741FFB43E2E2`

The optional British Emma/George asset remains unchanged and separately
available. Publishing was explicitly approved after gameplay confirmation.
