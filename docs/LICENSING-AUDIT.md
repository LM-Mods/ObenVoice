# ObenVoice licensing and redistribution audit

**Audit date:** 14 November 2026

**Scope:** public source repository, Core distribution and optional add-ons

**Nature:** release-compliance record, not legal advice or legal certification

## Release readiness

| Target | Status | Finding |
|---|---|---|
| Public GitHub source | **PASS** | MIT with `Copyright (c) 2026 LM-Mods` is approved; separate third-party and data terms are present. |
| Core GitHub Release | **SOURCE EVIDENCE PASS; LIVE LINK UPDATE PENDING** | The frozen v1.0.2-beta.1 archive passed integrity, notice and privacy checks and gameplay testing on BepInEx 6 and BepInEx 5.4.23.6. GPL/LGPL source locations are version-pinned. Absolute links are prepared in the repository release description, but still require a separately approved edit to the live GitHub release; continued source availability must then be maintained. |
| Core v1.0.3-beta.1 candidate | **PASS FOR PRIVATE RELEASE PREPARATION** | Rowan and Mara retain the Apache-2.0 licence, derivative modification notice, exact hashes and the documented Stalburg/Kokoro provenance chain. Exact regeneration is not claimed because their generation program was not supplied. Final publication remains subject to owner approval. |
| Core Nexus Mods release | **PASS WITH UPLOAD-TIME CHECKS** | Core is prepared; Nexus scanning and the AI-Generated Content classification remain upload-time actions. |
| British voice pack v1.0.1 | **PASS** | Both unmodified files match the pinned Apache-2.0 Kokoro revision; attribution and licence accompany the pack. |
| Stalburg Voices Vol. 1 v1.0.1 | **PASS** | The 20 derivative tensors have per-file source/output provenance, generator source, Apache-2.0 text and upstream dataset acknowledgements. |

No status in this table authorises changing published assets or release tags.

## Original project material

The curated C# plugin, Python backend, tests, documentation and packaging tools
were independently developed for Obenseuer. The reviewed backend files state
that they are not derived from Universal TTS Mod, and no Universal TTS Mod
source was identified in the curated repository. This is a finding about the
material reviewed, not a guarantee concerning unavailable material.

The approved project licence is MIT with `Copyright (c) 2026 LM-Mods`. LM-Mods
is a usable public identity if the person or organisation controlling it owns
or is authorised to license the original project material. The owner approved
the wording on 8 November 2026. AI-generated fragments are not
represented as necessarily exclusive or independently copyrightable; the
licence grants whatever rights the licensor is authorised to grant.

Third-party libraries, models, voices and data are not covered by the project
MIT licence.

## Component inventory and treatment

| Component | Version/revision | Licence | Evidence and treatment |
|---|---|---|---|
| Original ObenVoice material | v1.0.3-beta.1 source | MIT | Root licence approved for LM-Mods |
| `characters.json` | 292 game-registry records | CC BY-SA 4.0 compilation terms | Includes 282 game-scanned records, two records flagged for game-scan review and eight game test/placeholder entries; provenance and data boundaries are in `data/LICENSE.md` |
| Obenseuer Modding Framework API | 0.12.1 reference | MIT | Referenced for integration; framework and game assemblies are not bundled |
| Python embedded runtime | 3.12.10 | PSF and bundled component terms | Complete runtime licence file retained |
| Kokoro / Misaki | 0.9.4 / 0.9.4 | Apache-2.0 | Package licences retained; used without source modifications except normal configuration/integration |
| Kokoro-82M model and six Core voices | `f3ff3571791e39611d31c381e3a41a3af07b4987` | Apache-2.0 repository declaration | Model, voice hashes, source and licence retained |
| PyTorch | 2.15.0 | BSD-style and bundled third-party terms | PyTorch and vendored notices retained |
| NumPy and scientific stack | Inventory versions | BSD and other permissive terms | Package metadata and collected notices retained |
| Transformers | Inventory version | Apache-2.0 | Package licence retained |
| Tokenizers | 0.23.3 | Apache-2.0 | Explicit component record added; complete Apache-2.0 text accompanies Core |
| Loguru | 0.7.3 | MIT | Exact upstream 0.7.3 MIT notice added |
| num2words | 0.5.14 | LGPL | Source package and licence retained |
| certifi | Inventory version | MPL-2.0 | Source/data and notice retained |
| SoundDevice / PortAudio | Inventory versions | MIT / MIT-style | Notices retained |
| SoundFile / libsndfile | 0.14.0 / 1.2.2 | BSD / LGPL-2.1 | SoundFile metadata and licence retained; libsndfile is a separate DLL with its LGPL text and exact source location recorded |
| eSpeak NG | 1.52.0, commit `4870adf` | GPL-3.0 plus component terms | Unmodified binary/data; exact tagged corresponding source and notices recorded |
| phonemizer-fork | 3.3.2 | GPL-3.0-or-later | Unmodified Python source package and GPL text retained |
| espeakng-loader | 0.2.4 | Component-specific; carries eSpeak binary/data | Loader source, package metadata, wheel hash and eSpeak obligations recorded |
| British `bf_emma` | Kokoro revision `f3ff357…` | Apache-2.0 | Unmodified; SHA-256 `D0A423DEABF4A52B4F49318C51742C54E21BB89BBBE9A12141E7758DDB5DA701` |
| British `bm_george` | Kokoro revision `f3ff357…` | Apache-2.0 | Unmodified; SHA-256 `F1BC812213DC59774769E5C80004B13EEB79BD78130B11B2D7F934542DAB811B` |
| Stalburg Voices Vol. 1 | 20 v1.0.1 derivative tensors | Apache-2.0 upstream declaration; source-dataset attribution retained | Output and source-anchor hashes, transformation parameters and generator source are recorded in the pack's `PROVENANCE.json`, `NOTICE.txt` and `GENERATOR.py` |
| Rowan and Mara player voices | v1.0.3-beta.1 Core derivatives | Apache-2.0 inherited through the documented Stalburg/Kokoro chain | Two seeded procedural style tensors derived from all 20 Stalburg tensors; output and contributor hashes, pinned upstream anchors, modification notice and inherited acknowledgements are retained. The generation program was not supplied, so exact regeneration and independent training are not claimed. |

The complete Python package/version inventory is in
`docs/RUNTIME-DEPENDENCIES.md`. Package metadata and collected licence files
must remain in every Core release; an inventory row alone is not treated as a
substitute for the required licence text or notice.

## Character data

The current character database is a compilation of limited factual identity
information generated from a local Obenseuer 0.5.19 character-registry scan.
It contains 292 game-registry records: 282 game-scanned records, two records
flagged for game-scan review and eight game test/placeholder entries. It is not
a claim that all 292 are ordinary, encounterable or fully verified NPCs.
Scanner diagnostics, personal paths and research prose are excluded.

The database-specific attribution, modification statement and share-alike
notice are in `data/LICENSE.md` and must accompany the data in Core. This does
not place the program code under CC BY-SA.

## GPL and LGPL compliance

The Core package identifies eSpeak NG 1.52.0, phonemizer-fork 3.3.2 and
espeakng-loader 0.2.4 precisely. No upstream modifications were identified.
Full GPL and component licence texts are retained. The focused review verified
the eSpeak release commit and archive, phonemizer-fork's hashed 3.3.2 source
distribution and espeakng-loader's exact 0.2.4 source commit. PyPI publishes no
0.2.4 loader sdist, so the pinned upstream commit and archive are recorded.

The packaged libsndfile DLL reports version 1.2.2, is dynamically loaded and
retains its LGPL-2.1 text; num2words 0.5.14 is installed as Python source and
retains its LGPL-2.1 text. Their exact source locations are also recorded in
`docs/ESPEAK-NG-REDISTRIBUTION.md`.

For network distribution under GPLv3 section 6(d), clear source directions
must be kept next to the binary download and the distributor must ensure that
equivalent source access remains available for as long as required. The public
Core release description therefore needs a direct link to the redistribution
record. Upstream hosting is acceptable only while that access is maintained;
it is not a permanent guarantee. A written source offer is not being used as a
substitute, and no formal legal certification is claimed.

The components are distributed as identifiable runtime packages rather than
incorporated source modifications to ObenVoice. This packaging supports the
project's MIT treatment, but this audit is not a formal legal opinion on every
possible copyleft interpretation.

## Privacy and release binary

The frozen v1.0.2-beta.1 candidate excludes Python bytecode caches, logs, user
settings, Overrides and PDB files. Its deterministic, path-mapped shared DLL
has SHA-256
`27CE12D012A048EDF89E44821D5FCC75F86BEB929AE7F1C9FD8A616D268FC230`.
A clean source rebuild reproduced those exact DLL bytes. The frozen DLL passed
gameplay testing on BepInEx 6 and BepInEx 5.4.23.6, including NPC/player
dialogue, F8/F9 functionality, backend operation and clean shutdown.

For v1.0.3-beta.1, the project owner reports gameplay approval of the combined
Random Voices and dedicated-player-voice candidate on both supported loaders.
The final candidate must still pass its recorded archive, path, user-data and
privacy checks before publication.

## Optional voice packs

The British voice files match the pinned Kokoro revision and retain the
Apache-2.0 licence and attribution alongside the download. No modifications to
the weights were identified.

Stalburg Voices Vol. 1 contains 20 procedurally modified Kokoro-compatible
style tensors. Its distribution records exact source anchors and output hashes,
transformation parameters, generator source, the Apache-2.0 text and the SIWIS
and Koniwa acknowledgements inherited from upstream model documentation.

## Platform compliance

GitHub source and release pages should link this audit and the third-party
notices prominently. A public repository must include the project licence and
the separate data terms.

Nexus Mods requires accurate attribution and AI disclosure. Because OpenAI
Codex produced the majority of the code, the planned upload should select
**AI-Generated Content**, not AI Assisted. The main description must name the
tool and the affected output, link the public source/audit, explain the local
Python runtime, and state that Core operates locally using loopback rather than
a cloud speech service. Platform scanning and moderation remain upload-time
checks and are not recorded as passed here.

## Final assessment

Core v1.0.2-beta.1, British Voices v1.0.1 and Stalburg Voices Vol. 1 v1.0.1
were published on 11 November 2026. The proposed v1.0.3-beta.1 replacement
Core adds the approved Random Voices system and Rowan/Mara player voices.
Redistribution is supportable on the recorded Apache-2.0 provenance and notice
treatment, with the explicit reproducibility limitation above. This is not a
legal certification. Publication remains blocked until the owner separately
approves the prepared source and verified release candidate.
