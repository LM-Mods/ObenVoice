using System;
using ObenseuerTts;

internal static class SpeechTextRulesTests
{
    private static void Equal(string expected, string actual, string name)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new Exception(name + ": expected '" + expected + "' but got '" + actual + "'");
    }

    private static int Main()
    {
        Equal("Hmm...", SpeechTextRules.ApplyPronunciationReplacements("Hmh"), "standalone expression gains a natural pause");
        Equal("Hmm?... A landlord?", SpeechTextRules.ApplyPronunciationReplacements("Hmh?... A landlord?"), "dialogue example");
        Equal("Hmm Hmm Hmm", SpeechTextRules.ApplyPronunciationReplacements("hmh HMH HmH"), "case insensitive");
        Equal("Schmh Hmhology ahmh Hmhm", SpeechTextRules.ApplyPronunciationReplacements("Schmh Hmhology ahmh Hmhm"), "longer words unchanged");
        Equal(string.Empty, SpeechTextRules.ApplyPronunciationReplacements(null), "null safe");
        int warnings = 0;
        int count = SpeechTextRules.LoadPronunciationReplacements(
            "# custom rules\nHmh => Mmm\nDr. Pepper => Doctor Pepper\nbroken line",
            message => warnings++);
        if (count != 2 || warnings != 1) throw new Exception("custom rule parsing count/warning mismatch");
        Equal("Mmm...", SpeechTextRules.ApplyPronunciationReplacements("Hmh"), "custom short expression");
        Equal("Ask Doctor Pepper, not Dr. Peppercorn.", SpeechTextRules.ApplyPronunciationReplacements("Ask Dr. Pepper, not Dr. Peppercorn."), "boundary-aware phrase replacement");
        count = SpeechTextRules.LoadPronunciationReplacements("price => $1 and $& literal", message => warnings++);
        if (count != 1) throw new Exception("literal replacement parsing count mismatch");
        Equal("$1 and $& literal", SpeechTextRules.ApplyPronunciationReplacements("price"), "replacement regex tokens stay literal");

        EditableSpeechRuleDocument pronunciation = EditableSpeechRuleDocument.Parse("# keep\nHmh => Hmm\nbroken", false, message => warnings++);
        if (pronunciation.Entries.Count != 1 || !pronunciation.Serialize("# heading").Contains("broken"))
            throw new Exception("pronunciation editor did not preserve malformed content");
        EditableSpeechRuleDocument exclusions = EditableSpeechRuleDocument.Parse("Doorphone => Bzzzz!\n* => Alarm!", true, null);
        if (!SpeechExclusionRules.IsExcluded(exclusions.Entries, " doorphone ", "Bzzzz!"))
            throw new Exception("exact speaker/text exclusion did not match");
        if (!SpeechExclusionRules.IsExcluded(exclusions.Entries, "Someone", " Alarm! "))
            throw new Exception("text-only wildcard exclusion did not match");
        if (SpeechExclusionRules.IsExcluded(exclusions.Entries, "NPC", "I used the doorphone."))
            throw new Exception("unrelated dialogue was broadly excluded");
        EditableSpeechRuleDocument upgradedExclusions = EditableSpeechRuleDocument.Parse(
            "# existing file\nDoorphone => Bzzzz!\nNPC => Keep my custom rule", true, null);
        if (!SpeechExclusionDefaults.AddUpgradeDefaults(upgradedExclusions))
            throw new Exception("older exclusions were not migrated");
        if (upgradedExclusions.Entries.Count != 4 ||
            !SpeechExclusionRules.IsExcluded(upgradedExclusions.Entries, "NPC", "...") ||
            !SpeechExclusionRules.IsExcluded(upgradedExclusions.Entries, "Player", "aaaah!") ||
            !SpeechExclusionRules.IsExcluded(upgradedExclusions.Entries, "Doorphone", "Bzzzz!") ||
            !SpeechExclusionRules.IsExcluded(upgradedExclusions.Entries, "NPC", "Keep my custom rule"))
            throw new Exception("migration did not preserve and add exact exclusions");
        if (SpeechExclusionRules.IsExcluded(upgradedExclusions.Entries, "NPC", "Wait...") ||
            SpeechExclusionRules.IsExcluded(upgradedExclusions.Entries, "NPC", "AAAH!"))
            throw new Exception("migrated exclusions were too broad");

        string upgradedContent = upgradedExclusions.Serialize("# heading");
        EditableSpeechRuleDocument upgradedReload = EditableSpeechRuleDocument.Parse(upgradedContent, true, null);
        if (SpeechExclusionDefaults.AddUpgradeDefaults(upgradedReload))
            throw new Exception("migration marker did not prevent repeated defaults");
        upgradedReload.Entries.RemoveAll(entry => entry.Second == "...");
        EditableSpeechRuleDocument deliberateRemoval = EditableSpeechRuleDocument.Parse(upgradedReload.Serialize("# heading"), true, null);
        if (SpeechExclusionDefaults.AddUpgradeDefaults(deliberateRemoval) ||
            SpeechExclusionRules.IsExcluded(deliberateRemoval.Entries, "NPC", "..."))
            throw new Exception("deliberately removed exclusion was silently restored");

        EditableSpeechRuleDocument restoredDefaults = EditableSpeechRuleDocument.Parse(SpeechExclusionDefaults.DefaultContent, true, null);
        if (restoredDefaults.Entries.Count != 3 ||
            !SpeechExclusionRules.IsExcluded(restoredDefaults.Entries, "Doorphone", "Bzzzz!") ||
            !SpeechExclusionRules.IsExcluded(restoredDefaults.Entries, "NPC", "Bzzzz!") ||
            !SpeechExclusionRules.IsExcluded(restoredDefaults.Entries, "Player", "...") ||
            !SpeechExclusionRules.IsExcluded(restoredDefaults.Entries, "NPC", "AAAAH!"))
            throw new Exception("restored defaults did not contain all three exclusions");
        pronunciation.Entries.Add(new EditableSpeechRule("testword", "test word"));
        pronunciation.Entries[1] = new EditableSpeechRule("edited", "spoken edit");
        EditableSpeechRuleDocument persisted = EditableSpeechRuleDocument.Parse(pronunciation.Serialize("# heading"), false, null);
        if (persisted.Entries.Count != 2 || persisted.Entries[1].First != "edited")
            throw new Exception("pronunciation add/edit persistence failed");
        persisted.Entries.RemoveAt(1);
        if (EditableSpeechRuleDocument.Parse(persisted.Serialize("# heading"), false, null).Entries.Count != 1)
            throw new Exception("pronunciation delete persistence failed");
        EditableSpeechRuleDocument resetPronunciation = EditableSpeechRuleDocument.Parse("Hmh => Hmm", false, null);
        if (resetPronunciation.Entries.Count != 1 || exclusions.Entries.Count != 2)
            throw new Exception("pronunciation reset was not independent from exclusions");
        Console.WriteLine("speech pronunciation replacement tests passed");
        return 0;
    }
}
