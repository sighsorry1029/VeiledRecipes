[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $manifestFile,

    [Parameter(Mandatory = $true)]
    [string] $versionString
)

$ErrorActionPreference = "Stop"
$resolvedManifestPath = (Resolve-Path -LiteralPath $manifestFile).Path
$manifestText = Get-Content -LiteralPath $resolvedManifestPath -Raw
$manifest = $manifestText | ConvertFrom-Json
if ($manifest.PSObject.Properties.Name -notcontains "version_number") {
    throw "manifest.json does not contain version_number."
}

if ($versionString -notmatch '^\d+\.\d+\.\d+$') {
    throw "Invalid package version '$versionString'. Expected major.minor.patch."
}

$versionPattern = '("version_number"\s*:\s*")[^"]*(")'
$versionMatches = [regex]::Matches($manifestText, $versionPattern)
if ($versionMatches.Count -ne 1) {
    throw "Expected exactly one version_number property in manifest.json."
}

if ([string]$manifest.version_number -eq $versionString) {
    return
}

$updatedText = [regex]::Replace(
    $manifestText,
    $versionPattern,
    { param ($match) $match.Groups[1].Value + $versionString + $match.Groups[2].Value })
$firstNewline = [regex]::Match($manifestText, "`r`n|`n|`r")
$lineEnding = if ($firstNewline.Success) { $firstNewline.Value } else { [Environment]::NewLine }
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($resolvedManifestPath, $updatedText.TrimEnd([char[]]"`r`n") + $lineEnding, $utf8WithoutBom)

$updatedManifest = Get-Content -LiteralPath $resolvedManifestPath -Raw | ConvertFrom-Json
if ($updatedManifest.version_number -ne $versionString) {
    throw "Failed to update manifest version to $versionString."
}
