$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$credentialSource=Get-Content -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Tools/ModeratorCredential.cs') -Raw
# Compile the production marshalling and storage implementation with a unique disposable
# target. Never read, replace or delete the user's real TreasureRunner credential.
$testTarget='TreasureRunner/Test/'+[Guid]::NewGuid().ToString('N')
$credentialSource=$credentialSource.Replace('const string Target="TreasureRunner/CommunityModeration";',('const string Target="'+$testTarget+'";'))
$credentialSource="#define TREASURERUNNER_ADMIN`n#define UNITY_STANDALONE_WIN`n"+$credentialSource+@'

namespace TreasureRunner { public static class DesktopTools { public static bool Smoke { get { return false; } } } }
'@
Add-Type -TypeDefinition $credentialSource
$credentialChecks=[System.Collections.Generic.List[string]]::new()
try {
 if([TreasureRunner.ModeratorCredential]::Load() -ne ''){throw 'Isolated test target unexpectedly occupied'}
 if(-not [TreasureRunner.ModeratorCredential]::Store('treasurerunner-local-test-no-github-permission')){throw 'Windows credential write failed'}
 if([TreasureRunner.ModeratorCredential]::Load() -ne 'treasurerunner-local-test-no-github-permission'){throw 'Stored credential roundtrip failed'}
 $credentialChecks.Add('PASS: Production Windows credential marshalling and protected storage roundtrip')
 if(-not [TreasureRunner.ModeratorCredential]::Store('')){throw 'Windows test credential removal failed'}
 if([TreasureRunner.ModeratorCredential]::Load() -ne ''){throw 'Removed credential still present'}
 $credentialChecks.Add('PASS: Removal prevents later credential reload')
 $credentialChecks.Add('PASS: Unique test target only; no real GitHub token read or changed')
} finally {
 if(-not [TreasureRunner.ModeratorCredential]::Store('')){throw 'Disposable credential cleanup failed'}
}
$credentialChecks.Add('ALL WINDOWS CREDENTIAL CHECKS PASSED')
$credentialChecks | Set-Content -LiteralPath (Join-Path $projectRoot 'Logs/LiveEditor/CredentialChecks.txt') -Encoding utf8
$credentialChecks
