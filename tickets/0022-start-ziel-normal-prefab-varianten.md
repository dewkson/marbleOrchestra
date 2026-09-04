---
id: 0022
title: Block-Varianten - Rollen-Prefabs und wählbarer Sprung-/Nahtlos-Übergang
type: Feature
priority: Medium
status: Open
area: Tooling
created: 2026-08-29
---

# 0022 - Block-Varianten - Rollen-Prefabs und wählbarer Sprung-/Nahtlos-Übergang

## Beschreibung

Für Start-, Ziel- und normale Track-Blöcke sollen eigene Prefab-Varianten
erstellt werden, statt alle mit derselben generischen Konfiguration zu
instanziieren. Aktuell noch nicht relevant/dringend - Ticket dient nur
der Erfassung, keine Umsetzung zum jetzigen Zeitpunkt.

**Ergänzung 2026-09-04:** Aktuell wird von jedem TrackBlock zum nächsten
ein Höhensprung gemacht. Das war so vorgesehen, weil später Instrumente
eingefügt werden sollen, bei denen es Sinn macht, dass die Kugel kleine
Sprünge macht. Allerdings soll nicht jeder Block ein Instrument
triggern. Deshalb soll es auch die Möglichkeit geben, Übergänge zwischen
Trackblocks ohne Höhenunterschied zu erstellen. Dabei soll die
Kugelrinne durchgängig sein und z.B. bei einem Kurven-Block auch
tatsächlich eine Kurve machen (statt eines Sprungs). Allgemein sollen
unterschiedliche Block-Varianten existieren, und es soll pro Verbindung
zwischen zwei Blöcken entscheidbar sein, ob ein Sprung oder ein
nahtloser Übergang stattfindet.

## Details

- Baut auf [[0018]] (universelles `TrackBlock`-Prefab) und [[0017]]
  (`TrackBlockSpawner`, instanziiert aktuell für jede Zelle - Start,
  Ziel und Zwischenzellen - dieselbe Konfiguration) auf.
- Denkbarer Ansatzpunkt für die Rollen-Varianten: Unity Prefab Variants
  von `Assets/Prefabs/TrackBlock.prefab`, oder unterschiedliche
  `IBlockProfile`-Implementierungen/Zusatz-Components je Rolle (Start/
  Ziel/Normal), die der Spawner anhand von `i == 0` /
  `i == path.Count - 1` auswählt.
- Berührt außerdem den bestehenden Höhen-/Übergangsmechanismus aus
  [[0020]] (`TrackBlockSpawner`/`TrackBlock.Height`, Treppen-Modell mit
  `heightDropPerCell`/`tiltFraction`) und [[0021]] (Übergang wird
  bewusst NICHT geglättet, siehe dortige Notiz) - der dort als "gewollt"
  dokumentierte Sprung soll optional bleiben, nicht mehr zwingend.
  Betrifft vermutlich auch [[0027]] (`BlockDefinition`, datengetriebenes
  Modell), da "Sprung vs. nahtlos" naheliegend als Eigenschaft der
  Verbindung zwischen zwei `BlockDefinition`-Einträgen modelliert würde.
- Akzeptanzkriterien (grobe erste Fassung):
  - Es existieren mehrere Block-Varianten (mindestens: Rolle
    Start/Ziel/Normal wie ursprünglich beschrieben, sowie Kurven- vs.
    Geradeaus-Varianten für den Übergangs-Anwendungsfall).
  - Pro Verbindung zwischen zwei benachbarten Blöcken ist wählbar, ob
    ein Höhensprung (bisheriges Verhalten, für spätere
    Instrumenten-Trigger) oder ein nahtloser Übergang ohne
    Höhenunterschied stattfindet.
  - Bei nahtlosem Übergang ist die Kugelrinne durchgängig (kein
    sichtbarer Bruch/Fall), und bei einem Kurven-Block folgt die Rinne
    tatsächlich der Kurve, statt gerade zur nächsten Blockposition zu
    springen.
  - Keine Akzeptanzkriterien für den Prefab-Varianten-Teil final
    ausgearbeitet - explizit erst später relevant, siehe Beschreibung.

## Notizen
