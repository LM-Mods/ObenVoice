# Local build references

This directory is intentionally excluded from Git except for this file.

To build the plugin, place these locally obtained references here:

- `ObenseuerCore.dll` from Obenseuer Modding Framework 0.12.1
- `0Harmony.dll` from the installed supported BepInEx distribution

Obenseuer and Unity assemblies are referenced directly from the local game installation. Do not commit or redistribute game assemblies through this repository.

The optional `ObenseuerCore.BepInEx5.dll` may be kept locally for API comparison, but the plugin project does not compile against it.
