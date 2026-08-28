---
id: 0016
title: "Start"/"Ziel"-Beschriftung auf den 2D-Pipe-Karten
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-08-28
---

# 0016 - "Start"/"Ziel"-Beschriftung auf den 2D-Pipe-Karten

## Beschreibung

Auf den 2D Karten "Start" und "Ziel" soll der jeweilige Text auf die
entsprechenden Karten geschrieben werden.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/PipeVisual.cs` - zeichnet die 2D-Pipe-Darstellung
  zur Laufzeit (Hintergrund, Hub, Arme) rein prozedural per SpriteRenderer;
  aktuell kein Text-Element vorhanden.
- `Assets/Scripts/Grid/PathPipe.cs` - kennt die `Role` (`PipeRole.Start` /
  `PipeRole.Goal` / `PipeRole.Normal`) des Pipes und ruft
  `PipeVisual.Refresh()` auf.
- Ticket 0009 hat die Rollen-Unterscheidung bereits für den
  Level-Grid-Editor (Unity-Editor-Tool, `LevelGridEditorWindow.cs`)
  umgesetzt - das betrifft nur die Editor-Ansicht beim Level-Bauen, nicht
  die Laufzeit-2D-Darstellung im Spiel selbst, um die es hier geht.

Akzeptanzkriterien (grobe erste Fassung):
- Pipes mit `Role == Start` zeigen sichtbar den Text "Start" auf der Karte.
- Pipes mit `Role == Goal` zeigen sichtbar den Text "Ziel" auf der Karte.
- Normale Pipes bleiben unverändert (kein Text).

## Notizen

