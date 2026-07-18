[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$EmbyReferencePath,
    [switch]$SkipSelfTest,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-SingleMatchValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $content = Get-Content -LiteralPath $Path -Raw
    $match = [regex]::Match($content, $Pattern)
    if (-not $match.Success) {
        throw "Failed to read $Label from $Path."
    }

    return $match.Groups[1].Value.Trim()
}

function Get-ReleaseLabel {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VersionText
    )

    try {
        $version = [Version]$VersionText
        if ($version.Build -ge 0) {
            return "{0}.{1}.{2}" -f $version.Major, $version.Minor, $version.Build
        }
    }
    catch {
    }

    return $VersionText
}

function Require-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Write-Host ""
    Write-Host ("> dotnet " + ($Arguments -join " ")) -ForegroundColor Cyan

    Push-Location $WorkingDirectory
    try {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet command failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

function Get-ChangelogExcerpt {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$VersionText
    )

    $lines = Get-Content -LiteralPath $Path
    $headingPattern = '^##\s+' + [regex]::Escape($VersionText) + '\b'
    $startIndex = -1

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $headingPattern) {
            $startIndex = $i
            break
        }
    }

    if ($startIndex -lt 0) {
        return "_No changelog section found for $VersionText._"
    }

    $buffer = New-Object System.Collections.Generic.List[string]
    for ($i = $startIndex + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s+') {
            break
        }

        $buffer.Add($lines[$i])
    }

    $text = ($buffer -join [Environment]::NewLine).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return "_Changelog section is empty for $VersionText._"
    }

    return $text
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

$solutionPath = Join-Path $repoRoot "EmbyFeishu.sln"
$projectPath = Join-Path $repoRoot "src\EmbyFeishu\EmbyFeishu.csproj"
$selfTestProjectPath = Join-Path $repoRoot "tools\EmbyFeishu.SelfTest\EmbyFeishu.SelfTest.csproj"
$pluginSourcePath = Join-Path $repoRoot "src\EmbyFeishu\bin\$Configuration\EmbyFeishu.dll"
$propsPath = Join-Path $repoRoot "Directory.Build.props"
$pluginCodePath = Join-Path $repoRoot "src\EmbyFeishu\Plugin.cs"
$changelogPath = Join-Path $repoRoot "CHANGELOG.md"
$releaseRoot = Join-Path $repoRoot "release"

$versionText = Get-SingleMatchValue -Path $propsPath -Pattern '<EmbyFeishuVersion>([^<]+)</EmbyFeishuVersion>' -Label "EmbyFeishuVersion"
$pluginGuid = Get-SingleMatchValue -Path $pluginCodePath -Pattern 'PluginGuid\s*=\s*new Guid\("([^"]+)"\)' -Label "plugin guid"
$releaseLabel = Get-ReleaseLabel -VersionText $versionText
$packageName = "EmbyFeishu-v$releaseLabel"
$releaseDir = Join-Path $releaseRoot $packageName
$zipPath = Join-Path $releaseRoot ($packageName + ".zip")

if ([string]::IsNullOrWhiteSpace($EmbyReferencePath)) {
    $EmbyReferencePath = Join-Path $repoRoot "lib\emby\4.9.5.0"
}

if (-not (Test-Path -LiteralPath $EmbyReferencePath -PathType Container)) {
    throw "Emby reference path not found: $EmbyReferencePath"
}

$resolvedEmbyReferencePath = (Resolve-Path -LiteralPath $EmbyReferencePath).Path
$requiredEmbyDlls = @(
    "MediaBrowser.Common.dll",
    "MediaBrowser.Controller.dll",
    "MediaBrowser.Model.dll",
    "Emby.Web.GenericEdit.dll"
)

foreach ($dllName in $requiredEmbyDlls) {
    Require-File -Path (Join-Path $resolvedEmbyReferencePath $dllName)
}

if ((Test-Path -LiteralPath $releaseDir) -or (Test-Path -LiteralPath $zipPath)) {
    if (-not $Force) {
        throw "Release output already exists. Re-run with -Force to overwrite: $releaseDir"
    }

    if (Test-Path -LiteralPath $releaseDir) {
        Remove-Item -LiteralPath $releaseDir -Recurse -Force
    }

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
}

$msbuildArgs = @("-p:EmbyReferencePath=$resolvedEmbyReferencePath")

$restoreArgs = @("restore", $solutionPath) + $msbuildArgs
$buildArgs = @("build", $solutionPath, "-c", $Configuration, "--no-restore") + $msbuildArgs

Invoke-DotNet -WorkingDirectory $repoRoot -Arguments $restoreArgs
Invoke-DotNet -WorkingDirectory $repoRoot -Arguments $buildArgs

if (-not $SkipSelfTest) {
    $selfTestArgs = @("run", "--project", $selfTestProjectPath, "-c", $Configuration, "--no-build")
    Invoke-DotNet -WorkingDirectory $repoRoot -Arguments $selfTestArgs
}

Require-File -Path $pluginSourcePath

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

$pluginTargetPath = Join-Path $releaseDir "EmbyFeishu.dll"
$hashFilePath = Join-Path $releaseDir "SHA256SUMS.txt"
$releaseNotesPath = Join-Path $releaseDir "RELEASE-NOTES.md"

Copy-Item -LiteralPath $pluginSourcePath -Destination $pluginTargetPath -Force

$hash = (Get-FileHash -LiteralPath $pluginTargetPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $hashFilePath -Value ($hash + " *EmbyFeishu.dll") -Encoding ascii

$verificationLines = @(
    "- dotnet restore EmbyFeishu.sln",
    "- dotnet build EmbyFeishu.sln -c $Configuration --no-restore"
)

if ($SkipSelfTest) {
    $verificationLines += "- SelfTest skipped via -SkipSelfTest."
}
else {
    $verificationLines += "- dotnet run --project tools/EmbyFeishu.SelfTest/EmbyFeishu.SelfTest.csproj -c $Configuration --no-build"
}

$releaseNotes = @"
# EmbyFeishu v$versionText Release Notes

- Target: Emby Server 4.9.5.0 / netstandard2.0
- Release Date: $(Get-Date -Format 'yyyy-MM-dd')
- Plugin GUID: $pluginGuid

## Artifacts

| File | Description |
| --- | --- |
| EmbyFeishu.dll | Plugin binary. Deployment only needs this file. |
| SHA256SUMS.txt | SHA-256 checksum for EmbyFeishu.dll. |
| RELEASE-NOTES.md | This release note file. |
| $packageName.zip | Zipped release directory for upload or backup. |

- AssemblyVersion / FileVersion / InformationalVersion: $versionText
- SHA-256: $hash
- Excludes .deps.json, .pdb, Emby server DLLs, and test tools.

## Changelog Excerpt

$(Get-ChangelogExcerpt -Path $changelogPath -VersionText $versionText)

## Verification

$($verificationLines -join [Environment]::NewLine)

## Upgrade

1. Stop Emby Server.
2. Replace the old plugin with EmbyFeishu.dll from this directory.
3. Start Emby Server again.
4. Existing configuration remains in place.

## Integrity Check

    Get-FileHash EmbyFeishu.dll -Algorithm SHA256
"@

Set-Content -LiteralPath $releaseNotesPath -Value $releaseNotes -Encoding utf8
Compress-Archive -Path $releaseDir -DestinationPath $zipPath -CompressionLevel Optimal -Force

Write-Host ""
Write-Host "Release completed successfully." -ForegroundColor Green
Write-Host "Version      : $versionText"
Write-Host "Output dir   : $releaseDir"
Write-Host "Archive      : $zipPath"
Write-Host "Plugin SHA256: $hash"
