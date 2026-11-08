using System;
using ObenseuerTts;

internal static class SpeakerAnnouncementStateTests
{
    private static void Expect(bool actual, bool expected, string label)
    {
        if (actual != expected) throw new Exception(label + ": expected " + expected + ", got " + actual);
    }

    private static void Main()
    {
        SpeakerAnnouncementState state = new SpeakerAnnouncementState();

        Expect(state.ShouldAnnounce(true, true, "A"), true, "first A line");
        state.OnConversationStateChanged(true);
        Expect(state.ShouldAnnounce(true, true, "A"), false, "late conversation-start edge preserves first speaker");
        Expect(state.ShouldAnnounce(true, true, "A"), false, "second consecutive A line");
        Expect(state.ShouldAnnounce(true, true, "B"), true, "speaker change to B");
        Expect(state.ShouldAnnounce(true, true, "A"), true, "return to A");
        state.OnConversationStateChanged(false);
        state.OnConversationStateChanged(true);
        Expect(state.ShouldAnnounce(true, true, "A"), true, "new conversation A line");
        Expect(state.ShouldAnnounce(false, true, "A"), false, "option off");
        Expect(state.ShouldAnnounce(true, true, "A"), true, "option re-enabled");
        Expect(state.ShouldAnnounce(true, false, "A"), true, "bark behavior preserved");
        Expect(state.ShouldAnnounce(true, false, "A"), true, "consecutive bark behavior preserved");

        Console.WriteLine("speaker announcement state tests passed");
    }
}
