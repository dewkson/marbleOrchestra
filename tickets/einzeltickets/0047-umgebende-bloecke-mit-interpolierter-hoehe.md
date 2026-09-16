---
id: 0047
title: Umgebende Blöcke mit interpolierter Höhe
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-09-16
---

# 0047 - Umgebende Blöcke mit interpolierter Höhe

## Beschreibung

Surrounding Blocks. Es sollen nicht nur die relevanten Murmelbahnen als
Blöcke dargestellt werden, sondern diese sollen auch von benachbarten
Blöcken umgeben sein (Ohne Murmelbahn). Sprich: Diejenigen Blöcke, die um
die Sublevels drumherum liegen und auch die nicht "bebaubaren" (Ticket 45)
Blöcke sollen visualisiert werden und unterschiedliche Höhen abbilden.
Dabei soll sich die Höhe dieser Blöcke, die keine Murmelbahn enthalten,
daran orientieren wie hoch die benachbarten Blöcke sind. Wenn zum Beispiel
ein nicht Murmelbahn Block einen rechten und linken Nachbarn hat, dann soll
er sich von der Höhe genau dazwischen befinden. Wenn solch ein Block nur
einen direkten Nachbarn mit Murmelbahn hat, dann soll er auch den nächsten
Nachbarn in die gleiche Richtung berücksichtigen. Ist ein stetiges Gefälle
in Richtung der Nachbarn vorhanden, soll dieser Block entsprechend auch
steigen. Mit diesem Vorgehen soll die Murmelbahn in eine Umgebung gesetzt
werden, die die Höhenunterschiede realistisch fortsetzt und so das Terrain
definiert.

## Details

Betroffene Dateien/Systeme:

- `Assets/Scripts/Grid/TrackBlockSpawner.cs` - spawnt aktuell ausschließlich
  Blöcke entlang der validierten Murmelbahn (Start→Goal, siehe
  `runningExitY`/`height`-Kette ab ca. Zeile 348). Höhe wird nur pro
  Track-Block aus dem vorherigen Bahn-Block abgeleitet (0039). Für
  umgebende Blöcke ohne Murmelbahn braucht es eine neue Spawn- und
  Höhenlogik, die stattdessen von den Höhen der Nachbar-Zellen im Grid
  ausgeht statt von einer Bahn-Kette.
- `Assets/Scripts/Grid/TrackBlock.cs` - hält `Height`/`BottomY` pro Block;
  die gleiche Struktur müsste auch für Nicht-Bahn-Blöcke nutzbar sein.
- `Assets/Scripts/Grid/PathGrid.cs` - kennt Grid-Ausdehnung
  (`Width`/`Height`) und belegte/unbelegte Zellen; liefert die Grundlage,
  um zu bestimmen, welche Zellen "umgebend" (kein Pipe/keine Bahn) sind.
- Ticket [[0045-nicht-bebaubare-zellen-fuer-variable-grid-formen]]: die
  dort eingeführte Markierung nicht bebaubarer Zellen ist laut
  Beschreibung Teil der hier zu visualisierenden Umgebung.
- Ticket [[0046-sublevels-auf-einem-grid-definieren]]: "Blöcke, die um die
  Sublevels drumherum liegen" setzt voraus, dass Sublevel-Bereiche im Grid
  bekannt sind.
- `Assets/Scripts/Grid/TerrainDecoration.cs` /
  `TerrainDecorationSettings.cs` - bestehendes Muster für rein kosmetische,
  pro Block deterministische Zusatz-Geometrie; ggf. Vorbild für die
  visuelle Gestaltung der umgebenden Blöcke.

Akzeptanzkriterien (aus der Beschreibung abgeleitet):

- Zellen ohne Murmelbahn (umliegend oder nicht bebaubar) werden ebenfalls
  als Block dargestellt, nicht nur die Bahn-Zellen.
- Die Höhe eines solchen Blocks orientiert sich an den Höhen seiner
  direkten Nachbarn: bei zwei gegenüberliegenden Nachbarn liegt er genau
  dazwischen.
- Hat ein solcher Block nur in eine Richtung einen direkten
  Bahn-Nachbarn, wird zusätzlich dessen übernächster Nachbar in derselben
  Richtung einbezogen, um ein Gefälle fortzusetzen statt abzuflachen.
- Bei einem durchgehenden Gefälle über mehrere Nachbarn hinweg setzt sich
  dieses Gefälle in den umgebenden Blöcken sichtbar fort (steigt/fällt
  entsprechend), statt abrupt auf eine mittlere Höhe zu springen.

Offene Frage für die Umsetzung: das genaue Interpolations-/Extrapolations-
verfahren für Fälle mit mehr als zwei relevanten Nachbarn oder ganz ohne
direkten Bahn-Nachbarn ist aus der Beschreibung nicht eindeutig ableitbar
und müsste beim Umsetzen festgelegt werden.

## Notizen
