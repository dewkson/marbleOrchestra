---
id: 0001
title: Kamera passt sich an Level-Grid-Größe an
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-08-25
---

# 0001 - Kamera passt sich an Level-Grid-Größe an

## Beschreibung

Die MainCamera braucht eine Funktion, die bei Start des Spiels prüft, wie
groß das aktuelle Level Grid ist und dann die Kamera Eigenschaften so
einstellt, dass das Level in Gänze auf dem Screen zu sehen ist? Aktuell ist
die Kamera fix auf ein 4x3 Grid eingestellt.

## Details

- Die Grid-Größe steckt in `Assets/Scripts/Grid/LevelData.cs` (`Width`/`Height`,
  Default 4x3), pro Level individuell einstellbar über `ResizeGrid`.
- Es gibt bisher kein eigenes Kamera-Script im Projekt - `GridInputHandler.cs`
  greift nur lesend auf `Camera.main` zu, stellt aber keine Kamera-Werte ein.
  Die Fix-Einstellung auf 4x3 liegt vermutlich als statischer Wert direkt auf
  der MainCamera-Komponente in der Szene, nicht im Code.
- Akzeptanzkriterien:
  - Beim Start eines Levels wird `LevelData.Width`/`Height` des aktuell
    geladenen Levels ausgelesen.
  - Die MainCamera (orthographic size bzw. Position) wird so berechnet, dass
    das gesamte Grid unabhängig von Breite/Höhe und Bildschirm-Seitenverhältnis
    sichtbar ist.
  - Funktioniert auch für Grids, die von 4x3 abweichen (z.B. schmaler/breiter,
    kleiner/größer).

## Notizen
