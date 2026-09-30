---
id: 0058
title: Top-Down-Minimap-Kamera im 3D-Play-Modus
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-09-30
---

# 0058 - Top-Down-Minimap-Kamera im 3D-Play-Modus

## Beschreibung

zusätzliche Top Down Kamera Perspektive im 3D Play Modus. Ich möchte die Möglichkeit haben einen zusätzlichen 2D View von oben orthogonal runter auf die Oberfläche vom Level zu haben, etwas kleiner dargestellt wie eine Art 2D-Minimap

## Details

- Betroffene Stellen: `Assets/Scripts/Grid/CameraFitter.cs`, `BoundsCameraMath.cs` (Kamera-Fitting an Level-Bounds), `CameraModeTransition.cs`.
- Akzeptanzkriterien:
  - Im 3D-Play-Modus gibt es eine zusätzliche orthografische Kamera von oben auf die Level-Oberfläche.
  - Sie wird verkleinert (z.B. als Overlay in einer Bildschirmecke) neben der Hauptkamera dargestellt, wie eine 2D-Minimap.
  - Ein-/Ausblenden ist möglich ("die Möglichkeit haben").

## Notizen

