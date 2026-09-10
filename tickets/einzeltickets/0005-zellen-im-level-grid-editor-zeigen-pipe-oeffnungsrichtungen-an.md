---
id: 0005
title: Zellen im Level Grid Editor zeigen Pipe-Öffnungsrichtungen an
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-08-26
---

# 0005 - Zellen im Level Grid Editor zeigen Pipe-Öffnungsrichtungen an

## Beschreibung

Die Zellen im PipeSystem im Level Grid Editor sollen anzeigen können zu
welchen Seiten sie geöffnet sind.

## Details

- Betroffen ist `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`,
  Methode `DrawCell`: zeichnet aktuell pro Zelle nur `BackgroundColor` und
  einen zentrierten Hub-Swatch in `pipe.Color`, aber keine Information zu
  `pipe.Connections` (Richtungen).
- Zur Laufzeit gibt es dafür bereits ein Vorbild in
  `Assets/Scripts/Grid/PipeVisual.cs`: pro `Direction` in
  `DirectionExtensions.All` (Up/Right/Down/Left) wird ein "Arm" Richtung
  der jeweiligen Seite gezeichnet, wenn `PipeDefinition.Connections` das
  Flag gesetzt hat.
- Akzeptanzkriterien:
  - Im Level-Grid-Editor-Fenster ist pro Zelle mit Pipe sichtbar, zu
    welchen der vier Seiten (Up/Right/Down/Left) sie laut
    `PipeDefinition.Connections` geöffnet ist (z.B. analog zu den Armen aus
    `PipeVisual`, oder als vier Kanten-Markierungen an der Zelle).
  - Zellen ohne Pipe oder mit `Connections == None` zeigen keine
    Richtungsmarkierungen.

## Notizen

Umgesetzt in `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`: neue
Methode `DrawConnectionArms`, aufgerufen aus `DrawCell` direkt nach dem
Hub-Swatch. Zeichnet analog zu `PipeVisual` pro gesetztem `Direction`-Flag
in `pipe.Connections` einen Balken zur jeweiligen Kante der Zelle (oben/
rechts/unten/links), eingefärbt mit `pipe.Color`. Zellen ohne Pipe oder mit
`Connections == None` bleiben unverändert ohne Marker.
