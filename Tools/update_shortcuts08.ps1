$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$newToolRoot=Join-Path $projectRoot 'Builds/Publish-0.8.0/Windows-Werkzeuge'
$desktopToolLinks=@(
 @{link='C:/Users/Jir3n/Desktop/TreasureRunner – Levelbau.lnk';exe='TreasureRunner-Levelbau.exe'},
 @{link='C:/Users/Jir3n/Desktop/TreasureRunner – Verwaltung.lnk';exe='TreasureRunner-Verwaltung.exe'}
)
$shortcutShell=New-Object -ComObject WScript.Shell
$oldDesktopLinks=@()
foreach($item in $desktopToolLinks){
 $shortcut=$shortcutShell.CreateShortcut($item.link)
 $oldDesktopLinks+=@{path=$item.link;target=$shortcut.TargetPath;workingDirectory=$shortcut.WorkingDirectory;icon=$shortcut.IconLocation;arguments=$shortcut.Arguments}
}
$oldDesktopLinks | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectRoot 'Backups/Before08/Desktop-shortcuts.json')
foreach($item in $desktopToolLinks){
 $newTarget=Join-Path $newToolRoot $item.exe
 if(-not (Test-Path -LiteralPath $newTarget)){throw 'Geprüftes neues Programm fehlt.'}
 $shortcut=$shortcutShell.CreateShortcut($item.link);$shortcut.TargetPath=$newTarget;$shortcut.WorkingDirectory=$newToolRoot;$shortcut.IconLocation=$newTarget+',0';$shortcut.Arguments='';$shortcut.Save()
 $readBack=$shortcutShell.CreateShortcut($item.link)
 if($readBack.TargetPath -ne $newTarget){throw 'Verknüpfungsprüfung fehlgeschlagen.'}
 Write-Output ($item.link+' -> '+$readBack.TargetPath)
}
