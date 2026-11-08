"""Minimal ObenVoice engine add-on API v1 example."""


class SampleEngine:
    engine_id = "sample"
    available_voices = []

    def __init__(self, lang, addon_root, tts_root):
        self.lang = lang
        self.addon_root = addon_root
        self.tts_root = tts_root

    def speak(self, text, voice=None, speed=1.0, pitch_semitones=0.0):
        del text, voice, speed, pitch_semitones

    def stop(self):
        pass

    def close(self):
        self.stop()


def create_engine(lang, addon_root, tts_root):
    return SampleEngine(lang, addon_root, tts_root)
