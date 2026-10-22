# Level bauen und teilen – Version 0.5.0

Im Hauptmenü öffnet **Level-Editor** den Baukasten. Ein Baufeld zeigt 16 Spalten; mit den Abschnitt-Tasten erreicht man die weiteren Teile der Strecke. Anfangs sind 64, maximal 96 Spalten verfügbar, bei zwölf Höhenstufen. Tippen oder Ziehen setzt Bauteile. Der Radierer entfernt sie, Rückgängig nimmt Änderungen zurück. Über Welt wechseln wird eine der acht Comic-Landschaften gewählt.

Es gibt Boden, Münzen, Nahkampfgegner, Schützen, Springer, Stürmer, Bruchboden, Aufzug, Checkpoint, Ziel, Flamme, Kanone, Fallstein und Sprungfeder. Gegner, Fallen, Checkpoint und Ziel enthalten ein Bodenfeld. Münzen können in der Luft liegen. Start setzen legt den Einstieg fest. Genau ein Zielportal ist nötig. Eine einfache Erreichbarkeitsprüfung weist getrennte Ziele zurück; sie ersetzt kein eigenes Testspielen und prüft weder alle Hindernisse noch das Timing.

**Speichern** schreibt in einen von acht lokalen Bauplätzen. **Bauplatz** lädt einen anderen Entwurf oder beginnt dort ein neues Level. Vor einem Platzwechsel Änderungen speichern. Testspielen startet eine Kopie; der aktuelle Entwurf bleibt während derselben Spielsitzung erhalten. Nach einem App-Neustart muss der Entwurf zuvor gespeichert worden sein.

## Direkt teilen

1. Level fertig bauen und testspielen.
2. Teilen wählen. Der mit `TRL1:` beginnende Level-Code wird kopiert. Unter Android öffnet sich außerdem die Systemauswahl zum Teilen.
3. Die andere Person wählt Community → Level-Code importieren, fügt den vollständigen Code ein und importiert ihn.
4. Im Editor kann sie ihn testspielen und in einem freien Bauplatz speichern.

Level enthalten ausschließlich erlaubte Spielbauteile. Es werden keine Programme, eigenen Assets oder Skripte von Spielern geladen. Größenlimits gelten vor und nach dem Entpacken. In eigenen und Community-Leveln werden Spielpunkte und Levelmünzen getrennt gezählt; Kampagnenguthaben, Erfolge und Freischaltungen werden dadurch nicht erhöht.

## Für alle freigeben

**Freigeben** erklärt die Veröffentlichung und öffnet danach ein vorbereitetes GitHub-Issue im Repository TreasureRunner-Updates. Titel und Autorenname werden dabei öffentlich. Ein GitHub-Konto ist für diese Einreichung notwendig. Das Spiel enthält keine Zugangsdaten und veröffentlicht nicht eigenmächtig unter dem Konto eines Spielers. Ist der Code für die URL zu lang, wird er in die Zwischenablage kopiert und muss in das Formular eingefügt werden.

Die Einreichung ist noch keine Freigabe. Der Betreiber importiert und testet das Level, prüft Namen und Inhalt und nimmt es anschließend in `community/catalog.json` auf dem Zweig `TennoFarm` auf. Nach Öffnen der Community-Seite oder Katalog aktualisieren erscheint das Level im Spiel, ohne neue APK. Maximal 30 freigegebene Level und 256 KiB Kataloggröße sind für diese erste Version vorgesehen. Vier Beispiellevel werden zusätzlich mitgeliefert und funktionieren offline. Direkter Code-Austausch benötigt keine Freigabe.

Im Veröffentlichungspaket liegt `community/approve_level.py` als lokales Hilfsprogramm für den Betreiber. Es validiert einen gespeicherten Code und ergänzt oder aktualisiert einen Katalogeintrag. Vor der Veröffentlichung weiter selbst testspielen; die Prüfungen sind keine automatische Qualitätsfreigabe. Das Programm wird nicht vom Spiel ausgeführt.

## Kampagne und Upgrades

80 Level: zehn pro Welt, darunter Lauf- und Entdeckerstrecken, Aufstiege, Festungen, Brücken, Geheimwege, Tunnel, Zerfall, Aufzüge, Belagerungen und Finalen. Die alten 24 Level-IDs behalten ihre Welt und Kapitelzuordnung, damit bestehende Bestwerte nicht in eine andere Welt verschoben werden. Zusatzkapitel werden nach dem bisherigen Wächterlevel der jeweiligen Welt und anschließend nacheinander freigeschaltet.

Zusätzlich zu den bisherigen fünf Verbesserungen gibt es Münzmagnet, Schnellfeuer für Bogen/Stab, Schatzbonus, Rettungsrucksack und Schutzamulett. Alle Käufe verwenden ausschließlich erspielte Münzen.

## Updates

Die Update-Meldung ist eine Benachrichtigung **im geöffneten Spiel**. Beim Start, nach Rückkehr in die App und regelmäßig im Menü wird GitHub geprüft. Ein neues Release zeigt ein Popup mit Versionsnummer, Änderungen und Downloadfortschritt. Der Menüpunkt Updates erhält eine NEU-Markierung. Eine geprüfte, bereits vollständig heruntergeladene APK wird wiederverwendet. Installation bleibt eine manuelle Android-Bestätigung. Eine Benachrichtigung bei vollständig geschlossener App ist nicht implementiert.

0.5.0 wird auf Nutzerwunsch ohne Handytest vorbereitet. Automatische Editor-Prüfungen und gerenderte Ansichten werden im Prüfprotokoll dokumentiert; sie ersetzen keinen vollständigen Durchlauf aller Level und keinen Gerätetest.
