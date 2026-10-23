# TreasureRunner für Android

[Neueste Version herunterladen](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/latest)

## Version 0.6.0 – Neues Abenteuer

Eigenes Schatztruhen-Icon, eine Home-Startseite und überarbeitete Menüs. Der Button **Abenteuer starten** öffnet das Spielermenü mit Welten, Shop und Inventar.

Alle 80 Kampagnenlevel haben neue Hauptwege: längere Strecken, echte breite Sprunglücken, weltabhängige Höhenprofile und gezielte Herausforderungen statt gleicher Dreieck-Plattformfolgen. Es gibt eigene neue Waffenmodelle, zusätzliche Gegnerdetails und solide Kanonen-/Flammenwerfer-Gehäuse. Die Spielfigur landet auf den Gehäusen. Schwerttreffer werden im Animationsfenster entlang der sichtbaren Klinge geprüft; feste Wände blockieren sie. Der kürzere physikalische Grundsprung wurde mit etwa 0,77 Sekunden Luftzeit und 1,52 Metern Höhe gemessen.

Jede der vier Waffen hat drei eigene Verbesserungsstufen. Basis-Pfeile verursachen einen, Runenkugeln zwei Lebenspunkte Schaden; gesunde Standardgegner überleben mehrere Basisschüsse. Das allgemeine Charakter-Upgrade Angriff erhöht Fernkampfschaden nicht. Jedes Kampagnenlevel besitzt eine andere mehrtaktige Original-Musikfassung.

**Beim ersten Start von 0.6 wird der Kampagnenfortschritt für die neue Balance genau einmal zurückgesetzt.** Münzen, gekaufte Ausrüstung, Verbesserungen, Kapitel-Freischaltungen und Bestwerte starten neu. Profil, Einstellungen und selbst gebaute Level bleiben erhalten. Ein alter Kampagnenstand wird separat lokal gesichert. Das Spiel zum Aktualisieren nicht deinstallieren.

## Eigene Level bauen und spielen

Der Editor zeigt Vorschaubilder aus denselben Bauteilen wie das Spiel. **Testspielen** sichert einen separaten Testentwurf; **Zurück zum Editor** führt aus Pause und Ergebnis zurück. Vor **Freigeben** musst du die genaue Fassung bis zum Zielportal spielen. Jede Änderung entwertet ihren bestandenen Test.

**Freigeben** öffnet eine GitHub-Einreichung zur Prüfung. Das erzeugt noch keinen Community-Katalogeintrag. Deine Einreichung kannst du sofort öffnen: **Community → Code / GitHub-Link öffnen**, den Link zur Einreichung oder ihren vollständigen `TRL1:`-Code einfügen, importieren und **Testspielen** wählen. Für den Link wird Internet benötigt; ein kopierter Code funktioniert offline. Acht Bauplätze stehen zur Verfügung.

Die vier Demo-Level wurden entfernt. Der Katalog startet ohne ungeprüfte Einträge. Nach Betreiberprüfung erscheinen neue Community-Level ohne zusätzliche APK. Eigene Level vergeben keine Kampagnenmünzen.

[Editor-Anleitung](community/README.md) · [Community-Katalog](community/catalog.json)

## Installation und Updates

Android 8 oder neuer, ARM64. Aus den Release-Anhängen **TreasureRunner.apk** laden. Das geöffnete Spiel prüft beim Start, bei Rückkehr und regelmäßig im Menü auf Updates. Das Popup zeigt Version, Änderungen und Downloadfortschritt. Größe und SHA-256 werden geprüft, eine bereits vollständige geprüfte APK wird wiederverwendet. Nur die Installation benötigt die manuelle Android-Bestätigung. Nach erfolgreicher Aktualisierung wird die heruntergeladene Update-APK automatisch entfernt. Es gibt keine Update-Meldung bei vollständig geschlossener App.

## Prüfstand und eigener Code

1.088 vollständige Unity-Prüfungen und 47 anschließende gezielte Regressionen bestanden. Alle 80 Szenen aufgebaut; gemessene Sprungphysik, sichtbare Klingen-Kollision, Waffenbalance, solide Gehäuse, Editor-Vorschauen, Testpflicht und einmaliger Reset geprüft. Acht Weltansichten und Home-/Shop-/Editor-Ansichten kontrolliert. Einzelheiten und Artefakt-Hashes stehen im Release.

Offline-Alpha, auf Nutzerwunsch ohne Smartphone-Test. Kein vollständiger menschlicher Durchlauf aller 80 Level und keine abschließende Balancegarantie. Online-Freunde, Ranglisten und Koop benötigen weiterhin einen eingerichteten Serverdienst.

**TreasureRunner-Source-0.6.0.zip** enthält den freigegebenen eigenen C#-/Java-/Shader-Code, Editorwerkzeuge, Tests, eigene prozedurale Modelle/Musik und das eigene Spiel-Icon. Es ist ein Quellcode-Auszug, kein vollständiges startfertiges Unity-Projekt. Fremde Originalassets, Zugangsdaten, Spielstände und Signierschlüssel fehlen. Eine zusätzliche Open-Source-Lizenz wurde nicht erteilt.

0.5 führte den Levelbaukasten und die 80-Kapitel-Kampagne ein, 0.4 die Comic-Darstellung, 0.3 unter anderem Fernkampf und den GitHub-Updater. Der echte A55-Installationsablauf wurde historisch für 0.3 geprüft; das ersetzt keinen Gerätetest von 0.6.
