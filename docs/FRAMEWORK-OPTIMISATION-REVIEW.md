# Focused Modding Framework optimisation review

Reviewed against ObenseuerCore 0.12.1 on 9 November 2026. No framework refactoring was implemented.

## Recommendation

Keep the current framework integration. ObenVoice already uses the valuable parts: `FrameworkMod` lifecycle, mod/dependency metadata, `CoreLog`, `ModConfig`/`ModSetting`, numeric setting ranges and optional WebUI exposure. Harmony dialogue/audio hooks remain custom because the framework does not expose equivalent subtitle, confirmed-response or audio-source events.

## Ranked opportunities

### 1. Use `ModSetting<T>.Changed` for derived setting state — worthwhile

ObenVoice rebuilds its voice pools at startup and from its own F8 voice-pack controls. Framework `Changed` callbacks could refresh the derived pools when the same settings are edited through optional WebUI. This is a small, targeted change with a real consistency benefit. Effort is low; regression risk is low-to-medium because callbacks must avoid recursive writes and unnecessary assignment changes.

### 2. Evaluate `Events.SceneLoaded` and `Dialogue.Started`/`Ended` for transition state — conditionally worthwhile

ObenVoice currently polls scene and conversation state every 0.25 seconds to clear duplicate/speaker-announcement state. Framework events could simplify that transition code. Effort is low-to-medium, but regression risk is medium because event timing and coverage must be checked against ordinary conversations, barks and scene changes before removing the gameplay-tested fallback. No CPU or latency benefit is claimed without measurement.

### 3. Replace string hotkeys with `ModKeybind`/`Keybinds` — not currently worthwhile

The framework provides registered keybinds and `Down`/`Held` helpers. ObenVoice already has working F8/F9/End rebinding, conflict prevention and custom-menu presentation using existing persisted string keys. Migration and UI changes would be larger than the maintenance benefit and could disrupt configuration compatibility.

### 4. Remove compatibility logging/config wrappers — cosmetic only

`CompatibilityLogger` is a thin naming adapter over `CoreLog`, and `ModConfigCompatibility.Save` documents that framework settings already save on assignment. Removing the no-op calls and adapter could shorten code but would not materially improve reliability or performance. Defer until related code is otherwise being changed.

## Keep custom

- Structured bounded dialogue diagnostics and unknown-speaker tracking: framework logging does not provide these domain-specific file formats or retention behavior.
- Speaker assignments and editable pronunciation/exclusion files: these are user-editable shared runtime data, not ordinary scalar mod settings or per-save game data.
- F8/F9/popup input and camera suppression: the current behavior is gameplay-tested; available UI acquire/release APIs do not demonstrate an equivalent replacement for these custom windows.
- Dynamic installed-engine and voice lists: `ModConfig.Bind` exposes scalar/ranged settings but no dynamic option-list metadata suitable for the discovered add-ons.
