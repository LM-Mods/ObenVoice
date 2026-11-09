using System;
using System.IO;
using ObenseuerTts;

internal static class UnknownSpeakerTrackerTests
{
    private static void Expect(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }

    private static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ObenVoiceUnknownSpeakerTests-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "unknown-speakers.tsv");
        try
        {
            UnknownSpeakerTracker tracker = new UnknownSpeakerTracker();
            tracker.Load(path);
            tracker.RecordOccurrence("Mystery Person", "dialogue:42", "Hello there.", new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc));
            tracker.RecordOccurrence("Mystery Person", "dialogue:42", "Another line.", new DateTime(2026, 11, 9, 12, 1, 0, DateTimeKind.Utc));
            tracker.RecordChoice("Mystery Person", "dialogue:42", "af_heart", "random compatible voice", "unknown", true, new DateTime(2026, 11, 9, 12, 1, 1, DateTimeKind.Utc));
            Expect(tracker.Count == 1, "deduplicates by speaker id");
            UnknownSpeakerRecord record = tracker.Snapshot()[0];
            Expect(record.OccurrenceCount == 2, "counts occurrences");
            Expect(record.Voice == "af_heart" && record.Remembered, "records assignment");
            Expect(record.IdentityStatus == "unresolved candidate", "marks uncertain identity");

            UnknownSpeakerTracker reloaded = new UnknownSpeakerTracker();
            reloaded.Load(path);
            Expect(reloaded.Count == 1, "reloads persisted record");
            Expect(reloaded.Snapshot()[0].OccurrenceCount == 2, "preserves count after reload");
            string text = File.ReadAllText(path);
            Expect(text.Contains("Mystery Person") && text.Contains("random compatible voice"), "writes reviewable fields");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        Console.WriteLine("unknown speaker tracker tests passed");
    }
}
