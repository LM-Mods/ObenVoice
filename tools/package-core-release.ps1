[CmdletBinding()]
param(
    [switch]$ValidateOnly,
    [switch]$LicenseApproved,
    [switch]$BepInEx5GameplayPassed,
    [switch]$BepInEx6GameplayPassed,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$releaseName = 'ObenVoice-v1.0.0-beta.1-Core.zip'
$expectedDllSha256 = '59A0B0F5BE6BCD9C55029CCE84E279038B1DFFAE0C950B161D38AA781C5AF265'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$stagingRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'release-staging'))
$coreRoot = [IO.Path]::GetFullPath((Join-Path $stagingRoot 'core-rc'))
$outputRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'dist'))
$zipPath = Join-Path $outputRoot $releaseName
$checksumPath = $zipPath + '.sha256'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-RelativePath {
    param([string]$Root, [string]$Path)
    return $Path.Substring($Root.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')
}

function Test-FileContainsMarker {
    param([string]$Path, [string[]]$Markers)

    $stream = [IO.File]::OpenRead($Path)
    try {
        $buffer = New-Object byte[] (1024 * 1024)
        $tail = ''
        $maxMarker = ($Markers | ForEach-Object Length | Measure-Object -Maximum).Maximum
        while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
            $chunk = $tail + [Text.Encoding]::UTF8.GetString($buffer, 0, $read)
            foreach ($marker in $Markers) {
                if ($chunk.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -ge 0) { return $true }
            }
            $tailLength = [Math]::Min([int]$maxMarker, $chunk.Length)
            $tail = $chunk.Substring($chunk.Length - $tailLength)
        }
        return $false
    }
    finally { $stream.Dispose() }
}

function Get-StreamSha256 {
    param([IO.Stream]$Stream)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '') }
    finally { $sha.Dispose() }
}

Assert-True (Test-Path -LiteralPath $coreRoot -PathType Container) "Core candidate is missing: $coreRoot"
$stagePrefix = $stagingRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
Assert-True ($coreRoot.StartsWith($stagePrefix, [StringComparison]::OrdinalIgnoreCase)) 'Core candidate escaped release-staging.'

$licenseText = Get-Content -LiteralPath (Join-Path $repoRoot 'LICENSE') -Raw
Assert-True ($licenseText.Contains('Copyright (c) 2026 LM-Mods')) 'The LM-Mods MIT notice is missing.'

if (-not $ValidateOnly) {
    Assert-True $LicenseApproved 'Packaging stopped: pass -LicenseApproved only after explicit user approval of the LM-Mods MIT notice.'
    Assert-True $BepInEx5GameplayPassed 'Packaging stopped: pass -BepInEx5GameplayPassed only after the sanitised DLL passes the focused BepInEx 5 gameplay test.'
    Assert-True $BepInEx6GameplayPassed 'Packaging stopped: pass -BepInEx6GameplayPassed only after the sanitised DLL passes the focused BepInEx 6 gameplay test.'
}

$required = @(
    'BepInEx/plugins/ObenseuerDialogueTTS/ObenseuerDialogueTTS.dll',
    'BepInEx/plugins/ObenseuerDialogueTTS/characters.json',
    'BepInEx/plugins/ObenseuerDialogueTTS/CHARACTER-DATA-LICENSE.md',
    'ObenseuerTTS/backend/tts_server.py',
    'ObenseuerTTS/backend/tts_engines.py',
    'ObenseuerTTS/backend/text_cleaner.py',
    'ObenseuerTTS/backend/tts_protocol.py',
    'ObenseuerTTS/runtime/python.exe',
    'ObenseuerTTS/model-cache/hub/models--hexgrad--Kokoro-82M/snapshots/f3ff3571791e39611d31c381e3a41a3af07b4987/kokoro-v1_0.pth',
    'LICENSE',
    'THIRD-PARTY-NOTICES.md',
    'LICENSING-AUDIT.md',
    'RUNTIME_DEPENDENCIES.md',
    'ESPEAK_NG_REDISTRIBUTION.md',
    'licenses/eSpeak-NG-1.52.0-GPL-3.0.txt',
    'licenses/Phonemizer-Fork-3.3.2-GPL-3.0-or-later.txt',
    'licenses/Loguru-0.7.3-MIT.txt',
    'licenses/Tokenizers-0.23.3-NOTICE.md'
)
$voices = 'af_heart', 'af_bella', 'af_sarah', 'am_adam', 'am_michael', 'am_onyx'
foreach ($voice in $voices) {
    $required += "ObenseuerTTS/model-cache/hub/models--hexgrad--Kokoro-82M/snapshots/f3ff3571791e39611d31c381e3a41a3af07b4987/voices/$voice.pt"
}
foreach ($relative in $required) {
    Assert-True (Test-Path -LiteralPath (Join-Path $coreRoot $relative) -PathType Leaf) "Required release file is missing: $relative"
}

$dllPath = Join-Path $coreRoot 'BepInEx/plugins/ObenseuerDialogueTTS/ObenseuerDialogueTTS.dll'
$dllHash = (Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash
Assert-True ($dllHash -eq $expectedDllSha256) "Sanitised DLL hash mismatch. Expected $expectedDllSha256, got $dllHash."

$files = @(Get-ChildItem -LiteralPath $coreRoot -Recurse -File -Force)
$prohibitedNames = @('speaker_assignments.json', 'tts_ready.txt', '.env', '.DS_Store')
$prohibitedExtensions = @('.pyc', '.pdb', '.log', '.tmp', '.bak', '.user', '.suo')
$prohibited = @($files | Where-Object {
    $relative = Get-RelativePath $coreRoot $_.FullName
    $_.Name -in $prohibitedNames -or $_.Extension -in $prohibitedExtensions -or
    $relative -match '(^|/)(__pycache__|BepInEx/config|\.git)(/|$)'
})
Assert-True ($prohibited.Count -eq 0) ('Prohibited release files found: ' + (($prohibited | ForEach-Object FullName) -join ', '))

$allowedExecutables = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@(
    'ObenseuerTTS/runtime/python.exe', 'ObenseuerTTS/runtime/pythonw.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/cli.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/cli-32.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/cli-64.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/cli-arm64.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/gui.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/gui-32.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/gui-64.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/setuptools/gui-arm64.exe',
    'ObenseuerTTS/runtime/Lib/site-packages/torch/bin/protoc.exe'
) | ForEach-Object { [void]$allowedExecutables.Add($_) }
$unexpectedExecutables = @($files | Where-Object Extension -eq '.exe' | Where-Object {
    -not $allowedExecutables.Contains((Get-RelativePath $coreRoot $_.FullName))
})
Assert-True ($unexpectedExecutables.Count -eq 0) ('Unexpected executables found: ' + (($unexpectedExecutables | ForEach-Object FullName) -join ', '))

$pocket = @($files | Where-Object {
    $relative = Get-RelativePath $coreRoot $_.FullName
    $relative -match '^ObenseuerTTS/engine-addons/pocket(/|$)' -or
    $_.Name -in @('bill_boerst.safetensors', 'model.safetensors', 'tokenizer.model')
})
Assert-True ($pocket.Count -eq 0) 'Pocket material is present in Core; Pocket is not approved for release.'

$markers = @($repoRoot, 'Documents\ChatGPT')
if (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) { $markers += $env:USERPROFILE }
foreach ($file in $files) {
    if (Test-FileContainsMarker -Path $file.FullName -Markers $markers) {
        throw "Private local path marker found in: $(Get-RelativePath $coreRoot $file.FullName)"
    }
}

Write-Host "Validated $($files.Count) files. DLL SHA-256: $dllHash"
if ($ValidateOnly) {
    Write-Host 'Validation-only completed. No ZIP or checksum was created.'
    if (-not ($LicenseApproved -and $BepInEx5GameplayPassed -and $BepInEx6GameplayPassed)) {
        Write-Warning 'Final packaging remains gated by explicit licence approval and both gameplay confirmations.'
    }
    exit 0
}

if ((Test-Path -LiteralPath $zipPath) -or (Test-Path -LiteralPath $checksumPath)) {
    Assert-True $Force 'Release output already exists. Review it or rerun with -Force to replace only the dist outputs.'
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    if (Test-Path -LiteralPath $checksumPath) { Remove-Item -LiteralPath $checksumPath -Force }
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($coreRoot, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)

$sourceByPath = @{}
foreach ($file in $files) { $sourceByPath[(Get-RelativePath $coreRoot $file.FullName)] = $file }
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | Where-Object { -not [string]::IsNullOrEmpty($_.Name) })
    Assert-True ($entries.Count -eq $files.Count) "ZIP entry count mismatch: expected $($files.Count), got $($entries.Count)."
    foreach ($entry in $entries) {
        $name = $entry.FullName.Replace('\', '/')
        Assert-True (-not ($name.StartsWith('/') -or $name.Contains('../'))) "Unsafe ZIP entry: $name"
        Assert-True ($sourceByPath.ContainsKey($name)) "Unexpected ZIP entry: $name"
        $source = $sourceByPath[$name]
        Assert-True ($entry.Length -eq $source.Length) "ZIP length mismatch: $name"
        $sourceStream = [IO.File]::OpenRead($source.FullName)
        $entryStream = $entry.Open()
        try {
            Assert-True ((Get-StreamSha256 $sourceStream) -eq (Get-StreamSha256 $entryStream)) "ZIP integrity mismatch: $name"
        }
        finally { $sourceStream.Dispose(); $entryStream.Dispose() }
    }
}
finally { $archive.Dispose() }

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
[IO.File]::WriteAllText($checksumPath, "$zipHash  $releaseName`r`n", [Text.UTF8Encoding]::new($false))
Write-Host "Created: $zipPath"
Write-Host "SHA-256: $zipHash"
Write-Host "Checksum: $checksumPath"
