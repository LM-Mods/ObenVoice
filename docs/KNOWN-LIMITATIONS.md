# Known limitations

- One shared plugin DLL targets BepInEx 5 and BepInEx 6. The project owner
  reports gameplay approval of the v1.0.3-beta.1 development candidate on
  BepInEx 6 and BepInEx 5.4.23.6.
- Unidentified `???` dialogue speakers can share one remembered assignment
  because the game does not always expose a distinct usable identity for them.
- Synthetic pronunciation and emotional delivery vary by voice and line.
- Very short hesitation sounds such as `Hmh`, `Ahhh`, `Hmm`, `Um` and `Urgh?`
  can sound unnatural or occasionally be skipped.
- Kokoro cancellation occurs between generated chunks; the currently executing model chunk is not interrupted.
- Add-ons and voice packs are discovered at game startup rather than hot-installed.
- Existing saved character profiles do not automatically adopt later automatic-profile defaults.
- Future Obenseuer or Dialogue System updates may require revised hooks.

Dialogue interrupted when the player advances the conversation is not considered a backend failure.
