# ObenVoice v1.0.1-beta.1

This beta update focuses on clearer controls, better diagnostics and safer
dialogue customisation.

## What changed

- Refined the F8 settings layout and compact F9 player-voice controls.
- Added an explicit Apply action when changing the selected voice engine.
- Added stable Random assignment and persistent tracking for unknown speakers.
- Improved Advanced dialogue diagnostics, including recent-event flagging and
  clearer save/clear feedback.
- Moved confirmed non-speech filtering into the editable F8 exclusions list.
- Made the three fresh-install/default-restoration exclusions global exact
  matches: `Bzzzz!`, `...` and `AAAAH!`.
- Improved reset-button consistency and general UI usability.

## Installation

Install BepInEx 5 or 6 and Obenseuer Modding Framework / ObenseuerCore 0.12.1
separately. Extract `ObenVoice-v1.0.1-beta.1-Core.zip` into the main Obenseuer
folder containing `Obenseuer.exe` and allow Windows to merge folders.

Core includes Kokoro, six default voices and its private Python runtime.
Existing configuration, pronunciation rules, speech exclusions, assignments
and separately installed add-ons remain user-managed;
back them up before upgrading.

The optional British Emma/George voice pack remains available separately
under its existing Apache-2.0 attribution.

## Development transparency

ObenVoice code was generated using OpenAI Codex under human-led design,
direction, selection, gameplay testing and release decisions. Original
ObenVoice material is MIT licensed; bundled third-party components retain
their own licences and notices.
