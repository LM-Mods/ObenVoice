# ObenVoice licensing and redistribution audit

**Audit date:** 8 November 2026  
**Scope:** public source repository, Core distribution and optional add-ons  
**Nature:** release-compliance record, not legal advice or legal certification

## Release readiness

| Target | Status | Finding |
|---|---|---|
| Public GitHub source | **PASS** | MIT with `Copyright (c) 2026 LM-Mods` is approved; separate third-party and data terms are present. |
| Core GitHub Release | **PASS** | Licensing/privacy corrections, BepInEx 6 gameplay testing and the BepInEx 5 compatibility-reference build are complete; final archive integrity was verified during packaging. |
| Core Nexus Mods release | **PASS WITH UPLOAD-TIME CHECKS** | Core is prepared; Nexus scanning and the AI-Generated Content classification remain upload-time actions. |
| British voice pack | **PASS** | Both unmodified files match the pinned Apache-2.0 Kokoro revision. |

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
| Original ObenVoice material | 4.1.1 source | MIT | Root licence approved for LM-Mods |
| `characters.json` | 88 adapted records | CC BY-SA 4.0 | Stalburg Wiki source, changes and share-alike treatment recorded in `data/LICENSE.md` |
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

The complete Python package/version inventory is in
`docs/RUNTIME-DEPENDENCIES.md`. Package metadata and collected licence files
must remain in every Core release; an inventory row alone is not treated as a
substitute for the required licence text or notice.

## Character data

Limited factual character-identification information was adapted from the
[Stalburg Wiki Obenseuer/Characters page](https://stalburg.net/Obenseuer/Characters),
whose footer applies CC BY-SA 4.0 unless otherwise noted. ObenVoice transformed
that information into JSON and supplemented names, aliases and gender
classifications through in-game curation. Wiki prose was not copied into the
database.

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

The accepted gameplay-tested DLL and Python caches contained private local
build metadata. They remain untouched in the protected checkpoint and are not
suitable for public distribution.

The separate release candidate excludes Python bytecode caches and uses a DLL
compiled from the same source with deterministic, path-mapped, no-PDB settings.
Its SHA-256 is
`59A0B0F5BE6BCD9C55029CCE84E279038B1DFFAE0C950B161D38AA781C5AF265`.
The BepInEx 5-reference and BepInEx 6-reference builds were byte-identical.
An automated metadata comparison found no differences across 447 assembly,
reference, type, field and method/IL records relative to the accepted DLL.
The replacement passed focused gameplay testing on BepInEx 5 and BepInEx 6,
including NPC/player dialogue, F8/F9 functionality and clean Steam shutdown.

## Optional voice pack

The British voice files match the pinned Kokoro revision and retain the
Apache-2.0 licence and attribution alongside the download. No modifications to
the weights were identified.

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

The documentation, source attribution and missing Loguru/Tokenizers records
have been corrected. The LM-Mods MIT notice is approved, the sanitised DLL
passed both supported loaders, and the staged Core passed licence, privacy,
install and upgrade checks. Core and the British voices are release-ready.
