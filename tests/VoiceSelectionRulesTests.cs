using System;
using System.Collections.Generic;
using ObenseuerTts;

internal static class VoiceSelectionRulesTests
{
    private static void Expect(string actual, string expected, string label)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new Exception(label + ": expected " + expected + ", got " + actual);
    }

    private static void Main()
    {
        HashSet<string> installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "af_heart", "am_adam", "bf_emma", "bm_george", "af_obv_player_mara_test", "am_obv_player_rowan_test"
        };
        HashSet<string> npcEligible = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);
        npcEligible.Remove("af_obv_player_mara_test");
        npcEligible.Remove("am_obv_player_rowan_test");

        Expect(string.Join(",", VoiceSelectionRules.BuildRuntimePool("af_heart,bf_emma", installed, "af_heart")), "af_heart,bf_emma", "installed external voice enabled");
        Expect(string.Join(",", VoiceSelectionRules.BuildRuntimePool("bf_missing", installed, "af_heart")), "af_heart", "removed pack fallback");
        Expect(VoiceSelectionRules.FilterVoiceSpecToPool("bf_emma,af_heart", new string[] { "af_heart" }), "af_heart", "disabled saved voice removed from blend");
        Expect(VoiceSelectionRules.FilterVoiceSpecToPool("bf_emma", new string[] { "af_heart" }), "", "disabled saved voice requires fallback");
        Expect(VoiceSelectionRules.SetEnabled("bf_emma", "bf_emma", false, "af_heart"), "af_heart", "last voice cannot leave empty pool");
        string persisted = VoiceSelectionRules.SetEnabled("af_heart", "bf_emma", true, "af_heart");
        Expect(persisted, "af_heart,bf_emma", "external selection persisted");
        Expect(string.Join(",", VoiceSelectionRules.BuildRuntimePool(persisted, installed, "af_heart")), "af_heart,bf_emma", "persisted selection reload");
        Expect(VoiceSelectionRules.MergeConfigured("af_heart,bf_emma", "am_adam,bm_george"), "af_heart,bf_emma,am_adam,bm_george", "unknown pool follows enabled genders");
        string randomVoice = VoiceSelectionRules.StableVoice("Mystery Person", new string[] { "af_heart", "am_adam", "bf_emma" }, "af_heart");
        Expect(VoiceSelectionRules.StableVoice("Mystery Person", new string[] { "af_heart", "am_adam", "bf_emma" }, "af_heart"), randomVoice, "random assignment remains stable");
        if (Array.IndexOf(new string[] { "af_heart", "am_adam", "bf_emma" }, randomVoice) < 0) throw new Exception("random assignment was outside compatible pool");
        if (!VoiceSelectionRules.IsVoiceSpecAvailable("af_heart,bf_emma,bm_george", installed)) throw new Exception("mixed-pack preset voices were not resolved from the installed inventory");
        if (!VoiceSelectionRules.IsVoiceSpecAvailable("af_obv_player_mara_test,am_obv_player_rowan_test", installed)) throw new Exception("player-only voices were unavailable to the player inventory");
        if (VoiceSelectionRules.IsVoiceSpecAvailable("af_obv_player_mara_test", npcEligible) || VoiceSelectionRules.IsVoiceSpecAvailable("am_obv_player_rowan_test", npcEligible)) throw new Exception("player-only voice entered the NPC-eligible inventory");
        if (VoiceSelectionRules.IsVoiceSpecAvailable("bf_missing", installed)) throw new Exception("missing voice reported available");

        Console.WriteLine("voice selection rules tests passed");
    }
}
