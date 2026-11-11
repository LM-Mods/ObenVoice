# ObenVoice licensing and redistribution audit

**Audit date:** 11 November 2026

**Scope:** public source repository, Core distribution and optional add-ons

**Nature:** release-compliance record, not legal advice or legal certification

## Release readiness

| Target | Status | Finding |
|---|---|---|
| Public GitHub source | **PASS** | MIT with `Copyright (c) 2026 LM-Mods` is approved; separate third-party and data terms are present. |
| Core GitHub Release | **PASS** | The frozen v1.0.2-beta.1 archive passed integrity, licensing and privacy checks and gameplay testing on BepInEx 6 and BepInEx 5.4.23.6. |
| Core Nexus Mods release | **PASS WITH UPLOAD-TIME CHECKS** | Core is prepared; Nexus scanning and the AI-Generated Content classification remain upload-time actions. |
| British voice pack v1.0.1 | **PASS** | Both unmodified files match the pinned Apache-2.0 Kokoro revision; attribution and licence accompany the pack. |
| Stalburg Voices Vol. 1 v1.0.1 | **PASS** | The 20 derivative tensors have per-file source/output provenance, generator source, Apache-2.0 text and upstream dataset acknowledgements. |

No status in this table authorises an upload or publication.

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
| Original ObenVoice material | v1.0.2-beta.1 source | MIT | Root licence approved for LM-Mods |
| `characters.json` | 292 game-scanned identity records | CC BY-SA 4.0 compilation terms | Local Obenseuer 0.5.19 registry-scan provenance and data boundaries recorded in `data/LICENSE.md` |
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
| SoundFile / libsndfile | Inventory versions | BSD / LGPL | Notices and applicable source/package material retained |
| eSpeak NG | 1.52.0, commit `4870adf` | GPL-3.0 plus component terms | Unmodified binary/data; exact tagged corresponding source and notices recorded |
| phonemizer-fork | 3.3.2 | GPL-3.0-or-later | Unmodified Python source package and GPL text retained |
| espeakng-loader | 0.2.4 | Component-specific; carries eSpeak binary/data | Loader source, package metadata, wheel hash and eSpeak obligations recorded |
| British `bf_emma` | Kokoro revision `f3ff357…` | Apache-2.0 | Unmodified; SHA-256 `D0A423DEABF4A52B4F49318C51742C54E21BB89BBBE9A12141E7758DDB5DA701` |
| British `bm_george` | Kokoro revision `f3ff357…` | Apache-2.0 | Unmodified; SHA-256 `F1BC812213DC59774769E5C80004B13EEB79BD78130B11B2D7F934542DAB811B` |
| Stalburg Voices Vol. 1 | 20 v1.0.1 derivative tensors | Apache-2.0 upstream declaration; source-dataset attribution retained | Output and source-anchor hashes, transformation parameters and generator source are recorded in the pack's `PROVENANCE.json`, `NOTICE.txt` and `GENERATOR.py` |

The complete Python package/version inventory is in
`docs/RUNTIME-DEPENDENCIES.md`. Package metadata and collected licence files
must remain in every Core release; an inventory row alone is not treated as a
substitute for the required licence text or notice.

## Character data

The current character database is a compilation of limited factual identity
information generated from a local Obenseuer 0.5.19 character-registry scan.
It contains 292 scanned game character IDs, displayed names and game-provided
genders. Scanner diagnostics, personal paths and research prose are excluded.

The database-specific attribution, modification statement and share-alike
notice are in `data/LICENSE.md` and must accompany the data in Core. This does
not place the program code under CC BY-SA.

## GPL and LGPL compliance

The Core package identifies eSpeak NG 1.52.0, phonemizer-fork 3.3.2 and
espeakng-loader 0.2.4 precisely. No upstream modifications were identified.
Full GPL and component licence texts are retained. Exact tagged/source archive
locations and integrity records are in `docs/ESPEAK-NG-REDISTRIBUTION.md`.

For network distribution, the exact corresponding-source directions must be
kept beside or clearly linked from the binary download and remain available
for as long as required. Preserving matching source archives as release assets
is the most robust option, but publication of such assets has not been
performed or authorised. A written source offer is not being used as a
substitute.

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

The documentation and source attribution are current. The LM-Mods MIT notice
is approved, the frozen DLL passed both supported loaders, and the staged Core
passed licence, privacy, install and upgrade checks. Core v1.0.2-beta.1,
British Voices v1.0.1 and Stalburg Voices Vol. 1 v1.0.1 are release-ready,
subject to the owner's separate publication approval.
