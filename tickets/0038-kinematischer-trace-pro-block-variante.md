---
id: 0038
title: Kinematischer Bewegungs-Trace pro Block-Variante definierbar
type: Feature
priority: Medium
status: Open
area: Physics
created: 2026-09-04
---

# 0038 - Kinematischer Bewegungs-Trace pro Block-Variante definierbar

## Beschreibung

Für die einzelnen Block-Varianten soll definierbar sein, welchen Pfad
die Kugel im kinematischen 3D-Modus tatsächlich nimmt, und wo sie wie
schnell ist, um auch Sprünge simulieren zu können. Da die Blöcke
unterschiedlich sind und die Kugel sich jeweils anders verhält, wäre es
sinnvoll, wenn man den Trace pro Block definieren kann.

## Details

- Betrifft `MovementMode.Kinematic3D` aus [[0014]]: aktuell liefert
  `TrackTerrainGenerator.SampleGroovePosition()` für eine fraktionale
  Bahn-Position eine Weltposition auf dem Rinnenboden, mit konstantem
  Tempo (`cellsPerSecond`) über `MarbleController.RunAlongPath3D()` -
  ein einheitlicher Bewegungsverlauf für alle Blöcke.
- Hängt eng mit [[0020]]/[[0021]] zusammen (Höhensprung zwischen
  Blöcken, dort bewusst nicht geglättet) und mit der in [[0022]]
  ergänzten Möglichkeit, Übergänge als Sprung oder nahtlos zu
  definieren - ein Sprung-Übergang bräuchte vermutlich einen eigenen
  Trace (z.B. Wurfparabel mit Geschwindigkeitsänderung), während ein
  nahtloser Übergang dem bisherigen Rinnenverlauf folgt.
- Passt konzeptionell zu [[0027]] (`BlockDefinition`, datengetriebenes
  Modell pro Block) - der Trace (Pfad + Geschwindigkeitsprofil) wäre ein
  weiterer, pro Block-Variante konfigurierbarer Aspekt in diesem Sinne.
- Akzeptanzkriterien (grobe erste Fassung):
  - Jede Block-Variante kann ihren eigenen Bewegungs-Trace für die
    Kugel im Kinematic3D-Modus definieren (Positionsverlauf über die
    Blocklänge, nicht nur die bisherige lineare Interpolation).
  - Der Trace kann zusätzlich ein Geschwindigkeitsprofil festlegen
    (Kugel muss nicht mit konstantem Tempo durch den Block laufen).
  - Damit lassen sich Sprünge (z.B. kurze Flugphase mit Beschleunigung/
    Verzögerung) pro Block-Variante simulieren, ohne die Physics3D-Engine
    zu verwenden.

## Notizen
