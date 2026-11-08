"""Small, game-focused dialogue text normaliser.

Independently implemented for Obenseuer Dialogue TTS. This file is original
project code and is not derived from Universal TTS Mod.
"""

import html
import re
import unicodedata


_RICH_TEXT = re.compile(r"<[^>]{1,200}>")
_CONTROL = re.compile(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]")
_SPACE = re.compile(r"\s+")
_UNITY_ESCAPE = re.compile(r"\\[nrt]")
_SPRITE_TAG = re.compile(r"\[(?:sprite|icon|color|size|font)(?:=[^\]]*)?\]", re.I)
_CLOSING_TAG = re.compile(r"\[/(?:color|size|font)\]", re.I)


class TextCleaner:
    """Convert subtitle text into safe, natural plain text for speech."""

    @staticmethod
    def clean(text: object) -> str:
        if text is None:
            return ""
        value = unicodedata.normalize("NFKC", str(text))
        value = html.unescape(value)
        value = _UNITY_ESCAPE.sub(" ", value)
        value = _RICH_TEXT.sub(" ", value)
        value = _SPRITE_TAG.sub(" ", value)
        value = _CLOSING_TAG.sub(" ", value)
        value = _CONTROL.sub(" ", value)
        value = value.replace("…", "...").replace("—", " - ").replace("–", " - ")
        value = value.replace("“", '"').replace("”", '"').replace("’", "'")
        return _SPACE.sub(" ", value).strip()

