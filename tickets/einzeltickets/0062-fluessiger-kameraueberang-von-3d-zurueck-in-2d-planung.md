---
id: 0062
title: Flüssiger Kameraübergang von 3D zurück in 2D-Planung
type: Bug
priority: Medium
status: Done
area: Gameplay
created: 2026-10-19
---

# 0062 - Flüssiger Kameraübergang von 3D zurück in 2D-Planung

## Beschreibung

das Fade out aus dem 3D Playmodus in den 2D Planungsmodus sieht nicht flüssig aus. Es muss immer die aktuelle Kamera berücksichtigt werden und dann über eine smoothe transition die Kamera wieder in den top town Modus gefahren werden

## Details

- Betroffen vermutlich `Assets/Scripts/Grid/CameraModeTransition.cs` (Kamerafahrt zwischen 2D-Planung und 3D, siehe Ticket 0029) sowie ggf. die Kameraführung aus den Tickets 0035/0044/0057.
- Akzeptanzkriterium: Beim Wechsel von 3D-Play zurück in den 2D-Planungsmodus startet die Transition von der tatsächlich aktuellen Kameraposition/-rotation/-zoom (Follow-, freie oder mehrspurige Kamera) und fährt per Smooth-Transition in die Top-Down-Ansicht, ohne Sprung.

## Notizen

Ursache war nicht die Kamera (`CameraModeTransition` startet bereits von
der aktuellen Pose), sondern `MarbleController.Stop()`: es zerstörte die
3D-Blöcke sofort (`ClearAll`) und blendete das 2D-Grid ein, während die
Kamera noch zurückschwenkte. Fix: neue Coroutine
`TrackBlockSpawner.ShrinkToFlat` (Umkehrung von `GrowToGameplay`) lässt die
Blöcke parallel zur Kamerafahrt auf die flache Höhe absinken; erst danach
räumt `MarbleController.FinishStop` auf und zeigt das Grid. Play während
des Absinkens beendet den Abbau sofort. Im Editor getestet und für gut befunden.
