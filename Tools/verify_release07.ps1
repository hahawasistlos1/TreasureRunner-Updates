$ErrorActionPreference = 'Stop'
$gameRoot = Split-Path $PSScriptRoot -Parent
$expectedApk = Get-Content "$gameRoot/Logs/ArtifactVerification07.json" -Raw | ConvertFrom-Json
$expectedSource = Get-Content "$gameRoot/Logs/PublicSourceVerification07.json" -Raw | ConvertFrom-Json
$expectedWindows = Get-Content "$gameRoot/Logs/WindowsToolsVerification.json" -Raw | ConvertFrom-Json
$publicRelease = Invoke-RestMethod -Uri ('https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates/releases/latest?check=' + [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()) -Headers @{'User-Agent'='TreasureRunner-Release-Verification'}
if ($publicRelease.tag_name -ne 'v0.7.0' -or $publicRelease.draft -or $publicRelease.prerelease) { throw '0.7.0 ist noch nicht das öffentliche Latest-Release.' }
$verifiedFiles = @()
foreach($expected in @(
    @{name='TreasureRunner.apk';data=$expectedApk},
    @{name='TreasureRunner-Source-0.7.0.zip';data=$expectedSource},
    @{name='TreasureRunner-Windows-Werkzeuge.zip';data=$expectedWindows}
)) {
    $asset = $publicRelease.assets | Where-Object name -eq $expected.name
    if (-not $asset -or $asset.state -ne 'uploaded' -or $asset.size -ne $expected.data.bytes -or $asset.digest -ne ('sha256:' + $expected.data.sha256)) { throw ('Öffentliche Datei stimmt nicht überein: ' + $expected.name) }
    $verifiedFiles += @{name=$asset.name;bytes=$asset.size;sha256=$asset.digest;url=$asset.browser_download_url;matches=$true}
}
$publicReport = [ordered]@{version=$publicRelease.tag_name;url=$publicRelease.html_url;published_at=$publicRelease.published_at;draft=$publicRelease.draft;prerelease=$publicRelease.prerelease;assets=$verifiedFiles;gameChecks=535;heartChecks=33;windowsEditorChecks=15;windowsAdminChecks=9;deviceTest=$false}
$publicReport | ConvertTo-Json -Depth 8 | Set-Content "$gameRoot/Logs/GitHubReleaseVerification07.json" -Encoding utf8NoBOM
$publicReport | ConvertTo-Json -Depth 8
