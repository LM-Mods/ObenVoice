using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ObenseuerTts
{
    internal sealed class DialogueDiagnosticEntry
    {
        internal DateTime TimestampUtc;
        internal string Kind = string.Empty;
        internal string Speaker = string.Empty;
        internal string RawText = string.Empty;
        internal string ProcessedText = string.Empty;
        internal string Voice = string.Empty;
        internal string Engine = string.Empty;
        internal string Outcome = string.Empty;
    }

    internal sealed class DialogueDiagnostics
    {
        internal const int RecentEventLimit = 4;
        internal const int MaximumLogBytes = 256 * 1024;

        private readonly Queue<DialogueDiagnosticEntry> recent = new Queue<DialogueDiagnosticEntry>();

        internal int Count
        {
            get { return recent.Count; }
        }

        internal void Add(DialogueDiagnosticEntry entry)
        {
            if (entry == null) return;
            while (recent.Count >= RecentEventLimit) recent.Dequeue();
            recent.Enqueue(entry);
        }

        internal DialogueDiagnosticEntry[] Snapshot()
        {
            return recent.ToArray();
        }

        internal int AppendFlaggedEvents(string path, DateTime flaggedUtc)
        {
            DialogueDiagnosticEntry[] snapshot = Snapshot();
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Dialogue flagged " + flaggedUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture) + " ===");
            if (snapshot.Length == 0) report.AppendLine("No dialogue events have been captured yet.");
            for (int i = 0; i < snapshot.Length; i++)
            {
                DialogueDiagnosticEntry item = snapshot[i];
                report.AppendLine("Event " + (i + 1).ToString(CultureInfo.InvariantCulture) + " of " + snapshot.Length.ToString(CultureInfo.InvariantCulture));
                report.AppendLine("  time: " + item.TimestampUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture));
                report.AppendLine("  type: " + OneLine(item.Kind));
                report.AppendLine("  speaker: " + OneLine(item.Speaker));
                report.AppendLine("  raw: " + OneLine(item.RawText));
                report.AppendLine("  processed: " + OneLine(item.ProcessedText));
                report.AppendLine("  voice: " + OneLine(item.Voice));
                report.AppendLine("  engine: " + OneLine(item.Engine));
                report.AppendLine("  outcome: " + OneLine(item.Outcome));
            }
            report.AppendLine();

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            BoundExistingLog(path, Encoding.UTF8.GetByteCount(report.ToString()));
            File.AppendAllText(path, report.ToString(), new UTF8Encoding(false));
            return snapshot.Length;
        }

        internal static void Clear(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static string OneLine(string value)
        {
            if (string.IsNullOrEmpty(value)) return "(none)";
            return value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private static void BoundExistingLog(string path, int incomingBytes)
        {
            if (!File.Exists(path)) return;
            FileInfo file = new FileInfo(path);
            if (file.Length + incomingBytes <= MaximumLogBytes) return;

            string existing = File.ReadAllText(path, Encoding.UTF8);
            int targetCharacters = Math.Max(0, (MaximumLogBytes - incomingBytes) / 2);
            if (existing.Length > targetCharacters) existing = existing.Substring(existing.Length - targetCharacters);
            int firstHeader = existing.IndexOf("=== Dialogue flagged ", StringComparison.Ordinal);
            if (firstHeader > 0) existing = existing.Substring(firstHeader);
            File.WriteAllText(path, existing, new UTF8Encoding(false));
        }
    }
}
