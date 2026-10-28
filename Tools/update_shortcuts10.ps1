$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$desktopToolLinks=@(
 @{link='C:/Users/Jir3n/Desktop/TreasureRunner – Levelbau.lnk';folder='Builds/Publish-0.10.0/Windows-Editor';exe='TreasureRunner-Levelbau.exe'},
 @{link='C:/Users/Jir3n/Desktop/TreasureRunner – Verwaltung.lnk';folder='Builds/Private-0.10.0/Verwaltung';exe='TreasureRunner-Verwaltung.exe'}
)
$shortcutShell=New-Object -ComObject WScript.Shell
$backupRoot=Join-Path $projectRoot 'Backups/Before10'
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
$backupFile=Join-Path $backupRoot 'Desktop-shortcuts.json'
if(-not (Test-Path -LiteralPath $backupFile)){
 $oldDesktopLinks=@()
 foreach($item in $desktopToolLinks){
  $shortcut=$shortcutShell.CreateShortcut($item.link)
  $oldDesktopLinks+=@{path=$item.link;target=$shortcut.TargetPath;workingDirectory=$shortcut.WorkingDirectory;icon=$shortcut.IconLocation;arguments=$shortcut.Arguments}
  if(Test-Path -LiteralPath $item.link){Copy-Item -LiteralPath $item.link -Destination $backupRoot}
 }
 $oldDesktopLinks | ConvertTo-Json | Set-Content -LiteralPath $backupFile
}
foreach($item in $desktopToolLinks){
 $newToolRoot=Join-Path $projectRoot $item.folder
 $newTarget=Join-Path $newToolRoot $item.exe
 if(-not (Test-Path -LiteralPath $newTarget)){throw 'Geprüftes neues Programm fehlt.'}
 $shortcut=$shortcutShell.CreateShortcut($item.link)
 $shortcut.TargetPath=$newTarget;$shortcut.WorkingDirectory=$newToolRoot;$shortcut.IconLocation=$newTarget+',0';$shortcut.Arguments='';$shortcut.Save()
 $readBack=$shortcutShell.CreateShortcut($item.link)
 if($readBack.TargetPath -ne $newTarget){throw 'Verknüpfungsprüfung fehlgeschlagen.'}
 Write-Output ($item.link+' -> '+$readBack.TargetPath)
}
