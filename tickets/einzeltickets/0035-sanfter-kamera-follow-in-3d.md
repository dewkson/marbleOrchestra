---
id: 0035
title: Sanfter Kamera-Follow in 3D statt fixierter Kamera
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-04
---

# 0035 - Sanfter Kamera-Follow in 3D statt fixierter Kamera

## Beschreibung

sanfter Kamera-Follow in 3D anstatt fixierte Kamera

## Details

Betroffenes bestehendes System:
- `Assets/Scripts/Grid/CameraModeTransition.cs` - lerpt beim Wechsel von
  2D-Planung zu 3D-Simulation (siehe [[0029]]) einmalig in eine feste
  isometrische Pose (`ComputeIsometricPose`, berechnet aus
  `TrackBlockSpawner.TryGetTracksWorldBounds()`). Danach bleibt die Kamera
  während der gesamten Simulation unbewegt fixiert - genau das beschreibt
  der User hier als "fixierte Kamera".
- `Assets/Scripts/Grid/MarbleController.cs` - steuert die Murmel während
  der Simulation (`IsPlaying`); vermutlich das Ziel-Objekt, dem die Kamera
  in 3D sanft folgen soll.
- `Assets/Scripts/Grid/CameraFitter.cs` / `BoundsCameraMath.cs` - bisherige
  Bounds-/Fit-Logik, die für einen Follow-Modus ggf. nicht mehr die
  gesamte Bahn, sondern nur einen Ausschnitt um die Murmel berücksichtigen
  müsste.

Keine Akzeptanzkriterien ausgearbeitet - Beschreibung liefert nur die
Zielrichtung (Kamera folgt der Murmel in 3D mit sanfter/gedämpfter
Bewegung statt starr zu stehen), keine konkrete Distanz/Winkel- oder
Dämpfungs-Vorgabe.

## Notizen

Umgesetzt in `CameraModeTransition.cs`: nach dem bestehenden einmaligen
Übergang in die isometrische Ansicht (weiterhin die gesamte Bahn
umfassend) folgt die Kamera nun laufend `MarbleController`s aktuell
verfolgter Murmel (`MarbleController.PrimaryMarbleTransform`, neu
ergänzt - erste aktive Murmel aus der internen Liste) über
`Vector3.SmoothDamp`/`Mathf.SmoothDamp` (Position und
`orthographicSize`), bei gleichbleibender isometrischer Rotation. Auf
Wunsch näher gezoomt als die Übersichtsansicht: dedizierte
`followDistance`/`followOrthographicSize`-Werte statt der
Bounds-basierten Framing-Größe. Stoppt die Simulation, greift wieder
der bestehende Übergang zurück in die 2D-Planungsansicht.