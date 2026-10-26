# Waffen, Figurenfähigkeiten und Kaufverlauf über 80 Level

Stand 26.10.2026: lokaler Entwicklungsstand nach Nutzerauftrag. Keine APK erstellt, keine Versionsnummer erhöht und nichts veröffentlicht. Die installierte/veröffentlichte 0.8.0 enthält diese Ergänzungen noch nicht. Frühere lokale Ergänzungen für Tastengröße und Hintergrundmeldungen bleiben erhalten.

## Figuren und Ausrüstung

Die vier Figuren bleiben kostenlos wählbar. Herzen, Sprung, Bewegung und Spieler-Hitbox bleiben identisch. Die bisherigen vier Waffen können alle Figuren verwenden. Zusätzlich besitzt jede Figur eine kaufbare Spezialwaffe und eine eigene Fähigkeit mit drei Kaufstufen:

| Figur | Spezialwaffe | Freischaltung | Automatische Fähigkeit |
| --- | --- | --- | --- |
| Goblin | Schatzsucherspeer, 900 Münzen | 12 verschiedene abgeschlossene Level | Schatzsinn: zusätzliche Münzanziehung aus 0,5 / 1 / 1,5 m, ergänzt den Magneten |
| Skelett-Ritter | Knochenhammer, 1.400 Münzen | 28 Level | Knochenwache: einen Treffer abfangen, danach 24 / 20 / 16 s Ladezeit; kein Schutz vor Abstürzen |
| Skelett-Späher | Späherarmbrust, 2.100 Münzen | 44 Level | Fokus: Bogen-/Bolzentreffer verlangsamt Gegner für 1 / 1,5 / 2 s; 12 / 10 / 8 s Ladezeit |
| Skelett-Magier | Kristallfroststab, 2.800 Münzen | 68 Level | Runenecho: ein Magietreffer erhält +1 Schaden; 12 / 10 / 8 s Ladezeit |

Die Fähigkeiten wirken automatisch, sobald sie gekauft sind. Damit ist keine zusätzliche Touch-Taste nötig. Das HUD nennt die ausgewählte Fähigkeit, Kaufstufe und Bereitschaft/Ladezeit. Aktivierungen zeigen einen Effekt und eine Meldung. Der normale Schutzschild-Pickup wird vor der Knochenwache verbraucht, wodurch eine gleichzeitig bereite Klassenfähigkeit erhalten bleibt. Normale Treffer kosten weiterhin ein halbes Herz.

Die Armbrust verschießt sichtbare, schnellere Bolzen (16 statt 12 m/s). Der Froststab verschießt sichtbare Frostkugeln mit geringerem Grundschaden und 0,7 s Verlangsamung. Verlangsamung reduziert das tatsächliche Gegnertempo auf 65 %, stapelt nicht und endet mit Rückkehr zum vorherigen Tempo. Speer und Hammer besitzen eigene Modelle und echte Kontaktmarker; Nahkampftreffer werden weiterhin ausschließlich entlang der sichtbaren animierten Waffe ausgewertet. Der Hammer durchbricht Panzerung. Runen- und Froststab sind magisch und können Magieschilde treffen, aber keine magieresistenten Gegner umgehen. Neue Fernkampfwaffen erhalten keine allgemeinen Nahkampf-Schadensboni.

Der Waffenwechsel enthält nur die vier gemeinsamen Waffen und die gekaufte Spezialwaffe der aktiven Figur. Beim Figurenwechsel mit einer fremden Spezialwaffe wird das Schwert ausgerüstet; gekaufte Waffen und deren Verbesserungen bleiben im Inventar. Inventar und Shop nennen die Zuordnung.

## Freischaltungen und Wirtschaft

Gezählt werden ausschließlich verschiedene gültige Kampagnen-IDs mit mindestens einem Abschluss. Wiederholungen, doppelte Speicherzeilen, eine hohe alte Freischalt-ID und Community-/Editor-Testabschlüsse erhöhen den Zähler nicht. Das berücksichtigt die bestehende nichtlineare Welt- und ID-Anordnung.

- Fähigkeit: Kaufstufen nach 4 / 36 / 72 verschiedenen Abschlüssen; Preise je Figur 180 / 600 / 1.500 Münzen.
- Waffenverbesserungen: Stufe 1 nach mindestens 8 Abschlüssen und der Spezialwaffen-Freischaltung, Stufe 2 nach mindestens 40 Abschlüssen und der Spezialwaffen-Freischaltung, Stufe 3 nach 72 Abschlüssen.
- Die fünf allgemeinen Fünfstufen-Upgrades: nächste Stufe nach 0 / 14 / 33 / 53 / 72 Abschlüssen.
- Abenteuer-Upgrades verteilen ihre jeweilige Maximalstufe ebenfalls bis Abschluss 72 (Fünfstufen-Items wie oben; vier Stufen 0 / 14 / 43 / 72; drei Stufen 0 / 14 / 72).

Alle bestehenden Preise und Erstabschlussbelohnungen bleiben erhalten. Neue Waffenverbesserungen kosten pro Rang den Grundpreis mal 1 / 2 / 4: Speer 350, Hammer 450, Armbrust 550, Froststab 650 Münzen als Grundpreis. Shop und Charakterseite zeigen Rang, Preis, benötigte Abschlüsse und Kaufstatus. Auch direkte Serviceaufrufe prüfen die Freischaltung vor dem Abbuchen.

Automatisierte Wirtschaftssimulation mit echten Kauf- und Abschlussdiensten: die ersten 72 unterschiedlichen IDs geben zusammen **71.100 Münzen allein durch einmalige Abschlussboni**. Alle alten und neuen Waffen, sämtliche Waffenränge, alle vier Fähigkeiten auf Stufe 3 sowie alle allgemeinen/Abenteuer-Upgrades kosten zusammen **64.842 Münzen**. Es verbleiben **6.258 Münzen**. Eingesammelte Münzen, Gegnerbeute, Schatzbonus und Wiederholungen sind dabei nicht eingerechnet. Die 72 niedrigsten IDs sind zugleich die konservativste beliebige Menge von 72 Abschlussboni, da die vorhandene Belohnung mit der ID steigt. Die letzten Kaufstufen bleiben bis Abschluss 72 gesperrt. Danach bleiben acht Level zum Nutzen der vollständigen Ausrüstung.

Das ist eine Dienst-/Wirtschaftssimulation, kein menschlicher Komplettdurchlauf aller 80 Level. Spielgefühl und reale Kaufentscheidungen müssen später zusätzlich am Gerät erprobt werden.

## Speicherstand, Baukasten und Datenpatches

Kein neuer Kampagnenreset. Die vier vorhandenen Waffenränge behalten ihre Indizes 0–3. Vier neue Indizes 4–7 werden angehängt, und `characterSkills` ergänzt vier eigene Ränge. Alte kleinere Arrays werden beim Laden/Kauf erweitert, ohne die bisherigen Einträge abzuschneiden. Fehlende Fähigkeitsfelder bedeuten Stufe 0. Bereits gekaufte Waffenränge, Fähigkeiten, Münzen und Inventar werden durch neue Freischaltbedingungen nicht rückwirkend entzogen. Eigene Leveldateien werden nicht bearbeitet.

Alle acht Waffen erscheinen automatisch in der gemeinsamen Editorpalette unter Beute, mit echten Bauteilvorschauen. In Editor-/Community-Levels können alle Figuren alle Waffen testen, unabhängig vom Kampagnenkauf. Gekaufte Klassenfähigkeiten sind dort deaktiviert, damit der getestete Level nicht vom bezahlten Kampagnenstand abhängt. Die neue Waffenmechanik (z. B. Frostkugeln) bleibt vorhanden.

Die Datenpatch-Validierung kennt jetzt alle acht IDs und behält die gleichen Mengen-, Werte- und Prüfsummengrenzen bei. Es wurde kein öffentlicher Datenpatch angelegt. Modelle und Effekte verwenden eigene Geometrie und die vorhandenen korrekt genannten Projektassets; keine neuen Fremdassets oder Lizenzänderungen nötig.

## Prüfung und Nachweise

Neue Unity-Spielmodusprüfung: `Assets/Scripts/Diagnostics/ArsenalChecks.cs`; Bericht `Logs/Arsenal/Checks.txt`. Sie prüft alle Kaufgrenzen jeweils vorher und genau am Grenzwert, Duplikate/Wiederholungen, vollständige Kaufwirtschaft, alte Arrays, echte vier Spezialangriffe, Grenzen der Nahkampfreichweite, tatsächlichen Waffenwechsel je Figur, Münzanziehung, Schaden/Schutz, Verlangsamung/Ende, Fähigkeits-Cooldowns/Pause, alle 24 Schild-Waffen-Kombinationen, Editorzugang, Speichern und echte gerenderte Waffenvorschauen. Die Tests verwenden ausschließlich einen RAM-Spielstand und kontrollieren die unveränderten echten Speicherwerte und veröffentlichten Dateien.

Ergänzende Regressionen prüfen die bisherigen 16 Figur-/Waffenangriffe und den bestehenden Datenpatchkanal. Endgültige Zahlen/Ergebnisse sowie Hashes sind in `Logs/Arsenal/Verification.json` festgehalten. Die PNGs unter `Logs/Arsenal/` sind Unity-Editor-Vorschauen, keine Handyaufnahmen. Backups liegen unter `Backups/ArsenalProgression/`.

Abschluss: **230 neue Prüfungen, 120 Figurenprüfungen und 148 Datenpatchprüfungen bestanden (498 insgesamt)**. Android-C#-Spielerskripte wurden separat mit Android-Defines zu DLLs kompiliert (`Logs/Arsenal/AndroidScriptCompilation.txt`), ohne APK-Bau. Shop, Figurenseite und vier unterschiedliche Waffenmodelle wurden als gerenderte Vorschauen visuell kontrolliert. Kein neuer Handytest, da keine neue APK beauftragt wurde.

Die ersten Diagnoseanläufe sind erhalten: ein falscher Diagnose-Propertyname wurde korrigiert; beim neuen Fernkampftest musste die per Rigidbody versetzte Figur erst ihren interpolierten Transform erreichen. Die Figurenregression wartet nach der Screenshot-Canvas-Rücksetzung und misst die Gehstrecke anhand fester Physikschritte. Dies sind Testaufbau-Korrekturen; die Treffer-/Bewegungsanforderungen wurden nicht entfernt.
