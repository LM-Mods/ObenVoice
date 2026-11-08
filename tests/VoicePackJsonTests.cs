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
        Console.WriteLine("voice-pack manifest JSON test passed with two nested voices");
    }
}
