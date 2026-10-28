# Jägerbogen: übergroße Darstellung beim Laufen

27.10.2026. Fehler am angeschlossenenA55 selbst nachgestellt: importierter Jägerbogen riesig, beim Richtungswechsel große braune/blaue Flächen vor der Kamera. Aufnahmen43–45 unter Logs/DeviceLiveEditor. Das war ein echter Darstellungsfehler; frühere Kampftests prüften Treffer, aber nicht die tatsächliche Waffengröße.

## Ursache und Korrektur

Das importierte Bow-Mesh besitzt gespeicherte Bounds von0 trotz vorhandener sichtbarerGeometrie. Die automatische Waffengrößenanpassung verwendete deshalb den Mindestnenner0.01 und vergrößerte das Modell massiv. Die Knochen-/Waffen-Weltskalierung selbst war korrekt.

AssetBlockSurface.GeometryBounds berechnet bei leeren oder nichtendlichen Bounds lesbarer statischerMeshes die Grenzen mit RecalculateBounds neu. LibraryWeaponFit weist weiterhin ungültigeModelle zurück und verwendet dann die vorhandene Ersatzdarstellung. Beim Bogen liegt der Griff jetzt in der Mitte. Schaden, Reichweite, Preise, Upgrades, Physik und gespeicherteDaten wurden nicht verändert.

## Prüfung

TreasureRunnerWeaponPoseTests misst Weltabmessungen unabhängig über tatsächlicheMeshpunkte. Alle10Figuren und8Waffen, wiederholtes Rechts-/Linkslaufen mit geprüfter Facing-/Velocity-Änderung, Handposition und Angriffspose sowie Bogenränge0–3 bestanden. Dazu Plattform-Kollisionen und echte Kampfregression für alleFiguren bestanden. Reale Spielstände/Backups im lokalen Test unverändert.

Nutzer bestätigte ausdrücklich korrigierteTestAPK bauen und installieren, nicht veröffentlichen. SeparateAPK Builds/BowFix-Test/TreasureRunner-BowFix-Test.apk, Version0.9.0/Code10, SHA256 cb40dd022ee735c67491c51810c2710406b998e15c3cb27d6cc34026096e07a2. Paket/ARM64/Signatur/Native-Funktionen/Icon geprüft, gleiche Signatur wie0.9. AufA55 als Aktualisierung ohneDeinstallation/Datenlöschung installiert. Danach normalenBogen beimRechtslaufen, Linkslaufen und Schießen tatsächlich angesehen; Pfeil sichtbar. Kein neuerGame-Laufzeitfehler beobachtet. Gerät zumOriginaleditor zurückgeführt.

Alle3Level-/Entwurfsdateien sind vor/nachInstallation und nachGerätetest bytegleich (Backups/BowFix-Device). VorherigeTestAPK und veröffentlichteDateien unverändert, keineVeröffentlichung. Sammelbericht Logs/WeaponPose/Verification.json. Windows-Projektquelle enthält denselbenFix, bestehendeWindows-EntwicklungsEXEs wurden in diesemAuftrag nicht neu gebaut. Keine allgemeine Fehlerfreiheit oder vollständigeHandyprüfung allerFiguren/Upgrades behauptet; diese Varianten wurden lokal geprüft, dasA55 prüfte die gemeldeteFigur im Custom-Test mit Bogenrang0.
