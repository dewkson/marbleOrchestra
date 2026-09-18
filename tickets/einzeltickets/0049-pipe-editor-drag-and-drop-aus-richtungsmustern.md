---
id: 0049
title: Pipe-Editor per Drag & Drop aus Richtungsmustern statt Kartenliste
type: Feature
priority: Medium
status: Open
area: Level Editor
created: 2026-09-18
---

# 0049 - Pipe-Editor per Drag & Drop aus Richtungsmustern statt Kartenliste

## Beschreibung

Editor für Pipes so verbessern, dass nicht eine Liste mit allen bekannten
Karten zur Auswahl steht, sondern geordnet alle möglichen
Richtungsausprägungen. Dann soll man über Drag and Drop aus den Mustern
die Karten auf das Grid ziehen können und Attribute wie locked, start
oder stop löst man indem man eine Zelle auswählt und dann über toggles
das jeweilige Attribut an oder ausschaltet.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` - `DrawPalette()`
  (ca. Zeile 309-381) zeigt aktuell im Pipe-Layer eine scrollbare Liste
  aller bekannten `PipeDefinition`-Assets (`availablePipes`) als
  Klick-Auswahl (`DrawPaletteEntry`), die per anschließendem Klick auf
  eine Zelle gemalt wird (kein Drag & Drop).
- `DrawCustomPipeBuilder()` (Zeile 383+) erlaubt bereits das
  Zusammenstellen einer Pipe aus Richtungs-Toggles
  (`DrawDirectionToggles()`), plus Background-Color, `Role`
  (Normal/Start/Goal, siehe Ticket 0009) und `Locked` (Ticket 0008) - die
  Attribute werden hier aber direkt beim Brush-Bauen gesetzt, nicht
  nachträglich pro ausgewählter Zelle.
- `GetOrCreateCustomPipe`/`CreateCustomPipeAsset`/`BuildPipeId` (ab Zeile
  767) erzeugen bei Bedarf neue `PipeDefinition`-Assets aus
  Richtungskombination + Rolle + Locked - dieser Mechanismus wäre
  vermutlich weiter nutzbar, wenn Attribute nachträglich per Zellauswahl
  geändert werden.
- `Assets/Scripts/Grid/PipeDefinition.cs` - definiert `Connections`
  (Richtungs-Flags), `Role`, `Locked`, `Color` als Datenmodell einer Pipe.

Akzeptanzkriterien (aus der Beschreibung abgeleitet):
- Die Palette zeigt statt einer Liste vorhandener `PipeDefinition`-Assets
  geordnet alle möglichen Richtungsausprägungen (Kombinationen der
  Connection-Richtungen) als Muster an.
- Karten werden per Drag & Drop aus der Palette auf eine Grid-Zelle
  gezogen, statt wie bisher "Palette-Eintrag anklicken, dann Zelle
  anklicken".
- Attribute wie `Locked`, `Start`/`Goal`-Rolle werden nicht mehr beim
  Bauen der Palette-Karte festgelegt, sondern nachträglich: Zelle
  auswählen, dann per Toggles das jeweilige Attribut an-/ausschalten.
- Offen/zu klären vor Umsetzung: ob die bisherige Liste bekannter
  `PipeDefinition`-Assets (`availablePipes`) dadurch komplett ersetzt wird
  oder weiterhin parallel existiert; wie das Zellauswahl-Attribut-Panel
  UI-seitig in `LevelGridEditorWindow` eingebunden wird (z.B. analog zum
  bestehenden SubLevel-Panel).

## Notizen

