---
id: 0022
title: Prefab-Varianten für Start-, Ziel- und Normal-Blöcke
type: Task
priority: Low
status: Open
area: Tooling
created: 2026-08-29
---

# 0022 - Prefab-Varianten für Start-, Ziel- und Normal-Blöcke

## Beschreibung

Für Start-, Ziel- und normale Track-Blöcke sollen eigene Prefab-Varianten
erstellt werden, statt alle mit derselben generischen Konfiguration zu
instanziieren. Aktuell noch nicht relevant/dringend - Ticket dient nur
der Erfassung, keine Umsetzung zum jetzigen Zeitpunkt.

## Details

- Baut auf [[0018]] (universelles `TrackBlock`-Prefab) und [[0017]]
  (`TrackBlockSpawner`, instanziiert aktuell für jede Zelle - Start,
  Ziel und Zwischenzellen - dieselbe Konfiguration) auf.
- Denkbarer Ansatzpunkt: Unity Prefab Variants von
  `Assets/Prefabs/TrackBlock.prefab`, oder unterschiedliche
  `IBlockProfile`-Implementierungen/Zusatz-Components je Rolle (Start/
  Ziel/Normal), die der Spawner anhand von `i == 0` /
  `i == path.Count - 1` auswählt.
- Keine Akzeptanzkriterien/Details ausgearbeitet - explizit erst später
  relevant, siehe Beschreibung.

## Notizen
