using System;

namespace ObenseuerTts
{
    internal sealed class SpeakerAnnouncementState
    {
        private string previousConversationSpeakerKey = string.Empty;

        internal void Reset()
        {
            previousConversationSpeakerKey = string.Empty;
        }

        internal void OnConversationStateChanged(bool conversationActive)
        {
            // Obenseuer can expose the first subtitle before isConversationActive
            // changes to true. Preserve that first speaker on the late start edge;
            // the preceding conversation-end edge already resets genuine sessions.
            if (!conversationActive) Reset();
        }

        internal bool ShouldAnnounce(bool enabled, bool conversationLine, string speakerKey)
        {
            if (!conversationLine)
                return enabled && !string.IsNullOrEmpty(speakerKey);

            if (!enabled)
            {
                Reset();
                return false;
            }

            if (string.IsNullOrEmpty(speakerKey)) return false;

            bool announce = !string.Equals(previousConversationSpeakerKey, speakerKey, StringComparison.OrdinalIgnoreCase);
            previousConversationSpeakerKey = speakerKey;
            return announce;
        }
    }
}
