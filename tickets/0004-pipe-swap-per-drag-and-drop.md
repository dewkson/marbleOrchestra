---
id: 0004
title: Pipe-Swap per Drag & Drop mit Visualisierung
type: Feature
priority: Low
status: Open
area: Gameplay
created: 2026-08-25
---

# 0004 - Pipe-Swap per Drag & Drop mit Visualisierung

## Beschreibung

Das Swappen von Karten muss auch über Drag and Drop funktionieren und das
benötigt eine entsprechende Visualisierung. Eher ein unwichtiges Ticket.

## Details

- Betroffen ist `Assets/Scripts/Grid/GridInputHandler.cs`: aktuell nur
  Click-Klick-Swap (erste Pipe anklicken -> markieren, zweite Pipe anklicken
  -> `grid.SwapCards(selectedPipe.Coord, clicked.Coord)`). `IsLocked`-Pipes
  werden bereits vom Swap ausgeschlossen.
  Die eigentliche Swap-Logik liegt in `PathGrid.SwapCards`
  (`Assets/Scripts/Grid/PathGrid.cs`).
- Zusätzlich zum bestehenden Click-Klick-Ablauf soll Drag & Drop als
  alternativer Eingabeweg funktionieren: Pipe A gedrückt halten und auf
  Pipe B ziehen, um sie zu tauschen.
- "Entsprechende Visualisierung" ist in der Beschreibung nicht weiter
  spezifiziert - vermutlich ein visuelles Feedback während des Ziehens
  (z.B. Pipe folgt dem Cursor / Ziel-Zelle wird hervorgehoben), analog zum
  bestehenden `SetSelected`-Highlight auf `PathPipe`.

## Notizen
