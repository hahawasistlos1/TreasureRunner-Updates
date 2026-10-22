# TreasureRunner für Android

[Neueste Version herunterladen](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/latest)

## Version 0.5.0 – 80 Level und Levelbaukasten

Die Kampagne umfasst 80 Level in acht bunten Comic-Welten. Zehn Leveltypen verändern den Ablauf: Entdecken, Aufstieg, Festung, Brücken, Geheimwege, Tunnel, Zerfall, Aufzüge, Belagerung und Finale. Dazu gehören längere Laufstrecken, obere Schatzwege, bewegliche Plattformen, Sprungfedern, Schatzblöcke und Schaltertore. Springer und Stürmer ergänzen die bisherigen Gegner und Schützen. Fünf weitere Shop-Upgrades verbessern Münzmagnet, Fernkampfgeschwindigkeit, Schatzbonus, Wiederbelebungen und den Schutz nach Treffern.

Alte Level-IDs und gekaufte Ausrüstung behalten ihre Bedeutung. Die Zusatzkapitel werden nach dem bisherigen Wächterlevel der jeweiligen Welt freigeschaltet.

## Eigene Level bauen und teilen

Im Spiel: **Level-Editor** öffnen, Bauteil auswählen, tippen oder ziehen, Start und Ziel setzen, testspielen und speichern. Es gibt acht lokale Bauplätze, 14 Bauteilarten, zwölf Höhenstufen und bis zu 96 Spalten.

**Teilen** erzeugt einen Level-Code. Die andere Person wählt **Community → Level-Code importieren** und fügt ihn ein. Unter Android kann der Code über die Systemauswahl verschickt werden.

**Freigeben** öffnet eine vorbereitete GitHub-Einreichung. Nach Prüfung und Aufnahme durch den Betreiber erscheint das Level in der Community-Auswahl, ohne neue APK. Für die Einreichung wird ein GitHub-Konto benötigt; direktes Teilen per Code benötigt keines. Vier Beispiellevel werden auch offline mitgeliefert. Eigene Level vergeben keine Kampagnenmünzen und schalten keine Kampagnenkapitel frei.

[Anleitung und Freigabeablauf](community/README.md) · [Community-Katalog](community/catalog.json)

## Installation und Update-Meldung

Android 8 oder neuer, ARM64. Bei der ersten Installation `TreasureRunner.apk` aus den Release-Anhängen laden. Das geöffnete Spiel prüft beim Start, nach Rückkehr in die App und regelmäßig im Menü auf neue Versionen. Ein Popup zeigt Version, Änderungen und den automatischen Download mit Fortschrittsbalken. Der Menüpunkt Updates erhält eine NEU-Markierung. Eine Meldung bei vollständig geschlossener App ist nicht vorgesehen.

Dateigröße und SHA-256 werden vor der Installation geprüft. Eine bereits vollständig geprüfte APK wird wiederverwendet. Die Installation muss in Android manuell bestätigt werden. Nach erfolgreichem Update wird die heruntergeladene Update-APK automatisch entfernt. Das Spiel zum Aktualisieren nicht deinstallieren, damit der lokale Spielstand erhalten bleibt.

## Prüfstand und Grenzen

Offline-Alpha. Der automatisierte Prüfstand und die Grenzen der jeweiligen Version stehen in ihren Release-Hinweisen. Nicht alle 80 Level sind vollständig von Hand durchgespielt oder abschließend ausbalanciert. 0.5.0 wird auf Nutzerwunsch ohne Smartphone-Test erstellt. Online-Freunde, Online-Ranglisten und Koop sind weiterhin nicht verbunden. Die Community-Auswahl verteilt Leveldaten; sie ist kein Mehrspielerserver.

## Eigener Spielcode

Der Release-Anhang `TreasureRunner-Source-0.5.0.zip` enthält den freigegebenen eigenen C#-/Java-/Shader-Code, Editorwerkzeuge und Tests. Es handelt sich um einen Quellcode-Auszug, kein vollständiges startfertiges Unity-Projekt. Originalassets anderer Anbieter, Zugangsdaten, Spielstände und Signierschlüssel sind nicht enthalten. Eine zusätzliche Open-Source-Lizenz wurde nicht erteilt.

## Frühere Versionen

0.4.0 führte die Comic-Darstellung ein. 0.3.0 ergänzte unter anderem Fernkampf, Fallen und den GitHub-Updater. Gerätetests und der echte Installationsablauf auf dem Samsung A55 wurden für 0.3.0 bestätigt; diese historischen Tests ersetzen keinen Gerätetest der neuen Version.
