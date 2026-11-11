using System;
using System.Collections.Generic;
using System.Globalization;

namespace ObenseuerTts
{
    internal sealed class NpcIdentityRecord
    {
        internal string InternalName = string.Empty;
        internal string DisplayName = string.Empty;
        internal string Gender = string.Empty;
        internal string[] Aliases = new string[0];
        internal string GameCharacterId = string.Empty;
        internal string VerificationStatus = string.Empty;
        internal string ResearchNotes = string.Empty;
        internal string Region = string.Empty;
        internal string Source = string.Empty;
        internal string PreferredVoice = string.Empty;
    }

    internal sealed class NpcIdentityMatch
    {
        internal NpcIdentityRecord Record;
        internal bool Ambiguous;
        internal string Method = string.Empty;
    }

    internal sealed class NpcIdentityDatabase
    {
        private readonly List<NpcIdentityRecord> records = new List<NpcIdentityRecord>();
        private readonly Dictionary<string, List<NpcIdentityRecord>> names = new Dictionary<string, List<NpcIdentityRecord>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<NpcIdentityRecord>> firstNames = new Dictionary<string, List<NpcIdentityRecord>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<NpcIdentityRecord>> gameIds = new Dictionary<string, List<NpcIdentityRecord>>(StringComparer.OrdinalIgnoreCase);

        internal int Count { get { return records.Count; } }
        internal IList<NpcIdentityRecord> Records { get { return records.AsReadOnly(); } }

        internal static NpcIdentityDatabase Parse(string json)
        {
            SimpleJsonValue root = SimpleJson.Parse(json);
            if (root.Kind != SimpleJsonKind.Object) throw new FormatException("NPC database root must be an object.");
            SimpleJsonValue characters = root.Get("characters");
            if (characters == null || characters.Kind != SimpleJsonKind.Array) throw new FormatException("NPC database requires a characters array.");

            NpcIdentityDatabase database = new NpcIdentityDatabase();
            foreach (SimpleJsonValue value in characters.Array)
            {
                if (value == null || value.Kind != SimpleJsonKind.Object) throw new FormatException("Every NPC database entry must be an object.");
                NpcIdentityRecord record = new NpcIdentityRecord();
                record.InternalName = Clean(value.StringOrEmpty("internalName"));
                record.DisplayName = Clean(value.StringOrEmpty("displayName"));
                record.Gender = Clean(value.StringOrEmpty("gender")).ToLowerInvariant();
                record.Aliases = ReadStringArray(value.Get("aliases"));
                record.GameCharacterId = ReadId(value.Get("gameCharacterId"));
                if (record.GameCharacterId.Length == 0) record.GameCharacterId = ReadId(value.Get("gameId"));
                record.VerificationStatus = Clean(value.StringOrEmpty("verificationStatus"));
                record.ResearchNotes = Clean(value.StringOrEmpty("researchNotes"));
                record.Region = Clean(value.StringOrEmpty("region"));
                record.Source = Clean(value.StringOrEmpty("source"));
                record.PreferredVoice = Clean(value.StringOrEmpty("preferredVoice"));
                if (record.InternalName.Length == 0 || record.DisplayName.Length == 0)
                    throw new FormatException("NPC database entries require internalName and displayName.");
                database.Add(record);
            }
            return database;
        }

        internal NpcIdentityMatch Resolve(string gameCharacterId, string name)
        {
            string id = Clean(gameCharacterId);
            if (id.Length > 0)
            {
                NpcIdentityMatch byId = ResolveIndex(gameIds, id, "verified game character ID");
                if (byId.Record != null || byId.Ambiguous) return byId;
            }

            string cleanedName = Clean(name);
            if (cleanedName.Length == 0) return new NpcIdentityMatch();
            if (cleanedName.IndexOf(' ') < 0) return ResolveSingleToken(cleanedName);
            NpcIdentityMatch exact = ResolveIndex(names, cleanedName, "exact name or alias");
            if (exact.Record != null || exact.Ambiguous) return exact;
            return new NpcIdentityMatch();
        }

        private NpcIdentityMatch ResolveSingleToken(string key)
        {
            List<NpcIdentityRecord> combined = new List<NpcIdentityRecord>();
            List<NpcIdentityRecord> matches;
            if (names.TryGetValue(key, out matches))
                foreach (NpcIdentityRecord record in matches) if (!combined.Contains(record)) combined.Add(record);
            if (firstNames.TryGetValue(key, out matches))
                foreach (NpcIdentityRecord record in matches) if (!combined.Contains(record)) combined.Add(record);
            if (combined.Count == 1) return new NpcIdentityMatch { Record = combined[0], Method = "unique name or alias" };
            if (combined.Count > 1) return new NpcIdentityMatch { Ambiguous = true, Method = "ambiguous name or alias" };
            return new NpcIdentityMatch();
        }

        private void Add(NpcIdentityRecord record)
        {
            records.Add(record);
            AddIndex(names, record.InternalName, record);
            AddIndex(names, record.DisplayName, record);
            foreach (string alias in record.Aliases) AddIndex(names, alias, record);
            AddIndex(firstNames, FirstName(record.DisplayName), record);
            if (record.GameCharacterId.Length > 0) AddIndex(gameIds, record.GameCharacterId, record);
        }

        private static NpcIdentityMatch ResolveIndex(Dictionary<string, List<NpcIdentityRecord>> index, string key, string method)
        {
            List<NpcIdentityRecord> matches;
            if (!index.TryGetValue(key, out matches) || matches.Count == 0) return new NpcIdentityMatch();
            if (matches.Count == 1) return new NpcIdentityMatch { Record = matches[0], Method = method };
            return new NpcIdentityMatch { Ambiguous = true, Method = "ambiguous " + method };
        }

        private static void AddIndex(Dictionary<string, List<NpcIdentityRecord>> index, string key, NpcIdentityRecord record)
        {
            string cleaned = Clean(key);
            if (cleaned.Length == 0) return;
            List<NpcIdentityRecord> matches;
            if (!index.TryGetValue(cleaned, out matches))
            {
                matches = new List<NpcIdentityRecord>();
                index[cleaned] = matches;
            }
            if (!matches.Contains(record)) matches.Add(record);
        }

        private static string[] ReadStringArray(SimpleJsonValue value)
        {
            if (value == null) return new string[0];
            if (value.Kind != SimpleJsonKind.Array) throw new FormatException("NPC aliases must be an array.");
            List<string> result = new List<string>();
            foreach (SimpleJsonValue item in value.Array)
            {
                if (item.Kind != SimpleJsonKind.String) throw new FormatException("NPC aliases must contain strings.");
                string alias = Clean(item.String);
                if (alias.Length > 0 && !result.Exists(existing => string.Equals(existing, alias, StringComparison.OrdinalIgnoreCase))) result.Add(alias);
            }
            return result.ToArray();
        }

        private static string ReadId(SimpleJsonValue value)
        {
            if (value == null || value.Kind == SimpleJsonKind.Null) return string.Empty;
            if (value.Kind == SimpleJsonKind.String) return Clean(value.String);
            if (value.Kind == SimpleJsonKind.Number && value.Number == Math.Truncate(value.Number))
                return value.Number.ToString("0", CultureInfo.InvariantCulture);
            throw new FormatException("gameCharacterId must be a string, integer or null.");
        }

        private static string FirstName(string value)
        {
            string cleaned = Clean(value);
            int space = cleaned.IndexOf(' ');
            return space < 0 ? cleaned : cleaned.Substring(0, space);
        }

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }
}
