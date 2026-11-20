---
id: 0064
title: Spielmodus-Auswahl (Card Swap / 15-Puzzle) im Level Grid Editor
type: Feature
priority: Medium
status: Open
area: Level Editor
created: 2026-11-20
---

# 0064 - Spielmodus-Auswahl (Card Swap / 15-Puzzle) im Level Grid Editor

## Beschreibung

Im Level Grid Editor soll ein neuer Bereich/Reiter für den Spielmodus angelegt werden. Der bisherige Modus soll "Card Swap" heißen, der neue "15-puzzle". (Teil von 0063)

## Details

- Neuer Reiter/Bereich in `LevelGridEditorWindow` zur Wahl des Spielmodus pro Level.
- Der Modus wird in `LevelData` gespeichert (Default: Card Swap, damit bestehende Levels unverändert bleiben).
- Bestehender Swap-Modus wird überall (UI, Code-Bezeichner, Doku) als "Card Swap" benannt.

## Notizen

- 2026-11-20: Neues Enum `GameMode` (`CardSwap`, `FifteenPuzzle`) in `GameMode.cs`, Feld `gameMode` in `LevelData` (Default Card Swap). Reiter "Card Swap | 15-Puzzle" oben im Level Grid Editor (`DrawGameModeTabs`) mit modusspezifischem Bereich.
