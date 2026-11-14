using System;
using System.IO;
using ObenseuerTts;

internal static class VoicePackJsonTests
{
    private static void Main(string[] args)
    {
        if (args.Length != 1) throw new Exception("manifest path argument required");
        VoicePackManifest manifest = VoicePackJson.Parse(File.ReadAllText(args[0]));
        if (manifest == null) throw new Exception("manifest was null");
        if (manifest.manifestVersion != 1) throw new Exception("manifest version was not parsed");
        if (manifest.id != "british-test" || manifest.engine != "kokoro") throw new Exception("manifest identity was not parsed");
        if (manifest.voices == null || manifest.voices.Length != 2) throw new Exception("nested voices array was not parsed");
        if (manifest.voices[0].id != "bf_emma" || manifest.voices[0].gender != "female") throw new Exception("Emma entry was not parsed");
        if (manifest.voices[1].id != "bm_george" || manifest.voices[1].gender != "male") throw new Exception("George entry was not parsed");
        if (manifest.shortCode != "BR") throw new Exception("British short code was not parsed");
        VoicePackManifest legacyManifest = VoicePackJson.Parse("{\"manifestVersion\":1,\"id\":\"legacy\",\"name\":\"Legacy\",\"engine\":\"kokoro\",\"voices\":[]}");
        if (legacyManifest.shortCode != string.Empty) throw new Exception("older manifest without shortCode lost compatibility");
        VoicePackManifest playerManifest = VoicePackJson.Parse(File.ReadAllText(Path.Combine("data", "player-voices", "manifest.json")));
        if (playerManifest.role != "player" || playerManifest.voices.Length != 2) throw new Exception("player-only manifest role was not parsed");
        string[] names = new string[] { "z-pack", "A-pack", "a-pack" };
        Array.Sort(names, VoicePackOrdering.CompareNames);
        if (string.Join(",", names) != "A-pack,a-pack,z-pack") throw new Exception("deterministic pack ordering failed");
        Console.WriteLine("voice-pack and player-only manifest JSON tests passed");
    }
}
