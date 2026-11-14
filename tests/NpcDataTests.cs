using ObenseuerTts;
using System;
using System.IO;
using System.Linq;

internal static class NpcDataTests
{
    private static void Expect(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("FAILED: " + label);
    }

    private static void Main(string[] args)
    {
        string databasePath = args.Length > 0 ? args[0] : Path.Combine("data", "characters.json");
        NpcIdentityDatabase database = NpcIdentityDatabase.Parse(File.ReadAllText(databasePath));
        Expect(database.Count == 292, "parses all 292 game-scanned character records");
        Expect(database.Records.Select(item => item.GameCharacterId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 292, "keeps every game character ID separate");
        Expect(database.Resolve("27", "Mr. Duck").Record.Gender == "unknown", "preserves the game unknown gender without guessing");
        Expect(database.Resolve("28", "Hostel Placeholder guy").Record.VerificationStatus == "test-placeholder", "identifies test and placeholder records");

        NpcIdentityMatch alias = database.Resolve(string.Empty, "Agnes");
        Expect(alias.Record != null && alias.Record.DisplayName == "Agnes Takala", "resolves a unique exact first name");
        NpcIdentityMatch repeatedFirstName = database.Resolve(string.Empty, "Teuvo");
        Expect(repeatedFirstName.Record == null && repeatedFirstName.Ambiguous, "rejects duplicate first name");
        Expect(database.Resolve("59", "Not Agnes").Record.DisplayName == "Agnes Takala", "game character ID takes priority over a mismatched name");

        string idJson = "{\"characters\":[" +
            "{\"internalName\":\"one\",\"displayName\":\"Shared\",\"gender\":\"male\",\"aliases\":[],\"gameCharacterId\":\"42\",\"verificationStatus\":\"verified\"}," +
            "{\"internalName\":\"two\",\"displayName\":\"Shared\",\"gender\":\"unknown\",\"aliases\":[]}" +
            "]}";
        NpcIdentityDatabase idDatabase = NpcIdentityDatabase.Parse(idJson);
        Expect(idDatabase.Resolve("42", "Shared").Record.InternalName == "one", "verified ID wins over ambiguous name");
        Expect(idDatabase.Resolve(string.Empty, "Shared").Ambiguous, "ambiguous exact name remains unresolved");
        Expect(idDatabase.Records[1].Gender == "unknown", "preserves missing/unknown gender safely");

        NpcVoiceOverridesDocument empty = NpcVoiceOverridesDocument.Parse(NpcVoiceOverridesDocument.EmptyJson);
        Expect(empty.Overrides.Count == 0, "loads generated empty overrides file");
        string overrideJson = "{\"version\":1,\"overrides\":[{\"internalName\":\"Agnes Takala\",\"displayName\":\"Agnes Takala\",\"voice\":\"af_test\",\"speed\":1.1,\"pitch\":-0.5}]}";
        NpcVoiceOverridesDocument overrides = NpcVoiceOverridesDocument.Parse(overrideJson);
        Expect(overrides.Overrides.Count == 1 && overrides.Overrides[0].Voice == "af_test", "loads valid override");
        Expect(NpcVoiceOverridesDocument.Parse(overrides.ToJson()).Overrides.Count == 1, "round-trips offline override edits");
        NpcVoiceOverridesDocument automaticDocument = NpcVoiceOverridesDocument.Parse("{\"version\":1,\"overrides\":[{\"speakerId\":\"dialogue:7\",\"displayName\":\"Unknown NPC\",\"voice\":\"am_test\",\"speed\":0.95,\"pitch\":0.2,\"automatic\":true,\"ignore\":false}]}");
        NpcVoiceEntry automaticEntry = automaticDocument.Overrides[0];
        Expect(automaticEntry.Automatic && automaticEntry.SpeakerId == "dialogue:7", "loads automatic assignment in the shared overrides document");
        Expect(automaticDocument.FindAny(string.Empty, "dialogue:7", "Unknown NPC", null) == automaticEntry, "Random Voices reuses a legacy saved assignment without rewriting it");
        Expect(automaticDocument.FindAutomatic(string.Empty, "dialogue:7", "Unknown NPC", null) == automaticEntry, "reuses automatic unknown-speaker assignment by reliable speaker ID");
        Expect(NpcVoiceOverridesDocument.Parse(automaticDocument.ToJson()).Overrides[0].Automatic, "round-trips automatic assignment marker");
        bool duplicateRejected = false;
        try { NpcVoiceOverridesDocument.Parse("{\"version\":1,\"overrides\":[{\"speakerId\":\"dialogue:7\",\"voice\":\"af_test\"},{\"speakerId\":\"dialogue:7\",\"voice\":\"am_test\",\"automatic\":true}]}"); }
        catch (FormatException) { duplicateRejected = true; }
        Expect(duplicateRejected, "rejects duplicate personal and automatic records for one identity");
        bool malformedRejected = false;
        try { NpcVoiceOverridesDocument.Parse("{\"version\":1,\"overrides\":[}"); }
        catch (FormatException) { malformedRejected = true; }
        Expect(malformedRejected, "rejects malformed overrides without repair");

        string temporary = Path.Combine("tests", "ObenVoice-NpcDataTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            string overridesPath = Path.Combine(temporary, "npc-voice-overrides.json");
            NpcVoiceOverridesLoadResult created = NpcVoiceOverridesStorage.Load(overridesPath, true);
            Expect(created.Valid && created.Created && File.Exists(overridesPath), "creates empty Overrides JSON on first startup (valid=" + created.Valid + ", created=" + created.Created + ", error=" + created.Error + ")");
            File.WriteAllText(overridesPath, overrideJson);
            NpcVoiceOverridesLoadResult offlineEdit = NpcVoiceOverridesStorage.Load(overridesPath, false);
            Expect(offlineEdit.Valid && offlineEdit.Document.Overrides.Count == 1, "loads valid offline file edit on next startup");
            string malformed = "{ invalid offline edit";
            File.WriteAllText(overridesPath, malformed);
            NpcVoiceOverridesLoadResult protectedLoad = NpcVoiceOverridesStorage.Load(overridesPath, true);
            Expect(!protectedLoad.Valid && File.ReadAllText(overridesPath) == malformed, "preserves malformed offline edit without overwrite");
        }
        finally
        {
            Directory.Delete(temporary, true);
        }

        string presetJson = "{\"presetVersion\":1,\"id\":\"expanded\",\"name\":\"Example Expanded\",\"provisional\":true,\"fallbackMode\":\"active\",\"assignments\":[{\"internalName\":\"Agnes Takala\",\"voice\":\"af_test\",\"speed\":1.15,\"pitch\":-0.4}]}";
        NpcVoicePreset preset = NpcVoicePreset.Parse(presetJson, "test-pack");
        Expect(preset.StableId == "test-pack/expanded" && preset.Provisional, "uses stable pack-qualified preset ID");
        Expect(preset.Find(string.Empty, "Agnes Takala", alias.Record).Voice == "af_test", "matches preset by canonical internal name");
        Expect(overrides.Find(string.Empty, "Agnes Takala", alias.Record).Voice == "af_test", "explicit override resolves for same NPC");
        NpcVoiceEntry overrideEntry = overrides.Find(string.Empty, "Agnes Takala", alias.Record);
        Expect(overrides.FindAny(string.Empty, string.Empty, "Agnes Takala", alias.Record) == overrideEntry, "Random Voices reuses an existing assignment without categories");
        NpcVoiceEntry presetEntry = preset.Find(string.Empty, "Agnes Takala", alias.Record);
        Expect(presetEntry.HasSpeed && presetEntry.HasPitch && Math.Abs(presetEntry.Speed - 1.15f) < 0.001f && Math.Abs(presetEntry.Pitch + 0.4f) < 0.001f, "preset schema preserves per-NPC speed and pitch");
        Expect(NpcVoiceResolutionPriority.Resolve(overrideEntry, presetEntry, automaticEntry) == NpcVoiceResolutionLayer.Override, "override wins over preset and automatic choice");
        Expect(NpcVoiceResolutionPriority.Resolve(null, presetEntry, automaticEntry) == NpcVoiceResolutionLayer.Preset, "preset wins over automatic choice");
        Expect(NpcVoiceResolutionPriority.Resolve(null, null, automaticEntry) == NpcVoiceResolutionLayer.SavedAutomatic, "saved automatic choice remains below presets");
        automaticDocument.Overrides.Remove(automaticEntry);
        Expect(automaticDocument.Overrides.Count == 0 && preset.Find(string.Empty, "Agnes Takala", alias.Record).Voice == "af_test", "individual reset removes only its personal record and does not change presets");

        NpcVoicePreset defaultPreset = NpcVoicePreset.Parse(File.ReadAllText(Path.Combine("data", "character-presets", "default.json")), "core");
        Expect(defaultPreset.StableId == "core/default" && defaultPreset.Assignments.Count == 292 && defaultPreset.FallbackMode == "active", "external Default loads the complete 292-character casting");
        Expect(defaultPreset.Assignments.Select(item => item.GameCharacterId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 292, "Default keeps all game character IDs unique");
        Expect(defaultPreset.Assignments.All(item => item.Voice.Length > 0 && item.HasSpeed && item.HasPitch), "Default supplies voice, speed and pitch for every casting entry");
        NpcVoicePreset britExpanded = NpcVoicePreset.Parse(File.ReadAllText(Path.Combine("data", "character-presets", "brit-expanded.json")), "british-test");
        NpcVoicePreset britPackOnly = NpcVoicePreset.Parse(File.ReadAllText(Path.Combine("data", "character-presets", "brit-pack-only.json")), "british-test");
        Expect(britExpanded.Name == "Brit - Expanded" && britPackOnly.Name == "Brit - Pack Only", "loads the agreed British preset display names");
        Expect(britExpanded.Assignments.Count == 292 && britPackOnly.Assignments.Count == 292, "British presets contain complete 292-character castings");
        Expect(britExpanded.Assignments.Select(item => item.GameCharacterId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 292 && britPackOnly.Assignments.Select(item => item.GameCharacterId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 292, "British presets keep every game character ID unique");
        NpcVoicePreset blankTemplate = NpcVoicePreset.Parse(File.ReadAllText(Path.Combine("data", "templates", "blank-292-character-casting.json")), "template");
        Expect(blankTemplate.Assignments.Count == 292, "blank casting template covers all 292 game identities");
        Expect(blankTemplate.Assignments.All(item => item.Voice.Length == 0 && !item.HasSpeed && !item.HasPitch), "blank template does not pretend to contain voice, speed or pitch assignments");
        Expect(blankTemplate.Find("59", "Agnes Takala", alias.Record) == null, "blank assignments are not treated as usable casting and cannot replace Default automatic behavior");

        Console.WriteLine("NPC database, preset and override tests passed");
    }
}
