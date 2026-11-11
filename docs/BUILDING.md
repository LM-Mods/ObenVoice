# Building from source

## Requirements

- Windows
- .NET SDK capable of targeting .NET Framework 4.7.2
- Local Obenseuer installation
- ObenseuerCore 0.12.1 and Harmony build references

Place `ObenseuerCore.dll` and `0Harmony.dll` in `dependencies` as described there. Game and Unity assemblies are read from the local Obenseuer installation and are not redistributable project dependencies.

## Build

From the repository root:

```powershell
dotnet build src\plugin\ObenseuerDialogueTTS.csproj -c Release
```

For a non-default Steam library:

```powershell
dotnet build src\plugin\ObenseuerDialogueTTS.csproj -c Release -p:GameDir="D:\Games\Steam\steamapps\common\Obenseuer"
```

Output is written beneath `artifacts\build` and remains excluded from Git.

Release builds are deterministic, omit PDB/debug-directory paths and map the
source root to a public virtual path. To statically compile against the
BepInEx 5 framework assembly without replacing local dependencies:

```powershell
dotnet build src\plugin\ObenseuerDialogueTTS.csproj -c Release `
  -p:ObenseuerCorePath="dependencies\ObenseuerCore.BepInEx5.dll"
```

The assembly name, plugin ID and runtime paths intentionally retain their legacy internal names for compatibility. Building does not package the Python runtime or model files.

## Focused tests

The C# test projects are standalone executables under `tests`. Python tests use the standard Python installation and the source backend; mock socket lifecycle tests require permission to open localhost ports.

Historical test records are version-specific and do not validate a newly built
DLL. Any new release DLL must complete the relevant focused checklist before
it is described as gameplay-tested. The current published build and hash are
recorded in [RELEASE-ASSETS-v1.0.2-beta.1.md](RELEASE-ASSETS-v1.0.2-beta.1.md);
[RELEASE-TESTING.md](RELEASE-TESTING.md) is retained as older release evidence.
