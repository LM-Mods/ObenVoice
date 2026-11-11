# Known limitations

- One shared plugin DLL targets BepInEx 5 and BepInEx 6. The v1.0.2-beta.1 DLL
  passed gameplay testing on BepInEx 6 and BepInEx 5.4.23.6.
- Synthetic pronunciation and emotional delivery vary by voice and line.
- Very short hesitation sounds such as `Hmh`, `Ahhh`, `Hmm`, `Um` and `Urgh?`
  can sound unnatural or occasionally be skipped.
- Kokoro cancellation occurs between generated chunks; the currently executing model chunk is not interrupted.
- Add-ons and voice packs are discovered at game startup rather than hot-installed.
- Existing saved character profiles do not automatically adopt later automatic-profile defaults.
- Future Obenseuer or Dialogue System updates may require revised hooks.

Dialogue interrupted when the player advances the conversation is not considered a backend failure.
