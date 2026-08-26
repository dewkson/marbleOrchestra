---
id: 0008
title: Locked-Status von Pipes im Level Grid Editor anzeigen
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-08-26
---

# 0008 - Locked-Status von Pipes im Level Grid Editor anzeigen

## Beschreibung

Der LevelEditor sollte bei den Pipe-Karten auch zeigen können, ob diese
Locked sind oder nicht.

## Details

- Betroffen: `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`,
  Methode `DrawCell` - zeichnet aktuell Hintergrundfarbe, Hub-Farbe und
  Richtungsarme einer Pipe, aber keinen Hinweis auf `PipeDefinition.Locked`.
- `Assets/Scripts/Grid/PipeDefinition.cs` hat das bestehende `Locked`-Feld
  (`bool locked`, Property `Locked`), das auch von der "Randomize"-Funktion
  im selben Editor-Fenster ausgewertet wird (fixierte Pipes werden dort
  ausgespart).
- Akzeptanzkriterien:
  - Zellen mit einer Pipe, deren `Locked == true` ist, sind im Level-Grid-
    Editor-Fenster optisch klar von nicht-fixierten Pipes unterscheidbar
    (z.B. Rahmen/Icon/Overlay).
  - Zellen ohne Pipe oder mit `Locked == false` zeigen diesen Hinweis nicht.

## Notizen

Umgesetzt in `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`: neue
Methode `DrawLockedBorder`, aufgerufen aus `DrawCell` wenn `pipe.Locked ==
true`. Zeichnet einen 3px dicken orangenen Rahmen um die gesamte Zelle -
unabhängig von Skin klar erkennbar. Zellen ohne Pipe oder mit
`Locked == false` bleiben unverändert.
