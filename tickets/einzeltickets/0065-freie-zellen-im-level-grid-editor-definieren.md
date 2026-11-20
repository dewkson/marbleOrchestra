---
id: 0065
title: Freie Zellen im Level Grid Editor definieren (15-Puzzle)
type: Feature
priority: Medium
status: Open
area: Level Editor
created: 2026-11-20
---

# 0065 - Freie Zellen im Level Grid Editor definieren (15-Puzzle)

## Beschreibung

Im 15-Puzzle-Modus soll es neben den Pipekarten auch freie Zellen geben, auf die Nachbarkarten geschoben werden können. (Teil von 0063)

## Details

- Im Reiter "15-puzzle" des Level Grid Editors können Zellen als "frei" (leer, ohne Pipekarte) markiert werden.
- Speicherung in `LevelData`; Abgrenzung zu nicht bebaubaren Zellen (0045) beachten: freie Zellen sind bebaubar, nur aktuell unbesetzt.
- Randomize (0002) soll im 15-Puzzle-Modus freie Zellen berücksichtigen.

## Notizen

- 2026-11-20: Kein eigener Marker - freie Zelle = nicht blockierte Zelle ohne Karte (`LevelData.IsFreeAt`/`CountFreeCellsIn`). Editor zeigt sie im 15-Puzzle-Modus als "frei", der 15-Puzzle-Bereich listet freie Zellen pro SubLevel und warnt bei 0. Randomize mischt per Zufallszügen (`RandomizeBySlides`), "Block Empty Cells" fragt im 15-Puzzle-Modus nach.
