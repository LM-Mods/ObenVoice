using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ObenseuerTts
{
    internal sealed class EditableSpeechRule
    {
        internal string First;
        internal string Second;

        internal EditableSpeechRule(string first, string second)
        {
            First = first ?? string.Empty;
            Second = second ?? string.Empty;
        }
    }

    internal sealed class EditableSpeechRuleDocument
    {
        internal readonly List<EditableSpeechRule> Entries = new List<EditableSpeechRule>();
        internal readonly List<string> PreservedLines = new List<string>();

        internal static EditableSpeechRuleDocument Parse(string content, bool exclusions, Action<string> warning)
        {
            EditableSpeechRuleDocument document = new EditableSpeechRuleDocument();
            string[] lines = (content ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    document.PreservedLines.Add(raw);
                    continue;
                }
                int separator = line.IndexOf("=>", StringComparison.Ordinal);
                string first = separator < 0 ? string.Empty : line.Substring(0, separator).Trim();
                string second = separator < 0 ? string.Empty : line.Substring(separator + 2).Trim();
                bool valid = exclusions
                    ? separator >= 0 && first.Length > 0 && second.Length > 0 && first.Length <= 128 && second.Length <= 256
                    : separator > 0 && first.Length <= 64 && second.Length <= 128;
                if (!valid)
                {
                    document.PreservedLines.Add(raw);
                    if (warning != null) warning("Preserved malformed rule line " + (i + 1) + " without loading it.");
                    continue;
                }
                document.Entries.Add(new EditableSpeechRule(first, second));
            }
            return document;
        }

        internal string Serialize(string heading)
        {
            StringBuilder output = new StringBuilder();
            bool hasHeading = false;
            foreach (string line in PreservedLines)
            {
                if (line.StartsWith("#", StringComparison.Ordinal)) hasHeading = true;
                output.Append(line).Append("\r\n");
            }
            if (!hasHeading && !string.IsNullOrEmpty(heading)) output.Insert(0, heading + "\r\n");
            if (output.Length > 0 && !output.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal)) output.Append("\r\n");
            foreach (EditableSpeechRule entry in Entries)
                output.Append(entry.First.Trim()).Append(" => ").Append(entry.Second.Trim()).Append("\r\n");
            return output.ToString();
        }
    }

    internal static class SpeechExclusionRules
    {
        internal static bool IsExcluded(IList<EditableSpeechRule> entries, string speaker, string text)
        {
            string normalizedSpeaker = Normalize(speaker);
            string normalizedText = Normalize(text);
            if (entries == null) return false;
            foreach (EditableSpeechRule entry in entries)
            {
                string ruleSpeaker = Normalize(entry.First);
                string ruleText = Normalize(entry.Second);
                bool speakerMatches = ruleSpeaker == "*" || string.Equals(ruleSpeaker, normalizedSpeaker, StringComparison.OrdinalIgnoreCase);
                bool textMatches = ruleText == "*" || string.Equals(ruleText, normalizedText, StringComparison.OrdinalIgnoreCase);
                if (speakerMatches && textMatches) return true;
            }
            return false;
        }

        private static string Normalize(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
        }
    }

    internal static class SpeechExclusionDefaults
    {
        internal const string VersionMarker = "# ObenVoice speech exclusions version: 2";
        internal const string DefaultContent =
            "# Exact speech exclusions. Use * to match any speaker or any text.\r\n" +
            "# Format: speaker => spoken text\r\n" +
            VersionMarker + "\r\n\r\n" +
            "Doorphone => Bzzzz!\r\n" +
            "* => ...\r\n" +
            "* => AAAAH!\r\n";

        internal static bool AddUpgradeDefaults(EditableSpeechRuleDocument document)
        {
            if (document == null) return false;
            foreach (string line in document.PreservedLines)
                if (string.Equals((line ?? string.Empty).Trim(), VersionMarker, StringComparison.Ordinal)) return false;

            document.PreservedLines.Add(VersionMarker);
            AddIfMissing(document.Entries, "*", "...");
            AddIfMissing(document.Entries, "*", "AAAAH!");
            return true;
        }

        private static void AddIfMissing(IList<EditableSpeechRule> entries, string speaker, string text)
        {
            foreach (EditableSpeechRule entry in entries)
                if (string.Equals((entry.First ?? string.Empty).Trim(), speaker, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((entry.Second ?? string.Empty).Trim(), text, StringComparison.OrdinalIgnoreCase)) return;
            entries.Add(new EditableSpeechRule(speaker, text));
        }
    }
}
