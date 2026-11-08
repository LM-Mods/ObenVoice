"""Line protocol used by Obenseuer Dialogue TTS.

Independently implemented for Obenseuer Dialogue TTS. This file is original
project code and is not derived from Universal TTS Mod.
"""

from dataclasses import dataclass
from typing import Optional


STOP = "STOP"
SHUTDOWN = "SHUTDOWN"
SET_VOICE = "SET_VOICE"
SET_SPEED = "SET_SPEED"
SET_PITCH = "SET_PITCH"
SET_DEBUG = "SET_DEBUG"
SET_LANG = "SET_LANG"


@dataclass(frozen=True)
class Message:
    kind: str
    value: Optional[str] = None


def parse_message(raw_line: str) -> Optional[Message]:
    """Parse one UTF-8, newline-delimited message from the C# plug-in."""
    line = raw_line.rstrip("\r\n")
    if not line:
        return None
    if line in (STOP, SHUTDOWN):
        return Message(line)
    for command in (SET_VOICE, SET_SPEED, SET_PITCH, SET_DEBUG, SET_LANG):
        prefix = command + ":"
        if line.startswith(prefix):
            return Message(command, line[len(prefix) :].strip())
    return Message("SPEAK", line)

