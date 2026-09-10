---
id: 0019
title: Block-Gefälle automatisch aus 2D-Pfadrichtung ableiten
type: Feature
priority: Medium
status: Done
area: Physics
created: 2026-08-29
---

# 0019 - Block-Gefälle automatisch aus 2D-Pfadrichtung ableiten

## Beschreibung

Die Richtung des geplanten 2D-Pfades soll automatisch bestimmen, zu
welcher Seite ein 3D-Block abschüssig ist. Jeder Block muss so
ausgerichtet werden, dass eine Kugel physikalisch in die im 2D-Plan
vorgegebene Richtung weiterrollt.

## Details

- Baut auf [[0017]] (3D-Terrain als einzelne Blöcke pro Pfadabschnitt)
  und [[0018]] (universelles 3D-Block-Prefab mit Ausrichtungs-Parameter)
  auf - dieses Ticket betrifft konkret die automatische Berechnung der
  Ausrichtung/Neigung jedes einzelnen Blocks aus der im 2D-Plan
  hinterlegten Pfadrichtung.
- Bereits vorhandene Gefälle-Logik für die durchgehende Bahn findet sich
  in `Assets/Scripts/Grid/TrackTerrainGenerator.cs` (siehe [[0013]]) -
  vermutlich Ausgangspunkt/Referenz für die Übertragung auf einzelne
  Blöcke.
- Akzeptanzkriterien (grobe erste Fassung):
  - Für jeden Block wird aus der Richtung des zugehörigen 2D-Pfad-
    abschnitts automatisch die Neigungsseite (abschüssige Richtung)
    bestimmt.
  - Die resultierende Ausrichtung ist physikalisch konsistent, sodass
    eine Kugel allein durch Schwerkraft in Pfadrichtung weiterrollt.

## Notizen

- 2026-08-29: Erste Umsetzung nutzte einen Bisektor-Yaw an Kurven. Nach
  User-Feedback korrigiert: Kein Bisektor mehr - jeder Block zeigt in
  Richtung seines eigenen ausgehenden Pfadsegments (beim Ziel-Block: das
  eingehende), was per Definition immer ein exakter Gitterschritt und
  damit immer ein Vielfaches von 90° ist, nie diagonal. Siehe
  `TrackBlockSpawner.ComputeYawDegrees`.
- 2026-08-29: Zwischenzeitlich wurde `TiltDegrees` gar nicht mehr gesetzt
  (flache Blöcke, reiner Fall). Nach weiterem User-Feedback korrigiert:
  Blöcke sollen weiterhin sichtbar geneigt sein, UND trotzdem soll ein
  kleiner Fall am Übergang bleiben - siehe [[0020]]-Notiz zu
  `tiltFraction`. Die Neigung wird weiterhin direkt in die
  Oberflächen-Mesh gebacken, nicht auf den Transform angewendet (siehe
  [[0018]]).
- Noch offen: horizontale (X/Z) Verkettung an Kurven, siehe [[0021]].

