using System;
using System.Collections.Generic;

namespace ObenseuerTts
{
    internal static class VoiceSelectionRules
    {
        internal static string[] ParseConfigured(string configured)
        {
            List<string> result = new List<string>();
            foreach (string part in (configured ?? string.Empty).Split(','))
            {
                string voice = part.Trim();
                if (voice.Length > 0 && !result.Exists(item => string.Equals(item, voice, StringComparison.OrdinalIgnoreCase)))
                    result.Add(voice);
            }
            return result.ToArray();
        }

        internal static string[] BuildRuntimePool(string configured, ISet<string> available, string fallback)
        {
            List<string> result = new List<string>();
            foreach (string voice in ParseConfigured(configured))
                if (available.Contains(voice)) result.Add(voice);
            if (result.Count == 0 && available.Contains(fallback)) result.Add(fallback);
            return result.ToArray();
        }

        internal static string FilterVoiceSpecToPool(string requested, string[] enabledPool)
        {
            HashSet<string> enabled = new HashSet<string>(enabledPool ?? new string[0], StringComparer.OrdinalIgnoreCase);
            List<string> result = new List<string>();
            foreach (string voice in ParseConfigured(requested))
                if (enabled.Contains(voice)) result.Add(voice);
            return string.Join(",", result.ToArray());
        }

        internal static bool IsVoiceSpecAvailable(string requested, ISet<string> available)
        {
            string[] voices = ParseConfigured(requested);
            if (voices.Length == 0) return false;
            foreach (string voice in voices)
                if (!available.Contains(voice)) return false;
            return true;
        }

        internal static string SetEnabled(string configured, string voice, bool enabled, string requiredFallback)
        {
            List<string> voices = new List<string>(ParseConfigured(configured));
            voices.RemoveAll(item => string.Equals(item, voice, StringComparison.OrdinalIgnoreCase));
            if (enabled) voices.Add(voice);
            if (voices.Count == 0) voices.Add(requiredFallback);
            return string.Join(",", voices.ToArray());
        }

        internal static string MergeConfigured(string first, string second)
        {
            List<string> result = new List<string>();
            foreach (string configured in new string[] { first, second })
                foreach (string voice in ParseConfigured(configured))
                    if (!result.Exists(item => string.Equals(item, voice, StringComparison.OrdinalIgnoreCase))) result.Add(voice);
            return string.Join(",", result.ToArray());
        }

        internal static string StableVoice(string speakerName, string[] pool, string fallback)
        {
            if (pool == null || pool.Length == 0) return fallback;
            int hash = 17;
            string key = speakerName ?? string.Empty;
            for (int i = 0; i < key.Length; i++) hash = unchecked(hash * 31 + char.ToUpperInvariant(key[i]));
            return pool[(hash & 0x7fffffff) % pool.Length];
        }
    }
}
