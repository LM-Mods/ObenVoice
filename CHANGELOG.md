# Changelog

## v1.0.0-beta.1 — planned

Public release label for internal build 4.1.1.

- Offline Kokoro core with stable character voices.
- Shared loader-neutral plugin architecture for BepInEx 5/6 through ObenseuerCore.
- F8 engine selection and owned backend restart.
- F8 voice, dialogue, voice-pack, pronunciation and status controls.
- F9 player voice controls.
- Editable pronunciation replacements with immediate reload.
- Exact speaker/text speech exclusions, including `Doorphone => Bzzzz!`.
- Optional engine add-on API version 1 and Kokoro voice-pack manifest version 1.
- Backend cancellation, bounded queueing, rotating logs and ownership-safe shutdown.

The accepted 4.1.1 build was gameplay-tested on BepInEx 5 and BepInEx 6.

Release-compliance preparation added the approved LM-Mods MIT licence,
CC BY-SA character-data attribution, missing Loguru/Tokenizers notices, a
public licensing audit, prominent development disclosure and deterministic
path-sanitised DLL build settings. The replacement release DLL passed focused
gameplay testing on BepInEx 5 and BepInEx 6.
