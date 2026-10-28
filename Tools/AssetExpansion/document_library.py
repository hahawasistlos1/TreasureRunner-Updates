from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
catalog=json.loads((root/'Assets/Resources/AssetLibrary/catalog.json').read_text(encoding='utf-8-sig'))
lines=['# Gemeinsame Assetbibliothek – lokaler Entwicklungsstand','',
       f"18 Paketfamilien in {len(catalog['packs'])} eingelesenen Ausgaben sind lokal eingebunden: {len(catalog['entries'])} Bibliothekseinträge, {len(catalog['images'])} Bilder, {len(catalog['audio'])} Audioclips und {len(catalog['characters'])} zusätzliche Figurenvarianten. Die vorhandenen vier ursprünglichen Figuren bleiben erhalten. Die Bibliothek und Kategorien sind in App und Windows-Werkzeugen gemeinsam und unabhängig von der Grundwelt.",
       '', 'Die veröffentlichte APK 0.9.0 enthält diese Erweiterung noch nicht. Es wurde dafür keine neue APK erstellt, keine Versionsnummer erhöht und nichts veröffentlicht. Neue Levelbauteile verwenden Format 4; dessen Freigabe für den bisherigen öffentlichen 0.9.0-Katalog bleibt gesperrt.',
       '', '## Tatsächlich eingelesene Pakete', '', '| Paket | Urheber | Lizenz | Quelle |','|---|---|---|---|']
for p in catalog['packs']:lines.append(f"| {p['title']} | {p['author']} | {p['license']} | [Anbieter]({p['url']}) |")
lines += ['', 'Die vollständigen Lizenztexte liegen unter Assets/Resources/AssetLibrary/Licenses, sind in den Credits erreichbar und werden beiden Windows-Entwicklungsausgaben beigefügt. Archivquellen und Dateiprüfsummen stehen unter Logs/AssetExpansion/Imports.json. Die Original-ZIPs im Nutzerordner pack bleiben unverändert. Letzte Prüfung des Ordners: 18 ZIPs, davon 16 bytegleich mit bereits eingelesenen Archiven und zwei neuere Ausgaben (Adventurers 2.0 FREE, Dungeon Pack 1.1 FREE). Der erneute Download eines vorhandenen Pakets zählt nicht als weiteres der 14 fehlenden Pakete.',
          '', '## Noch nicht vorhanden', '',
          '- Unity Asset Store: 2D Hand Painted Platformer Environment, Stylized Hand Painted Dungeon Free, Fantasy Skybox FREE, Cartoon FX Remaster Free.',
          '- KayKit: Forest, Fantasy Weapons Bits, Platformer.',
          '- Quaternius: Ultimate Monsters, Ultimate Stylized Nature, Ultimate Modular Ruins, Fantasy Props MegaKit Standard, Universal Animation Library Standard, Universal Animation Library 2 Standard.',
          '- Abstraction / Tallbeard: FREE Music Loop Bundle.',
          '', 'Diese 14 Pakete werden nicht als fertig eingebaut aufgeführt. Bei mehreren Downloadseiten verhindern eine Browserprüfung beziehungsweise fehlende lokale Store-Downloads die Übernahme. Es werden nur die ausdrücklich kostenlosen Ausgaben verwendet. Neue Dateien können im Projektordner pack bereitgestellt werden; anschließend lassen sie sich einzeln mit Lizenz- und Herkunftsprüfung ergänzen.',
          '', '## Bedienung und Prüfung', '',
          'Im Level-Editor: Bauteil wählen → gemeinsame Kategorien; für Bilder, Menüdesign und Audio: Gesamte Assetbibliothek. Bilder behalten ihr Seitenverhältnis, Geräusche können vorgehört werden. Auswählbare Figuren nutzen ihre vorhandene Charakterklasse und deren gekaufte Verbesserungen.',
          '', 'Die Figurengröße und Waffenanbindung, Vorschaubilder, Levelcodes, Spielflächen der neuen Kacheln/Rampen sowie alle acht Grundwelten werden in Unity geprüft. Weitere Berichte: Logs/AssetExpansion/Checks.txt. Android wird ausschließlich zu Test-DLLs kompiliert. Windows-Entwicklungsausgaben liegen getrennt unter Builds/AssetExpansion-Dev; veröffentlichte Ausgaben und Desktoplinks bleiben erhalten.']
(root/'Documentation/Assetbibliothek-Entwicklung.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
credits=root/'Documentation/Credits.md';existing=credits.read_text(encoding='utf-8-sig');marker='\n## Erweiterte gemeinsame Assetbibliothek\n'
existing=existing.split(marker)[0]
existing+=marker+'\nLokale, noch unveröffentlichte Erweiterung. Vollständige Quellen- und Paketübersicht: [Assetbibliothek-Entwicklung.md](Assetbibliothek-Entwicklung.md). Alle tatsächlich verwendeten Einzelpakete werden zusätzlich in den Spiel-Credits mit ihrer vollständigen Lizenz aufgeführt. Neu bezogene Dateien von Quaternius, Unity Asset Store oder Abstraction sind noch nicht enthalten.\n'
credits.write_text(existing,encoding='utf-8')
print('Wrote actual package-edition inventory and credits; no publication.')
