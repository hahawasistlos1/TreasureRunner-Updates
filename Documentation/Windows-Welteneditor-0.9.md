# TreasureRunner – Windows-Welteneditor ab 0.9.0

Das öffentliche ZIP vollständig entpacken und **TreasureRunner-Levelbau.exe** starten. Der Unterordner Runtime wird benötigt; Unity muss nicht installiert sein. Diese Ausgabe enthält ausschließlich den Welteneditor, keine Verwaltungsoberfläche. Auch `--level-admin` schaltet keine Verwaltung frei.

## Bauen und einreichen

Alle Kategorien und Bauteile sind unabhängig von der gewählten Grundkulisse verfügbar. Bauteil wählen, im Raster tippen/ziehen, speichern oder über einen Code bzw. GitHub-Einreichungslink importieren. Acht Bauplätze und vorhandene Entwürfe bleiben erhalten.

**Testspielen ist freiwillig.** Ein gültiges Level kann ohne vorheriges Durchspielen über **Einreichen** an den Betreiber geschickt werden. Das GitHub-Formular bewusst absenden. Einreichen ist keine öffentliche Freigabe. Der Betreiber entscheidet, welche Level in der Community-Auswahl erscheinen. Ungültige Leveldaten, beispielsweise ein fehlendes Ziel, werden weiterhin abgelehnt. Private Tests und direktes Teilen per Code bleiben möglich.

## Nur beim Betreiber: Verwaltung 0.9.0

Die lokale **TreasureRunner-Verwaltung.exe** liegt separat unter `Builds/Private-0.9.0/Verwaltung`, gehört nicht zum öffentlichen Download und ist über die Verwaltungs-Verknüpfung auf dem Besitzer-PC erreichbar.

„Einreichungen laden“ zeigt gültige offene GitHub-Einreichungen. Auswahl, Kartenansicht und freiwilliger 3D-Prüflauf sind verfügbar. **Für alle freigeben** benötigt keinen abgeschlossenen Test. Vor dem Schreiben werden Einreichung und Katalog erneut gelesen; zwischenzeitliche Änderungen verlangen eine erneute Entscheidung. „Freigegebene Level“ erlaubt weiterhin gezieltes Zurücknehmen. Lokale Kopien und ursprüngliche Einreichungen werden dadurch nicht gelöscht.

Für Schreibzugriff benötigt der Besitzer ein eigenes Fine-grained-GitHub-Token, begrenzt auf **hahawasistlos1/TreasureRunner-Updates**, **Contents: Read and write**. Das Programm prüft zusätzlich, dass das Token zum Betreiber **hahawasistlos1** gehört. Zugangsdaten bleiben nur für die Sitzung im Arbeitsspeicher. Kein Token ist im Spiel, im öffentlichen Code oder im Download enthalten. GitHubs Repository-Berechtigungen bleiben die eigentliche Schreibzugriffsgrenze.

## Versionsbindung

0.9.0 liest und verwaltet ausschließlich `community/0.9.0/catalog.json` mit `moderationProtocol: 4` und `gameVersion: "0.9.0"`. Alte Verwaltungsprogramme kennen diesen Pfad nicht; ihre Freigaben in `community/catalog*.json` werden von 0.9.0 nicht geladen. Die neue Verwaltung verwaltet ihrerseits keine alten Kataloge. Wer Schreibrechte auf das gesamte GitHub-Repository besitzt, kann Dateien außerhalb eines Programms weiterhin manuell ändern. Die alten Programme und ihre Zugangsrechte werden durch diese Trennung nicht vom Besitzer-PC entfernt.

Level-Codes und eigene Bauplätze bleiben unabhängig davon importierbar. Bei späteren Spielversionen wird der Pfad automatisch aus deren vollständiger Versionsnummer gebildet. Keine automatische APK-Erstellung beim Freigeben eines Levels.

Die öffentliche Git-Quelle ist ein Auszug der eigenen Spielquellen, kein vollständig startfertiges Unity-Projekt mit fremden Asset-Store-Modellen. Credits und erforderliche Paketquellen liegen im Repository und im Download unter Lizenzen.
