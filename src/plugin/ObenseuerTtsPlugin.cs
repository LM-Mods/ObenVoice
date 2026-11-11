using HarmonyLib;
using ObenseuerCore;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerTts
{
    internal static class Paths
    {
        internal static string GameRootPath
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        internal static string PluginPath
        {
            get { return Path.Combine(GameRootPath, "BepInEx", "plugins"); }
        }

        internal static string ConfigPath
        {
            get { return Path.Combine(GameRootPath, "BepInEx", "config"); }
        }
    }

    internal static class ModConfigCompatibility
    {
        // ObenseuerCore creates the backing ConfigFile with SaveOnConfigSet enabled.
        internal static void Save(this ModConfig config) { }
    }

    internal struct CompatibilityLogger
    {
        private readonly CoreLog log;

        internal CompatibilityLogger(CoreLog log)
        {
            this.log = log;
        }

        internal void LogInfo(string message) { log.Info(message); }
        internal void LogWarning(string message) { log.Warn(message); }
        internal void LogError(string message) { log.Error(message); }
    }

    [Serializable]
    public sealed class CharacterDatabaseFile
    {
        public CharacterRecord[] characters = new CharacterRecord[0];
    }

    [Serializable]
    public sealed class CharacterRecord
    {
        public string internalName;
        public string displayName;
        public string gender;
        public string[] aliases = new string[0];
        public string region;
        public string source;
        public string preferredVoice;
    }

    [Serializable]
    internal sealed class EngineAddonManifest
    {
        public string id = string.Empty;
    }

    [ModInfo("com.kokoro.tts.obenseuer", "ObenVoice", "4.2.0")]
    [ModDependency("obenseuer.moddingframework", "0.12.1")]
    public sealed class ObenseuerTtsPlugin : FrameworkMod
    {
        internal static ObenseuerTtsPlugin Instance;

        private CompatibilityLogger Logger
        {
            get { return new CompatibilityLogger(Log); }
        }

        private ModSetting<string> host;
        private ModSetting<int> port;
        private ModSetting<string> voice;
        private ModSetting<bool> autoConnect;
        private ModSetting<string> femaleVoicePool;
        private ModSetting<string> maleVoicePool;
        private ModSetting<string> unknownVoicePool;
        private ModSetting<string> femaleCharacterNames;
        private ModSetting<string> maleCharacterNames;
        private ModSetting<bool> enableStableVariation;
        private ModSetting<bool> enableVoiceBlending;
        private ModSetting<int> voiceBlendChancePercent;
        private ModSetting<float> minimumSpeed;
        private ModSetting<float> maximumSpeed;
        private ModSetting<float> minimumPitch;
        private ModSetting<float> maximumPitch;
        private ModSetting<string> variationSeed;
        private ModSetting<string> playerVoice;
        private ModSetting<float> playerSpeed;
        private ModSetting<float> playerPitch;
        private ModSetting<bool> speakNpcDialogue;
        private ModSetting<bool> speakPlayerDialogue;
        private ModSetting<bool> speakBarks;
        private ModSetting<bool> prefixSpeakerName;
        private ModSetting<bool> muteOriginalDialogueAudio;
        private ModSetting<bool> muteOriginalBarkAudio;
        private ModSetting<int> minimumCharacters;
        private ModSetting<int> maximumCharacters;
        private ModSetting<string> ignoredExactTerms;
        private ModSetting<float> duplicateCooldownSeconds;
        private ModSetting<bool> unknownSpeakerPrompt;
        private ModSetting<bool> autoStartBackend;
        private ModSetting<bool> closeBackendOnGameExit;
        private ModSetting<string> preferredTtsEngine;
        private ModSetting<string> selectedEngineVoicePack;
        private ModSetting<string> kokoroVoiceMode;
        private ModSetting<string> selectedKokoroVoicePack;
        private ModSetting<string> selectedNpcVoicePreset;
        private ModSetting<bool> debugLogging;
        private ModSetting<bool> showStatusOverlay;
        private ModSetting<string> settingsHotkey;
        private ModSetting<string> playerVoiceHotkey;
        private ModSetting<string> flagDialogueHotkey;

        private readonly HashSet<string> femaleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> maleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private NpcIdentityDatabase npcDatabase = new NpcIdentityDatabase();
        private NpcVoiceOverridesDocument npcVoiceOverrides = new NpcVoiceOverridesDocument();
        private string npcVoiceOverridesPath = string.Empty;
        private string npcVoiceOverridesStatus = string.Empty;
        private bool npcVoiceOverridesLoadValid = true;
        private readonly Dictionary<string, float> recentDialogue = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Queue<PendingUnknown> pendingUnknownSpeakers = new Queue<PendingUnknown>();
        private readonly HashSet<string> queuedUnknownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly SpeakerAnnouncementState speakerAnnouncementState = new SpeakerAnnouncementState();
        private readonly DialogueDiagnostics dialogueDiagnostics = new DialogueDiagnostics();
        private readonly UnknownSpeakerTracker unknownSpeakerTracker = new UnknownSpeakerTracker();
        private readonly List<DiscoveredVoice> availableKokoroVoices = new List<DiscoveredVoice>();
        private readonly List<DiscoveredVoicePack> discoveredKokoroPacks = new List<DiscoveredVoicePack>();
        private readonly List<NpcVoicePreset> discoveredNpcPresets = new List<NpcVoicePreset>();
        private readonly HashSet<string> installedKokoroVoiceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string[] femaleVoices = new string[0];
        private string[] maleVoices = new string[0];
        private string[] unknownVoices = new string[0];

        private TcpClient client;
        private StreamWriter writer;
        private Thread readerThread;
        private readonly object connectionLock = new object();
        private float nextReconnectAt;
        private float nextStatusPollAt;
        private int connectInProgress;
        private int restartInProgress;
        private readonly object backendLifecycleLock = new object();
        private Process backendProcess;
        private bool startedBackend;
        private bool backendStartAttempted;
        private int backendShutdownStarted;
        private string backendState = "Initialising";
        private string runningTtsEngine = "Not running";
        private string launchedTtsEngine = string.Empty;
        private bool runningEngineConfirmed;
        private string lastSceneName = string.Empty;
        private float nextGameStateCheckAt;
        private bool lastConversationActive;
        private bool queueNextNpcAfterPlayer;
        private DateTime queueNextNpcAfterPlayerUntilUtc;
        private PendingUnknown activeUnknown;
        private bool choosingUnknownVoice;
        private bool rememberUnknownChoice = true;
        private bool showSettingsWindow;
        private bool showPlayerVoiceWindow;
        private bool showNpcOverridesWindow;
        private Vector2 femaleVoiceScroll;
        private Vector2 maleVoiceScroll;
        private Vector2 playerVoiceScroll;
        private Vector2 settingsVoicePickerScroll;
        private Vector2 overrideNpcScroll;
        private Vector2 overrideVoiceScroll;
        private Vector2 voicePackSelectorScroll;
        private Vector2 npcPresetSelectorScroll;
        private int settingsPage;
        private bool confirmResetDefaults;
        private bool confirmClearDiagnosticLog;
        private bool settingsCursorCaptured;
        private CursorLockMode cursorLockBeforeSettings;
        private bool cursorVisibleBeforeSettings;
        private bool showDefaultVoiceChoices;
        private bool showEngineChoices;
        private bool showQuickPlayerVoiceChoices;
        private bool showVoicePackChoices;
        private bool showNpcPresetChoices;
        private bool showOverrideVoiceChoices;
        private bool showAutomaticVariation;
        private bool overrideAddMode;
        private string overrideSearch = string.Empty;
        private NpcIdentityRecord overrideSelectedNpc;
        private NpcVoiceEntry overrideSelectedEntry;
        private string overrideVoice = string.Empty;
        private float overrideSpeed = 1.0f;
        private float overridePitch;
        private string overrideBaselineVoice = string.Empty;
        private float overrideBaselineSpeed = 1.0f;
        private float overrideBaselinePitch;
        private string overrideEditorStatus = string.Empty;
        private int capturingHotkey;
        private string diagnosticStatus = "Press the flag key after a problem to save the four most recent dialogue events.";
        private string notificationText = string.Empty;
        private float notificationUntil;
        private readonly List<string> manualEngineVoicePacks = new List<string>();
        private readonly HashSet<string> installedEngineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kokoro" };
        private bool manualEngineVoicePacksAvailable;
        private int pronunciationRuleCount;
        private EditableSpeechRuleDocument pronunciationDocument = new EditableSpeechRuleDocument();
        private EditableSpeechRuleDocument exclusionDocument = new EditableSpeechRuleDocument();
        private Vector2 pronunciationScroll;
        private Vector2 exclusionScroll;
        private string pronunciationOriginal = string.Empty;
        private string pronunciationReplacement = string.Empty;
        private string exclusionSpeaker = string.Empty;
        private string exclusionText = string.Empty;
        private int selectedPronunciation = -1;
        private int selectedExclusion = -1;
        private bool confirmPronunciationReset;
        private bool confirmExclusionReset;
        private bool typingInRuleEditor;
        private const string DefaultPronunciationFile = "# Speech-only pronunciation replacements. Format: source => spoken replacement\r\n# Matching is case-insensitive and respects word boundaries.\r\n\r\nHmh => Hmm\r\n";
        private const string DefaultExclusionFile = SpeechExclusionDefaults.DefaultContent;
        private static readonly string[] BundledKokoroVoices = new string[] { "af_heart", "af_bella", "af_sarah", "am_adam", "am_michael", "am_onyx" };

        private enum DialogueKind
        {
            Npc,
            Player,
            Bark
        }

        private sealed class PendingUnknown
        {
            internal string Name;
            internal string RawText;
            internal string CleanText;
            internal string SpeakerId;
            internal DialogueKind Kind;
        }

        private sealed class SpeakerResolution
        {
            internal string RawName;
            internal string MatchedName;
            internal string SpeakerId;
            internal string Gender;
            internal string MatchMethod;
            internal string GenderSource;
            internal string VoiceSource;
            internal SpeechProfile Profile;
            internal bool Ignore;
            internal NpcIdentityRecord Identity;
            internal string GameCharacterId;
            internal bool NeedsAutomaticSave;
        }

        private sealed class DiscoveredVoice
        {
            internal string Id;
            internal string DisplayName;
            internal string Gender;
            internal string PackId;
            internal string PackName;
            internal string ShortCode;
            internal bool External;
        }

        private sealed class DiscoveredVoicePack
        {
            internal string Id;
            internal string Name;
            internal string ShortCode;
            internal readonly List<string> VoiceIds = new List<string>();
        }

        private struct SpeechProfile
        {
            internal string Voice;
            internal float Speed;
            internal float Pitch;

            internal SpeechProfile(string voiceName, float speechSpeed, float pitchSemitones)
            {
                Voice = voiceName;
                Speed = speechSpeed;
                Pitch = pitchSemitones;
            }
        }

        protected override void OnLoad()
        {
            Instance = this;
            ScanInstalledEngineAddons();
            host = Config.Bind("Connection", "Host", "127.0.0.1", "Address of the local voice service. Keep the default for the packaged offline backend.");
            port = Config.Bind("Connection", "BackendPort", 5050, "Port used by the local voice service. Change it only if another program uses this port.");
            autoConnect = Config.Bind("Connection", "AutoConnect", true, "Reconnect automatically while the local voice service starts or recovers.");
            voice = Config.Bind("Voices", "DefaultVoice", "af_heart", "Fallback voice used when character-specific voices are unavailable or disabled.");
            femaleVoicePool = Config.Bind("Voices", "FemaleVoicePool", "af_heart,af_bella,af_sarah", "Voices used for known female characters.");
            maleVoicePool = Config.Bind("Voices", "MaleVoicePool", "am_adam,am_michael,am_onyx", "Voices used for known male characters.");
            unknownVoicePool = Config.Bind("Voices", "UnknownVoicePool", "af_heart,af_bella,af_sarah,am_adam,am_michael,am_onyx", "Voices used when a speaker is not in either list.");
            enableStableVariation = Config.Bind("Stable character variation", "Enabled", true, "Give every named speaker a deterministic voice, speed, and pitch profile that remains unchanged between sessions.");
            enableVoiceBlending = Config.Bind("Stable character variation", "EnableVoiceBlending", true, "Allow some characters to use a stable blend of two voices from the appropriate pool.");
            voiceBlendChancePercent = Config.Bind("Stable character variation", "VoiceBlendChancePercent", 30, "Chance that a named character receives a stable two-voice blend when the selected engine supports blending.", 0, 100);
            minimumSpeed = Config.Bind("Stable character variation", "MinimumSpeed", 0.94f, "Lowest deterministic Kokoro character speech speed.", 0.5f, 2.0f);
            maximumSpeed = Config.Bind("Stable character variation", "MaximumSpeed", 1.06f, "Highest deterministic Kokoro character speech speed.", 0.5f, 2.0f);
            minimumPitch = Config.Bind("Stable character variation", "MinimumPitchSemitones", -1.0f, "Lowest Kokoro pitch adjustment. Keeping within about -2 sounds most natural.", -6.0f, 6.0f);
            maximumPitch = Config.Bind("Stable character variation", "MaximumPitchSemitones", 1.0f, "Highest Kokoro pitch adjustment. Keeping within about +2 sounds most natural.", -6.0f, 6.0f);
            variationSeed = Config.Bind("Stable character variation", "VariationSeed", "Obenseuer", "Advanced: change this only if you want every automatic Kokoro character voice to be reassigned.");
            playerVoice = Config.Bind("Player dialogue", "PlayerVoice", "af_heart", "Voice used only for the response that the player confirms.");
            playerSpeed = Config.Bind("Player dialogue", "PlayerSpeed", 1.0f, "How fast the player's chosen replies are spoken. 1.0 is normal speed. Only used by Kokoro.", 0.5f, 2.0f);
            playerPitch = Config.Bind("Player dialogue", "PlayerPitchSemitones", 0.0f, "Player voice tone: move left for deeper or right for higher. 0 is normal. Only used by Kokoro.", -6.0f, 6.0f);
            femaleCharacterNames = Config.Bind("Character lists", "FemaleCharacters",
                "Agnes Takala;Amanda Korhonen;Anya;Ayla Green;Caroline;Eija Lurppu;Eliana;Elli Horn;Elsa Varis;Emilie;Fenella Hunt;Ines Hietala;Ingrid Claesson;Leena Borg;Liouba Bennett;Lizzy Mopper;Martta Lapinjoki;Meri Haka;Milla Kolleen;Mirjam Freighter;Mona Hall;Nina Lund;Pia Horn;Sara Claine;Sirkku Maltanen;Tatyana Gopnikova;Tea Henderson;Vivi Bondar;Zoya Gubenko",
                "Advanced built-in list. Leave this alone unless you are correcting a character match.");
            maleCharacterNames = Config.Bind("Character lists", "MaleCharacters",
                "Adam Holmgren;Aleksi Kivi;Axel Norberg;Börje Nyberg;Crazy Neighbor;Daan Claesson;David Lund;Director Murhapuro;Einar Kaivuri;Elias Karhu;Father Tobias;Fredrik Walker;Hall Kääbus;Hank Möbelmann;Hanss;Harri Hyvinkää;Heikki Horn;Heikki Mailanen;Henrik Korpinen;Jappe Järvenpää;Jarmo Pettinen;Jaska Virtanen;Jesper Kumpula;Jim Nilsen;Joel Hietala;Johannes Eskola;Jonne Reis;Jörge Af Guldsköld;Juha Tuusula;Kalle Ketku;Karl Hagen;Keijo Korsonen;Kirka Kääbus;Kössi Kelmi;Lasse Lurjus;Leon Borg;Levo Claesson;Louie Green;Lukas;Malone Petronis;Mason Jokinen;Max Masher;Mike Palmer;Moray Larson;Niko Kerava;Olle Leino;Pate Rantanen;Patrik Axelsson;Pentti Penttilä;Pera Harju;Per Otter;Petro Blokuvati;Samuel Jonasson;Sami Claesson;Taneli Kurju;Tapani Kattila;Veeti Pärisevä;Ville Sköldgangster;Walt Bondar",
                "Advanced built-in list. Leave this alone unless you are correcting a character match.");
            speakNpcDialogue = Config.Bind("Dialogue", "SpeakNpcDialogue", true, "Read NPC conversation lines.");
            speakPlayerDialogue = Config.Bind("Dialogue", "SpeakPlayerDialogue", false, "Read the player character's conversation lines.");
            speakBarks = Config.Bind("Dialogue", "SpeakBarks", true, "Read short world/NPC bark subtitles.");
            prefixSpeakerName = Config.Bind("Dialogue", "PrefixSpeakerName", false, "Say the speaker name when a speaker begins speaking in a conversation.");
            muteOriginalDialogueAudio = Config.Bind("Original audio", "MuteOriginalDialogueAudio", true, "Mute Obenseuer's original character sounds during conversations.");
            muteOriginalBarkAudio = Config.Bind("Original audio", "MuteOriginalBarkAudio", true, "Mute Obenseuer's original character sounds during world barks.");
            minimumCharacters = Config.Bind("Filtering", "MinimumCharacters", 2, "Recommended default: ignore tiny bits of text after the game formatting is cleaned away.", 1, 100);
            maximumCharacters = Config.Bind("Filtering", "MaximumCharacters", 1000, "Safety limit for an unusually long line. Leave at the default unless normal dialogue is being cut short.", 50, 5000);
            ignoredExactTerms = Config.Bind("Filtering", "IgnoredExactTerms", "", "Leave blank normally. Add a complete word or line here only if you never want it spoken. Separate multiple entries with commas.");
            duplicateCooldownSeconds = Config.Bind("Filtering", "DuplicateCooldownSeconds", 3.0f, "Recommended default: stops an accidental immediate repeat. Set to 0 only if you want every repeat spoken.", 0.0f, 30.0f);
            unknownSpeakerPrompt = Config.Bind("Speaker matching", "UnknownSpeakerPrompt", true, "Recommended: ask you once when the mod cannot safely identify a named speaker.");
            autoStartBackend = Config.Bind("Backend", "AutoStartBackend", true, "Start the packaged local backend automatically when Steam launches the game.");
            closeBackendOnGameExit = Config.Bind("Backend", "CloseBackendOnGameExit", true, "Close only the backend process started by this plugin when the game exits.");
            preferredTtsEngine = Config.Bind("Backend", "PreferredTtsEngine", "kokoro", "Voice Engine. Enter an installed engine ID (for example, kokoro). Apply it from F8 or restart the game.");
            // The legacy key name is retained so existing optional-engine voice-pack selections remain compatible.
            selectedEngineVoicePack = Config.Bind("Voice packs", "SelectedPocketVoicePack", "", "Engine Voice Pack. Optional folder used by a compatible voice engine add-on. Leave blank for that engine's default voice; applied on the next engine start.");
            kokoroVoiceMode = Config.Bind("Voice packs", "KokoroVoiceMode", "custom", "Automatic Kokoro voice source: default, all, pack or custom. Custom preserves the individual voice toggles.");
            selectedKokoroVoicePack = Config.Bind("Voice packs", "SelectedKokoroVoicePack", "", "Stable ID of the external Kokoro pack used when KokoroVoiceMode is pack.");
            selectedNpcVoicePreset = Config.Bind("NPC voices", "SelectedPreset", "", "Stable ID of the selected installed NPC voice preset. Blank uses Default automatic behaviour.");
            debugLogging = Config.Bind("Diagnostics", "DebugLogging", false, "Write continuous detailed troubleshooting messages to the game log. Leave off during normal play.");
            showStatusOverlay = Config.Bind("Diagnostics", "ShowStatusOverlay", true, "Show brief messages while the voice service starts, reconnects or needs attention.");
            settingsHotkey = Config.Bind("General", "SettingsHotkey", "F8", "Open or close the full ObenVoice settings menu.");
            playerVoiceHotkey = Config.Bind("General", "PlayerVoiceHotkey", "F9", "Open or close the player voice controls.");
            flagDialogueHotkey = Config.Bind("Diagnostics", "FlagDialogueHotkey", "End", "Save the four most recent dialogue events to the bounded diagnostic log without enabling continuous detailed logging.");

            if (!IsInstalledEngine(preferredTtsEngine.Value))
            {
                Logger.LogWarning("Selected TTS engine '" + preferredTtsEngine.Value + "' is not currently installed; Kokoro will run until an available engine is selected.");
            }

            ScanInstalledVoicePacks();
            LoadPronunciationRules();
            LoadEditableRuleFiles();
            LoadVoiceRules();
            LoadCharacterDatabase();
            LoadNpcVoiceOverrides(true);
            try { unknownSpeakerTracker.Load(GetUnknownSpeakerLogPath()); }
            catch (Exception ex) { Logger.LogWarning("Could not load unknown-speaker tracking log: " + ex.Message); }
            if (manualEngineVoicePacksAvailable) ScanManualEngineVoicePacks();
            Harmony harmony = new Harmony("com.kokoro.tts.obenseuer");
            harmony.PatchAll();
            bool cameraPatchInstalled = false;
            Patches cameraPatches = Harmony.GetPatchInfo(AccessTools.Method(typeof(ThirdPersonCameraController), "LateUpdate"));
            if (cameraPatches != null)
            {
                foreach (Patch patch in cameraPatches.Prefixes)
                {
                    if (string.Equals(patch.owner, "com.kokoro.tts.obenseuer", StringComparison.Ordinal))
                    {
                        cameraPatchInstalled = true;
                        break;
                    }
                }
            }
            Logger.LogInfo("Camera look diagnostic: ThirdPersonCameraController.LateUpdate prefix installed=" + cameraPatchInstalled);
            nextReconnectAt = 0f;
            nextStatusPollAt = 0f;
            nextGameStateCheckAt = 0f;
            lastSceneName = SceneManager.GetActiveScene().name;
            try { lastConversationActive = DialogueManager.isConversationActive; } catch { }
            if (autoStartBackend.Value) StartBackendIfNeeded();
            BeginConnect();
            Logger.LogInfo("ObenVoice 4.2.0 development build loaded.");
        }

        private void LoadVoiceRules()
        {
            femaleNames.Clear();
            maleNames.Clear();
            AddNames(femaleNames, femaleCharacterNames.Value);
            AddNames(maleNames, maleCharacterNames.Value);
            femaleVoices = BuildActiveVoicePool("female", "af_heart");
            maleVoices = BuildActiveVoicePool("male", "am_adam");
            HashSet<string> enabledNpcVoices = new HashSet<string>(femaleVoices, StringComparer.OrdinalIgnoreCase);
            enabledNpcVoices.UnionWith(maleVoices);
            unknownVoices = VoiceSelectionRules.BuildRuntimePool(unknownVoicePool.Value, enabledNpcVoices, femaleVoices.Length > 0 ? femaleVoices[0] : "af_heart");
        }

        private string[] BuildActiveVoicePool(string gender, string fallback)
        {
            string mode = (kokoroVoiceMode == null ? "custom" : kokoroVoiceMode.Value ?? "custom").Trim().ToLowerInvariant();
            if (mode == "default")
            {
                List<string> defaults = new List<string>();
                foreach (DiscoveredVoice item in availableKokoroVoices)
                    if (!item.External && string.Equals(item.Gender, gender, StringComparison.OrdinalIgnoreCase)) defaults.Add(item.Id);
                return defaults.Count == 0 ? new string[] { fallback } : defaults.ToArray();
            }
            if (mode == "all")
            {
                List<string> all = new List<string>();
                foreach (DiscoveredVoice item in availableKokoroVoices)
                    if (string.Equals(item.Gender, gender, StringComparison.OrdinalIgnoreCase) && !all.Contains(item.Id)) all.Add(item.Id);
                return all.Count == 0 ? new string[] { fallback } : all.ToArray();
            }
            if (mode == "pack")
            {
                DiscoveredVoicePack pack = FindKokoroPack(selectedKokoroVoicePack == null ? string.Empty : selectedKokoroVoicePack.Value);
                List<string> voices = new List<string>();
                if (pack != null)
                    foreach (string id in pack.VoiceIds)
                    {
                        DiscoveredVoice item = FindVoice(id);
                        if (item != null && string.Equals(item.Gender, gender, StringComparison.OrdinalIgnoreCase)) voices.Add(item.Id);
                    }
                if (voices.Count > 0) return voices.ToArray();
                return BuildDefaultVoicePool(gender, fallback);
            }
            string configured = string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase) ? maleVoicePool.Value : femaleVoicePool.Value;
            return VoiceSelectionRules.BuildRuntimePool(configured, installedKokoroVoiceIds, fallback);
        }

        private string[] BuildDefaultVoicePool(string gender, string fallback)
        {
            List<string> values = new List<string>();
            foreach (DiscoveredVoice item in availableKokoroVoices)
                if (!item.External && string.Equals(item.Gender, gender, StringComparison.OrdinalIgnoreCase)) values.Add(item.Id);
            return values.Count == 0 ? new string[] { fallback } : values.ToArray();
        }

        private DiscoveredVoicePack FindKokoroPack(string id)
        {
            return discoveredKokoroPacks.Find(item => string.Equals(item.Id, id ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        private DiscoveredVoice FindVoice(string id)
        {
            return availableKokoroVoices.Find(item => string.Equals(item.Id, id ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        private string GetObenVoiceRoot()
        {
            return Path.Combine(Paths.GameRootPath, "ObenVoice");
        }

        private string GetLegacyRoot()
        {
            return Path.Combine(Paths.GameRootPath, "ObenseuerTTS");
        }

        private string GetBackendRoot()
        {
            string primary = GetObenVoiceRoot();
            if (File.Exists(Path.Combine(primary, "runtime", "python.exe")) && File.Exists(Path.Combine(primary, "backend", "tts_server.py"))) return primary;
            string legacy = GetLegacyRoot();
            if (File.Exists(Path.Combine(legacy, "runtime", "python.exe")) && File.Exists(Path.Combine(legacy, "backend", "tts_server.py"))) return legacy;
            return primary;
        }

        private string GetModelCacheRoot(string backendRoot)
        {
            string primary = Path.Combine(GetObenVoiceRoot(), "model-cache");
            if (Directory.Exists(primary)) return primary;
            string legacy = Path.Combine(GetLegacyRoot(), "model-cache");
            return Directory.Exists(legacy) ? legacy : Path.Combine(backendRoot, "model-cache");
        }

        private string GetUserDataRoot()
        {
            string primary = Path.Combine(GetObenVoiceRoot(), "UserData");
            string legacy = GetLegacyRoot();
            string[] names = new string[] { "npc-voice-overrides.json", "pronunciation-replacements.txt", "speech-exclusions.txt" };
            foreach (string name in names) if (File.Exists(Path.Combine(primary, name))) return primary;
            foreach (string name in names) if (File.Exists(Path.Combine(legacy, name))) return legacy;
            return primary;
        }

        private string GetLogRoot()
        {
            string primary = Path.Combine(GetObenVoiceRoot(), "Logs");
            string legacy = GetLegacyRoot();
            string[] names = new string[] { "tts_server.log", "dialogue-diagnostics.log", "unknown-speakers.tsv" };
            foreach (string name in names) if (File.Exists(Path.Combine(primary, name))) return primary;
            foreach (string name in names) if (File.Exists(Path.Combine(legacy, name))) return legacy;
            return primary;
        }

        private void LoadCharacterDatabase()
        {
            npcDatabase = new NpcIdentityDatabase();

            string path = Path.Combine(GetCharactersRoot(), "characters.json");
            string legacyPath = Path.Combine(Paths.PluginPath, "ObenseuerDialogueTTS", "characters.json");
            if (!File.Exists(path) && File.Exists(legacyPath)) path = legacyPath;
            if (!File.Exists(path))
            {
                Logger.LogWarning("Character database is missing from both the primary and legacy locations.");
                return;
            }

            try
            {
                npcDatabase = NpcIdentityDatabase.Parse(File.ReadAllText(path, Encoding.UTF8));
                Logger.LogInfo("Loaded " + npcDatabase.Count + " Obenseuer character records from " + path + ".");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not load bundled speaker database: " + ex.Message);
            }
        }

        private void LoadNpcVoiceOverrides(bool createIfMissing)
        {
            npcVoiceOverridesPath = Path.Combine(GetUserDataRoot(), "npc-voice-overrides.json");
            NpcVoiceOverridesLoadResult loaded = NpcVoiceOverridesStorage.Load(npcVoiceOverridesPath, createIfMissing);
            npcVoiceOverrides = loaded.Document;
            npcVoiceOverridesLoadValid = loaded.Valid;
            if (loaded.Valid)
            {
                if (loaded.Created)
                {
                    npcVoiceOverridesStatus = "Created empty NPC Voice Overrides file";
                }
                else npcVoiceOverridesStatus = "Loaded " + npcVoiceOverrides.Overrides.Count + " NPC Voice Overrides";
                Logger.LogInfo(npcVoiceOverridesStatus + ".");
            }
            else
            {
                npcVoiceOverridesStatus = "Overrides file is invalid; original preserved";
                Logger.LogWarning("Could not load NPC Voice Overrides; the existing file was not changed: " + loaded.Error);
            }
        }

        private bool SaveNpcVoiceOverrides()
        {
            if (!npcVoiceOverridesLoadValid)
            {
                overrideEditorStatus = "Cannot save: repair or replace the malformed Overrides file, then reload it.";
                return false;
            }
            string error = npcVoiceOverrides.Validate();
            if (error.Length > 0)
            {
                overrideEditorStatus = error;
                return false;
            }
            try
            {
                NpcVoiceOverridesStorage.Save(npcVoiceOverridesPath, npcVoiceOverrides);
                npcVoiceOverridesStatus = "Saved " + npcVoiceOverrides.Overrides.Count + " NPC Voice Overrides";
                overrideEditorStatus = npcVoiceOverridesStatus;
                return true;
            }
            catch (Exception ex)
            {
                overrideEditorStatus = "Could not save NPC Voice Overrides";
                Logger.LogWarning(overrideEditorStatus + ": " + ex.Message);
                return false;
            }
        }

        private static CharacterDatabaseFile ParseBundledCharacterDatabase(string json)
        {
            List<CharacterRecord> records = new List<CharacterRecord>();
            MatchCollection objects = Regex.Matches(json ?? string.Empty, @"\{\s*""internalName""[\s\S]*?\}", RegexOptions.CultureInvariant);
            foreach (Match item in objects)
            {
                CharacterRecord record = new CharacterRecord();
                record.internalName = ReadJsonString(item.Value, "internalName");
                record.displayName = ReadJsonString(item.Value, "displayName");
                record.gender = ReadJsonString(item.Value, "gender");
                record.region = ReadJsonString(item.Value, "region");
                record.source = ReadJsonString(item.Value, "source");
                record.preferredVoice = ReadJsonString(item.Value, "preferredVoice");
                if (!string.IsNullOrEmpty(record.displayName)) records.Add(record);
            }
            CharacterDatabaseFile file = new CharacterDatabaseFile();
            file.characters = records.ToArray();
            return file;
        }

        private static string ReadJsonString(string jsonObject, string field)
        {
            string pattern = "\\\"" + Regex.Escape(field) + "\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"\\\\])*)\\\"";
            Match match = Regex.Match(jsonObject, pattern, RegexOptions.CultureInvariant);
            if (!match.Success) return string.Empty;
            return Regex.Unescape(match.Groups["value"].Value.Replace("\\/", "/"));
        }

        private static string FirstName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;
            string trimmed = fullName.Trim();
            int separator = trimmed.IndexOf(' ');
            return separator > 0 ? trimmed.Substring(0, separator) : trimmed;
        }

        private static void AddNames(HashSet<string> destination, string raw)
        {
            foreach (string name in SplitList(raw, ';')) destination.Add(name);
        }

        private static string[] SplitList(string raw, char separator)
        {
            if (string.IsNullOrEmpty(raw)) return new string[0];
            string[] values = raw.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            List<string> cleaned = new List<string>();
            foreach (string value in values)
            {
                string item = value.Trim();
                if (item.Length > 0) cleaned.Add(item);
            }
            return cleaned.ToArray();
        }

        private void Update()
        {
            bool textEditorOpen = typingInRuleEditor || showNpcOverridesWindow;
            if (textEditorOpen) Input.ResetInputAxes();
            if (!textEditorOpen && capturingHotkey == 0 && IsHotkeyDown(flagDialogueHotkey.Value)) FlagRecentDialogue();
            if (!textEditorOpen && capturingHotkey == 0 && IsHotkeyDown(settingsHotkey.Value))
            {
                SetPlayerVoiceWindowVisible(false);
                SetSettingsWindowVisible(!showSettingsWindow);
                confirmResetDefaults = false;
                confirmClearDiagnosticLog = false;
            }

            if (!textEditorOpen && capturingHotkey == 0 && IsHotkeyDown(playerVoiceHotkey.Value))
            {
                SetSettingsWindowVisible(false);
                SetPlayerVoiceWindowVisible(!showPlayerVoiceWindow);
            }

            RefreshMenuInputCapture();

            if (restartInProgress == 0 && autoConnect.Value && !IsConnected() && Time.unscaledTime >= nextReconnectAt)
            {
                nextReconnectAt = Time.unscaledTime + 5f;
                if (autoStartBackend.Value) StartBackendIfNeeded();
                backendState = startedBackend ? "Loading model / reconnecting" : "Reconnecting";
                BeginConnect();
            }

            if (Time.unscaledTime >= nextStatusPollAt)
            {
                nextStatusPollAt = Time.unscaledTime + 0.5f;
                PollBackendStatus();
            }

            if (Time.unscaledTime < nextGameStateCheckAt) return;
            nextGameStateCheckAt = Time.unscaledTime + 0.25f;

            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.Equals(sceneName, lastSceneName, StringComparison.Ordinal))
            {
                lastSceneName = sceneName;
                ClearDuplicateState("scene changed to " + sceneName);
            }

            bool conversationActive = false;
            try { conversationActive = DialogueManager.isConversationActive; } catch { }
            if (conversationActive != lastConversationActive)
            {
                lastConversationActive = conversationActive;
                speakerAnnouncementState.OnConversationStateChanged(conversationActive);
                ClearDuplicateState(conversationActive ? "conversation started" : "conversation ended", false);
            }
        }

        internal void HandleSubtitle(Subtitle subtitle, bool isBark)
        {
            if (subtitle == null || subtitle.formattedText == null) return;
            PixelCrushers.DialogueSystem.CharacterInfo speaker = subtitle.speakerInfo;
            DialogueKind kind = isBark ? DialogueKind.Bark : DialogueKind.Npc;
            string rawText = subtitle.formattedText.text ?? string.Empty;
            string text = CleanText(rawText);
            string rawSpeakerName = GetSpeakerName(speaker);
            string speakerDetails = DescribeSpeaker(speaker, rawSpeakerName);

            if (isBark && !speakBarks.Value)
            {
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "skipped: bark speech is disabled");
                return;
            }
            if (!isBark && speaker != null && speaker.isPlayer)
            {
                // Confirmed player responses are handled by ConversationView.SelectResponse.
                // Ignoring PC subtitles here prevents double speech and menu-option previews.
                DebugLog("Ignored player subtitle; waiting for confirmed response selection.");
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "skipped: player subtitle preview; waiting for confirmed response");
                return;
            }
            if (!isBark && speaker != null && speaker.isNPC && !speakNpcDialogue.Value)
            {
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "skipped: NPC dialogue speech is disabled");
                return;
            }
            if (SpeechExclusionRules.IsExcluded(exclusionDocument.Entries, rawSpeakerName, text))
            {
                DebugLog("Speech excluded before speaker resolution: speaker=" + rawSpeakerName + " text=" + text);
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "filtered: matched a speech exclusion rule");
                return;
            }
            if (string.IsNullOrEmpty(text))
            {
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "filtered: no speakable text remained after cleaning");
                return;
            }
            if (text.Length < Math.Max(1, minimumCharacters.Value))
            {
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "filtered: processed text is shorter than MinimumCharacters");
                return;
            }
            if (IsIgnored(text))
            {
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "filtered: matched IgnoredExactTerms");
                return;
            }

            int max = Math.Max(50, maximumCharacters.Value);
            if (text.Length > max) text = text.Substring(0, max);

            string speakerId = speaker == null ? string.Empty : "dialogue:" + speaker.id.ToString(CultureInfo.InvariantCulture);
            DebugLog("Dialogue type=" + kind + " raw speaker=" + rawSpeakerName + " speaker ID=" + speakerId + " raw text=" + rawText + " clean text=" + text);

            if (IsAccidentalDuplicate(kind, rawSpeakerName, text))
            {
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "filtered: duplicate within cooldown");
                return;
            }

            SpeakerResolution resolution;
            if (!TryResolveSpeaker(speaker, rawSpeakerName, rawText, text, kind, out resolution))
            {
                RecordDialogueDiagnostic(kind, speakerDetails, rawText, text, string.Empty, "waiting: unknown speaker choice required");
                return;
            }
            if (resolution.Ignore)
            {
                RecordDialogueDiagnostic(kind, DescribeResolution(speakerDetails, resolution), rawText, text, resolution.Profile.Voice, "filtered: saved speaker assignment is set to ignore");
                return;
            }
            string announcementKey = string.IsNullOrEmpty(resolution.SpeakerId) ? resolution.MatchedName : resolution.SpeakerId;
            if (speakerAnnouncementState.ShouldAnnounce(prefixSpeakerName.Value, !isBark, announcementKey) && resolution.MatchedName.Length > 0)
                text = resolution.MatchedName + ": " + text;

            LogResolution(resolution, kind, rawText, text);
            SaveAutomaticAssignment(resolution);
            bool followPlayer = !isBark && queueNextNpcAfterPlayer && DateTime.UtcNow <= queueNextNpcAfterPlayerUntilUtc;
            if (!isBark) queueNextNpcAfterPlayer = false;
            bool sent = SendLine(text, resolution.Profile, !followPlayer);
            RecordDialogueDiagnostic(kind, DescribeResolution(speakerDetails, resolution), rawText, text, resolution.Profile.Voice, sent ? "queued for speech" : "failed: voice service unavailable or send error");
        }

        internal void HandleSelectedResponse(SelectedResponseEventArgs args)
        {
            if (args == null || args.response == null || args.response.formattedText == null) return;
            string rawText = args.response.formattedText.text ?? string.Empty;
            string text = CleanText(rawText);
            const string playerDetails = "name=Player; source=confirmed response";
            if (!speakPlayerDialogue.Value)
            {
                RecordDialogueDiagnostic(DialogueKind.Player, playerDetails, rawText, text, string.Empty, "skipped: player dialogue speech is disabled");
                return;
            }
            if (SpeechExclusionRules.IsExcluded(exclusionDocument.Entries, "Player", text))
            {
                RecordDialogueDiagnostic(DialogueKind.Player, playerDetails, rawText, text, string.Empty, "filtered: matched a speech exclusion rule");
                return;
            }
            if (string.IsNullOrEmpty(text))
            {
                RecordDialogueDiagnostic(DialogueKind.Player, playerDetails, rawText, text, string.Empty, "filtered: no speakable text remained after cleaning");
                return;
            }
            if (text.Length < Math.Max(1, minimumCharacters.Value))
            {
                RecordDialogueDiagnostic(DialogueKind.Player, playerDetails, rawText, text, string.Empty, "filtered: processed text is shorter than MinimumCharacters");
                return;
            }
            if (IsIgnored(text))
            {
                RecordDialogueDiagnostic(DialogueKind.Player, playerDetails, rawText, text, string.Empty, "filtered: matched IgnoredExactTerms");
                return;
            }
            if (IsAccidentalDuplicate(DialogueKind.Player, "Player", text))
            {
                RecordDialogueDiagnostic(DialogueKind.Player, playerDetails, rawText, text, string.Empty, "filtered: duplicate within cooldown");
                return;
            }

            SpeechProfile profile = new SpeechProfile(
                ResolveInstalledVoiceSpec(string.IsNullOrEmpty(playerVoice.Value) ? voice.Value : playerVoice.Value.Trim(), "af_heart"),
                Clamp(playerSpeed.Value, 0.5f, 2.0f),
                Clamp(playerPitch.Value, -4.0f, 4.0f));
            DebugLog("Player line detection=confirmed response selection raw text=" + rawText + " clean text=" + text + " voice=" + profile.Voice);
            bool sent = SendLine(text, profile);
            RecordDialogueDiagnostic(DialogueKind.Player, playerDetails, rawText, text, profile.Voice, sent ? "queued for speech" : "failed: voice service unavailable or send error");
            if (sent)
            {
                queueNextNpcAfterPlayer = true;
                queueNextNpcAfterPlayerUntilUtc = DateTime.UtcNow.AddSeconds(3.0);
            }
        }

        internal bool ShouldMuteOriginalVoiceAudio()
        {
            bool conversationActive = false;
            try { conversationActive = DialogueManager.isConversationActive; } catch { }
            return conversationActive ? muteOriginalDialogueAudio.Value : muteOriginalBarkAudio.Value;
        }

        private bool TryResolveSpeaker(PixelCrushers.DialogueSystem.CharacterInfo speaker, string rawName, string rawText, string cleanText, DialogueKind kind, out SpeakerResolution resolution)
        {
            resolution = new SpeakerResolution();
            resolution.RawName = rawName ?? string.Empty;
            resolution.MatchedName = resolution.RawName;
            resolution.SpeakerId = speaker == null ? string.Empty : "dialogue:" + speaker.id.ToString(CultureInfo.InvariantCulture);
            resolution.Gender = "unknown";
            resolution.MatchMethod = "unmatched";
            resolution.GenderSource = "none";
            resolution.VoiceSource = "DefaultVoice";

            if (string.IsNullOrEmpty(resolution.RawName))
            {
                resolution.MatchMethod = "blank speaker";
                resolution.Profile = new SpeechProfile(ResolveInstalledVoiceSpec(voice.Value, "af_heart"), 1.0f, 0.0f);
                return true;
            }

            string gameName;
            string gameGender;
            string gameId;
            if (TryGetGameIdentity(speaker, out gameName, out gameGender, out gameId))
            {
                if (!string.IsNullOrEmpty(gameName)) resolution.MatchedName = gameName;
                if (!string.IsNullOrEmpty(gameId)) resolution.SpeakerId = resolution.SpeakerId + "/game:" + gameId;
                if (IsKnownGender(gameGender))
                {
                    resolution.Gender = gameGender;
                    resolution.MatchMethod = "game Character metadata";
                    resolution.GenderSource = "game Character.GenderString";
                }
            }

            NpcIdentityMatch identityMatch = null;
            NpcIdentityRecord identity = null;
            {
                identityMatch = npcDatabase.Resolve(gameId, resolution.MatchedName);
                if (identityMatch.Record == null && !identityMatch.Ambiguous && !string.Equals(resolution.MatchedName, resolution.RawName, StringComparison.OrdinalIgnoreCase))
                    identityMatch = npcDatabase.Resolve(gameId, resolution.RawName);
                identity = identityMatch.Record;
                if (identity != null)
                {
                    resolution.MatchedName = identity.DisplayName;
                    if (!IsKnownGender(resolution.Gender))
                    {
                        resolution.Gender = NormalizeGender(identity.Gender);
                        resolution.GenderSource = string.IsNullOrEmpty(identity.Source) ? "bundled database" : identity.Source;
                    }
                    resolution.MatchMethod = "bundled database " + identityMatch.Method;
                }
            }
            resolution.Identity = identity;
            resolution.GameCharacterId = gameId;

            NpcVoiceEntry explicitOverride = npcVoiceOverrides.FindUser(gameId, resolution.SpeakerId, resolution.RawName, identity);
            NpcVoicePreset selectedPreset = FindSelectedNpcPreset();
            NpcVoiceEntry presetAssignment = selectedPreset == null ? null : selectedPreset.Find(gameId, resolution.RawName, identity);
            NpcVoiceEntry savedAutomatic = npcVoiceOverrides.FindAutomatic(gameId, resolution.SpeakerId, resolution.RawName, identity);
            NpcVoiceResolutionLayer layer = NpcVoiceResolutionPriority.Resolve(explicitOverride, presetAssignment, savedAutomatic);
            if (layer == NpcVoiceResolutionLayer.Override)
            {
                ApplyNpcVoiceEntry(resolution, explicitOverride, "NPC Voice Override", true);
                return true;
            }
            if (selectedPreset != null)
            {
                if (layer == NpcVoiceResolutionLayer.Preset)
                {
                    ApplyNpcVoiceEntry(resolution, presetAssignment, "NPC preset " + selectedPreset.StableId, false);
                    return true;
                }
            }

            if (layer == NpcVoiceResolutionLayer.SavedAutomatic)
            {
                ApplyNpcVoiceEntry(resolution, savedAutomatic, "Saved automatic assignment", false);
                return true;
            }

            if (!IsKnownGender(resolution.Gender))
            {
                if (maleNames.Contains(resolution.RawName))
                {
                    resolution.Gender = "male";
                    resolution.MatchMethod = "configured exact full-name";
                    resolution.GenderSource = "MaleCharacters";
                }
                else if (femaleNames.Contains(resolution.RawName))
                {
                    resolution.Gender = "female";
                    resolution.MatchMethod = "configured exact full-name";
                    resolution.GenderSource = "FemaleCharacters";
                }
            }

            if (!IsKnownGender(resolution.Gender))
            {
                if (unknownSpeakerPrompt.Value)
                {
                    QueueUnknownSpeaker(resolution.RawName, rawText, cleanText, resolution.SpeakerId, kind);
                    DebugLog("Unknown speaker prompt queued for " + resolution.RawName);
                    return false;
                }

                resolution.MatchMethod = "unknown safe default";
                resolution.GenderSource = "unknown";
                resolution.VoiceSource = "DefaultVoice";
                resolution.Profile = BuildProfile(resolution.RawName, "unknown", ResolveInstalledVoiceSpec(voice.Value, "af_heart"), false);
                resolution.NeedsAutomaticSave = true;
                return true;
            }

            resolution.VoiceSource = string.Equals(resolution.Gender, "male", StringComparison.OrdinalIgnoreCase) ? "MaleVoicePool" : "FemaleVoicePool";
            resolution.Profile = BuildProfile(resolution.MatchedName, resolution.Gender, null, true);
            resolution.NeedsAutomaticSave = true;
            return true;
        }

        private void ApplyNpcVoiceEntry(SpeakerResolution resolution, NpcVoiceEntry entry, string source, bool explicitOverride)
        {
            string requestedVoice = entry.Voice;
            SpeechProfile profile = BuildProfile(resolution.MatchedName, resolution.Gender, requestedVoice, string.IsNullOrEmpty(requestedVoice));
            if (entry.HasSpeed) profile.Speed = Clamp(entry.Speed, 0.5f, 2.0f);
            if (entry.HasPitch) profile.Pitch = Clamp(entry.Pitch, -4.0f, 4.0f);
            resolution.Profile = profile;
            resolution.Ignore = entry.Ignore;
            resolution.MatchMethod = source;
            resolution.VoiceSource = source;
            if (requestedVoice.Length > 0 && !VoiceSelectionRules.IsVoiceSpecAvailable(requestedVoice, installedKokoroVoiceIds))
                resolution.VoiceSource += " (missing voice fallback)";
            if (explicitOverride) resolution.GenderSource = "NPC Voice Overrides";
        }

        private NpcVoicePreset FindSelectedNpcPreset()
        {
            string selected = selectedNpcVoicePreset == null ? string.Empty : selectedNpcVoicePreset.Value ?? string.Empty;
            if (selected.Trim().Length == 0)
                return discoveredNpcPresets.Find(item => string.Equals(item.StableId, "core/default", StringComparison.OrdinalIgnoreCase));
            return discoveredNpcPresets.Find(item => string.Equals(item.StableId, selected, StringComparison.OrdinalIgnoreCase));
        }

        private bool TryGetGameIdentity(PixelCrushers.DialogueSystem.CharacterInfo speaker, out string fullName, out string gender, out string characterId)
        {
            fullName = string.Empty;
            gender = string.Empty;
            characterId = string.Empty;
            if (speaker == null) return false;
            try
            {
                Transform actor = speaker.transform;
                if (actor == null && !string.IsNullOrEmpty(speaker.nameInDatabase)) actor = PixelCrushers.DialogueSystem.CharacterInfo.GetRegisteredActorTransform(speaker.nameInDatabase);
                if (actor == null) return false;

                Character character = null;
                NPCInfo info = actor.GetComponentInParent<NPCInfo>();
                if (info != null) character = info.character;
                if (character == null)
                {
                    NPCController controller = actor.GetComponentInParent<NPCController>();
                    if (controller != null) character = controller.character;
                }
                if (character == null) return false;

                fullName = character.FullName;
                gender = NormalizeGender(character.GenderString);
                characterId = character.ID.ToString(CultureInfo.InvariantCulture);
                DebugLog("Verified game character metadata name=" + fullName + " ID=" + characterId + " GenderString=" + character.GenderString);
                return true;
            }
            catch (Exception ex)
            {
                DebugLog("Game metadata lookup failed: " + ex.Message);
                return false;
            }
        }

        private SpeechProfile BuildProfile(string stableName, string gender, string forcedVoice, bool allowBlend)
        {
            string[] pool = GetEnabledPool(gender);

            if (!string.IsNullOrEmpty(forcedVoice) && !VoiceSelectionRules.IsVoiceSpecAvailable(forcedVoice, installedKokoroVoiceIds))
            {
                forcedVoice = null;
                allowBlend = true;
            }

            string selectedVoice = string.IsNullOrEmpty(forcedVoice) ? StablePoolVoice(stableName, pool, ResolveInstalledVoiceSpec(voice.Value, "af_heart")) : forcedVoice;
            if (allowBlend && enableStableVariation.Value && enableVoiceBlending.Value && pool != null && pool.Length > 1)
            {
                int chance = Math.Max(0, Math.Min(100, voiceBlendChancePercent.Value));
                if (StableUnit(stableName, "blend-chance") * 100.0 < chance)
                {
                    int first = StableIndex(stableName, "blend-first", pool.Length);
                    int second = StableIndex(stableName, "blend-second", pool.Length - 1);
                    if (second >= first) second++;
                    selectedVoice = pool[first] + "," + pool[second];
                }
            }

            if (!enableStableVariation.Value) return new SpeechProfile(selectedVoice, 1.0f, 0.0f);
            float lowSpeed = Math.Min(minimumSpeed.Value, maximumSpeed.Value);
            float highSpeed = Math.Max(minimumSpeed.Value, maximumSpeed.Value);
            float lowPitch = Math.Min(minimumPitch.Value, maximumPitch.Value);
            float highPitch = Math.Max(minimumPitch.Value, maximumPitch.Value);
            float selectedSpeed = Quantize((float)(lowSpeed + (highSpeed - lowSpeed) * StableUnit(stableName, "speed")), 0.005f);
            float selectedPitch = Quantize((float)(lowPitch + (highPitch - lowPitch) * StableUnit(stableName, "pitch")), 0.10f);
            return new SpeechProfile(selectedVoice, Clamp(selectedSpeed, 0.5f, 2.0f), Clamp(selectedPitch, -4f, 4f));
        }

        private string[] GetEnabledPool(string gender)
        {
            NpcVoicePreset preset = FindSelectedNpcPreset();
            if (preset != null && string.Equals(preset.FallbackMode, "pack", StringComparison.OrdinalIgnoreCase))
            {
                DiscoveredVoicePack pack = FindKokoroPack(preset.PackId);
                List<string> presetPool = new List<string>();
                if (pack != null)
                    foreach (string id in pack.VoiceIds)
                    {
                        DiscoveredVoice candidate = FindVoice(id);
                        if (candidate != null && (string.Equals(candidate.Gender, gender, StringComparison.OrdinalIgnoreCase) || !IsKnownGender(gender))) presetPool.Add(id);
                    }
                if (presetPool.Count > 0) return presetPool.ToArray();
            }
            if (string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase)) return maleVoices;
            if (string.Equals(gender, "female", StringComparison.OrdinalIgnoreCase)) return femaleVoices;
            return unknownVoices;
        }

        private bool IsVoiceEnabledForGender(string voiceSpec, string gender)
        {
            string[] pool = GetEnabledPool(gender);
            return !string.IsNullOrEmpty(VoiceSelectionRules.FilterVoiceSpecToPool(voiceSpec, pool));
        }

        private string ResolveSavedNpcVoice(string requested, string gender, string stableName)
        {
            string[] pool = GetEnabledPool(gender);
            string enabled = VoiceSelectionRules.FilterVoiceSpecToPool(requested, pool);
            if (!string.IsNullOrEmpty(enabled)) return enabled;
            return StablePoolVoice(stableName, pool, string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase) ? "am_adam" : "af_heart");
        }

        private string ResolveInstalledVoiceSpec(string requested, string fallback)
        {
            if (VoiceSelectionRules.IsVoiceSpecAvailable(requested, installedKokoroVoiceIds)) return requested;
            if (installedKokoroVoiceIds.Contains(fallback)) return fallback;
            return "af_heart";
        }

        private static bool IsKnownGender(string gender)
        {
            return string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase) || string.Equals(gender, "female", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeGender(string gender)
        {
            if (string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase) || string.Equals(gender, "M", StringComparison.OrdinalIgnoreCase)) return "male";
            if (string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase) || string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase)) return "female";
            return "unknown";
        }

        private int StableIndex(string speakerName, string field, int count)
        {
            if (count <= 1) return 0;
            return (int)(StableHash(speakerName, field) % (uint)count);
        }

        private double StableUnit(string speakerName, string field)
        {
            return StableHash(speakerName, field) / (double)uint.MaxValue;
        }

        private uint StableHash(string speakerName, string field)
        {
            string key = (variationSeed.Value ?? string.Empty) + "|" + (speakerName ?? string.Empty) + "|" + field;
            uint hash = 2166136261u;
            for (int i = 0; i < key.Length; i++)
            {
                hash ^= char.ToUpperInvariant(key[i]);
                hash = unchecked(hash * 16777619u);
            }
            return hash;
        }

        private static float Quantize(float value, float step)
        {
            return step <= 0f ? value : (float)Math.Round(value / step) * step;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
        private static string StablePoolVoice(string speakerName, string[] pool, string fallback)
        {
            return VoiceSelectionRules.StableVoice(speakerName, pool, fallback);
        }

        private static string GetSpeakerName(PixelCrushers.DialogueSystem.CharacterInfo speaker)
        {
            if (speaker == null) return string.Empty;
            string name = speaker.Name;
            if (string.IsNullOrEmpty(name)) name = speaker.nameInDatabase;
            if (string.IsNullOrEmpty(name)) return string.Empty;
            return Path.GetFileName(name).Trim();
        }

        private bool IsIgnored(string text)
        {
            string configured = ignoredExactTerms.Value;
            if (string.IsNullOrEmpty(configured)) return false;
            string[] terms = configured.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string term in terms)
            {
                if (string.Equals(text, term.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private bool IsAccidentalDuplicate(DialogueKind kind, string speakerName, string text)
        {
            float cooldown = Math.Max(0.0f, duplicateCooldownSeconds.Value);
            if (cooldown <= 0.0f) return false;
            float now = Time.realtimeSinceStartup;
            string key = kind.ToString() + "|" + (speakerName ?? string.Empty).Trim().ToUpperInvariant() + "|" + text;
            float elapsed;
            if (DuplicateWithinWindow(recentDialogue, key, now, cooldown, out elapsed))
            {
                DebugLog("Duplicate suppressed: same type+speaker+text after " + elapsed.ToString("0.00", CultureInfo.InvariantCulture) + "s key=" + key);
                return true;
            }

            if (recentDialogue.Count > 200)
            {
                List<string> expired = new List<string>();
                foreach (KeyValuePair<string, float> item in recentDialogue)
                    if (now - item.Value > Math.Max(10.0f, cooldown * 3.0f)) expired.Add(item.Key);
                foreach (string expiredKey in expired) recentDialogue.Remove(expiredKey);
            }
            return false;
        }

        internal static bool DuplicateWithinWindow(Dictionary<string, float> state, string key, float now, float cooldown, out float elapsed)
        {
            elapsed = float.MaxValue;
            float previous;
            if (state.TryGetValue(key, out previous)) elapsed = now - previous;
            state[key] = now;
            return cooldown > 0.0f && elapsed >= 0.0f && elapsed <= cooldown;
        }

        private void ClearDuplicateState(string reason, bool resetSpeakerAnnouncement = true)
        {
            if (recentDialogue.Count > 0) recentDialogue.Clear();
            if (resetSpeakerAnnouncement) speakerAnnouncementState.Reset();
            DebugLog("Duplicate state cleared: " + reason);
        }

        internal static string CleanText(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string text = value.Replace("\\n", " ").Replace("\\r", " ").Replace("\\t", " ");
            text = text.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            text = Regex.Replace(text, @"\\([,.;:!?""'\-])", "$1");
            text = Regex.Replace(text, @"<\s*/?\s*(?:color|size|font|style|b|i|u|s|mark|alpha|align|indent|line-height|line-indent|link|margin|mspace|nobr|noparse|pos|rotate|space|sprite|sub|sup|voffset|width|cspace)[^>]*>", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]*>", " ");
            text = Regex.Replace(text, @"\{[^}]*\}", " ");
            text = Regex.Replace(text, @"\s*\([^()]*\)", " ");
            text = Regex.Replace(text, @"\\+", " ");
            text = Regex.Replace(text, @"\s*,(?:\s*,)+", ",");
            text = Regex.Replace(text, @";(?:\s*;)+", ";");
            text = Regex.Replace(text, @":(?:\s*:)+", ":");
            text = Regex.Replace(text, @"!+(?:\s*!+)*", "!");
            text = Regex.Replace(text, @"\?+(?:\s*\?+)*", "?");
            text = Regex.Replace(text, @"\.{4,}", "...");
            text = Regex.Replace(text, @"\s+([,.;:!?])", "$1");
            text = Regex.Replace(text, @"[,;:]([.!?])", "$1");
            text = Regex.Replace(text, @"([.!?])[,;:]+", "$1");
            text = SpeechTextRules.ApplyPronunciationReplacements(text);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }

        private void SaveAutomaticAssignment(SpeakerResolution resolution)
        {
            if (resolution == null || !resolution.NeedsAutomaticSave || string.IsNullOrEmpty(resolution.RawName)) return;
            if (!npcVoiceOverridesLoadValid) return;
            if (npcVoiceOverrides.FindUser(resolution.GameCharacterId, resolution.SpeakerId, resolution.RawName, resolution.Identity) != null) return;

            NpcVoiceEntry entry = CreateAutomaticEntry(resolution.GameCharacterId, resolution.SpeakerId,
                resolution.RawName, resolution.MatchedName, resolution.Identity, resolution.Profile, false);
            NpcVoiceEntry existing = npcVoiceOverrides.FindAutomatic(resolution.GameCharacterId, resolution.SpeakerId,
                resolution.RawName, resolution.Identity);
            if (NpcVoiceEntriesEqual(existing, entry)) return;
            if (existing != null) npcVoiceOverrides.Overrides.Remove(existing);
            npcVoiceOverrides.Overrides.Add(entry);
            SaveNpcVoiceOverrides();
        }

        private static NpcVoiceEntry CreateAutomaticEntry(string gameCharacterId, string speakerId, string rawName,
            string displayName, NpcIdentityRecord identity, SpeechProfile profile, bool ignore)
        {
            NpcVoiceEntry entry = new NpcVoiceEntry();
            entry.GameCharacterId = gameCharacterId ?? string.Empty;
            entry.SpeakerId = speakerId ?? string.Empty;
            entry.InternalName = identity == null ? string.Empty : identity.InternalName;
            entry.DisplayName = identity == null ? (string.IsNullOrEmpty(displayName) ? rawName : displayName) : identity.DisplayName;
            entry.Voice = profile.Voice;
            entry.Speed = profile.Speed;
            entry.Pitch = profile.Pitch;
            entry.HasSpeed = true;
            entry.HasPitch = true;
            entry.Automatic = true;
            entry.Ignore = ignore;
            return entry;
        }

        private static bool NpcVoiceEntriesEqual(NpcVoiceEntry left, NpcVoiceEntry right)
        {
            return left != null && right != null &&
                string.Equals(NpcVoiceData.IdentityKey(left), NpcVoiceData.IdentityKey(right), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(left.Voice, right.Voice, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(left.Speed - right.Speed) < 0.0001f && Math.Abs(left.Pitch - right.Pitch) < 0.0001f &&
                left.HasSpeed == right.HasSpeed && left.HasPitch == right.HasPitch && left.Automatic == right.Automatic && left.Ignore == right.Ignore;
        }

        private void LogResolution(SpeakerResolution resolution, DialogueKind kind, string rawText, string cleanText)
        {
            if (!debugLogging.Value) return;
            Logger.LogInfo("[TTS Debug] Speaker detected=" + resolution.RawName +
                " speaker ID=" + resolution.SpeakerId +
                " matched character=" + resolution.MatchedName +
                " match method=" + resolution.MatchMethod +
                " gender=" + resolution.Gender +
                " gender source=" + resolution.GenderSource +
                " selected voice=" + resolution.Profile.Voice +
                " voice source=" + resolution.VoiceSource +
                " dialogue type=" + kind +
                " raw text=" + rawText +
                " clean text=" + cleanText);
        }

        private void DebugLog(string message)
        {
            if (debugLogging != null && debugLogging.Value) Logger.LogInfo("[TTS Debug] " + message);
        }

        private static string DescribeSpeaker(PixelCrushers.DialogueSystem.CharacterInfo speaker, string displayName)
        {
            if (speaker == null) return "name=" + (displayName ?? string.Empty) + "; speakerInfo=(unavailable)";
            try
            {
                return "name=" + (displayName ?? string.Empty) +
                    "; dialogueId=" + speaker.id.ToString(CultureInfo.InvariantCulture) +
                    "; databaseName=" + (speaker.nameInDatabase ?? string.Empty) +
                    "; isNPC=" + speaker.isNPC +
                    "; isPlayer=" + speaker.isPlayer;
            }
            catch (Exception ex)
            {
                return "name=" + (displayName ?? string.Empty) + "; speakerInfoError=" + ex.Message;
            }
        }

        private static string DescribeResolution(string speakerDetails, SpeakerResolution resolution)
        {
            if (resolution == null) return speakerDetails;
            return speakerDetails +
                "; matchedName=" + resolution.MatchedName +
                "; resolvedId=" + resolution.SpeakerId +
                "; gender=" + resolution.Gender +
                "; match=" + resolution.MatchMethod +
                "; voiceSource=" + resolution.VoiceSource;
        }

        private void RecordDialogueDiagnostic(DialogueKind kind, string speaker, string rawText, string processedText, string selectedVoice, string outcome)
        {
            DialogueDiagnosticEntry entry = new DialogueDiagnosticEntry();
            entry.TimestampUtc = DateTime.UtcNow;
            entry.Kind = kind.ToString();
            entry.Speaker = speaker ?? string.Empty;
            entry.RawText = rawText ?? string.Empty;
            entry.ProcessedText = processedText ?? string.Empty;
            entry.Voice = selectedVoice ?? string.Empty;
            entry.Engine = "selected=" + (preferredTtsEngine == null ? string.Empty : preferredTtsEngine.Value) + "; running=" + runningTtsEngine;
            entry.Outcome = outcome ?? string.Empty;
            dialogueDiagnostics.Add(entry);
        }

        private string GetDialogueDiagnosticLogPath()
        {
            return Path.Combine(GetRuntimeLogDirectory(), "dialogue-diagnostics.log");
        }

        private string GetUnknownSpeakerLogPath()
        {
            return Path.Combine(GetRuntimeLogDirectory(), "unknown-speakers.tsv");
        }

        private string GetRuntimeLogDirectory()
        {
            return GetLogRoot();
        }

        private void FlagRecentDialogue()
        {
            try
            {
                int count = dialogueDiagnostics.AppendFlaggedEvents(GetDialogueDiagnosticLogPath(), DateTime.UtcNow);
                diagnosticStatus = count == 0
                    ? "Flag saved, but no dialogue events have been captured yet."
                    : "Saved " + count.ToString(CultureInfo.InvariantCulture) + " recent dialogue event" + (count == 1 ? string.Empty : "s") + ".";
                ShowNotification(count == 0
                    ? "Dialogue flagged — no events captured"
                    : "Dialogue flagged — " + count.ToString(CultureInfo.InvariantCulture) + " event" + (count == 1 ? string.Empty : "s") + " saved");
                Logger.LogInfo("Dialogue diagnostic flag saved " + count.ToString(CultureInfo.InvariantCulture) + " recent event(s) to " + GetDialogueDiagnosticLogPath());
            }
            catch (Exception ex)
            {
                diagnosticStatus = "Could not save the dialogue flag: " + ex.Message;
                ShowNotification("Dialogue flag failed — check the game log");
                Logger.LogWarning(diagnosticStatus);
            }
        }

        private void ShowNotification(string message)
        {
            notificationText = message ?? string.Empty;
            notificationUntil = Time.unscaledTime + 2.5f;
        }

        private void ClearDialogueDiagnosticLog()
        {
            try
            {
                DialogueDiagnostics.Clear(GetDialogueDiagnosticLogPath());
                diagnosticStatus = "Dialogue diagnostic log cleared.";
            }
            catch (Exception ex)
            {
                diagnosticStatus = "Could not clear the diagnostic log: " + ex.Message;
                Logger.LogWarning(diagnosticStatus);
            }
        }

        private void QueueUnknownSpeaker(string name, string rawText, string cleanText, string speakerId, DialogueKind kind)
        {
            if (string.IsNullOrEmpty(name) || queuedUnknownNames.Contains(name)) return;
            try { unknownSpeakerTracker.RecordOccurrence(name, speakerId, rawText, DateTime.UtcNow); }
            catch (Exception ex) { Logger.LogWarning("Could not update unknown-speaker tracking log: " + ex.Message); }
            PendingUnknown pending = new PendingUnknown();
            pending.Name = name;
            pending.RawText = rawText;
            pending.CleanText = cleanText;
            pending.SpeakerId = speakerId;
            pending.Kind = kind;
            pendingUnknownSpeakers.Enqueue(pending);
            queuedUnknownNames.Add(name);
            if (activeUnknown == null) ActivateNextUnknown();
        }

        private void ActivateNextUnknown()
        {
            activeUnknown = pendingUnknownSpeakers.Count > 0 ? pendingUnknownSpeakers.Dequeue() : null;
            choosingUnknownVoice = false;
            if (activeUnknown != null) backendState = "Unknown speaker: " + activeUnknown.Name;
        }

        private void ApplyUnknownChoice(string gender, string exactVoice, bool ignore, string choiceMethod)
        {
            if (activeUnknown == null) return;
            string name = activeUnknown.Name;
            if (string.Equals(choiceMethod, "random compatible voice", StringComparison.Ordinal))
                exactVoice = StablePoolVoice(name, unknownVoices, ResolveInstalledVoiceSpec(voice.Value, "af_heart"));
            SpeechProfile profile = BuildProfile(name, gender, exactVoice, string.IsNullOrEmpty(exactVoice) && !ignore);
            if (rememberUnknownChoice)
            {
                if (!npcVoiceOverridesLoadValid)
                    DebugLog("Unknown speaker choice was not saved because npc-voice-overrides.json is invalid.");
                NpcVoiceEntry userEntry = npcVoiceOverrides.FindUser(string.Empty, activeUnknown.SpeakerId, name, null);
                if (npcVoiceOverridesLoadValid && userEntry == null)
                {
                    NpcVoiceEntry entry = CreateAutomaticEntry(string.Empty, activeUnknown.SpeakerId, name, name, null, profile, ignore);
                    NpcVoiceEntry existing = npcVoiceOverrides.FindAutomatic(string.Empty, activeUnknown.SpeakerId, name, null);
                    if (!NpcVoiceEntriesEqual(existing, entry))
                    {
                        if (existing != null) npcVoiceOverrides.Overrides.Remove(existing);
                        npcVoiceOverrides.Overrides.Add(entry);
                        SaveNpcVoiceOverrides();
                    }
                    DebugLog("Unknown speaker choice saved as an automatic assignment for " + name + ".");
                }
            }
            try
            {
                unknownSpeakerTracker.RecordChoice(name, activeUnknown.SpeakerId, profile.Voice,
                    ignore ? "ignored by user" : choiceMethod, gender, rememberUnknownChoice, DateTime.UtcNow);
            }
            catch (Exception ex) { Logger.LogWarning("Could not update unknown-speaker tracking log: " + ex.Message); }

            if (!ignore && !string.IsNullOrEmpty(activeUnknown.CleanText))
            {
                bool sent = SendLine(activeUnknown.CleanText, profile);
                RecordDialogueDiagnostic(activeUnknown.Kind,
                    "name=" + activeUnknown.Name + "; resolvedId=" + activeUnknown.SpeakerId + "; source=unknown speaker choice",
                    activeUnknown.RawText, activeUnknown.CleanText, profile.Voice,
                    sent ? "queued after unknown speaker choice" : "failed after unknown speaker choice: voice service unavailable or send error");
            }
            else if (ignore)
            {
                RecordDialogueDiagnostic(activeUnknown.Kind,
                    "name=" + activeUnknown.Name + "; resolvedId=" + activeUnknown.SpeakerId + "; source=unknown speaker choice",
                    activeUnknown.RawText, activeUnknown.CleanText, profile.Voice,
                    "filtered: user chose to ignore this speaker");
            }
            queuedUnknownNames.Remove(name);
            activeUnknown = null;
            ActivateNextUnknown();
        }

        private void OnGUI()
        {
            CaptureHotkeyInput();
            bool notificationVisible = !string.IsNullOrEmpty(notificationText) && Time.unscaledTime < notificationUntil;
            if (notificationVisible) GUI.Box(new Rect(12, 12, 360, 34), notificationText);
            if (showStatusOverlay != null && showStatusOverlay.Value && !string.Equals(backendState, "Connected", StringComparison.Ordinal))
            {
                GUI.Box(new Rect(12, notificationVisible ? 52 : 12, 360, 34), "ObenVoice: " + backendState);
            }

            if (activeUnknown != null && unknownSpeakerPrompt.Value)
            {
                float height = choosingUnknownVoice ? 245f : 230f;
                Rect area = new Rect((Screen.width - 440f) / 2f, (Screen.height - height) / 2f, 440f, height);
                GUI.Window(946210, area, DrawUnknownSpeakerWindow, "ObenVoice — Unknown speaker");
            }

            if (showSettingsWindow)
            {
                Rect area = new Rect((Screen.width - 760f) / 2f, (Screen.height - 470f) / 2f, 760f, 470f);
                GUI.Window(946211, area, DrawSettingsWindow, "ObenVoice Settings  (" + DescribeHotkey() + " to close)");
            }

            if (showPlayerVoiceWindow)
            {
                bool expanded = showQuickPlayerVoiceChoices && string.Equals(preferredTtsEngine.Value, "kokoro", StringComparison.OrdinalIgnoreCase);
                float height = expanded ? 470f : 310f;
                Rect area = new Rect((Screen.width - 510f) / 2f, (Screen.height - height) / 2f, 510f, height);
                GUI.Window(946212, area, DrawPlayerVoiceWindow, "Your Dialogue Voice  (" + DescribePlayerVoiceHotkey() + " to close)");
            }
            if (showNpcOverridesWindow)
            {
                Rect area = new Rect((Screen.width - 720f) / 2f, (Screen.height - 520f) / 2f, 720f, 520f);
                GUI.Window(946213, area, DrawNpcOverridesWindow, "NPC Voice Overrides");
            }
        }

        private void DrawSettingsWindow(int id)
        {
            GUI.Label(new Rect(20, 30, 710, 22), "Backend: " + backendState + "    |    Selected: " + preferredTtsEngine.Value + "    |    Running: " + runningTtsEngine);
            string[] pages = new string[] { "General", "Voice Packs", "NPC Voices", "Pronunciation", "Advanced" };
            settingsPage = GUI.Toolbar(new Rect(16, 58, 728, 28), settingsPage, pages);

            if (settingsPage == 0) DrawGeneralSettings();
            else if (settingsPage == 1) DrawVoicePackSettings();
            else if (settingsPage == 2) DrawVoiceSettings();
            else if (settingsPage == 3) DrawPronunciationSettings();
            else DrawAdvancedSettings();

            if (settingsPage != 3) typingInRuleEditor = false;

            GUI.Label(new Rect(20, 434, 300, 22), "Changes are saved automatically.");
            if (GUI.Button(new Rect(620, 430, 112, 26), "Close")) SetSettingsWindowVisible(false);
            GUI.DragWindow(new Rect(0, 0, 760, 25));
        }

        private void DrawPlayerVoiceWindow(int id)
        {
            if (!string.Equals(preferredTtsEngine.Value, "kokoro", StringComparison.OrdinalIgnoreCase))
            {
                GUI.Box(new Rect(18, 35, 474, 62), "The selected engine does not expose player voice controls here.\nChoose Kokoro in the F8 General tab to select a player voice.");
            }
            else
            {
                GUI.Label(new Rect(20, 42, 150, 22), "Your voice");
                if (GUI.Button(new Rect(170, 38, 310, 26), VoiceLabel(playerVoice.Value))) showQuickPlayerVoiceChoices = !showQuickPlayerVoiceChoices;
                speakPlayerDialogue.Value = GUI.Toggle(new Rect(20, 82, 300, 24), speakPlayerDialogue.Value, "Speak my selected dialogue");
                GUI.Label(new Rect(20, 122, 140, 22), "Your speed");
                playerSpeed.Value = GUI.HorizontalSlider(new Rect(160, 127, 180, 18), playerSpeed.Value, 0.5f, 2.0f);
                GUI.Label(new Rect(355, 122, 80, 22), playerSpeed.Value.ToString("0.00"));
                if (GUI.Button(new Rect(435, 118, 48, 24), "Reset")) playerSpeed.Value = playerSpeed.Default;
                GUI.Label(new Rect(20, 157, 140, 22), "Your tone");
                playerPitch.Value = GUI.HorizontalSlider(new Rect(160, 162, 180, 18), playerPitch.Value, -3.0f, 3.0f);
                GUI.Label(new Rect(355, 157, 75, 22), playerPitch.Value.ToString("0.00"));
                if (GUI.Button(new Rect(435, 153, 48, 24), "Reset")) playerPitch.Value = playerPitch.Default;
                GUI.Label(new Rect(20, 215, 460, 36), "Tone: left is deeper, right is higher. Changes apply to your next line.");
                if (showQuickPlayerVoiceChoices) DrawQuickPlayerVoiceChoices();
            }
            float buttonY = showQuickPlayerVoiceChoices && string.Equals(preferredTtsEngine.Value, "kokoro", StringComparison.OrdinalIgnoreCase) ? 426f : 266f;
            GUI.Label(new Rect(20, buttonY + 4f, 250, 22), "Changes are saved automatically.");
            if (GUI.Button(new Rect(385, buttonY, 105, 26), "Close")) SetPlayerVoiceWindowVisible(false);
            GUI.DragWindow(new Rect(0, 0, 510, 25));
        }

        private void DrawNpcOverridesWindow(int id)
        {
            GUI.Label(new Rect(18, 36, 300, 22), overrideAddMode ? "Add NPC from database" : "Saved NPC voice records");
            if (GUI.Button(new Rect(370, 32, 110, 26), overrideAddMode ? "Saved list" : "Add NPC"))
            {
                overrideAddMode = !overrideAddMode;
                overrideSearch = string.Empty;
                overrideSelectedNpc = null;
                overrideSelectedEntry = null;
            }
            if (GUI.Button(new Rect(490, 32, 110, 26), "Reload file"))
            {
                LoadNpcVoiceOverrides(false);
                overrideSelectedNpc = null;
                overrideSelectedEntry = null;
                overrideEditorStatus = npcVoiceOverridesStatus;
            }
            if (overrideAddMode)
                overrideSearch = GUI.TextField(new Rect(18, 64, 300, 26), overrideSearch ?? string.Empty);

            List<NpcIdentityRecord> visible = new List<NpcIdentityRecord>();
            if (overrideAddMode)
                foreach (NpcIdentityRecord npc in npcDatabase.Records)
                    if (string.IsNullOrEmpty(overrideSearch) || npc.DisplayName.IndexOf(overrideSearch, StringComparison.OrdinalIgnoreCase) >= 0 || npc.InternalName.IndexOf(overrideSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                        visible.Add(npc);
            int rowCount = overrideAddMode ? visible.Count : npcVoiceOverrides.Overrides.Count;
            float listHeight = Math.Max(350f, rowCount * 27f + 4f);
            overrideNpcScroll = GUI.BeginScrollView(new Rect(18, 96, 300, 360), overrideNpcScroll, new Rect(0, 0, 280, listHeight));
            for (int i = 0; i < rowCount; i++)
            {
                if (overrideAddMode)
                {
                    NpcIdentityRecord npc = visible[i];
                    if (GUI.Button(new Rect(2, 2 + i * 27f, 272, 25), npc.DisplayName)) SelectAddNpc(npc);
                }
                else
                {
                    NpcVoiceEntry entry = npcVoiceOverrides.Overrides[i];
                    string label = (entry.Automatic ? "Auto: " : "Override: ") + NpcVoiceEntryLabel(entry);
                    if (GUI.Button(new Rect(2, 2 + i * 27f, 272, 25), label)) SelectOverrideEntry(entry);
                }
            }
            GUI.EndScrollView();

            if (overrideSelectedNpc == null)
            {
                GUI.Box(new Rect(335, 96, 365, 150), overrideAddMode ? "Search, then select an NPC. Selecting does not save anything." : "Select a stored record to edit or reset it.");
            }
            else
            {
                GUI.Label(new Rect(340, 96, 350, 24), overrideSelectedNpc.DisplayName);
                GUI.Label(new Rect(340, 128, 82, 22), "Voice");
                if (GUI.Button(new Rect(425, 124, 265, 26), VoiceLabel(overrideVoice))) showOverrideVoiceChoices = !showOverrideVoiceChoices;
                GUI.Label(new Rect(340, 166, 82, 22), "Speed");
                overrideSpeed = GUI.HorizontalSlider(new Rect(425, 171, 210, 18), overrideSpeed, 0.5f, 2.0f);
                GUI.Label(new Rect(645, 166, 50, 22), overrideSpeed.ToString("0.00"));
                GUI.Label(new Rect(340, 204, 82, 22), "Pitch");
                overridePitch = GUI.HorizontalSlider(new Rect(425, 209, 210, 18), overridePitch, -4.0f, 4.0f);
                GUI.Label(new Rect(645, 204, 50, 22), overridePitch.ToString("0.0"));
                if (GUI.Button(new Rect(340, 246, 125, 28), "Save override")) SaveOverrideEditorEntry();
                if (GUI.Button(new Rect(475, 246, 145, 28), "Reset to Default")) RemoveOverrideEditorEntry();
                GUI.Label(new Rect(340, 286, 350, 56), "Reset removes only this personal record. The selected preset or automatic assignment will apply again.");
                if (showOverrideVoiceChoices) DrawOverrideVoiceChoices();
            }
            GUI.Label(new Rect(18, 470, 560, 22), string.IsNullOrEmpty(overrideEditorStatus) ? npcVoiceOverridesStatus : overrideEditorStatus);
            if (GUI.Button(new Rect(600, 466, 100, 28), "Close"))
            {
                showNpcOverridesWindow = false;
                showSettingsWindow = true;
                settingsPage = 2;
                showOverrideVoiceChoices = false;
                RefreshMenuInputCapture();
            }
            GUI.DragWindow(new Rect(0, 0, 720, 25));
        }

        private void DrawOverrideVoiceChoices()
        {
            GUI.Box(new Rect(335, 120, 365, 332), "Choose voice");
            float contentHeight = Math.Max(300f, availableKokoroVoices.Count * 28f + 4f);
            overrideVoiceScroll = GUI.BeginScrollView(new Rect(342, 146, 350, 297), overrideVoiceScroll, new Rect(0, 0, 330, contentHeight));
            for (int i = 0; i < availableKokoroVoices.Count; i++)
            {
                DiscoveredVoice item = availableKokoroVoices[i];
                if (GUI.Button(new Rect(2, 2 + i * 28f, 322, 26), FriendlyVoiceLabel(item)))
                {
                    overrideVoice = item.Id;
                    showOverrideVoiceChoices = false;
                }
            }
            GUI.EndScrollView();
        }

        private static string NpcVoiceEntryLabel(NpcVoiceEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.DisplayName)) return entry.DisplayName;
            if (!string.IsNullOrEmpty(entry.InternalName)) return entry.InternalName;
            if (!string.IsNullOrEmpty(entry.GameCharacterId)) return "Character " + entry.GameCharacterId;
            return entry.SpeakerId;
        }

        private void SelectOverrideEntry(NpcVoiceEntry entry)
        {
            overrideSelectedEntry = entry;
            NpcIdentityMatch match = npcDatabase.Resolve(entry.GameCharacterId, NpcVoiceEntryLabel(entry));
            overrideSelectedNpc = match.Record ?? new NpcIdentityRecord {
                GameCharacterId = entry.GameCharacterId, InternalName = entry.InternalName, DisplayName = NpcVoiceEntryLabel(entry)
            };
            SetOverrideEditorValues(entry.Voice, entry.HasSpeed ? entry.Speed : 1.0f, entry.HasPitch ? entry.Pitch : 0.0f);
            overrideEditorStatus = entry.Automatic ? "Editing saved automatic assignment" : "Editing personal override";
            showOverrideVoiceChoices = false;
        }

        private void SelectAddNpc(NpcIdentityRecord npc)
        {
            overrideSelectedNpc = npc;
            overrideSelectedEntry = null;
            NpcVoicePreset preset = FindSelectedNpcPreset();
            NpcVoiceEntry startingPoint = preset == null ? null : preset.Find(npc.GameCharacterId, npc.DisplayName, npc);
            SetOverrideEditorValues(startingPoint == null ? string.Empty : startingPoint.Voice,
                startingPoint == null || !startingPoint.HasSpeed ? 1.0f : startingPoint.Speed,
                startingPoint == null || !startingPoint.HasPitch ? 0.0f : startingPoint.Pitch);
            overrideEditorStatus = startingPoint == null ? "New override; nothing is saved yet" : "Starting from selected preset; nothing is saved yet";
            showOverrideVoiceChoices = false;
        }

        private void SetOverrideEditorValues(string voiceId, float speed, float pitch)
        {
            overrideVoice = voiceId ?? string.Empty;
            overrideSpeed = speed;
            overridePitch = pitch;
            overrideBaselineVoice = overrideVoice;
            overrideBaselineSpeed = speed;
            overrideBaselinePitch = pitch;
        }

        private void SaveOverrideEditorEntry()
        {
            if (overrideSelectedNpc == null) return;
            if (overrideSelectedEntry == null && string.Equals(overrideVoice, overrideBaselineVoice, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(overrideSpeed - overrideBaselineSpeed) < 0.0001f && Math.Abs(overridePitch - overrideBaselinePitch) < 0.0001f)
            {
                overrideEditorStatus = "No customization to save.";
                return;
            }
            NpcVoiceEntry entry = new NpcVoiceEntry();
            entry.GameCharacterId = overrideSelectedNpc.GameCharacterId;
            entry.SpeakerId = overrideSelectedEntry == null ? string.Empty : overrideSelectedEntry.SpeakerId;
            entry.InternalName = overrideSelectedNpc.InternalName;
            entry.DisplayName = overrideSelectedNpc.DisplayName;
            entry.Voice = (overrideVoice ?? string.Empty).Trim().ToLowerInvariant();
            entry.Speed = overrideSpeed;
            entry.Pitch = overridePitch;
            entry.HasSpeed = true;
            entry.HasPitch = true;
            entry.Automatic = false;
            string error = NpcVoiceData.ValidateEntry(entry);
            if (error.Length > 0)
            {
                overrideEditorStatus = error;
                return;
            }
            if (overrideSelectedEntry != null) npcVoiceOverrides.Overrides.Remove(overrideSelectedEntry);
            else
            {
                string key = NpcVoiceData.IdentityKey(entry);
                npcVoiceOverrides.Overrides.RemoveAll(item => string.Equals(NpcVoiceData.IdentityKey(item), key, StringComparison.OrdinalIgnoreCase));
            }
            npcVoiceOverrides.Overrides.Add(entry);
            if (SaveNpcVoiceOverrides()) SelectOverrideEntry(entry);
        }

        private void RemoveOverrideEditorEntry()
        {
            if (overrideSelectedEntry == null)
            {
                overrideEditorStatus = "No personal record exists for this NPC.";
                return;
            }
            npcVoiceOverrides.Overrides.Remove(overrideSelectedEntry);
            if (SaveNpcVoiceOverrides())
            {
                overrideSelectedEntry = null;
                overrideSelectedNpc = null;
                overrideEditorStatus = "Personal record removed; preset or automatic behavior restored.";
            }
        }

        private void DrawQuickPlayerVoiceChoices()
        {
            GUI.Box(new Rect(18, 235, 474, 180), "Choose voice");
            playerVoiceScroll = DrawSelectableVoiceList(new Rect(24, 260, 462, 148), playerVoiceScroll, false);
        }

        private void SetSettingsWindowVisible(bool visible)
        {
            if (showSettingsWindow == visible) return;
            showSettingsWindow = visible;
            if (!visible)
            {
                showDefaultVoiceChoices = false;
                capturingHotkey = 0;
                typingInRuleEditor = false;
                Config.Save();
            }
            RefreshMenuInputCapture();
        }

        private void SetPlayerVoiceWindowVisible(bool visible)
        {
            if (showPlayerVoiceWindow == visible) return;
            showPlayerVoiceWindow = visible;
            if (!visible)
            {
                showQuickPlayerVoiceChoices = false;
                Config.Save();
            }
            RefreshMenuInputCapture();
        }

        private void RefreshMenuInputCapture()
        {
            bool menuOpen = showSettingsWindow || showPlayerVoiceWindow || showNpcOverridesWindow;
            if (menuOpen)
            {
                if (!settingsCursorCaptured)
                {
                    cursorLockBeforeSettings = Cursor.lockState;
                    cursorVisibleBeforeSettings = Cursor.visible;
                    settingsCursorCaptured = true;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            if (!settingsCursorCaptured) return;
            if (Cursor.lockState == CursorLockMode.None && Cursor.visible)
            {
                Cursor.lockState = cursorLockBeforeSettings;
                Cursor.visible = cursorVisibleBeforeSettings;
            }
            settingsCursorCaptured = false;
        }

        internal static bool ShouldSuppressCameraLook()
        {
            return Instance != null && (Instance.showSettingsWindow || Instance.showPlayerVoiceWindow || Instance.showNpcOverridesWindow);
        }

        internal static void LogCameraDiagnostic(string message)
        {
            if (Instance != null) Instance.Logger.LogInfo("Camera look diagnostic: " + message);
        }

        private void DrawGeneralSettings()
        {
            GUI.Label(new Rect(20, 99, 145, 22), "Voice engine");
            if (GUI.Button(new Rect(170, 94, 220, 28), preferredTtsEngine.Value + "  ▼")) showEngineChoices = !showEngineChoices;
            bool oldEnabled = GUI.enabled;
            GUI.enabled = restartInProgress == 0 && IsInstalledEngine(preferredTtsEngine.Value) && SelectedEngineDiffersFromRunning();
            if (GUI.Button(new Rect(400, 94, 72, 28), "Apply")) RestartVoiceEngine();
            GUI.enabled = oldEnabled;
            GUI.Label(new Rect(485, 99, 235, 22), "Running: " + runningTtsEngine);

            autoStartBackend.Value = GUI.Toggle(new Rect(20, 132, 340, 24), autoStartBackend.Value, "Start offline voice service with the game");
            closeBackendOnGameExit.Value = GUI.Toggle(new Rect(380, 132, 340, 24), closeBackendOnGameExit.Value, "Close ObenVoice service on exit");
            showStatusOverlay.Value = GUI.Toggle(new Rect(20, 160, 340, 24), showStatusOverlay.Value, "Show brief service status messages");

            GUI.Label(new Rect(20, 195, 700, 22), "Dialogue");
            speakNpcDialogue.Value = GUI.Toggle(new Rect(20, 222, 330, 24), speakNpcDialogue.Value, "Speak NPC conversation dialogue");
            speakBarks.Value = GUI.Toggle(new Rect(380, 222, 330, 24), speakBarks.Value, "Speak NPC barks");
            speakPlayerDialogue.Value = GUI.Toggle(new Rect(20, 250, 330, 24), speakPlayerDialogue.Value, "Speak my selected dialogue");
            prefixSpeakerName.Value = GUI.Toggle(new Rect(380, 250, 330, 24), prefixSpeakerName.Value, "Say speaker name first");
            muteOriginalDialogueAudio.Value = GUI.Toggle(new Rect(20, 278, 330, 24), muteOriginalDialogueAudio.Value, "Mute original conversation voices");
            muteOriginalBarkAudio.Value = GUI.Toggle(new Rect(380, 278, 330, 24), muteOriginalBarkAudio.Value, "Mute original bark voices");

            GUI.Label(new Rect(20, 320, 150, 22), "Full settings key");
            DrawHotkeyField(new Rect(170, 314, 170, 28), settingsHotkey, 1);
            GUI.Label(new Rect(380, 320, 150, 22), "Player voice key");
            DrawHotkeyField(new Rect(530, 314, 170, 28), playerVoiceHotkey, 2);
            GUI.Label(new Rect(20, 365, 700, 38), "F8 opens all settings. F9 controls the player's selected voice, speed and tone.");

            if (showEngineChoices)
            {
                string[] engines = GetInstalledEngineIds();
                float height = engines.Length * 28f + 8f;
                GUI.Box(new Rect(170, 122, 220, height), string.Empty);
                for (int i = 0; i < engines.Length; i++)
                {
                    string engine = engines[i];
                    if (GUI.Button(new Rect(174, 126 + i * 28, 212, 26), engine))
                    {
                        preferredTtsEngine.Value = engine;
                        showEngineChoices = false;
                    }
                }
            }
        }

        private bool SelectedEngineDiffersFromRunning()
        {
            string selected = (preferredTtsEngine.Value ?? string.Empty).Trim();
            string running = (runningTtsEngine ?? string.Empty).Trim();
            if (selected.Length == 0 || string.Equals(selected, running, StringComparison.OrdinalIgnoreCase)) return false;
            return !startedBackend || !string.Equals(selected, launchedTtsEngine, StringComparison.OrdinalIgnoreCase);
        }

        private void ResetOrdinarySettings()
        {
            ResetSetting(host); ResetSetting(port); ResetSetting(voice); ResetSetting(autoConnect);
            ResetSetting(femaleVoicePool); ResetSetting(maleVoicePool); ResetSetting(unknownVoicePool);
            ResetSetting(enableStableVariation); ResetSetting(enableVoiceBlending); ResetSetting(voiceBlendChancePercent);
            ResetSetting(minimumSpeed); ResetSetting(maximumSpeed); ResetSetting(minimumPitch); ResetSetting(maximumPitch); ResetSetting(variationSeed);
            ResetSetting(playerVoice); ResetSetting(playerSpeed); ResetSetting(playerPitch);
            ResetSetting(femaleCharacterNames); ResetSetting(maleCharacterNames);
            ResetSetting(speakNpcDialogue); ResetSetting(speakPlayerDialogue); ResetSetting(speakBarks); ResetSetting(prefixSpeakerName);
            ResetSetting(muteOriginalDialogueAudio); ResetSetting(muteOriginalBarkAudio);
            ResetSetting(minimumCharacters); ResetSetting(maximumCharacters); ResetSetting(ignoredExactTerms); ResetSetting(duplicateCooldownSeconds);
            ResetSetting(unknownSpeakerPrompt);
            ResetSetting(autoStartBackend); ResetSetting(closeBackendOnGameExit); ResetSetting(preferredTtsEngine); ResetSetting(selectedEngineVoicePack);
            ResetSetting(kokoroVoiceMode); ResetSetting(selectedKokoroVoicePack); ResetSetting(selectedNpcVoicePreset);
            ResetSetting(debugLogging); ResetSetting(showStatusOverlay); ResetSetting(settingsHotkey); ResetSetting(playerVoiceHotkey); ResetSetting(flagDialogueHotkey);
            // The separate npc-voice-overrides.json file is deliberately untouched.
            ScanInstalledEngineAddons();
            ScanInstalledVoicePacks();
            if (manualEngineVoicePacksAvailable) ScanManualEngineVoicePacks();
            LoadVoiceRules();
            LoadCharacterDatabase();
            confirmResetDefaults = false;
            backendState = "Settings reset; restart engine to apply backend changes";
            Config.Save();
            Logger.LogInfo("Reset ordinary TTS settings to defaults; preserved assignments, NPC Voice Overrides and pronunciation rules.");
        }

        private static void ResetSetting<T>(ModSetting<T> setting)
        {
            if (setting != null) setting.Value = setting.Default;
        }

        private void DrawHotkeyField(Rect area, ModSetting<string> target, int field)
        {
            string label = capturingHotkey == field ? "Press a key..." : target.Value.ToString();
            if (GUI.Button(area, label)) capturingHotkey = field;
        }

        private void CaptureHotkeyInput()
        {
            if (capturingHotkey == 0 || Event.current == null || Event.current.type != EventType.KeyDown) return;
            KeyCode key = Event.current.keyCode;
            if (key == KeyCode.Escape)
            {
                capturingHotkey = 0;
                Event.current.Use();
                return;
            }
            if (key == KeyCode.None) return;
            ModSetting<string> target = capturingHotkey == 1 ? settingsHotkey : (capturingHotkey == 2 ? playerVoiceHotkey : flagDialogueHotkey);
            bool conflicts = (target != settingsHotkey && MainKey(settingsHotkey.Value) == key) ||
                (target != playerVoiceHotkey && MainKey(playerVoiceHotkey.Value) == key) ||
                (target != flagDialogueHotkey && MainKey(flagDialogueHotkey.Value) == key);
            if (!conflicts)
            {
                target.Value = key.ToString();
                Config.Save();
            }
            capturingHotkey = 0;
            Event.current.Use();
        }

        private static bool IsHotkeyDown(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string[] parts = text.Split('+');
            KeyCode main = ParseKey(parts[0]);
            if (main == KeyCode.None || !Input.GetKeyDown(main)) return false;
            for (int i = 1; i < parts.Length; i++)
            {
                KeyCode modifier = ParseKey(parts[i]);
                if (modifier == KeyCode.None || !Input.GetKey(modifier)) return false;
            }
            return true;
        }

        private static KeyCode MainKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return KeyCode.None;
            return ParseKey(text.Split('+')[0]);
        }

        private static KeyCode ParseKey(string text)
        {
            KeyCode key;
            return Enum.TryParse((text ?? string.Empty).Trim(), true, out key) ? key : KeyCode.None;
        }

        private void DrawVoiceSettings()
        {
            if (!string.Equals(preferredTtsEngine.Value, "kokoro", StringComparison.OrdinalIgnoreCase))
            {
                GUI.Box(new Rect(20, 105, 700, 80), "The selected engine does not expose character voice controls here.\nYour Kokoro character settings remain saved. Player voice controls remain in F9.");
                return;
            }

            GUI.Label(new Rect(20, 105, 105, 22), "NPC preset");
            if (GUI.Button(new Rect(125, 101, 300, 26), SelectedNpcPresetLabel() + "  ▼")) showNpcPresetChoices = !showNpcPresetChoices;
            if (GUI.Button(new Rect(445, 101, 150, 26), "Overrides..."))
            {
                showNpcOverridesWindow = true;
                showSettingsWindow = false;
                overrideEditorStatus = npcVoiceOverridesStatus;
                RefreshMenuInputCapture();
            }
            unknownSpeakerPrompt.Value = GUI.Toggle(new Rect(20, 142, 430, 24), unknownSpeakerPrompt.Value, "Ask when an unknown speaker is encountered");
            GUI.Label(new Rect(20, 174, 700, 42), "Priority: personal override → selected preset → saved automatic voice → new automatic voice.");
            showAutomaticVariation = GUI.Toggle(new Rect(20, 215, 300, 24), showAutomaticVariation, "Automatic variation controls");
            if (showAutomaticVariation)
            {
                GUI.Box(new Rect(20, 244, 700, 136), string.Empty);
                GUI.Label(new Rect(35, 252, 125, 22), "Speed minimum");
                minimumSpeed.Value = GUI.HorizontalSlider(new Rect(170, 257, 310, 18), minimumSpeed.Value, 0.5f, 2.0f);
                GUI.Label(new Rect(495, 252, 55, 22), minimumSpeed.Value.ToString("0.00"));
                if (GUI.Button(new Rect(565, 250, 62, 24), "Reset")) minimumSpeed.Value = minimumSpeed.Default;

                GUI.Label(new Rect(35, 282, 125, 22), "Speed maximum");
                maximumSpeed.Value = GUI.HorizontalSlider(new Rect(170, 287, 310, 18), maximumSpeed.Value, 0.5f, 2.0f);
                GUI.Label(new Rect(495, 282, 55, 22), maximumSpeed.Value.ToString("0.00"));
                if (GUI.Button(new Rect(565, 280, 62, 24), "Reset")) maximumSpeed.Value = maximumSpeed.Default;

                GUI.Label(new Rect(35, 312, 125, 22), "Tone minimum");
                minimumPitch.Value = GUI.HorizontalSlider(new Rect(170, 317, 310, 18), minimumPitch.Value, -4.0f, 4.0f);
                GUI.Label(new Rect(495, 312, 55, 22), minimumPitch.Value.ToString("0.0"));
                if (GUI.Button(new Rect(565, 310, 62, 24), "Reset")) minimumPitch.Value = minimumPitch.Default;

                GUI.Label(new Rect(35, 342, 125, 22), "Tone maximum");
                maximumPitch.Value = GUI.HorizontalSlider(new Rect(170, 347, 310, 18), maximumPitch.Value, -4.0f, 4.0f);
                GUI.Label(new Rect(495, 342, 55, 22), maximumPitch.Value.ToString("0.0"));
                if (GUI.Button(new Rect(565, 340, 62, 24), "Reset")) maximumPitch.Value = maximumPitch.Default;
            }
            DrawNpcPresetChoices();
        }

        private string SelectedNpcPresetLabel()
        {
            NpcVoicePreset preset = FindSelectedNpcPreset();
            if (preset != null) return preset.Name + (preset.Provisional ? " [Provisional]" : string.Empty);
            string selected = selectedNpcVoicePreset == null ? string.Empty : selectedNpcVoicePreset.Value ?? string.Empty;
            return selected.Length == 0 ? "Default" : "Missing preset: " + selected;
        }

        private void DrawNpcPresetChoices()
        {
            if (!showNpcPresetChoices) return;
            float height = Math.Min(245f, 40f + discoveredNpcPresets.Count * 30f);
            GUI.Box(new Rect(125, 156, 360, height), string.Empty);
            float contentHeight = 32f + discoveredNpcPresets.Count * 30f;
            npcPresetSelectorScroll = GUI.BeginScrollView(new Rect(131, 162, 348, height - 12f), npcPresetSelectorScroll, new Rect(0, 0, 328, contentHeight));
            if (GUI.Button(new Rect(2, 2, 320, 26), "Default"))
            {
                selectedNpcVoicePreset.Value = string.Empty;
                showNpcPresetChoices = false;
            }
            for (int i = 0; i < discoveredNpcPresets.Count; i++)
            {
                NpcVoicePreset preset = discoveredNpcPresets[i];
                if (string.Equals(preset.StableId, "core/default", StringComparison.OrdinalIgnoreCase)) continue;
                string label = preset.Name + (preset.Provisional ? " [Provisional]" : string.Empty);
                if (GUI.Button(new Rect(2, 32 + i * 30, 320, 26), label))
                {
                    selectedNpcVoicePreset.Value = preset.StableId;
                    showNpcPresetChoices = false;
                }
            }
            GUI.EndScrollView();
        }

        private static float DrawSliderWithReset(Rect sliderRect, ModSetting<float> setting, float minimum, float maximum)
        {
            float value = GUI.HorizontalSlider(sliderRect, setting.Value, minimum, maximum);
            if (GUI.Button(new Rect(sliderRect.xMax + 68f, sliderRect.y - 6f, 48f, 24f), "Reset")) value = setting.Default;
            return value;
        }

        private void DrawVoiceChoices()
        {
            if (!showDefaultVoiceChoices) return;
            GUI.Box(new Rect(490, 105, 240, 290), "Choose fallback voice");
            settingsVoicePickerScroll = DrawSelectableVoiceList(new Rect(498, 132, 224, 255), settingsVoicePickerScroll, true);
        }

        private Vector2 DrawSelectableVoiceList(Rect viewport, Vector2 scroll, bool selectFallback)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.wordWrap = selectFallback;
            GUIStyle selectedStyle = new GUIStyle(buttonStyle);
            selectedStyle.normal.background = GUI.skin.button.active.background;
            selectedStyle.hover.background = GUI.skin.button.active.background;
            float rowWidth = viewport.width - 28f;
            float contentHeight = 4f;
            foreach (string gender in new string[] { "female", "male" })
            {
                contentHeight += 24f;
                foreach (DiscoveredVoice voiceInfo in availableKokoroVoices)
                    if (string.Equals(voiceInfo.Gender, gender, StringComparison.OrdinalIgnoreCase))
                        contentHeight += selectFallback ? Math.Max(42f, buttonStyle.CalcHeight(new GUIContent(VoiceMenuLabel(voiceInfo)), rowWidth)) + 4f : 30f;
            }

            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, viewport.width - 20f, Math.Max(viewport.height - 4f, contentHeight)));
            float y = 2f;
            foreach (string gender in new string[] { "female", "male" })
            {
                GUI.Label(new Rect(4, y, rowWidth, 22), string.Equals(gender, "female", StringComparison.Ordinal) ? "Female" : "Male");
                y += 24f;
                foreach (DiscoveredVoice voiceInfo in availableKokoroVoices)
                {
                    if (!string.Equals(voiceInfo.Gender, gender, StringComparison.OrdinalIgnoreCase)) continue;
                    bool selected = string.Equals(selectFallback ? voice.Value : playerVoice.Value, voiceInfo.Id, StringComparison.OrdinalIgnoreCase);
                    string label = selectFallback ? VoiceMenuLabel(voiceInfo) : CompactVoiceMenuLabel(voiceInfo);
                    if (selected && !selectFallback) label = "[Selected] " + label;
                    float height = selectFallback ? Math.Max(42f, buttonStyle.CalcHeight(new GUIContent(label), rowWidth)) : 26f;
                    if (GUI.Button(new Rect(4, y, rowWidth, height), label, selected && !selectFallback ? selectedStyle : buttonStyle))
                    {
                        if (selectFallback) voice.Value = voiceInfo.Id;
                        else playerVoice.Value = voiceInfo.Id;
                        showDefaultVoiceChoices = false;
                        if (showPlayerVoiceWindow) showQuickPlayerVoiceChoices = false;
                    }
                    y += height + 4f;
                }
            }
            GUI.EndScrollView();
            return scroll;
        }

        private static string CompactVoiceMenuLabel(DiscoveredVoice voiceInfo)
        {
            return FriendlyVoiceLabel(voiceInfo);
        }

        private string VoiceLabel(string voiceName)
        {
            if (string.IsNullOrEmpty(voiceName)) return "Choose voice";
            DiscoveredVoice found = FindVoice(voiceName);
            return found == null ? voiceName : FriendlyVoiceLabel(found);
        }

        private void DrawDialogueSettings()
        {
            speakNpcDialogue.Value = GUI.Toggle(new Rect(20, 105, 350, 24), speakNpcDialogue.Value, "Speak NPC conversation dialogue");
            speakBarks.Value = GUI.Toggle(new Rect(20, 135, 350, 24), speakBarks.Value, "Speak world and NPC barks");
            prefixSpeakerName.Value = GUI.Toggle(new Rect(20, 165, 350, 24), prefixSpeakerName.Value, "Say speaker name before dialogue");
            muteOriginalDialogueAudio.Value = GUI.Toggle(new Rect(400, 105, 330, 24), muteOriginalDialogueAudio.Value, "Mute original conversation audio");
            muteOriginalBarkAudio.Value = GUI.Toggle(new Rect(400, 135, 330, 24), muteOriginalBarkAudio.Value, "Mute original bark audio");
            GUI.Label(new Rect(20, 250, 700, 42), "Text cleaning, action-only choice skipping, and duplicate protection remain enabled in the existing backend.");
        }

        private void DrawVoicePackSettings()
        {
            if (string.Equals(preferredTtsEngine.Value, "kokoro", StringComparison.OrdinalIgnoreCase))
            {
                DrawKokoroVoicePackSettings();
                return;
            }

            if (manualEngineVoicePacksAvailable)
            {
                DrawManualEngineVoicePackSettings();
                return;
            }

            GUI.Box(new Rect(20, 105, 700, 80), "Voice packs are engine-specific. This core currently manages external packs only for Kokoro.\nUse the selected optional engine's own compatible add-on support where provided.");
        }

        private void DrawKokoroVoicePackSettings()
        {
            GUI.Label(new Rect(20, 100, 120, 22), "Voice source");
            if (GUI.Button(new Rect(140, 96, 350, 28), SelectedVoicePackLabel() + "  ▼")) showVoicePackChoices = !showVoicePackChoices;
            GUI.Label(new Rect(20, 132, 700, 38), "Default uses built-in voices. All Voices combines defaults and installed packs. Custom preserves individual selections.");

            if (!string.Equals(kokoroVoiceMode.Value, "custom", StringComparison.OrdinalIgnoreCase))
            {
                GUI.Box(new Rect(20, 190, 700, 95), "Automatic female voices: " + femaleVoices.Length + "\nAutomatic male voices: " + maleVoices.Length + "\nExplicit overrides and preset assignments may still request any installed compatible voice.");
                GUI.Label(new Rect(20, 315, 700, 42), "Voice packs are discovered at startup. Restart the game after installing or removing a pack.");
                DrawVoicePackChoices();
                return;
            }

            GUI.Label(new Rect(20, 168, 330, 22), "Custom female voices");
            GUI.Label(new Rect(380, 168, 330, 22), "Custom male voices");

            List<DiscoveredVoice> female = new List<DiscoveredVoice>();
            List<DiscoveredVoice> male = new List<DiscoveredVoice>();
            foreach (DiscoveredVoice voiceInfo in availableKokoroVoices)
            {
                if (string.Equals(voiceInfo.Gender, "male", StringComparison.OrdinalIgnoreCase)) male.Add(voiceInfo);
                else female.Add(voiceInfo);
            }

            femaleVoiceScroll = DrawVoiceScrollList(female, new Rect(20, 193, 340, 207), femaleVoiceScroll);
            maleVoiceScroll = DrawVoiceScrollList(male, new Rect(380, 193, 340, 207), maleVoiceScroll);
            DrawVoicePackChoices();
        }

        private string SelectedVoicePackLabel()
        {
            string mode = (kokoroVoiceMode.Value ?? "custom").Trim().ToLowerInvariant();
            if (mode == "default") return "Default";
            if (mode == "all") return "All Voices";
            if (mode == "pack")
            {
                DiscoveredVoicePack pack = FindKokoroPack(selectedKokoroVoicePack.Value);
                return pack == null ? "Missing pack: " + selectedKokoroVoicePack.Value : pack.Name;
            }
            return "Custom";
        }

        private void DrawVoicePackChoices()
        {
            if (!showVoicePackChoices) return;
            float height = Math.Min(260f, 98f + discoveredKokoroPacks.Count * 30f);
            GUI.Box(new Rect(140, 124, 420, height), string.Empty);
            float contentHeight = 92f + discoveredKokoroPacks.Count * 30f;
            voicePackSelectorScroll = GUI.BeginScrollView(new Rect(146, 130, 408, height - 12f), voicePackSelectorScroll, new Rect(0, 0, 388, contentHeight));
            if (GUI.Button(new Rect(2, 2, 380, 26), "Default")) SelectKokoroVoiceSource("default", string.Empty);
            if (GUI.Button(new Rect(2, 32, 380, 26), "All Voices")) SelectKokoroVoiceSource("all", string.Empty);
            for (int i = 0; i < discoveredKokoroPacks.Count; i++)
            {
                DiscoveredVoicePack pack = discoveredKokoroPacks[i];
                if (GUI.Button(new Rect(2, 62 + i * 30, 380, 26), pack.Name)) SelectKokoroVoiceSource("pack", pack.Id);
            }
            if (GUI.Button(new Rect(2, 62 + discoveredKokoroPacks.Count * 30, 380, 26), "Custom")) SelectKokoroVoiceSource("custom", string.Empty);
            GUI.EndScrollView();
        }

        private void SelectKokoroVoiceSource(string mode, string packId)
        {
            kokoroVoiceMode.Value = mode;
            if (mode == "pack") selectedKokoroVoicePack.Value = packId;
            showVoicePackChoices = false;
            LoadVoiceRules();
            Config.Save();
        }

        private Vector2 DrawVoiceScrollList(List<DiscoveredVoice> voices, Rect viewport, Vector2 scroll)
        {
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.wordWrap = true;
            float rowWidth = viewport.width - 28f;
            float contentHeight = 4f;
            foreach (DiscoveredVoice voiceInfo in voices)
                contentHeight += Math.Max(42f, toggleStyle.CalcHeight(new GUIContent(VoiceMenuLabel(voiceInfo)), rowWidth)) + 4f;
            contentHeight = Math.Max(viewport.height - 4f, contentHeight);
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, viewport.width - 20f, contentHeight));
            float y = 2f;
            for (int i = 0; i < voices.Count; i++)
            {
                DiscoveredVoice voiceInfo = voices[i];
                string label = VoiceMenuLabel(voiceInfo);
                float height = Math.Max(42f, toggleStyle.CalcHeight(new GUIContent(label), rowWidth));
                bool current = IsVoiceEnabled(voiceInfo);
                bool selected = GUI.Toggle(new Rect(4, y, rowWidth, height), current, label, toggleStyle);
                if (selected != current) SetVoiceEnabled(voiceInfo, selected);
                y += height + 4f;
            }
            GUI.EndScrollView();
            return scroll;
        }

        private static string VoiceMenuLabel(DiscoveredVoice voiceInfo)
        {
            string origin = voiceInfo.External ? "Pack: " + voiceInfo.PackName : "Built-in default voice";
            return FriendlyVoiceLabel(voiceInfo) + "\n" + origin;
        }

        private static string FriendlyVoiceLabel(DiscoveredVoice voiceInfo)
        {
            string code = string.IsNullOrEmpty(voiceInfo.ShortCode) ? (voiceInfo.External ? voiceInfo.PackName : "DF") : voiceInfo.ShortCode;
            return voiceInfo.DisplayName + " (" + code + ")";
        }

        private void DrawManualEngineVoicePackSettings()
        {
            if (!manualEngineVoicePacksAvailable)
            {
                GUI.Box(new Rect(20, 105, 700, 80), "Voice packs are engine-specific. Install a voice pack compatible with the selected engine.\nOptional engine add-ons are required only for voice packs that use those engines.");
                return;
            }
            GUI.Label(new Rect(20, 105, 700, 22), "The selected optional engine manages its own compatible voices and packages.");
            GUI.Label(new Rect(20, 135, 700, 22), "Legacy optional-engine folders remain supported when already installed.");
            GUI.Label(new Rect(20, 165, 700, 22), "Restart after installing or removing an engine add-on or its voice package.");
            if (GUI.Button(new Rect(20, 195, 150, 28), "Use Engine Default")) selectedEngineVoicePack.Value = string.Empty;

            GUI.Label(new Rect(190, 200, 530, 22), "Engine Voice Pack: " + (string.IsNullOrEmpty(selectedEngineVoicePack.Value) ? "Engine default" : selectedEngineVoicePack.Value));
            if (manualEngineVoicePacks.Count == 0)
            {
                GUI.Box(new Rect(20, 240, 700, 70), "No legacy Core-managed voice folders were found.\nUse the selected engine add-on's documented package structure.\nCore does not create engine-specific voice folders automatically.");
            }
            else
            {
                int shown = Math.Min(8, manualEngineVoicePacks.Count);
                for (int i = 0; i < shown; i++)
                {
                    string name = manualEngineVoicePacks[i];
                    if (GUI.Button(new Rect(20 + (i % 2) * 350, 240 + (i / 2) * 32, 330, 27), name)) selectedEngineVoicePack.Value = name;
                }
            }
            GUI.Label(new Rect(20, 365, 700, 42), "Optional add-ons are user-managed. Review their source, dependencies, privacy behaviour and licence before installing them.");
        }

        private void DrawDataAndStatus()
        {
            GUI.Label(new Rect(20, 105, 700, 22), "NPC records: " + npcDatabase.Count + "    |    Overrides: " + npcVoiceOverrides.Overrides.Count + "    |    Presets: " + discoveredNpcPresets.Count);
            unknownSpeakerPrompt.Value = GUI.Toggle(new Rect(20, 145, 380, 24), unknownSpeakerPrompt.Value, "Ask when an unknown speaker is encountered");
            GUI.Label(new Rect(20, 190, 700, 42), "Character-database and reload controls are available in Advanced > Maintenance.");
            GUI.Label(new Rect(20, 245, 700, 70), "Shared data uses ObenVoice; personal files use ObenVoice/UserData on new installs.\nExisting legacy user data remains in place and is not moved automatically.\nEngine add-ons, voice packs and optional NPC presets are detected at startup.");
        }

        private void DrawAdvancedSettings()
        {
            GUI.Label(new Rect(20, 96, 700, 22), "Diagnostics");
            debugLogging.Value = GUI.Toggle(new Rect(20, 122, 330, 24), debugLogging.Value, "Continuous detailed logging (normally off)");
            GUI.Label(new Rect(390, 126, 90, 22), "Flag key");
            DrawHotkeyField(new Rect(480, 120, 210, 28), flagDialogueHotkey, 3);
            if (!confirmClearDiagnosticLog)
            {
                if (GUI.Button(new Rect(585, 152, 105, 26), "Clear Log")) confirmClearDiagnosticLog = true;
                GUI.Label(new Rect(20, 154, 550, 40), diagnosticStatus);
            }
            else
            {
                GUI.Label(new Rect(20, 154, 360, 22), "Clear diagnostic log? This deletes recorded entries.");
                if (GUI.Button(new Rect(400, 150, 90, 26), "Cancel")) confirmClearDiagnosticLog = false;
                if (GUI.Button(new Rect(500, 150, 100, 26), "Clear Log"))
                {
                    ClearDialogueDiagnosticLog();
                    confirmClearDiagnosticLog = false;
                }
            }

            GUI.Label(new Rect(20, 202, 700, 22), "Maintenance");
            bool oldEnabled = GUI.enabled;
            GUI.enabled = restartInProgress == 0 && IsInstalledEngine(preferredTtsEngine.Value);
            if (GUI.Button(new Rect(20, 228, 210, 28), restartInProgress == 0 ? "Restart Voice Engine" : "Restarting...")) RestartVoiceEngine();
            GUI.enabled = oldEnabled;
            if (GUI.Button(new Rect(250, 228, 230, 28), "Reload Pronunciation Rules"))
            {
                LoadPronunciationRules();
                LoadEditableRuleFiles();
                backendState = "Pronunciation and exclusion rules reloaded";
            }

            GUI.Label(new Rect(20, 275, 700, 22), "Reset");
            if (!confirmResetDefaults)
            {
                if (GUI.Button(new Rect(20, 301, 220, 28), "Reset Settings to Defaults")) confirmResetDefaults = true;
                GUI.Label(new Rect(260, 305, 450, 22), "Keeps Overrides, pronunciation rules and exclusions.");
            }
            else if (confirmResetDefaults)
            {
                GUI.Label(new Rect(20, 305, 360, 22), "Reset ordinary settings and keep local voice data?");
                if (GUI.Button(new Rect(390, 299, 90, 26), "Cancel")) confirmResetDefaults = false;
                if (GUI.Button(new Rect(490, 299, 90, 26), "Reset")) ResetOrdinarySettings();
            }

            GUI.Label(new Rect(20, 350, 700, 22), "Help");
            if (GUI.Button(new Rect(20, 376, 220, 28), "Help / Documentation"))
                Application.OpenURL("https://github.com/LM-Mods/ObenVoice#readme");
            GUI.Label(new Rect(260, 380, 450, 22), "Opens the ObenVoice README in your browser.");
        }

        private void DrawPronunciationSettings()
        {
            GUI.Box(new Rect(16, 96, 352, 322), "Pronunciation replacements");
            GUI.Box(new Rect(376, 96, 368, 322), "Speech exclusions");

            int previousPronunciation = selectedPronunciation;
            selectedPronunciation = DrawEditableRuleList(new Rect(24, 122, 336, 104), pronunciationDocument.Entries, pronunciationScroll, selectedPronunciation, out pronunciationScroll, false);
            if (selectedPronunciation != previousPronunciation && selectedPronunciation >= 0 && selectedPronunciation < pronunciationDocument.Entries.Count)
            {
                EditableSpeechRule entry = pronunciationDocument.Entries[selectedPronunciation];
                pronunciationOriginal = entry.First;
                pronunciationReplacement = entry.Second;
            }
            GUI.Label(new Rect(24, 232, 92, 20), "Original Text");
            GUI.SetNextControlName("TTSRulePronunciationOriginal");
            pronunciationOriginal = GUI.TextField(new Rect(116, 230, 242, 23), pronunciationOriginal, 64);
            GUI.Label(new Rect(24, 260, 92, 20), "Say Instead");
            GUI.SetNextControlName("TTSRulePronunciationReplacement");
            pronunciationReplacement = GUI.TextField(new Rect(116, 258, 242, 23), pronunciationReplacement, 128);
            if (GUI.Button(new Rect(24, 288, 72, 25), "Add")) SavePronunciationEntry(false);
            GUI.enabled = selectedPronunciation >= 0 && selectedPronunciation < pronunciationDocument.Entries.Count;
            if (GUI.Button(new Rect(102, 288, 72, 25), "Update")) SavePronunciationEntry(true);
            if (GUI.Button(new Rect(180, 288, 72, 25), "Delete")) DeletePronunciationEntry();
            GUI.enabled = true;
            if (GUI.Button(new Rect(258, 288, 100, 25), "Clear fields")) ClearPronunciationFields();
            DrawPronunciationReset();

            int previousExclusion = selectedExclusion;
            selectedExclusion = DrawEditableRuleList(new Rect(384, 122, 352, 104), exclusionDocument.Entries, exclusionScroll, selectedExclusion, out exclusionScroll, true);
            if (selectedExclusion != previousExclusion && selectedExclusion >= 0 && selectedExclusion < exclusionDocument.Entries.Count)
            {
                EditableSpeechRule entry = exclusionDocument.Entries[selectedExclusion];
                exclusionSpeaker = entry.First == "*" ? string.Empty : entry.First;
                exclusionText = entry.Second == "*" ? string.Empty : entry.Second;
            }
            GUI.Label(new Rect(384, 232, 92, 20), "Speaker");
            GUI.SetNextControlName("TTSRuleExclusionSpeaker");
            exclusionSpeaker = GUI.TextField(new Rect(476, 230, 258, 23), exclusionSpeaker, 128);
            GUI.Label(new Rect(384, 260, 92, 20), "Spoken Text");
            GUI.SetNextControlName("TTSRuleExclusionText");
            exclusionText = GUI.TextField(new Rect(476, 258, 258, 23), exclusionText, 256);
            if (GUI.Button(new Rect(384, 288, 72, 25), "Add")) SaveExclusionEntry(false);
            GUI.enabled = selectedExclusion >= 0 && selectedExclusion < exclusionDocument.Entries.Count;
            if (GUI.Button(new Rect(462, 288, 72, 25), "Update")) SaveExclusionEntry(true);
            if (GUI.Button(new Rect(540, 288, 72, 25), "Delete")) DeleteExclusionEntry();
            GUI.enabled = true;
            if (GUI.Button(new Rect(618, 288, 116, 25), "Clear fields")) ClearExclusionFields();
            DrawExclusionReset();

            string focused = GUI.GetNameOfFocusedControl();
            typingInRuleEditor = !string.IsNullOrEmpty(focused) && focused.StartsWith("TTSRule", StringComparison.Ordinal);
        }

        private static int DrawEditableRuleList(Rect area, List<EditableSpeechRule> entries, Vector2 scroll, int selected, out Vector2 updatedScroll, bool exclusions)
        {
            float contentHeight = Math.Max(area.height - 4f, entries.Count * 25f + 4f);
            scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, area.width - 18f, contentHeight));
            for (int i = 0; i < entries.Count; i++)
            {
                EditableSpeechRule entry = entries[i];
                string label = entry.First + "  →  " + entry.Second;
                if (GUI.Button(new Rect(2, 2 + i * 25f, area.width - 24f, 23f), label)) selected = i;
            }
            GUI.EndScrollView();
            updatedScroll = scroll;
            return selected;
        }

        private void SavePronunciationEntry(bool update)
        {
            string original = (pronunciationOriginal ?? string.Empty).Trim();
            string replacement = (pronunciationReplacement ?? string.Empty).Trim();
            if (original.Length == 0 || original.Contains("=>") || original.IndexOfAny(new char[] { '\r', '\n' }) >= 0 || replacement.IndexOfAny(new char[] { '\r', '\n' }) >= 0)
            {
                backendState = "Pronunciation entry is invalid";
                return;
            }
            if (update && selectedPronunciation >= 0 && selectedPronunciation < pronunciationDocument.Entries.Count)
                pronunciationDocument.Entries[selectedPronunciation] = new EditableSpeechRule(original, replacement);
            else
            {
                pronunciationDocument.Entries.Add(new EditableSpeechRule(original, replacement));
                selectedPronunciation = pronunciationDocument.Entries.Count - 1;
            }
            PersistPronunciationDocument();
        }

        private void DeletePronunciationEntry()
        {
            if (selectedPronunciation < 0 || selectedPronunciation >= pronunciationDocument.Entries.Count) return;
            pronunciationDocument.Entries.RemoveAt(selectedPronunciation);
            ClearPronunciationFields();
            PersistPronunciationDocument();
        }

        private void ClearPronunciationFields()
        {
            selectedPronunciation = -1;
            pronunciationOriginal = string.Empty;
            pronunciationReplacement = string.Empty;
        }

        private void SaveExclusionEntry(bool update)
        {
            string speaker = (exclusionSpeaker ?? string.Empty).Trim();
            string text = (exclusionText ?? string.Empty).Trim();
            if (speaker.Length == 0) speaker = "*";
            if (text.Length == 0) text = "*";
            if ((speaker == "*" && text == "*") || speaker.Contains("=>") || text.Contains("=>") || speaker.IndexOfAny(new char[] { '\r', '\n' }) >= 0 || text.IndexOfAny(new char[] { '\r', '\n' }) >= 0)
            {
                backendState = "Exclusion must specify a speaker, spoken text, or both";
                return;
            }
            if (update && selectedExclusion >= 0 && selectedExclusion < exclusionDocument.Entries.Count)
                exclusionDocument.Entries[selectedExclusion] = new EditableSpeechRule(speaker, text);
            else
            {
                exclusionDocument.Entries.Add(new EditableSpeechRule(speaker, text));
                selectedExclusion = exclusionDocument.Entries.Count - 1;
            }
            PersistExclusionDocument();
        }

        private void DeleteExclusionEntry()
        {
            if (selectedExclusion < 0 || selectedExclusion >= exclusionDocument.Entries.Count) return;
            exclusionDocument.Entries.RemoveAt(selectedExclusion);
            ClearExclusionFields();
            PersistExclusionDocument();
        }

        private void ClearExclusionFields()
        {
            selectedExclusion = -1;
            exclusionSpeaker = string.Empty;
            exclusionText = string.Empty;
        }

        private void DrawPronunciationReset()
        {
            if (!confirmPronunciationReset)
            {
                if (GUI.Button(new Rect(24, 326, 230, 25), "Reset pronunciation defaults")) confirmPronunciationReset = true;
            }
            else
            {
                GUI.Label(new Rect(24, 326, 138, 22), "Replace all rules?");
                if (GUI.Button(new Rect(164, 324, 62, 25), "Reset"))
                {
                    pronunciationDocument = EditableSpeechRuleDocument.Parse(DefaultPronunciationFile, false, null);
                    ClearPronunciationFields();
                    PersistPronunciationDocument();
                    confirmPronunciationReset = false;
                }
                if (GUI.Button(new Rect(232, 324, 62, 25), "Cancel")) confirmPronunciationReset = false;
            }
            GUI.Label(new Rect(24, 360, 332, 45), "Changes save immediately and affect the next spoken line. Malformed file lines are preserved.");
        }

        private void DrawExclusionReset()
        {
            if (!confirmExclusionReset)
            {
                if (GUI.Button(new Rect(384, 326, 230, 25), "Restore default exclusions")) confirmExclusionReset = true;
            }
            else
            {
                GUI.Label(new Rect(384, 326, 138, 22), "Replace exclusions?");
                if (GUI.Button(new Rect(524, 324, 62, 25), "Restore"))
                {
                    exclusionDocument = EditableSpeechRuleDocument.Parse(DefaultExclusionFile, true, null);
                    ClearExclusionFields();
                    PersistExclusionDocument();
                    confirmExclusionReset = false;
                }
                if (GUI.Button(new Rect(592, 324, 62, 25), "Cancel")) confirmExclusionReset = false;
            }
            GUI.Label(new Rect(384, 360, 346, 45), "Leave one field blank to match any value. Matching is exact; ordinary doorphone dialogue is unaffected.");
        }

        private string DescribeHotkey()
        {
            return settingsHotkey == null ? "F8" : settingsHotkey.Value.ToString();
        }

        private string DescribePlayerVoiceHotkey()
        {
            return playerVoiceHotkey == null ? "F9" : playerVoiceHotkey.Value.ToString();
        }

        private string[] GetEngineAddonRoots()
        {
            return new string[] {
                Path.Combine(GetObenVoiceRoot(), "engine-addons"),
                Path.Combine(GetLegacyRoot(), "engine-addons")
            };
        }

        private string GetPronunciationRulesPath()
        {
            return Path.Combine(GetUserDataRoot(), "pronunciation-replacements.txt");
        }

        private string GetSpeechExclusionsPath()
        {
            return Path.Combine(GetUserDataRoot(), "speech-exclusions.txt");
        }

        private void LoadEditableRuleFiles()
        {
            try
            {
                pronunciationDocument = EditableSpeechRuleDocument.Parse(File.ReadAllText(GetPronunciationRulesPath(), Encoding.UTF8), false, message => Logger.LogWarning(message));
            }
            catch (Exception ex)
            {
                pronunciationDocument = EditableSpeechRuleDocument.Parse(DefaultPronunciationFile, false, null);
                Logger.LogWarning("Could not load pronunciation editor data: " + ex.Message);
            }

            string exclusionsPath = GetSpeechExclusionsPath();
            try
            {
                if (!File.Exists(exclusionsPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(exclusionsPath));
                    File.WriteAllText(exclusionsPath, DefaultExclusionFile, new UTF8Encoding(false));
                }
                exclusionDocument = EditableSpeechRuleDocument.Parse(File.ReadAllText(exclusionsPath, Encoding.UTF8), true, message => Logger.LogWarning(message));
                if (SpeechExclusionDefaults.AddUpgradeDefaults(exclusionDocument))
                {
                    try
                    {
                        WriteTextSafely(exclusionsPath, exclusionDocument.Serialize("# Exact speech exclusions. Use * to match any speaker or any text."));
                        Logger.LogInfo("Added the version 2 editable speech-exclusion defaults without replacing existing rules.");
                    }
                    catch (Exception migrationException)
                    {
                        Logger.LogWarning("Applied the version 2 speech exclusions for this session but could not save the migration: " + migrationException.Message);
                    }
                }
                Logger.LogInfo("Loaded " + exclusionDocument.Entries.Count + " speech exclusion rules from " + exclusionsPath);
            }
            catch (Exception ex)
            {
                exclusionDocument = EditableSpeechRuleDocument.Parse(DefaultExclusionFile, true, null);
                Logger.LogWarning("Could not load speech exclusions; using the built-in default rules: " + ex.Message);
            }
        }

        private void PersistPronunciationDocument()
        {
            try
            {
                WriteTextSafely(GetPronunciationRulesPath(), pronunciationDocument.Serialize("# Speech-only pronunciation replacements. Format: source => spoken replacement"));
                LoadPronunciationRules();
                backendState = "Pronunciation rules saved and applied";
            }
            catch (Exception ex)
            {
                backendState = "Could not save pronunciation rules";
                Logger.LogWarning("Could not save pronunciation replacements: " + ex.Message);
            }
        }

        private void PersistExclusionDocument()
        {
            try
            {
                WriteTextSafely(GetSpeechExclusionsPath(), exclusionDocument.Serialize("# Exact speech exclusions. Format: speaker => spoken text"));
                backendState = "Speech exclusions saved and applied";
            }
            catch (Exception ex)
            {
                backendState = "Could not save speech exclusions";
                Logger.LogWarning("Could not save speech exclusions: " + ex.Message);
            }
        }

        private static void WriteTextSafely(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
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

        private void LoadPronunciationRules()
        {
            string path = GetPronunciationRulesPath();
            try
            {
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, DefaultPronunciationFile, new UTF8Encoding(false));
                }
                pronunciationRuleCount = SpeechTextRules.LoadPronunciationReplacements(File.ReadAllText(path, Encoding.UTF8), message => Logger.LogWarning(message));
                Logger.LogInfo("Loaded " + pronunciationRuleCount + " pronunciation replacement rules from " + path);
            }
            catch (Exception ex)
            {
                pronunciationRuleCount = SpeechTextRules.LoadPronunciationReplacements(null, null);
                Logger.LogWarning("Could not load pronunciation replacements; using the built-in Hmh rule: " + ex.Message);
            }
        }

        private void ScanInstalledEngineAddons()
        {
            installedEngineIds.Clear();
            installedEngineIds.Add("kokoro");
            try
            {
                HashSet<string> discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string root in GetEngineAddonRoots())
                {
                    if (!Directory.Exists(root)) continue;
                    string[] directories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);
                    Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
                    foreach (string directory in directories)
                    {
                        string manifestPath = Path.Combine(directory, "manifest.json");
                        if (!File.Exists(manifestPath)) continue;
                        EngineAddonManifest manifest = JsonUtility.FromJson<EngineAddonManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
                        string id = manifest == null ? string.Empty : (manifest.id ?? string.Empty).Trim().ToLowerInvariant();
                        string directoryName = Path.GetFileName(Path.GetDirectoryName(manifestPath));
                        if (!Regex.IsMatch(id, "^[a-z0-9][a-z0-9_-]{0,31}$") || !string.Equals(id, directoryName, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.LogWarning("Ignoring invalid TTS engine add-on manifest: " + manifestPath);
                            continue;
                        }
                        if (!File.Exists(Path.Combine(Path.GetDirectoryName(manifestPath), "engine.py")))
                        {
                            Logger.LogWarning("Ignoring incomplete TTS engine add-on: " + id);
                            continue;
                        }
                        if (!discovered.Add(id))
                        {
                            Logger.LogInfo("Ignoring legacy duplicate TTS engine add-on: " + id);
                            continue;
                        }
                        installedEngineIds.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan optional TTS engine add-ons: " + ex.Message);
            }
            manualEngineVoicePacksAvailable = installedEngineIds.Count > 1;
        }

        private string[] GetInstalledEngineIds()
        {
            List<string> engines = new List<string>(installedEngineIds);
            engines.Sort(StringComparer.OrdinalIgnoreCase);
            if (engines.Remove("kokoro")) engines.Insert(0, "kokoro");
            return engines.ToArray();
        }

        private bool IsInstalledEngine(string engineId)
        {
            return !string.IsNullOrEmpty(engineId) && installedEngineIds.Contains(engineId.Trim());
        }

        private string GetCharactersRoot()
        {
            return Path.Combine(GetObenVoiceRoot(), "Characters");
        }

        private string[] GetKokoroPackDirectories()
        {
            List<string> directories = new List<string>();
            string primaryRoot = Path.Combine(GetObenVoiceRoot(), "VoicePacks");
            string legacyRoot = Path.Combine(GetLegacyRoot(), "voice-packs", "kokoro");
            foreach (string root in new string[] { primaryRoot, legacyRoot })
            {
                if (!Directory.Exists(root)) continue;
                string[] rootDirectories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);
                Array.Sort(rootDirectories, delegate(string left, string right)
                {
                    return VoicePackOrdering.CompareNames(Path.GetFileName(left), Path.GetFileName(right));
                });
                foreach (string directory in rootDirectories)
                {
                    string full = Path.GetFullPath(directory);
                    if (!directories.Exists(item => string.Equals(item, full, StringComparison.OrdinalIgnoreCase))) directories.Add(full);
                }
            }
            return directories.ToArray();
        }

        private void ScanInstalledVoicePacks()
        {
            availableKokoroVoices.Clear();
            installedKokoroVoiceIds.Clear();
            discoveredKokoroPacks.Clear();
            discoveredNpcPresets.Clear();
            AddDiscoveredKokoroVoice("af_heart", "Heart", "female", "default", "Default", "DF", false);
            AddDiscoveredKokoroVoice("af_bella", "Bella", "female", "default", "Default", "DF", false);
            AddDiscoveredKokoroVoice("af_sarah", "Sarah", "female", "default", "Default", "DF", false);
            AddDiscoveredKokoroVoice("am_adam", "Adam", "male", "default", "Default", "DF", false);
            AddDiscoveredKokoroVoice("am_michael", "Michael", "male", "default", "Default", "DF", false);
            AddDiscoveredKokoroVoice("am_onyx", "Onyx", "male", "default", "Default", "DF", false);

            try
            {
                string[] packDirectories = GetKokoroPackDirectories();
                Logger.LogInfo("Scanning " + packDirectories.Length + " Kokoro voice-pack folder(s) from the primary and legacy roots.");
                if (packDirectories.Length == 0)
                {
                    Logger.LogInfo("No Kokoro voice-pack folders were found.");
                }
                HashSet<string> packIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string packDirectory in packDirectories)
                {
                    try
                    {
                        string manifestPath = Path.Combine(packDirectory, "manifest.json");
                        if (!File.Exists(manifestPath))
                        {
                            Logger.LogWarning("Ignoring voice-pack folder with no manifest.json: " + packDirectory);
                            continue;
                        }
                        VoicePackManifest manifest = VoicePackJson.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                        string packId = manifest == null ? string.Empty : (manifest.id ?? string.Empty).Trim().ToLowerInvariant();
                        string engineId = manifest == null ? string.Empty : (manifest.engine ?? string.Empty).Trim().ToLowerInvariant();
                        if (manifest == null || manifest.manifestVersion != 1 || manifest.voices == null ||
                            !Regex.IsMatch(packId, "^[a-z0-9][a-z0-9_-]{0,63}$") ||
                            !string.Equals(engineId, "kokoro", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.LogWarning("Ignoring invalid Kokoro voice-pack manifest (requires manifestVersion=1, engine=kokoro, and a valid id): " + manifestPath);
                            continue;
                        }

                        string packName = string.IsNullOrEmpty(manifest.name) ? packId : manifest.name.Trim();
                        if (!packIds.Add(packId))
                        {
                            Logger.LogWarning("Ignoring duplicate Kokoro voice-pack id: " + packId);
                            continue;
                        }
                        string shortCode = (manifest.shortCode ?? string.Empty).Trim();
                        if (!Regex.IsMatch(shortCode, "^[A-Za-z0-9]{1,8}$")) shortCode = string.Empty;
                        DiscoveredVoicePack discoveredPack = new DiscoveredVoicePack();
                        discoveredPack.Id = packId;
                        discoveredPack.Name = packName;
                        discoveredPack.ShortCode = shortCode;
                        discoveredKokoroPacks.Add(discoveredPack);
                        string packRoot = Path.GetFullPath(packDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                        Logger.LogInfo("Validated Kokoro voice pack '" + packName + "' with " + manifest.voices.Length + " manifest voice entries at " + packDirectory);
                        if (manifest.voices.Length == 0)
                        {
                            Logger.LogWarning("Kokoro voice-pack manifest contains no voices: " + manifestPath);
                            continue;
                        }
                        foreach (VoicePackVoice item in manifest.voices)
                        {
                            if (item == null) continue;
                            string voiceId = (item.id ?? string.Empty).Trim().ToLowerInvariant();
                            string gender = NormalizeGender(item.gender);
                            string voicePath = Path.GetFullPath(Path.Combine(packDirectory, item.file ?? string.Empty));
                            if (!Regex.IsMatch(voiceId, "^[a-z][a-z0-9_]{0,63}$") || !IsKnownGender(gender))
                            {
                                Logger.LogWarning("Ignoring voice entry with invalid id or gender in " + manifestPath + ": " + voiceId);
                                continue;
                            }
                            if (!voicePath.StartsWith(packRoot, StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.LogWarning("Ignoring voice file path outside its pack folder for " + voiceId + ": " + voicePath);
                                continue;
                            }
                            if (!string.Equals(Path.GetExtension(voicePath), ".pt", StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.LogWarning("Ignoring non-.pt Kokoro voice file for " + voiceId + ": " + voicePath);
                                continue;
                            }
                            if (!File.Exists(voicePath))
                            {
                                Logger.LogWarning("Kokoro voice file is missing for " + voiceId + ": " + voicePath);
                                continue;
                            }
                            if (AddDiscoveredKokoroVoice(voiceId, string.IsNullOrEmpty(item.displayName) ? voiceId : item.displayName.Trim(), gender, packId, packName, shortCode, true))
                            {
                                discoveredPack.VoiceIds.Add(voiceId);
                                Logger.LogInfo("Discovered external Kokoro voice " + voiceId + " (" + gender + ") from '" + packName + "' at " + voicePath);
                            }
                        }
                        if (discoveredPack.VoiceIds.Count == 0)
                        {
                            discoveredKokoroPacks.Remove(discoveredPack);
                            Logger.LogWarning("Ignoring Kokoro voice pack with no valid installed voices: " + packId);
                            continue;
                        }
                        ScanNpcPresets(packDirectory, discoveredPack);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Ignoring unreadable Kokoro voice pack " + packDirectory + ": " + ex.Message);
                    }
                }
                Logger.LogInfo("Discovered " + (availableKokoroVoices.Count - BundledKokoroVoices.Length) + " external Kokoro voices.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan external Kokoro voice packs: " + ex.Message);
            }
            ScanCharacterPresets();
        }

        private bool AddDiscoveredKokoroVoice(string id, string displayName, string gender, string packId, string packName, string shortCode, bool external)
        {
            if (!installedKokoroVoiceIds.Add(id))
            {
                if (external) Logger.LogWarning("Ignoring duplicate external Kokoro voice id: " + id);
                return false;
            }
            DiscoveredVoice voiceInfo = new DiscoveredVoice();
            voiceInfo.Id = id;
            voiceInfo.DisplayName = displayName;
            voiceInfo.Gender = gender;
            voiceInfo.PackId = packId;
            voiceInfo.PackName = packName;
            voiceInfo.ShortCode = shortCode;
            voiceInfo.External = external;
            availableKokoroVoices.Add(voiceInfo);
            return true;
        }

        private void ScanNpcPresets(string packDirectory, DiscoveredVoicePack pack)
        {
            string presetsDirectory = Path.Combine(packDirectory, "presets");
            if (!Directory.Exists(presetsDirectory)) return;
            string[] files = Directory.GetFiles(presetsDirectory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, delegate(string left, string right)
            {
                return VoicePackOrdering.CompareNames(Path.GetFileName(left), Path.GetFileName(right));
            });
            foreach (string file in files)
            {
                try
                {
                    NpcVoicePreset preset = NpcVoicePreset.Parse(File.ReadAllText(file, Encoding.UTF8), pack.Id);
                    if (discoveredNpcPresets.Exists(item => string.Equals(item.StableId, preset.StableId, StringComparison.OrdinalIgnoreCase)))
                    {
                        Logger.LogWarning("Ignoring duplicate NPC preset id: " + preset.StableId);
                        continue;
                    }
                    discoveredNpcPresets.Add(preset);
                    Logger.LogInfo("Discovered NPC preset '" + preset.Name + "' from " + pack.Name + ".");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Ignoring invalid NPC preset " + file + ": " + ex.Message);
                }
            }
        }

        private void ScanCharacterPresets()
        {
            string directory = GetCharactersRoot();
            if (!Directory.Exists(directory)) return;
            string[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, delegate(string left, string right)
            {
                return VoicePackOrdering.CompareNames(Path.GetFileName(left), Path.GetFileName(right));
            });
            foreach (string file in files)
            {
                if (string.Equals(Path.GetFileName(file), "characters.json", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    SimpleJsonValue root = SimpleJson.Parse(json);
                    string packId = root.StringOrEmpty("packId").Trim().ToLowerInvariant();
                    if (!Regex.IsMatch(packId, "^[a-z0-9][a-z0-9_-]{0,63}$"))
                        throw new FormatException("Central character preset requires a valid packId.");
                    if (!string.Equals(packId, "core", StringComparison.OrdinalIgnoreCase) && FindKokoroPack(packId) == null)
                    {
                        Logger.LogInfo("Skipping character preset because its voice pack is not installed: " + file);
                        continue;
                    }
                    NpcVoicePreset preset = NpcVoicePreset.Parse(json, packId);
                    int existing = discoveredNpcPresets.FindIndex(item => string.Equals(item.StableId, preset.StableId, StringComparison.OrdinalIgnoreCase));
                    if (existing >= 0) discoveredNpcPresets[existing] = preset;
                    else discoveredNpcPresets.Add(preset);
                    Logger.LogInfo("Loaded central character preset '" + preset.Name + "' from " + file + ".");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Ignoring invalid central character preset " + file + ": " + ex.Message);
                }
            }
        }

        private bool IsVoiceEnabled(DiscoveredVoice voiceInfo)
        {
            string[] pool = string.Equals(voiceInfo.Gender, "male", StringComparison.OrdinalIgnoreCase) ? maleVoices : femaleVoices;
            return Array.Exists(pool, item => string.Equals(item, voiceInfo.Id, StringComparison.OrdinalIgnoreCase));
        }

        private void SetVoiceEnabled(DiscoveredVoice voiceInfo, bool enabled)
        {
            if (string.Equals(voiceInfo.Gender, "male", StringComparison.OrdinalIgnoreCase))
                maleVoicePool.Value = VoiceSelectionRules.SetEnabled(maleVoicePool.Value, voiceInfo.Id, enabled, "am_adam");
            else
                femaleVoicePool.Value = VoiceSelectionRules.SetEnabled(femaleVoicePool.Value, voiceInfo.Id, enabled, "af_heart");
            unknownVoicePool.Value = VoiceSelectionRules.MergeConfigured(femaleVoicePool.Value, maleVoicePool.Value);
            LoadVoiceRules();
            Config.Save();
        }

        private string[] GetManualEngineVoicePackRoots()
        {
            return new string[] {
                Path.Combine(GetObenVoiceRoot(), "VoicePacks"),
                Path.Combine(GetLegacyRoot(), "VoicePacks")
            };
        }

        private void ScanManualEngineVoicePacks()
        {
            manualEngineVoicePacks.Clear();
            try
            {
                foreach (string root in GetManualEngineVoicePackRoots())
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (string directory in Directory.GetDirectories(root))
                    {
                        string name = Path.GetFileName(directory);
                        if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) continue;
                        string manifestPath = Path.Combine(directory, "manifest.json");
                        if (File.Exists(manifestPath))
                        {
                            VoicePackManifest manifest = VoicePackJson.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                            if (manifest != null && string.Equals(manifest.engine, "kokoro", StringComparison.OrdinalIgnoreCase)) continue;
                        }
                        if ((File.Exists(manifestPath) || Directory.GetFiles(directory, "*.safetensors", SearchOption.TopDirectoryOnly).Length > 0) &&
                            !manualEngineVoicePacks.Exists(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase)))
                            manualEngineVoicePacks.Add(name);
                    }
                }
                manualEngineVoicePacks.Sort(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(selectedEngineVoicePack.Value) && !manualEngineVoicePacks.Contains(selectedEngineVoicePack.Value))
                    selectedEngineVoicePack.Value = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan manual optional-engine voice packs: " + ex.Message);
            }
        }

        private void EnsureManualEngineVoicePackReadme(string root)
        {
            string path = Path.Combine(root, "README.txt");
            if (File.Exists(path)) return;
            string text =
                "ObenVoice - Optional Engine Voice Packs\r\n\r\n" +
                "Create one folder per compatible voice pack, for example:\r\n" +
                "  VoicePacks\\MyPack\\voice.safetensors\r\n\r\n" +
                "A manifest.json is optional. Without one, the mod uses the first .safetensors file in the pack folder.\r\n\r\n" +
                "This is your user-managed folder. You choose where files come from. The mod does not verify that a pack is safe, compatible, licensed, or redistributable; those checks are your responsibility.\r\n\r\n" +
                "The selected engine add-on determines which voice-file formats are compatible. Other engines need their own supported add-on.\r\n";
            File.WriteAllText(path, text, Encoding.UTF8);
        }

        private void DrawUnknownSpeakerWindow(int id)
        {
            if (activeUnknown == null) return;
            GUI.Label(new Rect(20, 30, 400, 24), activeUnknown.Name);
            rememberUnknownChoice = GUI.Toggle(new Rect(20, 58, 240, 22), rememberUnknownChoice, "Remember this choice");

            if (choosingUnknownVoice)
            {
                GUI.Label(new Rect(20, 85, 400, 22), "Choose an installed Kokoro voice:");
                List<string> choices = new List<string>();
                AddUniqueVoices(choices, femaleVoices);
                AddUniqueVoices(choices, maleVoices);
                AddUniqueVoices(choices, unknownVoices);
                int columns = 2;
                for (int i = 0; i < choices.Count && i < 10; i++)
                {
                    int row = i / columns;
                    int column = i % columns;
                    if (GUI.Button(new Rect(20 + column * 200, 112 + row * 27, 190, 24), choices[i]))
                        ApplyUnknownChoice("unknown", choices[i], false, "chosen voice");
                }
                if (GUI.Button(new Rect(320, 58, 90, 24), "Back")) choosingUnknownVoice = false;
            }
            else
            {
                if (GUI.Button(new Rect(20, 92, 120, 30), "Male")) ApplyUnknownChoice("male", null, false, "male voice pool");
                if (GUI.Button(new Rect(145, 92, 120, 30), "Female")) ApplyUnknownChoice("female", null, false, "female voice pool");
                if (GUI.Button(new Rect(270, 92, 140, 30), "Random")) ApplyUnknownChoice("unknown", null, false, "random compatible voice");
                if (GUI.Button(new Rect(20, 130, 190, 30), "Choose Voice...")) choosingUnknownVoice = true;
                if (GUI.Button(new Rect(216, 130, 194, 30), "Use Default")) ApplyUnknownChoice("unknown", voice.Value, false, "default voice");
                if (GUI.Button(new Rect(20, 168, 390, 30), "Ignore This Speaker")) ApplyUnknownChoice("unknown", voice.Value, true, "ignored by user");
            }
            GUI.DragWindow(new Rect(0, 0, 440, 25));
        }

        private static void AddUniqueVoices(List<string> destination, string[] source)
        {
            if (source == null) return;
            foreach (string item in source)
                if (!string.IsNullOrEmpty(item) && !destination.Contains(item)) destination.Add(item);
        }

        private bool IsConnected()
        {
            lock (connectionLock)
            {
                return client != null && client.Connected && writer != null;
            }
        }

        private void StartBackendIfNeeded()
        {
            if (backendShutdownStarted != 0 || backendStartAttempted || IsConnected()) return;
            backendStartAttempted = true;
            if (CanOpenBackendPort(150))
            {
                backendState = "Connecting to existing backend";
                DebugLog("Backend already owns " + host.Value + ":" + port.Value + "; not starting another process.");
                return;
            }

            try
            {
                string root = GetBackendRoot();
                string python = Path.Combine(root, "runtime", "python.exe");
                string server = Path.Combine(root, "backend", "tts_server.py");
                if (!File.Exists(python) || !File.Exists(server))
                {
                    backendState = "Backend files missing";
                    Logger.LogError("Auto-start could not find the packaged backend under " + root);
                    return;
                }

                string engine = (preferredTtsEngine.Value ?? "kokoro").Trim().ToLowerInvariant();
                if (engine != "mock" && !IsInstalledEngine(engine))
                {
                    Logger.LogWarning("Unsupported PreferredTtsEngine '" + engine + "'; using kokoro.");
                    engine = "kokoro";
                }

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = python;
                info.Arguments = QuoteArgument(server) + " a " + QuoteArgument(voice.Value) + " 1.0 --port " + port.Value.ToString(CultureInfo.InvariantCulture);
                info.WorkingDirectory = root;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.EnvironmentVariables["PYTHONNOUSERSITE"] = "1";
                info.EnvironmentVariables["PYTHONUTF8"] = "1";
                info.EnvironmentVariables["HF_HOME"] = GetModelCacheRoot(root);
                info.EnvironmentVariables["HF_HUB_OFFLINE"] = "1";
                info.EnvironmentVariables["TRANSFORMERS_OFFLINE"] = "1";
                info.EnvironmentVariables["HF_HUB_DISABLE_TELEMETRY"] = "1";
                info.EnvironmentVariables["TTS_ROOT"] = root;
                info.EnvironmentVariables["OBENVOICE_ROOT"] = GetObenVoiceRoot();
                info.EnvironmentVariables["OBENVOICE_LEGACY_ROOT"] = GetLegacyRoot();
                info.EnvironmentVariables["TTS_LOG_DIR"] = GetLogRoot();
                info.EnvironmentVariables["TTS_CACHE_DIR"] = Path.Combine(root, "cache");
                info.EnvironmentVariables["TTS_PORT"] = port.Value.ToString(CultureInfo.InvariantCulture);
                info.EnvironmentVariables["TTS_ENGINE"] = engine;
                info.EnvironmentVariables["TTS_VOICE_PACK"] = selectedEngineVoicePack.Value ?? string.Empty;
                info.EnvironmentVariables["TTS_DEBUG"] = debugLogging.Value ? "1" : "0";

                backendProcess = Process.Start(info);
                startedBackend = backendProcess != null;
                launchedTtsEngine = engine;
                runningEngineConfirmed = false;
                runningTtsEngine = startedBackend ? "Starting " + engine : "Not running";
                backendState = "Backend starting / loading model";
                Logger.LogInfo("Started packaged local TTS backend (PID " + (backendProcess == null ? "unknown" : backendProcess.Id.ToString(CultureInfo.InvariantCulture)) + ").");
                DebugLog("Backend auto-start path=" + server + " port=" + port.Value + " engine=" + engine);
            }
            catch (Exception ex)
            {
                backendState = "Backend start failed";
                backendStartAttempted = false;
                Logger.LogError("Could not auto-start the local TTS backend: " + ex.Message);
            }
        }

        private bool CanOpenBackendPort(int timeoutMilliseconds)
        {
            TcpClient probe = null;
            try
            {
                probe = new TcpClient();
                IAsyncResult result = probe.BeginConnect(host.Value, port.Value, null, null);
                if (!result.AsyncWaitHandle.WaitOne(timeoutMilliseconds)) return false;
                probe.EndConnect(result);
                return true;
            }
            catch { return false; }
            finally { if (probe != null) try { probe.Close(); } catch { } }
        }

        private void BeginConnect()
        {
            if (backendShutdownStarted != 0 || IsConnected() || Interlocked.Exchange(ref connectInProgress, 1) != 0) return;
            backendState = startedBackend ? "Loading model / connecting" : "Connecting";
            ThreadPool.QueueUserWorkItem(delegate
            {
                TcpClient pending = null;
                try
                {
                    pending = new TcpClient();
                    IAsyncResult result = pending.BeginConnect(host.Value, port.Value, null, null);
                    if (!result.AsyncWaitHandle.WaitOne(1000))
                    {
                        pending.Close();
                        return;
                    }
                    pending.EndConnect(result);
                    pending.NoDelay = true;
                    NetworkStream stream = pending.GetStream();
                    StreamWriter pendingWriter = new StreamWriter(stream, new UTF8Encoding(false));
                    pendingWriter.AutoFlush = true;
                    pendingWriter.WriteLine("SET_VOICE:" + voice.Value);
                    pendingWriter.WriteLine("SET_DEBUG:" + (debugLogging.Value ? "1" : "0"));

                    lock (connectionLock)
                    {
                        if (client != null && client.Connected)
                        {
                            pending.Close();
                            return;
                        }
                        client = pending;
                        writer = pendingWriter;
                        pending = null;
                    }

                    readerThread = new Thread(ReadBackendEvents);
                    readerThread.IsBackground = true;
                    readerThread.Start(stream);
                    backendState = "Connected";
                    runningTtsEngine = string.IsNullOrEmpty(launchedTtsEngine) ? "External / unknown" : launchedTtsEngine;
                    Logger.LogInfo("Connected to the local TTS backend.");
                }
                catch (Exception ex)
                {
                    if (pending != null) try { pending.Close(); } catch { }
                    DebugLog("Backend connection attempt failed: " + ex.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref connectInProgress, 0);
                }
            });
        }

        private void PollBackendStatus()
        {
            if (IsConnected())
            {
                backendState = "Connected";
                if (!runningEngineConfirmed) TryUpdateRunningEngineFromStatus();
                return;
            }

            if (startedBackend && backendProcess != null)
            {
                try
                {
                    if (backendProcess.HasExited)
                    {
                        backendState = "Backend stopped; retrying";
                        Process stoppedProcess = backendProcess;
                        backendProcess = null;
                        startedBackend = false;
                        backendStartAttempted = false;
                        launchedTtsEngine = string.Empty;
                        runningEngineConfirmed = false;
                        runningTtsEngine = "Not running";
                        try { stoppedProcess.Dispose(); } catch { }
                        return;
                    }
                }
                catch { }
            }

            string statusPath = Path.Combine(GetBackendRoot(), "tts_ready.txt");
            try
            {
                if (!File.Exists(statusPath)) return;
                string status = File.ReadAllText(statusPath, Encoding.UTF8).Trim();
                if (status.StartsWith("READY|", StringComparison.Ordinal))
                {
                    backendState = "Backend ready / connecting";
                    string[] parts = status.Split('|');
                    if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                    {
                        runningTtsEngine = parts[2];
                        runningEngineConfirmed = true;
                    }
                }
                else if (status.StartsWith("LOADING", StringComparison.Ordinal)) backendState = "Loading model";
                else if (status.StartsWith("STARTING", StringComparison.Ordinal)) backendState = "Backend starting";
                else if (status.StartsWith("ERROR|", StringComparison.Ordinal)) backendState = status.Substring(6);
            }
            catch { }
        }

        private void TryUpdateRunningEngineFromStatus()
        {
            string statusPath = Path.Combine(GetBackendRoot(), "tts_ready.txt");
            try
            {
                if (!File.Exists(statusPath)) return;
                string[] parts = File.ReadAllText(statusPath, Encoding.UTF8).Trim().Split('|');
                if (parts.Length >= 3 && string.Equals(parts[0], "READY", StringComparison.Ordinal) && !string.IsNullOrEmpty(parts[2]))
                {
                    runningTtsEngine = parts[2];
                    runningEngineConfirmed = true;
                }
            }
            catch { }
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private void ReadBackendEvents(object state)
        {
            try
            {
                StreamReader reader = new StreamReader((Stream)state, new UTF8Encoding(false));
                while (reader.ReadLine() != null) { }
            }
            catch { }
            finally
            {
                DebugLog("Socket disconnected; reconnect will be attempted.");
                backendState = "Disconnected / reconnecting";
                Disconnect();
            }
        }

        private bool SendLine(string text, SpeechProfile selectedProfile, bool interruptCurrent = true)
        {
            if (!IsConnected())
            {
                DebugLog("Speech skipped because backend is not connected; reconnect requested. Text=" + text);
                BeginConnect();
                return false;
            }
            lock (connectionLock)
            {
                try
                {
                    if (interruptCurrent) writer.WriteLine("STOP");
                    if (!string.IsNullOrEmpty(selectedProfile.Voice)) writer.WriteLine("SET_VOICE:" + selectedProfile.Voice);
                    writer.WriteLine("SET_SPEED:" + selectedProfile.Speed.ToString("0.000", CultureInfo.InvariantCulture));
                    writer.WriteLine("SET_PITCH:" + selectedProfile.Pitch.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine(text);
                    writer.Flush();
                    Logger.LogInfo("[TTS " + selectedProfile.Voice + " speed=" + selectedProfile.Speed.ToString("0.000", CultureInfo.InvariantCulture) + " pitch=" + selectedProfile.Pitch.ToString("0.00", CultureInfo.InvariantCulture) + "] " + text);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("TTS send failed: " + ex.Message);
                    CloseConnectionNoLock();
                    return false;
                }
            }
        }

        private void Disconnect()
        {
            lock (connectionLock)
            {
                CloseConnectionNoLock();
            }
        }

        private void CloseConnectionNoLock()
        {
            writer = null;
            if (client != null)
            {
                try { client.Close(); } catch { }
                client = null;
            }
        }

        private void OnApplicationQuit()
        {
            ShutdownOwnedBackend();
        }

        internal void HandleGameQuitRequest(string source)
        {
            Logger.LogInfo("Game quit intercepted at " + source + "; shutting down the owned TTS backend before Obenseuer terminates its process.");
            ShutdownOwnedBackend();
        }

        private void RestartVoiceEngine()
        {
            if (Interlocked.Exchange(ref restartInProgress, 1) != 0) return;
            string selected = (preferredTtsEngine.Value ?? string.Empty).Trim().ToLowerInvariant();
            if (!IsInstalledEngine(selected))
            {
                backendState = "Cannot restart: selected engine is unavailable";
                Interlocked.Exchange(ref restartInProgress, 0);
                return;
            }

            backendState = "Restarting voice engine";
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    lock (backendLifecycleLock)
                    {
                        if (backendShutdownStarted != 0) return;
                        bool portBusy = CanOpenBackendPort(200);
                        if (portBusy && !HasLiveOwnedBackendProcess())
                        {
                            backendState = "Restart blocked: backend on this port is not owned by the mod";
                            Logger.LogWarning("Voice engine restart refused because the process using the configured backend port is not owned by this plugin.");
                            return;
                        }

                        StopOwnedBackendProcess(false);
                        string statusPath = Path.Combine(GetBackendRoot(), "tts_ready.txt");
                        try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }
                        backendStartAttempted = false;
                        nextReconnectAt = 0f;
                        nextStatusPollAt = 0f;
                        StartBackendIfNeeded();
                        if (!startedBackend)
                        {
                            Logger.LogWarning("Voice engine restart could not start an owned backend process.");
                            return;
                        }
                        BeginConnect();
                        backendState = "Restarting / loading " + selected;
                        Logger.LogInfo("Voice engine restart started with selected engine '" + selected + "'.");
                    }
                }
                catch (Exception ex)
                {
                    backendState = "Voice engine restart failed";
                    Logger.LogError("Voice engine restart failed: " + ex.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref restartInProgress, 0);
                }
            });
        }

        private bool HasLiveOwnedBackendProcess()
        {
            if (!startedBackend || backendProcess == null) return false;
            try { return !backendProcess.HasExited; }
            catch { return false; }
        }

        private void ShutdownOwnedBackend()
        {
            if (Interlocked.Exchange(ref backendShutdownStarted, 1) != 0) return;
            lock (backendLifecycleLock) StopOwnedBackendProcess(true);
        }

        private void StopOwnedBackendProcess(bool respectExitPreference)
        {
            Process ownedProcess = backendProcess;
            if (!startedBackend || ownedProcess == null || (respectExitPreference && !closeBackendOnGameExit.Value))
            {
                Disconnect();
                return;
            }

            try
            {
                if (!ownedProcess.HasExited)
                {
                    lock (connectionLock)
                    {
                        if (writer != null)
                        {
                            try
                            {
                                writer.WriteLine("STOP");
                                writer.WriteLine("SHUTDOWN");
                                writer.Flush();
                            }
                            catch (Exception ex)
                            {
                                DebugLog("Graceful backend shutdown request failed: " + ex.Message);
                            }
                        }
                    }

                    if (!ownedProcess.WaitForExit(5000))
                    {
                        ownedProcess.Kill();
                        ownedProcess.WaitForExit(1500);
                        Logger.LogInfo("Forced the unresponsive TTS backend process started by this plugin to close.");
                    }
                    else Logger.LogInfo("The TTS backend process started by this plugin shut down cleanly.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not close the plugin-started backend: " + ex.Message);
            }
            finally
            {
                Disconnect();
                try { ownedProcess.Dispose(); } catch { }
                backendProcess = null;
                startedBackend = false;
                backendStartAttempted = false;
                launchedTtsEngine = string.Empty;
                runningEngineConfirmed = false;
                runningTtsEngine = "Not running";
            }
        }

        private void OnDestroy()
        {
            SetSettingsWindowVisible(false);
            SetPlayerVoiceWindowVisible(false);
            ShutdownOwnedBackend();
            Instance = null;
        }
    }

    [HarmonyPatch(typeof(ThirdPersonCameraController), "LateUpdate")]
    internal static class ThirdPersonCameraLateUpdatePatch
    {
        private static int prefixLogged;
        private static int suppressionLogged;

        private static void Prefix(ThirdPersonCameraController __instance, ref bool __state)
        {
            __state = __instance.menuEnabled;
            if (Interlocked.Exchange(ref prefixLogged, 1) == 0)
                ObenseuerTtsPlugin.LogCameraDiagnostic("LateUpdate prefix executed; original menuEnabled=" + __state);
            if (!ObenseuerTtsPlugin.ShouldSuppressCameraLook()) return;
            __instance.menuEnabled = true;
            if (Interlocked.Exchange(ref suppressionLogged, 1) == 0)
                ObenseuerTtsPlugin.LogCameraDiagnostic("TTS menu open; temporarily forcing menuEnabled during LateUpdate");
        }

        private static Exception Finalizer(ThirdPersonCameraController __instance, bool __state, Exception __exception)
        {
            __instance.menuEnabled = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(PauseMenu), "QuitDesktop")]
    internal static class PauseMenuQuitDesktopPatch
    {
        private static void Prefix()
        {
            if (ObenseuerTtsPlugin.Instance != null)
                ObenseuerTtsPlugin.Instance.HandleGameQuitRequest("PauseMenu.QuitDesktop");
        }
    }

    [HarmonyPatch(typeof(MainMenu), "QuitOK")]
    internal static class MainMenuQuitOkPatch
    {
        private static void Prefix()
        {
            if (ObenseuerTtsPlugin.Instance != null)
                ObenseuerTtsPlugin.Instance.HandleGameQuitRequest("MainMenu.QuitOK");
        }
    }

    [HarmonyPatch(typeof(StandardUISubtitlePanel), "ShowSubtitle")]
    internal static class ConversationSubtitlePatch
    {
        private static void Postfix(Subtitle subtitle)
        {
            if (ObenseuerTtsPlugin.Instance != null) ObenseuerTtsPlugin.Instance.HandleSubtitle(subtitle, false);
        }
    }

    [HarmonyPatch(typeof(SubtitleBarkUI), "Bark")]
    internal static class BarkSubtitlePatch
    {
        private static void Postfix(Subtitle subtitle)
        {
            if (ObenseuerTtsPlugin.Instance != null) ObenseuerTtsPlugin.Instance.HandleSubtitle(subtitle, true);
        }
    }

    [HarmonyPatch(typeof(ConversationView), "SelectResponse")]
    internal static class ConfirmedPlayerResponsePatch
    {
        private static void Postfix(SelectedResponseEventArgs e)
        {
            if (ObenseuerTtsPlugin.Instance != null) ObenseuerTtsPlugin.Instance.HandleSelectedResponse(e);
        }
    }

    [HarmonyPatch(typeof(VoiceTypewriterEffect), "PlayCharacterAudio")]
    internal static class OriginalVoiceAudioPatch
    {
        private static bool Prefix()
        {
            return ObenseuerTtsPlugin.Instance == null || !ObenseuerTtsPlugin.Instance.ShouldMuteOriginalVoiceAudio();
        }
    }
}
