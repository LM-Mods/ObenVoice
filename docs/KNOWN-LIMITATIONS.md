# Known limitations

- The path-sanitised release DLL was gameplay-tested on BepInEx 5 and BepInEx 6.
- Synthetic pronunciation and emotional delivery vary by voice and line.
- Punctuation-only dialogue can produce an unwanted brief vocalisation.
- Very short hesitation sounds such as `Hmh`, `Ahhh`, `Hmm`, `Um` and `Urgh?`
  can sound unnatural or occasionally be skipped.
- The Reset Pronunciation Defaults label can clip in the current F8 layout.
- Kokoro cancellation occurs between generated chunks; the currently executing model chunk is not interrupted.
- Add-ons and voice packs are discovered at game startup rather than hot-installed.
- Existing saved character profiles do not automatically adopt later automatic-profile defaults.
- Future Obenseuer or Dialogue System updates may require revised hooks.

Dialogue interrupted when the player advances the conversation is not considered a backend failure.
