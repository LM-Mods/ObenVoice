# Source, credits and licence boundaries

## Original project source

The Obenseuer-specific C# plugin is under `src/plugin`. The local Python
backend is under `src/backend`. These files were independently implemented for
ObenVoice and are not derived from Universal TTS Mod.

The root MIT licence applies only to original ObenVoice code and documentation
over which LM-Mods is authorised to grant rights. It does not relicense
third-party libraries, models, voices, game material or character data.

## Character database

`characters.json` is a compilation of limited factual identity information from
a local Obenseuer 0.5.19 character registry scan. Legacy-only research names
are excluded. It is separately distributed under CC BY-SA 4.0 to the extent
the project may license the compilation. The source and terms are recorded in
`data/LICENSE.md` in the repository and `CHARACTER-DATA-LICENSE.md` beside the
database in Core.

## Third-party components

See `THIRD-PARTY-NOTICES.md`, `docs/RUNTIME-DEPENDENCIES.md`,
`docs/ESPEAK-NG-REDISTRIBUTION.md` and `docs/LICENSING-AUDIT.md`. Release
packages preserve the corresponding licence texts and package metadata.

## No game files or affiliation

Game and Unity assemblies are local compile-time references and are not
included in the source repository or Core release. Obenseuer and its
characters and trademarks belong to their respective owners. ObenVoice is an
unofficial independent community project and is not affiliated with or
endorsed by Loiste Interactive or the third-party projects it integrates.
