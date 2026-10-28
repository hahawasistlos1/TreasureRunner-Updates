# Direkt in einer echten Levelszene bauen

Lokale Entwicklung vom 27.10.2026. Gemeinsamer Editor für Android und Windows. Am27.10.2026 nach ausdrücklichem Bauauftrag separate Test-APK erstellt und geprüft: Builds/LiveEditor-Test/TreasureRunner-Test.apk. Version0.9.0 / Code10 bleibt unverändert. Noch keine Veröffentlichung oder Handyinstallation dieser Test-APK.

## Bedienung

Ein leerer Bauplatz öffnet eine leere Szene mit Grundkulisse und einer Startfigur als Größenreferenz. Vorhandene gespeicherte Entwürfe werden weiterhin geöffnet. Unter Bauplatz kann nach Bestätigung ein neues leeres Level begonnen werden; vorhandene Dateien werden erst beim ausdrücklichen Speichern ersetzt.

Links stehen weiterhin alle weltunabhängigen Kategorien: Boden, Aufzüge, Gegner, Fallen, Beute, Natur, Dekoration und Spielziele. Bauteil wählen, dann direkt in der Szene tippen/klicken oder zum Zeichnen ziehen. Die Platzierung rastet weiterhin sauber ein; die Darstellung besteht aus zusammenhängender echter Spielgeometrie. Die grüne Vorschau zeigt die Platzierung des gewählten Bauteils.

Mit Hand: an lässt sich der Ausschnitt per Ziehen verschieben. Unter Windows funktioniert auch Ziehen mit rechter/mittlerer Maustaste. Mausrad sowie die Zoomtasten vergrößern oder verkleinern die Ansicht, Ansicht setzt die Kamera zurück. Abschnittnavigation springt entlang des Levels. Radierer, Start setzen, Undo/Redo, Breitenvergrößerung und Grundkulisse bleiben verfügbar. Die Kulisse beschränkt die Bauteilauswahl nicht.

Testspielen startet die reale Spielszene mit Kollisionen und Gegnerverhalten. Nach Rückkehr bleibt der Entwurf erhalten. Durchspielen ist weiterhin keine Pflicht für die Einreichung; gültige Start-/Zielposition und ein grundsätzlich erreichbarer Weg sind nötig. Unvollständige Entwürfe können gespeichert werden. Neue Bibliotheksbauteile bleiben bis zur passenden ausdrücklich veröffentlichten Spielversion vom bisherigen öffentlichen Katalog getrennt.

## Umsetzung und Schutz bestehender Daten

EditorLevelScene verwendet dieselbe LevelFactory.BuildPreview/BuildTile-Geometrie wie das Spiel, aber eine isolierte Kamera und Ebene. Runtime-Logik, Animationen, Physik und Schadensauslöser der Vorschau sind deaktiviert. Sichtbare Bauteile werden je Koordinate aktualisiert; beim Verschieben werden entfernte Objekte aus dem Vorschaufenster entfernt. Modelle, Blöcke, Münzen und feste Fallen behalten ihre tatsächlichen Proportionen. Der Editor erzeugt keine Änderung an Kampagnenfortschritt, Asset-IDs oder Käufen.

Die vorhandenen Bauteilminiaturen bleiben in der Auswahlpalette erhalten. Das frühere große Kachelmatrix-Feld wurde durch eine einzige RenderTexture für die echte Szene ersetzt. Android und beide privaten/öffentlichen Windows-Modi verwenden dieselbe Implementierung.

## Links und GitHub

Einfügen unterstützt jetzt auch kopierte Nachrichten mit genau einem eingebetteten Link. Leere Zwischenablage und zu lange Inhalte werden erklärt. GitHub-Einreichungslinks werden weiterhin ausschließlich für hahawasistlos1/TreasureRunner-Updates angenommen; fremde Hosts, fremde Ports und Benutzerinformationen werden abgelehnt. Freundeinladungen behalten ihre eigenen festen Ursprungs- und Formatprüfungen. Es wird kein fremder Code heruntergeladen oder ausgeführt.

Die private Verwaltung bietet zusätzlich Geschützt speichern und verbunden bleiben. Dabei speichert Windows ein persönliches Token in seiner Anmeldeinformationsverwaltung unter TreasureRunner/CommunityModeration, nur für das aktuelle Windows-Konto. Sitzungsnutzung bleibt möglich, gespeicherter Zugang kann entfernt werden. Ein GitHub-Token kann vom Besitzer weiterhin ablaufen oder widerrufen werden; diese Änderung hebt die GitHub-Prüfung nicht auf. Tokens werden nicht in Leveldateien, PlayerPrefs, Pakete, Logs oder Git geschrieben. Native Smoke-Tests greifen nicht auf echte gespeicherte Zugangsdaten zu. Kein bestehender Zugang wird beim Bauen/Prüfen zurückgesetzt.

## Prüfungen

TreasureRunnerLiveEditorTests prüft echte Zeigerkoordinaten in perspektivischer Ansicht, Platzieren/Radieren, Undo/Redo, Verschieben ohne Malen, Zoom, absolute Abschnittskoordinaten, alle acht Kulissen, ruhende Vorschauphysik und echten Testspielstart samt Rückkehr. Tests laufen in einem Memory-Spielstand und prüfen Erhalt des echten Spielstands/Backups. Berichte: Logs/LiveEditor/Checks.txt. Echte Unity-Bildausgaben: Builds/LiveEditor-Preview. Die Szenenbilder wurden visuell angesehen, der Kamerastandard danach für größere Bauteile eingestellt und erneut geprüft.

Separat BiomeTests mit echter Gegnerausrüstung, Pfeilen/Deckung, Materialfarben und Legacy-Skelett bestanden. Einladungsregression, Android-DLLs und native Windows-Prüfung werden im abschließenden Verification.json zusammengeführt. Ein aktueller Handytest steht aus. Die veröffentlichte APK bleibt unverändert.

## Abschließender Nachweis

Alle genannten Prüfgruppen bestanden; Sammelbericht Logs/LiveEditor/Verification.json. Beide Windows-Entwicklungsausgaben mit null Buildfehlern erzeugt und danach nativ ausgeführt. GitHub-Einreichungsqueue und freigegebener Katalog waren tatsächlich lesend erreichbar. Vollständige native Bauoberfläche und Rückkehr aus dem Testspiel als Bilder angesehen. 20 Lizenztexte je Werkzeug mit Quelle verglichen. Veröffentlichte0.9-APK und WindowsZIP erneut unverändert gehasht. Kein Handytest dieser neuen Änderungen, keine neue APK und kein Upload.

Lokaler Einstieg: Builds/AssetExpansion-Dev/Welteneditor/TreasureRunner-Levelbau-Entwicklung.exe. Die Desktopverknüpfungen haben bereits das neue Icon, starten aber weiterhin die bisherigen0.9-Programme; ihre Zielpfade und Argumente blieben unverändert.

Nachtrag Test-APK: Paket, Signatur, neuer Editor und neuesIcon vollständig geprüft. Sammelbericht Logs/LiveEditor/TestApk/Verification.json. Frühere Aussagen zu keiner neuenAPK beziehen sich auf den Entwicklungsstand vor dem ausdrücklichen Bauauftrag. Gerätetest steht aus.
