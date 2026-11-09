using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ObenseuerTts
{
    internal static class SpeechTextRules
    {
        private const int MaximumRules = 100;
        private static readonly Regex ShortExpression = new Regex(@"^(?:h+m+|m+|uh+|um+|ah+|oh+|huh)[.!?]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static PronunciationRule[] rules = BuiltInRules();

        private sealed class PronunciationRule
        {
            internal readonly Regex Pattern;
            internal readonly string Replacement;

            internal PronunciationRule(string source, string replacement)
            {
                string boundary = @"[\p{L}\p{N}_]";
                Pattern = new Regex("(?<!" + boundary + ")" + Regex.Escape(source) + "(?!" + boundary + ")", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                Replacement = replacement;
            }

            internal string Apply(string input)
            {
                // A MatchEvaluator keeps user-authored '$' sequences literal instead of
                // treating them as regular-expression group substitutions.
                return Pattern.Replace(input, delegate(Match match) { return Replacement; });
            }
        }

        internal static int LoadPronunciationReplacements(string content, Action<string> warning)
        {
            List<PronunciationRule> parsed = new List<PronunciationRule>();
            string[] lines = (content ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                int separator = line.IndexOf("=>", StringComparison.Ordinal);
                if (separator <= 0)
                {
                    if (warning != null) warning("Ignored pronunciation rule line " + (i + 1) + ": expected source => replacement");
                    continue;
                }
                string source = line.Substring(0, separator).Trim();
                string replacement = line.Substring(separator + 2).Trim();
                if (source.Length == 0 || source.Length > 64 || replacement.Length > 128)
                {
                    if (warning != null) warning("Ignored pronunciation rule line " + (i + 1) + ": source or replacement is too long");
                    continue;
                }
                parsed.Add(new PronunciationRule(source, replacement));
                if (parsed.Count == MaximumRules) break;
            }
            rules = parsed.Count == 0 ? BuiltInRules() : parsed.ToArray();
            return rules.Length;
        }

        internal static string ApplyPronunciationReplacements(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            string result = text;
            PronunciationRule[] snapshot = rules;
            for (int i = 0; i < snapshot.Length; i++) result = snapshot[i].Apply(result);
            if (ShortExpression.IsMatch(result) && !Regex.IsMatch(result, @"[.!?]$")) result += "...";
            return result;
        }

        internal static bool IsConfirmedNonSpeech(string text)
        {
            string value = (text ?? string.Empty).Trim();
            if (Regex.IsMatch(value, @"^(?:\.{3,}|…+)$", RegexOptions.CultureInvariant)) return true;
            return string.Equals(value, "AAAAH!", StringComparison.OrdinalIgnoreCase);
        }

        private static PronunciationRule[] BuiltInRules()
        {
            return new PronunciationRule[] { new PronunciationRule("Hmh", "Hmm") };
        }
    }
}
