# Development checkpoint

Updated: 9 November 2026

## Repository state

- Branch: `dev/f8-webui-diagnostics`
- Latest implementation commit: `750c9a8` (`Refine voice controls and diagnostic workflow`)
- Confirmed-exclusion commit: `282498e` (`Filter confirmed non-speech dialogue`)
- Public baseline: `aa9f00d` on `main`
- Public release files, frozen test baseline and remote repository were not changed.

## Completed

- Added exact built-in filtering for standalone `...`/Unicode ellipsis and `AAAAH!` for NPC and selected-player lines without broad grunt/exclamation filtering.
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
- Speech text/pronunciation tests: passed, including narrow ellipsis and `AAAAH!` cases.
- Unknown-speaker tracker tests: passed.
- Dialogue diagnostics tests: passed.
- Voice selection tests: passed, including stable compatible Random selection.
- Speaker announcement tests: passed.
- Voice-pack manifest test: passed.
- Mock backend shutdown and same-port restart tests: passed with localhost socket permission.

## Framework recommendation

Retain Obenseuer Modding Framework / ObenseuerCore 0.12.1.

The plugin directly uses `FrameworkMod` lifecycle, mod/dependency metadata, framework logging and many `ModConfig`/`ModSetting` bindings that also provide optional WebUI exposure. Harmony patching itself is already explicit. Removing the framework is technically possible, and losing WebUI is acceptable, but it would require replacing the lifecycle/bootstrap, logger, configuration migration/ranges and dependency handling. Most importantly, it would introduce a new BepInEx 5/6 compatibility layer where the current indirect reference produces one identical DLL. The maintenance and regression cost outweighs one fewer dependency at this stage.

## Known limitations and gameplay checks

- Visually verify the revised F8 General/Advanced/Pronunciation layouts and F9 compact selected row.
- Confirm Apply changes engines, while manual restart still restarts the current engine.
- Confirm Random remains assigned after a restart when Remember is enabled and that `unknown-speakers.tsv` is reviewable.
- Confirm End writes beside `tts_server.log`, displays the short notification and Clear Log requires confirmation.
- Confirm standalone ellipsis and exact `AAAAH!` no longer synthesize while the game's original sound remains unchanged.
- No GitHub push, release publication, Nexus upload or public artifact update has been performed.

## Local development output

The unzipped overlay is prepared under `outputs/ObenVoice-Development-750c9a8` with one DLL in the normal game-root plugin path. It intentionally reuses the existing installed Core backend, models, configuration and add-ons rather than duplicating unchanged runtime data.
