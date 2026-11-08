using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ObenseuerTts
{
    [Serializable]
    internal sealed class VoicePackManifest
    {
        public int manifestVersion = 0;
        public string id = string.Empty;
        public string name = string.Empty;
        public string engine = string.Empty;
        public VoicePackVoice[] voices = new VoicePackVoice[0];
    }

    [Serializable]
    internal sealed class VoicePackVoice
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string gender = string.Empty;
        public string file = string.Empty;
    }

    internal static class VoicePackJson
    {
        internal static VoicePackManifest Parse(string json)
        {
            string source = json ?? string.Empty;
            VoicePackManifest manifest = new VoicePackManifest();
            manifest.manifestVersion = ReadJsonInt(source, "manifestVersion");
            manifest.id = ReadJsonString(source, "id");
            manifest.name = ReadJsonString(source, "name");
            manifest.engine = ReadJsonString(source, "engine");

            List<VoicePackVoice> voices = new List<VoicePackVoice>();
            foreach (string item in ReadObjectArray(source, "voices"))
            {
                VoicePackVoice voice = new VoicePackVoice();
                voice.id = ReadJsonString(item, "id");
                voice.displayName = ReadJsonString(item, "displayName");
                voice.gender = ReadJsonString(item, "gender");
                voice.file = ReadJsonString(item, "file");
                voices.Add(voice);
            }
            manifest.voices = voices.ToArray();
            return manifest;
        }

        private static int ReadJsonInt(string json, string field)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(field) + "\\\"\\s*:\\s*(?<value>-?[0-9]+)", RegexOptions.CultureInvariant);
            int value;
            return match.Success && int.TryParse(match.Groups["value"].Value, out value) ? value : 0;
        }

        private static string ReadJsonString(string json, string field)
        {
            string pattern = "\\\"" + Regex.Escape(field) + "\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"\\\\])*)\\\"";
            Match match = Regex.Match(json ?? string.Empty, pattern, RegexOptions.CultureInvariant);
            if (!match.Success) return string.Empty;
            return Regex.Unescape(match.Groups["value"].Value.Replace("\\/", "/"));
        }

        private static List<string> ReadObjectArray(string json, string field)
        {
            List<string> objects = new List<string>();
            Match fieldMatch = Regex.Match(json, "\\\"" + Regex.Escape(field) + "\\\"\\s*:\\s*\\[", RegexOptions.CultureInvariant);
            if (!fieldMatch.Success) return objects;

            int depth = 0;
            int objectStart = -1;
            bool inString = false;
            bool escaped = false;
            for (int i = fieldMatch.Index + fieldMatch.Length; i < json.Length; i++)
            {
                char character = json[i];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (character == '\\') escaped = true;
                    else if (character == '"') inString = false;
                    continue;
                }
                if (character == '"')
                {
                    inString = true;
                    continue;
                }
                if (character == '{')
                {
                    if (depth == 0) objectStart = i;
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;
                    if (depth == 0 && objectStart >= 0)
                    {
                        objects.Add(json.Substring(objectStart, i - objectStart + 1));
                        objectStart = -1;
                    }
                }
                else if (character == ']' && depth == 0)
                {
                    break;
                }
            }
            return objects;
        }
    }
}
