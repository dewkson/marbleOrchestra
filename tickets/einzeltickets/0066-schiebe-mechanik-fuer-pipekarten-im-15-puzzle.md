---
id: 0066
title: Schiebe-Mechanik für Pipekarten im 15-Puzzle
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-11-20
---

# 0066 - Schiebe-Mechanik für Pipekarten im 15-Puzzle

## Beschreibung

Pipekarten (Nachbarn) sollen im 15-Puzzle-Modus auf eine angrenzende freie Zelle geschoben werden können. (Teil von 0063)

## Details

- Eingabe in `GridInputHandler`: Karte antippen/ziehen, die eine freie Nachbarzelle hat, verschiebt sie dorthin; die vorherige Zelle wird frei.
- Gesperrte (locked) Karten und nicht bebaubare Zellen dürfen nicht verschoben werden bzw. nicht Ziel sein.
- Murmelbahn/Pfadberechnung (`PathGrid`) muss mit freien Zellen umgehen können (Bahn bricht an freier Zelle ab).
- Der Card-Swap-Modus bleibt unverändert funktionsfähig.

## Notizen

- 2026-11-20: Regel in `FifteenPuzzleRules` (von Laufzeit und Editor geteilt). `PathGrid` baut für freie Zellen keine PathPipe und bietet `TryGetSlide`/`SlideCards` (begrenzt auf das eigene SubLevel). `GridInputHandler`: Klick schiebt zur nächsten freien Zelle, Drag wählt die Richtung. Card Swap unverändert.
- 2026-11-20: Feedback umgesetzt: Klick schiebt nur noch, wenn genau eine Richtung möglich ist (`FifteenPuzzleRules.TryGetOnlySlide`); sonst nur per Wischen.
