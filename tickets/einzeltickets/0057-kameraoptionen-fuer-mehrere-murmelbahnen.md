---
id: 0057
title: Kameraführung für mehrere Murmelbahnen im 3D-Modus
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-30
---

# 0057 - Kameraführung für mehrere Murmelbahnen im 3D-Modus

## Beschreibung

Ich möchte weitere Optionen zur Kameraführung haben: Aktuell gibt es nur die Geführte Kamera und die freie Kamera. Aber wenn wir in einem Sublevel schon 3 Murmelbahnen haben, dann geht die geführte Kamera auf genau eine Bahn und verfolgt diese Murmel. Zum einen möchte ich für die geführte Kamera zwischen den Bahnen switchen können. Und dann fänd ich auch einen Modus sinnvoll, bei dem nacheinander die verschiedenen Bahnen durchgewechselt werden für die geführte Kamera. Für diese Optionen hätte ich gerne Buttons in der UI im 3D Play Modus

## Details

- Betroffene Stellen: `Assets/Scripts/Grid/CameraModeTransition.cs` (Kameramodi), `FreeCameraReturnButtonUI.cs` (bestehende Kamera-Button-UI im 3D-Modus), ggf. `CameraFitter.cs`.
- Akzeptanzkriterien:
  - Die geführte Kamera kann per UI-Button manuell zwischen den Murmelbahnen eines Sublevels wechseln.
  - Zusätzlicher Modus: die geführte Kamera wechselt automatisch nacheinander durch die Bahnen.
  - Beide Optionen sind über Buttons in der UI im 3D-Play-Modus bedienbar.

## Notizen

