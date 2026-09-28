---
id: 0050
title: Separate Farbe pro Trackblock im Level-Editor
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-09-28
---

# 0050 - Separate Farbe pro Trackblock im Level-Editor

## Beschreibung

separate Trackblock Farben: Ich möchte im LevelEditor gerne die Möglichkeit
haben, die Trackblock farbe für spezifische Trackblocks einzustellen.
Aktuell gibt es eine global eingestellte Farbe für alle Trackblocks.

## Details

Betroffen: `Assets/Scripts/Grid/TrackBlockSpawner.cs`. Aktuell sind
`terrainColor` und `grooveColor` (sowie `tunnelColor`) globale
`[SerializeField]`-Werte auf dem Spawner, aus denen in `Awake()` je ein
einziges Shared Material (`sharedMaterial`, `sharedGrooveMaterial`,
`sharedTunnelMaterial`) erzeugt und für alle Blöcke gleichermaßen verwendet
wird (`TrackBlockSpawner.cs:80-82`, `:116-118`, `:1004`). Es gibt keinen
Mechanismus, einem einzelnen Block eine abweichende Farbe zuzuweisen.

Akzeptanzkriterien (grob):
- Im Level-Editor lässt sich für einzelne Trackblocks eine individuelle
  Farbe setzen, die von der globalen Default-Farbe abweicht.
- Blöcke ohne explizite Einstellung nutzen weiterhin die globale
  Default-Farbe.
- Offen/zu klären in der Umsetzung: ob dafür weiterhin Shared Materials
  pro Farbe sinnvoll sind oder auf MaterialPropertyBlocks pro Block
  umgestellt werden muss, und ob die Farbe pro Block im Leveldaten-Modell
  (analog zu Sound/FlashColor, siehe 0028) persistiert wird.

## Notizen

Umgesetzt: neuer per-Zelle Farb-Override, persistiert im Leveldaten-Modell
(analog zu HeightOverrides/BlockedLooks): `LevelData.blockColorOverrides`
(`List<ColorOverride>`, null = kein Override), erreichbar über
`GetBlockColorOverrideAt`/`SetBlockColorOverrideAt` und `PathGrid.
GetBlockColorOverride(coord)`.

`TrackBlockSpawner.ResolveBlockMaterial(cell)` liefert für eine Zelle mit
Override ein gecachtes Material für genau diese Farbe (ein Material pro
distinkter Override-Farbe statt pro Zelle, damit Zellen mit derselben
Farbe weiterhin dasselbe Material/Batching teilen - siehe
`overrideMaterialCache`), sonst das bisherige globale `sharedMaterial`.
Angewendet auf den Block-Körper selbst (Track- und Filler-Blöcke), die
Start/Goal-Tunnelwand und die Basis des Xylophon-Pads bei Trigger-Blöcken.
Rillenfarbe (`grooveColor`) und Tunnel-Innenfarbe (`tunnelColor`) bleiben
bewusst global/nicht überschreibbar - Ticket verlangte nur "eine
individuelle Farbe" pro Block.

Im Level-Editor einstellbar über ein neues "Custom Block Color"-Feld im
"Selected Cell"-Panel (für Pipe- und blockierte Zellen) sowie im
Multi-Edit-Panel aus [0051](0051-multiselekt-von-zellen-im-level-grid-editor.md)
("Block Color (3D)" mit Apply/Clear-Button, gilt für alle ausgewählten
Zellen unabhängig davon ob Pipe oder blockiert).
