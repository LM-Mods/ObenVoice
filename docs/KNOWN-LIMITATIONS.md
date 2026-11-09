# Known limitations

- One shared plugin DLL targets BepInEx 5 and BepInEx 6. Routine development
  gameplay testing uses BepInEx 6; BepInEx 5 is also checked by a separate
  compatibility-reference build.
- Synthetic pronunciation and emotional delivery vary by voice and line.
- Very short hesitation sounds such as `Hmh`, `Ahhh`, `Hmm`, `Um` and `Urgh?`
  can sound unnatural or occasionally be skipped.
- Kokoro cancellation occurs between generated chunks; the currently executing model chunk is not interrupted.
- Add-ons and voice packs are discovered at game startup rather than hot-installed.
- Existing saved character profiles do not automatically adopt later automatic-profile defaults.
- Future Obenseuer or Dialogue System updates may require revised hooks.

Dialogue interrupted when the player advances the conversation is not considered a backend failure.
