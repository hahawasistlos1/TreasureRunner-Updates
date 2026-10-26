# Einstellbare Touch-Tastengröße

Stand: 26.10.2026, lokale Quellenänderung nach Nutzerauftrag. Noch nicht in der installierten/veröffentlichten APK 0.8.0 enthalten. Keine APK gebaut, keine Version erhöht, nichts veröffentlicht.

## Bedienung

In **Einstellungen → Tastengröße** oder während eines Levels unter **Pause → Tastengröße** stehen vier getrennte Regler für Pfeile/Joystick, Sprung, Angriff und Waffenwechsel bereit. Bereich: 80–180 % in Zehnerschritten. Pfeile/Joystick, Sprung und Angriff starten mit 120 %, Waffenwechsel mit 100 %. Die Wahl zwischen Joystick und klassischen Pfeilen bleibt erhalten. Die Größen werden automatisch im vorhandenen Spielstand gespeichert. „Standardgrößen wiederherstellen“ setzt nur diese vier Größen zurück.

Sichtbare Tasten, Symbole und Touch-Flächen wachsen gemeinsam. Die Abstände und Positionen werden automatisch angepasst; bei knappem Bildschirmplatz wird die gesamte Steuerung passend verkleinert, damit sich die Bedienflächen nicht überschneiden. Das HUD bleibt frei. Bei Größen-/Bildschirmwechsel werden gehaltene Eingaben freigegeben, damit die Figur nicht ungewollt weiterläuft. Im Größenfenster bleibt das Spiel pausiert. „Fertig“ führt zurück zum Pausenmenü; Escape schließt zuerst das Größenfenster. Beim Fortsetzen wird ein noch offenes Größenfenster ebenfalls geschlossen.

## Umsetzung

- `Assets/Scripts/UI/TouchControlSizing.cs`: vier Gruppen, Normalisierung alter/ungültiger Werte, Speicherung, gemeinsame Layoutberechnung.
- `Assets/Scripts/UI/TouchControlSizePanel.cs`: gemeinsames Einstellungsfeld für Menü und Pause.
- `Assets/Scripts/UI/CampaignHUD.cs`: Anwendung auf echte Touch-Flächen, Eingabefreigabe, Dialogverwaltung und Größenanpassung an die verfügbare Fläche.
- `Assets/Scripts/Progression/ProgressData.cs`: vier additive Größenfelder; kein Zurücksetzen von Fortschritt, Inventar oder eigenen Levels.
- `Assets/Scripts/UI/TouchActionButton.cs`: gezieltes Freigeben gehaltener Finger.

Windows-Tastatursteuerung und veröffentlichte Windows-Werkzeuge bleiben unverändert. Die zuvor lokal ergänzten Android-Hintergrundbenachrichtigungen bleiben im Quellenstand enthalten.

## Prüfung

Automatisierte Unity-Spielmodusprüfungen: `Logs/TouchScaling/Checks.txt`, ergänzt durch die bestehende Steuerungsregression unter `Logs/TouchScaling/ControlRegression.txt`. Geprüft werden unter anderem Grenzwerte, alte Speicherstände, vier Bildschirmgrößen, beide Bewegungsmodi und alle 16 Kombinationen der minimalen/maximalen Gruppengrößen, echte UI-Raycasts, tatsächliche Bewegung und Sprung, Freigabe gehaltener Eingaben, Pause, Reset, Menüregler und Speicherrundlauf. Tests verwenden einen RAM-Spielstand und prüfen den unveränderten realen Spielstand/Backup.

Endergebnis: **308 Größenprüfungen und 26 Steuerungsprüfungen bestanden**, ohne Unityfehler. Der erste Steuerungsregressionslauf scheiterte unmittelbar nach der Test-Screenshotaufnahme: Diese ändert vorübergehend die Canvas-Größe, wodurch die neue Größenanpassung erwartungsgemäß gehaltene Eingaben freigibt. Der Test wartet nun vor dem nächsten Fingerdruck auf das wiederhergestellte Layout; alle ursprünglichen Bewegungsprüfungen bleiben bestehen. Erster Fehllauf ist in `Logs/TouchScaling/ControlRegression-first-failed.txt` erhalten. Beim visuellen Prüfen wurde außerdem ein zu hoher Slidergriff korrigiert und seine feste Höhe durch einen Test abgesichert.

Die PNG-Dateien unter `Logs/TouchScaling/` sind gerenderte Unity-Editor-Vorschauen, keine Handyaufnahmen. Sichtprüfung umfasst Standard-/Maximalpfeile, Joystick, Größenfenster und Einstellungen. Weitere Nachweise und Dateiprüfsummen: `Logs/TouchScaling/Verification.json`.

Kein Gerätetest dieser neuen Funktion, da dafür erst eine ausdrücklich beauftragte neue APK erforderlich ist.
