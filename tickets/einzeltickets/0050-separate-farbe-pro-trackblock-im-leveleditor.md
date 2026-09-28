---
id: 0050
title: Separate Farbe pro Trackblock im Level-Editor
type: Feature
priority: Medium
status: Open
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
