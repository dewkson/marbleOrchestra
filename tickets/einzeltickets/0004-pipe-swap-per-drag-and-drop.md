---
id: 0004
title: Pipe-Swap per Drag & Drop mit Visualisierung
type: Feature
priority: Low
status: Done
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

Umgesetzt in `Assets/Scripts/Grid/GridInputHandler.cs`, ohne den
bestehenden Klick-Klick-Ablauf anzutasten:
- Press+Release ohne nennenswerte Cursorbewegung (< `dragThresholdPixels`,
  Inspector-Feld, Default 8px) wird weiterhin als Klick behandelt -
  gleiches Verhalten wie vorher.
- Sobald sich der Cursor bei gedrückter Maustaste über den Threshold
  hinaus bewegt, beginnt ein Drag: die gehaltene Pipe folgt dem Cursor
  (Schnittpunkt des Kamerastrahls mit der Grid-Ebene, normal =
  `grid.transform.forward` - funktioniert unabhängig von der
  Grid-/Kamera-Orientierung, siehe Ticket 0029) und bekommt denselben
  `SetSelected`-Highlight wie die bestehende Klick-Auswahl. Die Pipe, über
  der der Cursor gerade schwebt, wird ebenfalls hervorgehoben.
- Beim Loslassen: liegt unter dem Cursor eine andere, nicht gesperrte
  Pipe, wird über `PathGrid.SwapCards` getauscht; sonst springt die
  gehaltene Pipe zurück auf ihren Ursprungsplatz (Drag ins Leere = Abbruch).
- Ein begonnener Drag löscht eine eventuell noch offene
  Klick-Klick-Auswahl, damit sich beide Eingabewege nicht in die Quere
  kommen.
- Während des Drags wird der eigene `BoxCollider` der gehaltenen Pipe
  deaktiviert, da sie sonst (am Cursor liegend) ihren eigenen
  Hover-/Drop-Raycast blockieren würde.
- `IsLocked`-Pipes sind wie schon vorher vom Swap ausgeschlossen (Start-
  als auch Zielseite).
