using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ObenseuerTts
{
    internal enum NpcVoiceResolutionLayer
    {
        Automatic,
        SavedAutomatic,
        Preset,
        Override
    }

    internal static class NpcVoiceResolutionPriority
    {
        internal static NpcVoiceResolutionLayer Resolve(NpcVoiceEntry explicitOverride, NpcVoiceEntry preset, NpcVoiceEntry savedAutomatic)
        {
            if (explicitOverride != null) return NpcVoiceResolutionLayer.Override;
            if (preset != null) return NpcVoiceResolutionLayer.Preset;
            if (savedAutomatic != null) return NpcVoiceResolutionLayer.SavedAutomatic;
            return NpcVoiceResolutionLayer.Automatic;
        }
    }

    internal sealed class NpcVoiceEntry
    {
        internal string GameCharacterId = string.Empty;
        internal string SpeakerId = string.Empty;
        internal string InternalName = string.Empty;
        internal string DisplayName = string.Empty;
        internal string Voice = string.Empty;
        internal float Speed = 1.0f;
        internal float Pitch;
        internal bool HasSpeed;
        internal bool HasPitch;
        internal bool Automatic;
        internal bool Ignore;
    }

    internal sealed class NpcVoiceOverridesDocument
    {
        internal const string EmptyJson = "{\r\n  \"version\": 1,\r\n  \"overrides\": []\r\n}\r\n";
        internal readonly List<NpcVoiceEntry> Overrides = new List<NpcVoiceEntry>();

        internal static NpcVoiceOverridesDocument Parse(string json)
        {
            SimpleJsonValue root = SimpleJson.Parse(json);
            if (root.Kind != SimpleJsonKind.Object) throw new FormatException("NPC Voice Overrides root must be an object.");
            double version;
            if (!root.TryNumber("version", out version) || version != 1) throw new FormatException("NPC Voice Overrides requires version 1.");
            SimpleJsonValue items = root.Get("overrides");
            if (items == null || items.Kind != SimpleJsonKind.Array) throw new FormatException("NPC Voice Overrides requires an overrides array.");
            NpcVoiceOverridesDocument document = new NpcVoiceOverridesDocument();
            foreach (SimpleJsonValue item in items.Array) document.Overrides.Add(NpcVoiceData.ParseEntry(item, "override"));
            string error = document.Validate();
            if (error.Length > 0) throw new FormatException(error);
            return document;
        }

        internal string Validate()
        {
            HashSet<string> identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (NpcVoiceEntry item in Overrides)
            {
                string error = NpcVoiceData.ValidateEntry(item);
                if (error.Length > 0) return error;
                string identity = NpcVoiceData.IdentityKey(item);
                if (!identities.Add(identity)) return "Duplicate NPC Voice Override identity: " + identity;
            }
            return string.Empty;
        }

        internal NpcVoiceEntry Find(string gameCharacterId, string rawName, NpcIdentityRecord identity)
        {
            return Find(gameCharacterId, string.Empty, rawName, identity, null);
        }

        internal NpcVoiceEntry Find(string gameCharacterId, string speakerId, string rawName, NpcIdentityRecord identity, bool? automatic)
        {
            NpcVoiceEntry match = null;
            foreach (NpcVoiceEntry item in Overrides)
            {
                if (automatic.HasValue && item.Automatic != automatic.Value) continue;
                bool matches = false;
                if (item.GameCharacterId.Length > 0 && gameCharacterId.Length > 0)
                    matches = string.Equals(item.GameCharacterId, gameCharacterId, StringComparison.OrdinalIgnoreCase);
                else if (identity != null && item.InternalName.Length > 0)
                    matches = string.Equals(item.InternalName, identity.InternalName, StringComparison.OrdinalIgnoreCase);
                else if (item.SpeakerId.Length > 0 && speakerId.Length > 0)
                    matches = string.Equals(item.SpeakerId, speakerId, StringComparison.OrdinalIgnoreCase);
                else if (identity != null && item.DisplayName.Length > 0)
                    matches = string.Equals(item.DisplayName, identity.DisplayName, StringComparison.OrdinalIgnoreCase);
                else if (item.DisplayName.Length > 0)
                    matches = string.Equals(item.DisplayName, rawName, StringComparison.OrdinalIgnoreCase);
                if (!matches) continue;
                if (match != null) return null;
                match = item;
            }
            return match;
        }

        internal NpcVoiceEntry FindUser(string gameCharacterId, string speakerId, string rawName, NpcIdentityRecord identity)
        {
            return Find(gameCharacterId, speakerId, rawName, identity, false);
        }

        internal NpcVoiceEntry FindAutomatic(string gameCharacterId, string speakerId, string rawName, NpcIdentityRecord identity)
        {
            return Find(gameCharacterId, speakerId, rawName, identity, true);
        }

        internal string ToJson()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\r\n  \"version\": 1,\r\n  \"overrides\": [");
            for (int i = 0; i < Overrides.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append("\r\n");
                NpcVoiceData.AppendEntry(json, Overrides[i], "    ");
            }
            if (Overrides.Count > 0) json.Append("\r\n  ");
            json.Append("]\r\n}\r\n");
            return json.ToString();
        }
    }

    internal sealed class NpcVoicePreset
    {
        internal string PackId = string.Empty;
        internal string Id = string.Empty;
        internal string Name = string.Empty;
        internal string Description = string.Empty;
        internal bool Provisional;
        internal string FallbackMode = "active";
        internal readonly List<NpcVoiceEntry> Assignments = new List<NpcVoiceEntry>();

        internal string StableId { get { return PackId + "/" + Id; } }

        internal static NpcVoicePreset Parse(string json, string packId)
        {
            SimpleJsonValue root = SimpleJson.Parse(json);
            if (root.Kind != SimpleJsonKind.Object) throw new FormatException("NPC preset root must be an object.");
            double version;
            if (!root.TryNumber("presetVersion", out version) || version != 1) throw new FormatException("NPC preset requires presetVersion 1.");
            NpcVoicePreset preset = new NpcVoicePreset();
            preset.PackId = (packId ?? string.Empty).Trim().ToLowerInvariant();
            preset.Id = root.StringOrEmpty("id").Trim().ToLowerInvariant();
            preset.Name = root.StringOrEmpty("name").Trim();
            preset.Description = root.StringOrEmpty("description").Trim();
            preset.Provisional = root.BoolOrDefault("provisional", false);
            preset.FallbackMode = root.StringOrEmpty("fallbackMode").Trim().ToLowerInvariant();
            if (preset.FallbackMode.Length == 0) preset.FallbackMode = "active";
            if (!Regex.IsMatch(preset.Id, "^[a-z0-9][a-z0-9_-]{0,63}$")) throw new FormatException("NPC preset has an invalid id.");
            if (preset.Name.Length == 0) preset.Name = preset.Id;
            if (preset.FallbackMode != "active" && preset.FallbackMode != "pack") throw new FormatException("NPC preset fallbackMode must be active or pack.");
            SimpleJsonValue assignments = root.Get("assignments");
            if (assignments == null || assignments.Kind != SimpleJsonKind.Array) throw new FormatException("NPC preset requires an assignments array.");
            HashSet<string> identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SimpleJsonValue item in assignments.Array)
            {
                NpcVoiceEntry assignment = NpcVoiceData.ParseEntry(item, "preset assignment");
                string error = NpcVoiceData.ValidateEntry(assignment);
                if (error.Length > 0) throw new FormatException(error);
                string identity = NpcVoiceData.IdentityKey(assignment);
                if (!identities.Add(identity)) throw new FormatException("Duplicate NPC preset identity: " + identity);
                preset.Assignments.Add(assignment);
            }
            return preset;
        }

        internal NpcVoiceEntry Find(string gameCharacterId, string rawName, NpcIdentityRecord identity)
        {
            NpcVoiceEntry match = null;
            foreach (NpcVoiceEntry item in Assignments)
            {
                if (item.Voice.Length == 0 && !item.HasSpeed && !item.HasPitch && !item.Ignore) continue;
                bool matches = false;
                if (item.GameCharacterId.Length > 0 && gameCharacterId.Length > 0)
                    matches = string.Equals(item.GameCharacterId, gameCharacterId, StringComparison.OrdinalIgnoreCase);
                else if (identity != null && item.InternalName.Length > 0)
                    matches = string.Equals(item.InternalName, identity.InternalName, StringComparison.OrdinalIgnoreCase);
                else if (identity != null && item.DisplayName.Length > 0)
                    matches = string.Equals(item.DisplayName, identity.DisplayName, StringComparison.OrdinalIgnoreCase);
                else if (item.DisplayName.Length > 0)
                    matches = string.Equals(item.DisplayName, rawName, StringComparison.OrdinalIgnoreCase);
                if (!matches) continue;
                if (match != null) return null;
                match = item;
            }
            return match;
        }
    }

    internal static class NpcVoiceData
    {
        private static readonly Regex VoiceId = new Regex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant);

        internal static NpcVoiceEntry ParseEntry(SimpleJsonValue value, string label)
        {
            if (value == null || value.Kind != SimpleJsonKind.Object) throw new FormatException("Every NPC " + label + " must be an object.");
            NpcVoiceEntry entry = new NpcVoiceEntry();
            entry.GameCharacterId = ReadId(value.Get("gameCharacterId"));
            entry.SpeakerId = value.StringOrEmpty("speakerId").Trim();
            entry.InternalName = value.StringOrEmpty("internalName").Trim();
            entry.DisplayName = value.StringOrEmpty("displayName").Trim();
            if (entry.DisplayName.Length == 0) entry.DisplayName = value.StringOrEmpty("npc").Trim();
            entry.Voice = value.StringOrEmpty("voice").Trim().ToLowerInvariant();
            entry.Automatic = value.BoolOrDefault("automatic", false);
            entry.Ignore = value.BoolOrDefault("ignore", false);
            double number;
            if (value.TryNumber("speed", out number))
            {
                entry.HasSpeed = true;
                entry.Speed = (float)number;
            }
            if (value.TryNumber("pitch", out number) || value.TryNumber("tone", out number))
            {
                entry.HasPitch = true;
                entry.Pitch = (float)number;
            }
            return entry;
        }

        internal static string ValidateEntry(NpcVoiceEntry entry)
        {
            if (entry == null) return "NPC voice entry is null.";
            if (IdentityKey(entry).Length == 0) return "NPC voice entry requires gameCharacterId, internalName, speakerId or displayName.";
            if (entry.Voice.Length > 0)
            {
                foreach (string part in entry.Voice.Split(','))
                    if (!VoiceId.IsMatch(part.Trim())) return "NPC voice entry has an invalid voice id: " + entry.Voice;
            }
            if (entry.HasSpeed && (entry.Speed < 0.5f || entry.Speed > 2.0f)) return "NPC voice speed must be between 0.5 and 2.0.";
            if (entry.HasPitch && (entry.Pitch < -4.0f || entry.Pitch > 4.0f)) return "NPC voice pitch must be between -4 and 4.";
            return string.Empty;
        }

        internal static string IdentityKey(NpcVoiceEntry entry)
        {
            if (entry == null) return string.Empty;
            if (!string.IsNullOrEmpty(entry.GameCharacterId)) return "id:" + entry.GameCharacterId.Trim();
            if (!string.IsNullOrEmpty(entry.InternalName)) return "internal:" + entry.InternalName.Trim();
            if (!string.IsNullOrEmpty(entry.SpeakerId)) return "speaker:" + entry.SpeakerId.Trim();
            if (!string.IsNullOrEmpty(entry.DisplayName)) return "name:" + entry.DisplayName.Trim();
            return string.Empty;
        }

        internal static void AppendEntry(StringBuilder json, NpcVoiceEntry entry, string indent)
        {
            List<string> fields = new List<string>();
            if (entry.GameCharacterId.Length > 0) fields.Add("\"gameCharacterId\": \"" + SimpleJson.Escape(entry.GameCharacterId) + "\"");
            if (entry.SpeakerId.Length > 0) fields.Add("\"speakerId\": \"" + SimpleJson.Escape(entry.SpeakerId) + "\"");
            if (entry.InternalName.Length > 0) fields.Add("\"internalName\": \"" + SimpleJson.Escape(entry.InternalName) + "\"");
            if (entry.DisplayName.Length > 0) fields.Add("\"displayName\": \"" + SimpleJson.Escape(entry.DisplayName) + "\"");
            if (entry.Voice.Length > 0) fields.Add("\"voice\": \"" + SimpleJson.Escape(entry.Voice) + "\"");
            if (entry.HasSpeed) fields.Add("\"speed\": " + entry.Speed.ToString("0.###", CultureInfo.InvariantCulture));
            if (entry.HasPitch) fields.Add("\"pitch\": " + entry.Pitch.ToString("0.###", CultureInfo.InvariantCulture));
            if (entry.Automatic) fields.Add("\"automatic\": true");
            if (entry.Ignore) fields.Add("\"ignore\": true");
            json.Append(indent).Append("{");
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append("\r\n").Append(indent).Append("  ").Append(fields[i]);
            }
            if (fields.Count > 0) json.Append("\r\n").Append(indent);
            json.Append("}");
        }

        private static string ReadId(SimpleJsonValue value)
        {
            if (value == null || value.Kind == SimpleJsonKind.Null) return string.Empty;
            if (value.Kind == SimpleJsonKind.String) return value.String.Trim();
            if (value.Kind == SimpleJsonKind.Number && value.Number == Math.Truncate(value.Number)) return value.Number.ToString("0", CultureInfo.InvariantCulture);
            throw new FormatException("gameCharacterId must be a string, integer or null.");
        }
    }

    internal sealed class NpcVoiceOverridesLoadResult
    {
        internal NpcVoiceOverridesDocument Document = new NpcVoiceOverridesDocument();
        internal bool Valid;
        internal bool Created;
        internal string Error = string.Empty;
    }

    internal static class NpcVoiceOverridesStorage
    {
        internal static NpcVoiceOverridesLoadResult Load(string path, bool createIfMissing)
        {
            NpcVoiceOverridesLoadResult result = new NpcVoiceOverridesLoadResult();
            try
            {
                if (!File.Exists(path))
                {
                    result.Valid = true;
                    if (createIfMissing)
                    {
                        WriteSafely(path, NpcVoiceOverridesDocument.EmptyJson);
                        result.Created = true;
                    }
                    return result;
                }
                result.Document = NpcVoiceOverridesDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
                result.Valid = true;
            }
            catch (Exception ex)
            {
                result.Document = new NpcVoiceOverridesDocument();
                result.Valid = false;
                result.Error = ex.Message;
            }
            return result;
        }

        internal static void Save(string path, NpcVoiceOverridesDocument document)
        {
            string error = document == null ? "NPC Voice Overrides document is missing." : document.Validate();
            if (error.Length > 0) throw new FormatException(error);
            WriteSafely(path, document.ToJson());
        }

        private static void WriteSafely(string path, string content)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string temporary = path + ".tmp";
            try
            {
                File.WriteAllText(temporary, content, new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
            }
        }
    }
}
