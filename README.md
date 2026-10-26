# TreasureRunner

Android-Jump-and-Run mit acht Welten, 80 Kampagnenleveln, acht Waffen, Figurenfähigkeiten und einem gemeinsamen Welteneditor.

## Version 0.9.0

- [Android-APK](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.9.0/TreasureRunner.apk)
- [Windows-Welteneditor](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.9.0/TreasureRunner-Windows-Editor.zip)
- [Änderungen und Release](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/tag/v0.9.0)
- [Eigener Spielcode direkt im Git-Repository](Assets/Scripts)
- [Anleitung zum Windows-Editor](Documentation/Windows-Welteneditor-0.9.md)

Das öffentliche Windows-Programm enthält ausschließlich den Welteneditor. Testspielen ist freiwillig; **Einreichen** schickt ein Level zur Entscheidung an den Betreiber. Nur hahawasistlos1 gibt Level öffentlich frei. Die separate Verwaltung wird nicht als Download verteilt. Android und die aktuelle Verwaltung nutzen ausschließlich `community/0.9.0/catalog.json` mit Protokoll 4 und passender Spielversion. Alte Verwaltungsprogramme können die neue Community-Auswahl über ihre alten Kataloge nicht ändern. Vorhandene eigene Codes/Bauplätze bleiben importierbar.

Größere und selbst skalierbare Touch-Tasten, zusätzliche Waffen und Fähigkeiten, korrigierte bewegliche Plattformen, automatische Datenpatches ohne Rücknahme und Hintergrund-Updatebenachrichtigungen sind enthalten. APK-Installation bleibt manuell; Android steuert die Zeitpunkte der Hintergrundprüfungen. Kein neuer Spielstandsreset. Die APK wurde auf einem A55 aktualisiert; Google-Profil und Wiederherstellung nach App-Neustart sind geprüft. Eigene Leveldateien blieben unverändert. Kein vollständiger Durchspieltest aller 80 Level.

Google-Anmeldung mit selbst gewähltem Nickname ist für Android eingerichtet und auf dem A55 geprüft. Das Spielprofil zeigt keine Google-E-Mail, keinen Google-Profilnamen und kein Profilbild. Firebase läuft im kostenlosen Spark-Tarif, die Nickname-Datenbank liegt in Europa (eur3). Freunde/Spielstände werden nicht automatisch online synchronisiert. [Einrichtung und Datenfluss](Documentation/Google-Anmeldung-Nickname.md).

## Quellen als Git-Dateien

Eigener C#-/Java-/Shader-Code, Editoren, Build-/Prüfwerkzeuge, eigene Menügrafiken und Schriften mit Lizenzen stehen direkt in der Git-Historie. Kein zusätzlicher Source-ZIP-Anhang. Dies ist ein Quellcode-Auszug, **kein vollständig startfertiges Unity-Projekt**: fremde Originalmodelle, ursprüngliche Szenengrundlage und Unity-Pakete müssen rechtmäßig ergänzt werden. Originalmodelle aus dem Asset Store, private Signierschlüssel, Tokens, Spielstände und Build-Caches werden nicht veröffentlicht. Für den eigenen Code wird keine zusätzliche Open-Source-Lizenz eingeräumt. [Credits und Asset-Quellen](Documentation/Credits.md).

Entwicklungsumgebung: Unity 6000.3.17f1 / URP, Android ARM64 ab API 26 und Windows x64. Aktuelle Build-Einstiege: `TreasureRunnerRelease09.Android`, `.WindowsEditor`, `.WindowsAdmin`. `.WindowsAdmin` ist eine eigene lokale Ausgabe und kein Teil des öffentlichen Windows-Pakets. Die Repository-Schreibrechte schützen die Freigaben auch dann, wenn jemand den öffentlich einsehbaren Verwaltungscode selbst kompiliert. Builds/Versionserhöhungen/Veröffentlichungen nur auf ausdrücklichen Besitzerauftrag.

## Einladungen und Updates

Die [Einladungsseite](https://hahawasistlos1.github.io/treasurerunner/invite/) öffnet unterstützte Apps oder bietet den offiziellen APK-Download. Nach Installation dieselbe Einladung erneut öffnen. Kleine Datenpatches verändern nur unterstützte Daten; neuer Code und neue Assets benötigen eine APK. Derzeit wird mit 0.9.0 kein zusätzlicher Datenpatch veröffentlicht.
