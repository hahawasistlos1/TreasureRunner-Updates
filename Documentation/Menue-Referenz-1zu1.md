# Menü nach der eigenen Bildvorlage

Stand: 24.10.2026. Der Nutzer hat klargestellt, dass die Vorlage sein eigenes Bild ist und das Menü 1:1 übernommen werden darf. Keine neue APK-, Versions- oder Veröffentlichungsfreigabe.

## Übernahme

Die vorherigen fünf Navigationsgruppen wurden durch die vollständige Vorlagen-Anordnung ersetzt: Original-Logo und Wald-/Burgkopf, Home/Levelauswahl/Weltkarte oben, Münzen und Einstellungen, alle vierzehn direkten Einträge links, große Abenteuerkarte, Charakterbereich rechts, fünf illustrierte Aktionskarten und drei Statuskarten unten. Die 1672×941-Vorlage bestimmt die Positionen und Größen; andere Seitenverhältnisse passen die Anordnung innerhalb der Safe Area an.

Die Nutzerdatei liegt unverändert unter `Assets/Resources/MenuArt/Reference/UserMenu.png`. Sie wird als gemeinsamer Grafik-Atlas genutzt: einzelne UV-Ausschnitte für Logo, Symbole, UI-freie Kartenbilder und sichtbare Waldszene. Kein vollständiger Screenshot liegt als fertiges Menü über der Anwendung. Schrift, Zahlen, Buttons, Auswahl und Unterseiten sind eigenständige Unity-Elemente. Die ursprüngliche Home-Startseite vor dem Spielerbereich bleibt erhalten.

Nur die von Anzeigen verdeckten Umgebungsbereiche der Abenteuerillustration wurden mit dem eingebauten OpenAI-Bildgenerierungswerkzeug ergänzt: `Assets/Resources/MenuArt/Reference/Adventure.png`. Kein CLI/API-Fallback. Die sichtbare Wald-/Brückenszene darüber stammt direkt und unverändert aus der Originalvorlage. Eingabe und exakter Prompt stehen in `Documentation/MenuArt-Prompts.txt`, Eintrag 3. Lato/Cinzel, vorhandene Assetquellen und Lizenzen bleiben erhalten. Die Credits nennen die Nutzer-Vorlage und die ergänzte Illustration getrennt.

## Funktionierende Anzeigen

- Guthaben aus dem Speicherstand; Plus öffnet den Shop, Käufe aktualisieren das Guthaben sofort.
- Das Kristallfeld ist ausdrücklich mit **Erfolge** beschriftet und zeigt deren echte Anzahl. Keine Premiumwährung ergänzt.
- Name, ausgewählte Figur, Farbvariante, Waffe und fünf Upgradestufen aus dem vorhandenen Spielstand. Engere echte 3D-Vorschau; Vorschau-Animatoren überschreiben die entspannte Pose nicht mehr. Spielanimationen unverändert.
- Der Charakterbalken zählt tatsächlich abgeschlossene Level von 80. Keine erfundenen XP oder Charakterlevel.
- Gewählter Level, Weltfortschritt, gespeicherte Münz-/Gegnerbestwerte und beste Zeit. Fehlende Detaildaten bleiben als Strich sichtbar, keine erfundenen Sollzahlen.
- Echte Rekorde/Gesamtbestwert, lokale Kontaktzahl mit Hinweis auf fehlenden Online-Status sowie der vorhandene Update-/Patchzugriff.
- Alle bisherigen Unterseiten, Shoptransaktionen, Ausrüstung, Levelstart und Rückkehr bleiben verbunden. Weltkarte enthält acht reale Weltziele mit den zugehörigen Levelseiten.

Die Anzeige weicht dort vom gemalten Beispiel ab, wo dessen Systeme oder Zahlen im echten Spiel nicht existieren. Speicherstände und eigene Level werden nicht zurückgesetzt.

## Dateien und Sicherung

`ReferenceMenuSkin.cs`: Koordinaten, gemeinsamer Atlas, Farben, Verlauf und echte Rahmenringe statt opaker Outline-Duplikate. `MainMenuController.cs`: vollständige Seitenleiste, Kopf und bestehende Seiten. `MenuDashboard.cs`: Vorlagenaufteilung und Datenbindung. `CharacterPreview.cs`: optionale engere Kamera und stabile Menüpose; Standardzoom der Figurenauswahl bleibt erhalten. `CreditsPage.cs`: Herkunftshinweise.

Sicherung vor der Umstellung: `Backups/ReferenceMenu/BeforeExactLayout.zip`. Die zwei neuen Menütexturen teilen sich ungefähr 12 MiB Grafikspeicher, ohne Mipmaps oder lesbare CPU-Kopie; kein Laden pro Button und keine externen Grafikaufrufe.

## Prüfung und Bereitstellung

Echte Unity-Renderings mit **Testspielstand**: `Builds/ReferenceMenuPreview/Dashboard-1672x941.png`, `Dashboard-Handy.png` (2340×1080) und `Dashboard-Tablet.png` (1200×900). Keine Handyaufnahmen und keine statischen Menü-Mockups.

Gezielte Tests: `Logs/ReferenceMenuTests.txt`. Home vor Dashboard, Originalgrafiken, echte Werte, alle Regionen, tatsächliche Pointer-Treffer auf den vierzehn Seitenleistenbuttons, alle Unterseiten, unveränderte vollständige Fortschrittsdaten beim Navigieren, alle vier echten Charaktere und ihre Vorschau-Animatoren, acht Weltziele, Kauf und sofortiges Guthaben, Ausrüsten, ausgewählter Levelstart, Rückkehr und Baukasten. Zusätzlich 547 bestehende Spiel-/Editorprüfungen in `Logs/ReferenceMenuRegression.txt`. Die vorhandenen Einladungstests werden im neuen Menü erneut ausgeführt.

Keine APK, Windows-Programme, Versionsänderung, Veröffentlichung, Installation oder Handytest. Weiterhin 0.7.0 / Android-Code 8; die drei bestehenden Release-Dateien bleiben unverändert.

Abschluss: 81 gezielte Menüprüfungen, 547 Spiel-/Editorregressionen und 56 Einladungsprüfungen bestanden, insgesamt 684. Alle erfolgreichen Durchläufe ohne Unity-Fehler. Der Grafik-Atlas bleibt exakt 1672×941, ohne automatische Anpassung an Zweierpotenzen. Drei freigegebene Release-Dateien per SHA-256 unverändert bestätigt: `Logs/ReferenceMenuArtifacts.json`. Keine laufenden eigenen Unity-Prüfprozesse mehr.

Lokales Ergänzungspaket für das vorhandene Unity-Projekt: `Builds/TreasureRunner-Menue-Referenz-Projektdateien.zip`. Enthält Quellen, eigene Menügrafiken, Schriften/Lizenzen, Dokumentation, Vorschauen und Prüfberichte. Keine APK und kein eigenständig installierbares Spielpaket.
