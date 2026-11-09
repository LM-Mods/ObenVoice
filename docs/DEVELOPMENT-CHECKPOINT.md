# Development checkpoint

Updated: 9 November 2026

## Repository state

- Branch: `dev/f8-webui-diagnostics`
- Latest implementation commit: `3d7941b` (`Consolidate confirmed speech exclusions`)
- Earlier functional commits: `282498e`, `750c9a8`
- Public baseline: `aa9f00d` on `main`
- Public release files, frozen test baseline and remote repository were not changed.

## Completed

- Consolidated standalone `...` and exact `AAAAH!` into the editable Any-speaker exclusion defaults alongside `Doorphone => Bzzzz!`; the separate hardcoded filter was removed.
- Added a version-marked one-time exclusion migration that preserves custom rules and does not restore an entry after the user deliberately removes it.
- Added F8 General **Apply** beside Voice Engine; it reuses Restart Voice Engine and is available only when the installed selection differs from the running/launched engine.
- Retained manual Restart Voice Engine in Advanced and kept F9 focused on player voices.
- Made F9 voice rows compact, single-line, scrollable and visibly marked when selected.
- Added stable Random to the unknown-speaker popup alongside Male, Female, Choose Voice, Use Default and Ignore; remembered assignments use the existing persistence path.
- Added bounded, deduplicated `ObenseuerTTS/unknown-speakers.tsv` tracking with unresolved identity status, timestamps, occurrence count, example dialogue, chosen voice/method/gender and remember state. It never edits `characters.json`.
- Moved `dialogue-diagnostics.log` beside `tts_server.log` under `ObenseuerTTS`.
- Added a non-interrupting 2.5-second top-left flag success/error notification reporting the actual event count.
- Removed Open Log and added Cancel-first confirmation before Clear Log.
- Moved character database, reload and saved-assignment maintenance controls from Data & Status to Advanced; retained the unknown-speaker option in Data & Status.
- Widened Reset Pronunciation Defaults so its label is visible.
- Replaced remaining generic-use Pocket identifiers/labels with engine terminology while preserving the legacy persisted key `SelectedPocketVoicePack` for compatibility.
- Updated usage, troubleshooting and known-limitations documentation.

## Verification

- Default .NET Framework 4.7.2 release build: passed, 0 warnings.
- BepInEx 5 compatibility-reference release build: passed, 0 warnings.
- Default and BepInEx 5 reference builds produce one identical deterministic DLL.
- Speech text/pronunciation tests: passed, including NPC/player wildcard matching, migration preservation, one-time behavior, deliberate removal and default restoration.
- Unknown-speaker tracker tests: passed.
- Dialogue diagnostics tests: passed.
- Voice selection tests: passed, including stable compatible Random selection.
- Speaker announcement tests: passed.
- Voice-pack manifest test: passed.
- Mock backend shutdown and same-port restart tests: passed with localhost socket permission.

## Framework recommendation

Retain Obenseuer Modding Framework / ObenseuerCore 0.12.1.

The plugin directly uses `FrameworkMod` lifecycle, mod/dependency metadata, framework logging and many `ModConfig`/`ModSetting` bindings that also provide optional WebUI exposure. Harmony patching itself is already explicit. Removing the framework is technically possible, and losing WebUI is acceptable, but it would require replacing the lifecycle/bootstrap, logger, configuration migration/ranges and dependency handling. Most importantly, it would introduce a new BepInEx 5/6 compatibility layer where the current indirect reference produces one identical DLL. The maintenance and regression cost outweighs one fewer dependency at this stage.

The focused optimisation review is recorded in `docs/FRAMEWORK-OPTIMISATION-REVIEW.md`. The only clearly worthwhile candidate is using `ModSetting<T>.Changed` to keep derived voice-pool state consistent after optional WebUI edits. Framework scene/dialogue events are a conditional future simplification; migrating working hotkeys or custom structured logs is not currently worthwhile.

## Known limitations and gameplay checks

- BepInEx 6 gameplay testing passed for the prior UI, engine, unknown-speaker and diagnostic changes, including suppression of `...` and `AAAAH!`.
- Focused follow-up: confirm the three defaults appear in F8 Speech Exclusions, an upgraded custom file gains the two new Any-speaker entries once, removing one remains respected, and Restore Default Exclusions restores all three.
- No GitHub push, release publication, Nexus upload or public artifact update has been performed.

## Local development output

The unzipped overlay is prepared under `outputs/ObenVoice-Development-3d7941b` with one DLL in the normal game-root plugin path. It intentionally reuses the existing installed Core backend, models, configuration and add-ons rather than duplicating unchanged runtime data.
