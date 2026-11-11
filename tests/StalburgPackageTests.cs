using ObenseuerTts;
using System;
using System.Collections.Generic;
using System.IO;

internal static class StalburgPackageTests
{
    private static void Expect(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("FAILED: " + label);
    }

    private static void Main()
    {
        string root = Path.Combine("packaging", "stalburg-voices");
        VoicePackManifest manifest = VoicePackJson.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
        Expect(manifest.id == "oben-npc-voice-lab-20", "preserves established pack ID");
        Expect(manifest.shortCode == "SV1", "loads SV1 friendly short code");
        Expect(manifest.voices.Length == 20, "contains 20 voice manifest entries");
        HashSet<string> voices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int male = 0;
        int female = 0;
        foreach (VoicePackVoice voice in manifest.voices)
        {
            voices.Add(voice.id);
            if (voice.gender == "male") male++;
            if (voice.gender == "female") female++;
        }
        Expect(male == 10 && female == 10, "contains 10 male and 10 female voices");

        NpcVoicePreset centralExpanded = NpcVoicePreset.Parse(File.ReadAllText(Path.Combine("data", "character-presets", "sv1-expanded.json")), manifest.id);
        NpcVoicePreset centralPackOnly = NpcVoicePreset.Parse(File.ReadAllText(Path.Combine("data", "character-presets", "sv1-pack-only.json")), manifest.id);
        Expect(centralExpanded.Name == "SV1 - Expanded" && centralPackOnly.Name == "SV1 - Pack Only", "uses the agreed SV1 preset display names");
        Expect(centralExpanded.StableId == manifest.id + "/mixed" && centralExpanded.Assignments.Count == 292 && centralExpanded.FallbackMode == "active", "central Expanded preset preserves its stable ID and complete casting");
        Expect(centralPackOnly.StableId == manifest.id + "/pack-only" && centralPackOnly.Assignments.Count == 292 && centralPackOnly.FallbackMode == "active", "central Pack Only preset preserves its stable ID and complete casting");
        int expandedGameIds = 0;
        int packOnlyGameIds = 0;
        foreach (NpcVoiceEntry assignment in centralExpanded.Assignments) if (assignment.GameCharacterId.Length > 0) expandedGameIds++;
        foreach (NpcVoiceEntry assignment in centralPackOnly.Assignments) if (assignment.GameCharacterId.Length > 0) packOnlyGameIds++;
        Expect(expandedGameIds == 292 && packOnlyGameIds == 292, "central presets attach every authoritative game character ID");
        HashSet<string> coreVoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "af_bella", "af_heart", "af_sarah", "am_adam", "am_michael", "am_onyx"
        };
        foreach (NpcVoiceEntry assignment in centralExpanded.Assignments) Expect(voices.Contains(assignment.Voice) || coreVoices.Contains(assignment.Voice), "Expanded references an installed SV1 or Core voice");
        foreach (NpcVoiceEntry assignment in centralPackOnly.Assignments) Expect(voices.Contains(assignment.Voice), "Pack Only references installed manifest voice");
        Console.WriteLine("Stalburg manifest and provisional preset tests passed");
    }
}
