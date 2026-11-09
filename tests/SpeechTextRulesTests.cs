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
        if (!SpeechTextRules.IsConfirmedNonSpeech("...") || !SpeechTextRules.IsConfirmedNonSpeech("……"))
            throw new Exception("standalone ellipsis was not excluded");
        if (!SpeechTextRules.IsConfirmedNonSpeech("AAAAH!") || !SpeechTextRules.IsConfirmedNonSpeech("aaaah!"))
            throw new Exception("confirmed AAAAH expression was not excluded");
        if (SpeechTextRules.IsConfirmedNonSpeech("Wait...") || SpeechTextRules.IsConfirmedNonSpeech("AAAH!") || SpeechTextRules.IsConfirmedNonSpeech(".."))
            throw new Exception("non-matching dialogue was over-broadly excluded");
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
