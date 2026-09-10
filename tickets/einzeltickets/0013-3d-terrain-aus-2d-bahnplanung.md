---
id: 0013
title: 3D-Terrain mit Gefälle und Rail-Einkerbung aus 2D-Bahn
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-08-28
---

# 0013 - 3D-Terrain mit Gefälle und Rail-Einkerbung aus 2D-Bahn

## Beschreibung

Die in 2D geplante Bahn soll in ein 3D Terrain überführt werden. Dafür
wäre es im einfachsten Fall notwendig, dass das Terrain immer in
Bahnrichtung eine negative Steigung besitzt (abschüssig ist). Außerdem
muss für die Murmelbahn eine Rail-Einkerbung vorgesehen sein, in der die
Murmel perfekt reinpasst und langlaufen kann.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/PathGrid.cs` / `PathPipe.cs` - definieren die in 2D
  geplante Bahn (Zellen, Richtungen), dienen vermutlich als
  Eingabe/Datenquelle für die 3D-Terrain-Generierung.
- `Assets/Scripts/Grid/PipeVisual.cs` - aktuelle visuelle Darstellung der
  Pipes; muss ggf. um eine 3D-Terrain-/Mesh-Repräsentation ergänzt oder
  ersetzt werden.
- Es existiert noch keine Terrain- oder Mesh-Generierungslogik im Projekt -
  diese muss neu aufgebaut werden.

Akzeptanzkriterien (grobe erste Fassung):
- Aus der geplanten 2D-Bahn wird ein 3D-Terrain/Mesh erzeugt, das dem
  Bahnverlauf folgt.
- Das Terrain hat entlang der Bahnrichtung durchgehend ein Gefälle
  (negative Steigung), damit die Murmel allein durch Schwerkraft rollen
  kann.
- Entlang der Bahn ist eine Rail-Einkerbung (Rinne/Nut) vorhanden, deren
  Querschnitt zur Murmelgröße passt, sodass die Murmel darin geführt
  läuft statt herauszurollen.

## Notizen

- 2026-08-28: Ergänzung nach erster Umsetzung - zwei Nachbesserungen:
  1. Bei mehreren gleichzeitigen Murmelbahnen (Start/Goal-Paaren, siehe
     0010) wurde bisher nur die zuletzt fertiggestellte Bahn als Terrain
     angezeigt. Jede valide Bahn soll ein Terrain-Stück (Rail-Einkerbung)
     bekommen.
  2. Das Terrain soll die gesamte Grid-Fläche als durchgehende Fläche
     abbilden (Draufsicht = Rechteck wie in der 2D-Planung), nicht nur die
     schmale Rail selbst - die Zwischenräume zwischen den Bahnen sollen
     ebenfalls Teil des Terrains sein.
- 2026-08-28: Erste Umsetzung von Punkt 2 (Höhe der Gesamtfläche per
  Inverse-Distance-Interpolation aus allen Bahn-Zellen) erzeugte an vielen
  Stellen falsche Höhen (Fläche lag mal deutlich über, mal deutlich unter
  der Bahn), weil weit entfernte Punkte der Bahn (z.B. Start/Ziel) den
  interpolierten Wert verfälscht haben. Korrigiert auf Projektion auf den
  jeweils nächstgelegenen Punkt der Bahn-Mittellinie (nearest-segment statt
  globaler Distanzgewichtung) - dadurch entspricht die Höhe der Fläche an
  jeder Stelle der lokalen Höhe der nächstgelegenen Bahnstelle.
- 2026-08-28: Auch die nearest-segment-Projektion der Gesamtfläche war
  weiterhin unsauber. Auf Vorschlag des Users die durchgehende Gesamtfläche
  (Punkt 2) wieder verworfen - stattdessen bekommt die schmale Bahn selbst
  seitlich extrudierte, in der Breite konfigurierbare Schultern
  (Querschnitt vorher nur "v", jetzt "---v---"). Das Rail-Profil wurde dabei
  von einer V- auf eine echte U-Form (Halbkreisbogen mit Radius aus dem
  Murmelradius) umgestellt, damit die Murmel formschlüssig hineinpasst.
- 2026-08-28: Start/Ziel-Enden der Bahn ergänzt: rundes Loch (Radius =
  grooveRadius, wie die Rinne) an Start und Ziel. Die Rinne mündet nur von
  der Fahrtrichtungsseite in dieses Loch, die gegenüberliegende Seite ist
  als flache Platte auf Schulterhöhe geschlossen (kein durchgehender
  Rinnen-Querschnitt in die falsche Richtung).

