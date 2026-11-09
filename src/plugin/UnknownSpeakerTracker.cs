using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ObenseuerTts
{
    internal sealed class UnknownSpeakerRecord
    {
        internal string Key = string.Empty;
        internal string Name = string.Empty;
        internal string SpeakerId = string.Empty;
        internal string IdentityStatus = "unresolved candidate";
        internal DateTime FirstSeenUtc;
        internal DateTime LastSeenUtc;
        internal int OccurrenceCount;
        internal string ExampleRawText = string.Empty;
        internal string Voice = string.Empty;
        internal string AssignmentMethod = string.Empty;
        internal string Gender = string.Empty;
        internal bool Remembered;
    }

    internal sealed class UnknownSpeakerTracker
    {
        internal const int MaximumRecords = 500;
        internal const int MaximumLogBytes = 256 * 1024;

        private readonly Dictionary<string, UnknownSpeakerRecord> records =
            new Dictionary<string, UnknownSpeakerRecord>(StringComparer.OrdinalIgnoreCase);
        private string path = string.Empty;

        internal int Count
        {
            get { return records.Count; }
        }

        internal void Load(string filePath)
        {
            path = filePath ?? string.Empty;
            records.Clear();
            if (path.Length == 0 || !File.Exists(path)) return;
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] parts = line.Split('\t');
                if (parts.Length < 12) continue;
                DateTime first;
                DateTime last;
                int count;
                if (!DateTime.TryParse(parts[4], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out first) ||
                    !DateTime.TryParse(parts[5], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out last) ||
                    !int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) continue;
                UnknownSpeakerRecord record = new UnknownSpeakerRecord();
                record.Key = parts[0];
                record.Name = parts[1];
                record.SpeakerId = parts[2];
                record.IdentityStatus = parts[3];
                record.FirstSeenUtc = first.ToUniversalTime();
                record.LastSeenUtc = last.ToUniversalTime();
                record.OccurrenceCount = Math.Max(1, count);
                record.Voice = parts[7];
                record.AssignmentMethod = parts[8];
                record.Gender = parts[9];
                record.Remembered = string.Equals(parts[10], "yes", StringComparison.OrdinalIgnoreCase);
                record.ExampleRawText = parts[11];
                if (!string.IsNullOrEmpty(record.Key)) records[record.Key] = record;
            }
            TrimToLimit();
        }

        internal void RecordOccurrence(string name, string speakerId, string rawText, DateTime timestampUtc)
        {
            UnknownSpeakerRecord record = GetOrCreate(name, speakerId, timestampUtc);
            record.LastSeenUtc = timestampUtc.ToUniversalTime();
            record.OccurrenceCount++;
            if (string.IsNullOrEmpty(record.ExampleRawText)) record.ExampleRawText = CleanField(rawText);
            Save();
        }

        internal void RecordChoice(string name, string speakerId, string voice, string method, string gender, bool remembered, DateTime timestampUtc)
        {
            UnknownSpeakerRecord record = GetOrCreate(name, speakerId, timestampUtc);
            record.LastSeenUtc = timestampUtc.ToUniversalTime();
            record.Voice = CleanField(voice);
            record.AssignmentMethod = CleanField(method);
            record.Gender = CleanField(gender);
            record.Remembered = remembered;
            Save();
        }

        internal UnknownSpeakerRecord[] Snapshot()
        {
            List<UnknownSpeakerRecord> values = SortedRecords();
            return values.ToArray();
        }

        private UnknownSpeakerRecord GetOrCreate(string name, string speakerId, DateTime timestampUtc)
        {
            string cleanName = CleanField(name);
            string cleanId = CleanField(speakerId);
            string key = cleanId.Length > 0 ? "id:" + cleanId : "name:" + cleanName;
            UnknownSpeakerRecord record;
            if (records.TryGetValue(key, out record)) return record;
            record = new UnknownSpeakerRecord();
            record.Key = key;
            record.Name = cleanName;
            record.SpeakerId = cleanId;
            record.FirstSeenUtc = timestampUtc.ToUniversalTime();
            record.LastSeenUtc = record.FirstSeenUtc;
            records[key] = record;
            return record;
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(path)) return;
            TrimToLimit();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            List<UnknownSpeakerRecord> values = SortedRecords();
            string content = Serialize(values);
            while (Encoding.UTF8.GetByteCount(content) > MaximumLogBytes && values.Count > 1)
            {
                records.Remove(values[values.Count - 1].Key);
                values.RemoveAt(values.Count - 1);
                content = Serialize(values);
            }
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private void TrimToLimit()
        {
            List<UnknownSpeakerRecord> values = SortedRecords();
            while (values.Count > MaximumRecords)
            {
                records.Remove(values[values.Count - 1].Key);
                values.RemoveAt(values.Count - 1);
            }
        }

        private List<UnknownSpeakerRecord> SortedRecords()
        {
            List<UnknownSpeakerRecord> values = new List<UnknownSpeakerRecord>(records.Values);
            values.Sort(delegate(UnknownSpeakerRecord left, UnknownSpeakerRecord right)
            {
                return right.LastSeenUtc.CompareTo(left.LastSeenUtc);
            });
            return values;
        }

        private static string Serialize(List<UnknownSpeakerRecord> values)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("# ObenVoice unknown speaker candidates (tab-separated; identities require manual review)");
            output.AppendLine("# key\tname\tspeaker_id\tidentity_status\tfirst_seen_utc\tlast_seen_utc\toccurrences\tvoice\tassignment_method\tgender\tremembered\texample_raw_text");
            foreach (UnknownSpeakerRecord record in values)
            {
                output.Append(CleanField(record.Key)).Append('\t')
                    .Append(CleanField(record.Name)).Append('\t')
                    .Append(CleanField(record.SpeakerId)).Append('\t')
                    .Append(CleanField(record.IdentityStatus)).Append('\t')
                    .Append(record.FirstSeenUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)).Append('\t')
                    .Append(record.LastSeenUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)).Append('\t')
                    .Append(record.OccurrenceCount.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(CleanField(record.Voice)).Append('\t')
                    .Append(CleanField(record.AssignmentMethod)).Append('\t')
                    .Append(CleanField(record.Gender)).Append('\t')
                    .Append(record.Remembered ? "yes" : "no").Append('\t')
                    .Append(CleanField(record.ExampleRawText)).AppendLine();
            }
            return output.ToString();
        }

        private static string CleanField(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
        }
    }
}
