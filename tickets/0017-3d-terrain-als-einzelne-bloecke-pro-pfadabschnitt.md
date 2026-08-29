---
id: 0017
title: 3D-Terrain als einzelne Blöcke pro Pfadabschnitt
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-08-29
---

# 0017 - 3D-Terrain als einzelne Blöcke pro Pfadabschnitt

## Beschreibung

Das aktuell durchgängige 3D-Terrain soll durch einzelne, klar voneinander
abgegrenzte Blöcke ersetzt werden. Jeder Block entspricht einem Abschnitt
des in der 2D-Planung definierten Pfades. Die bestehende 2D→3D-Pipeline
soll weiterhin automatisch aus dem geplanten Pfad die benötigten Blöcke
erzeugen.

## Details

Betroffenes System:
- `Assets/Scripts/Grid/TrackTerrainGenerator.cs` - erzeugt aktuell pro
  Bahn ein durchgehendes Terrain-Mesh (schmale Rail mit seitlichen
  Schultern, siehe Notizen in [[0013]]). Muss so umgebaut/erweitert
  werden, dass statt eines einzigen zusammenhängenden Meshes pro Bahn
  mehrere separate, klar abgegrenzte Block-Meshes entstehen - ein Block
  je Pfadabschnitt.
- Was genau einen "Abschnitt" des Pfades abgrenzt (z.B. pro Grid-Zelle,
  pro Pipe-Segment, pro geradem Teilstück zwischen Richtungswechseln)
  geht aus der Beschreibung nicht hervor und muss geklärt/entschieden
  werden.
- Die automatische Erzeugung aus dem geplanten Pfad (2D→3D-Pipeline)
  bleibt bestehen, nur die Struktur des Outputs ändert sich von einer
  durchgehenden Fläche zu einzelnen Blöcken.

## Notizen

- 2026-08-29: Block-Granularität geklärt (mit User abgestimmt): ein Block
  = eine Grid-Zelle, direkt aus `PathValidationResult.OrderedPath`.
- 2026-08-29: Phase 2 aus dem Implementierungsplan umgesetzt.
  `TrackTerrainGenerator.cs` zu `Assets/Scripts/Grid/
  TrackBlockSpawner.cs` umbenannt (git mv, `.meta`-GUID erhalten) und
  neu aufgebaut: instanziert pro Pfadzelle ein `TrackBlock`-Prefab
  ([[0018]]) mit `GrooveBlockProfile` statt eines einzigen
  durchgehenden Ribbon-Meshes. Diffing pro Bahn (nicht global), damit
  ein Pipe-Swap auf einer Bahn nicht alle anderen neu baut.
  `MarbleController` auf den neuen Typ umgestellt (Feld/Signaturen
  unverändert). Blöcke sind aktuell noch flach/unrotiert (Höhe/Neigung
  folgt in [[0019]]/[[0020]]); `SampleGroovePosition`/
  `GetShoulderWorldPosition` sind bewusst nur ein flacher Platzhalter,
  der in [[0021]] durch echte Verkettung über `EntryPointLocal`/
  `ExitPointLocal` ersetzt wird. Noch nicht im Editor getestet.
  Aufräum-Hinweis: Die GameObject "TrackBlockSpawner" (vormals
  "TrackTerrain") in der Szene trägt noch ungenutzte MeshFilter/
  MeshRenderer/MeshCollider-Komponenten vom alten Ribbon-Ansatz - rein
  kosmetisch, kann bei Gelegenheit im Editor entfernt werden.
- 2026-08-29: `sideWidth` als eigenes Feld entfernt - Blöcke haben jetzt
  eine quadratische Grundfläche (Breite = Länge = `grid.CellSize`);
  `SideWidth` wird automatisch aus `grooveRadius` abgeleitet
  (`grid.CellSize/2 - grooveRadius`), `grooveRadius` wird dafür auf
  maximal `grid.CellSize/2` gedeckelt.

