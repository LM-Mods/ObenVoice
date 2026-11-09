using System;
using System.IO;
using ObenseuerTts;

internal static class DialogueDiagnosticsTests
{
    private static void Expect(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }

    private static DialogueDiagnosticEntry Entry(int number)
    {
        return new DialogueDiagnosticEntry
        {
            TimestampUtc = new DateTime(2026, 11, 9, 12, 0, number, DateTimeKind.Utc),
            Kind = "Npc",
            Speaker = "Speaker " + number,
            RawText = "Raw " + number,
            ProcessedText = "Clean " + number,
            Voice = "voice_" + number,
            Engine = "kokoro",
            Outcome = "queued"
        };
    }

    private static void Main()
    {
        DialogueDiagnostics diagnostics = new DialogueDiagnostics();
        for (int i = 1; i <= 6; i++) diagnostics.Add(Entry(i));
        DialogueDiagnosticEntry[] snapshot = diagnostics.Snapshot();
        Expect(snapshot.Length == 4, "keeps four events");
        Expect(snapshot[0].Speaker == "Speaker 3", "drops oldest event");
        Expect(snapshot[3].Speaker == "Speaker 6", "keeps newest event");

        string root = Path.Combine(Path.GetTempPath(), "ObenVoiceDialogueDiagnosticsTests-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "dialogue-diagnostics.log");
        try
        {
            int written = diagnostics.AppendFlaggedEvents(path, new DateTime(2026, 11, 9, 13, 0, 0, DateTimeKind.Utc));
            string text = File.ReadAllText(path);
            Expect(written == 4, "reports written count");
            Expect(text.Contains("Speaker 3") && text.Contains("Speaker 6"), "writes snapshot details");
            Expect(!text.Contains("Speaker 2"), "does not write expired event");
            DialogueDiagnostics.Clear(path);
            Expect(!File.Exists(path), "clear removes log");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        Console.WriteLine("dialogue diagnostics tests passed");
    }
}
