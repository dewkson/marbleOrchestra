---
id: 0070
title: Levelabschluss - Winning Screen nach allen Sublevels
type: Feature
priority: Medium
status: Open
area: UI
created: 2026-11-24
---

# 0070 - Levelabschluss - Winning Screen nach allen Sublevels

## Beschreibung

Levelabschluss: Winning Text oder Screen wenn alle Sublevels gelöst worden sind.

## Details

- Betroffene Systeme: `MarbleController` (prüft bereits gültige Bahnen pro
  SubLevel, vgl. `CanPlay` / `HasCompletedTrackInActiveSubLevel`),
  `PathGrid` (SubLevels, `ActiveSubLevelIndex`), SubLevel-Konzept aus 0046.
- Akzeptanzkriterien:
  - Sobald alle SubLevels eines Levels gelöst sind, erscheint ein
    Winning-Text bzw. -Screen.
  - Levels ohne SubLevels: Abschluss, sobald die (einzige) Bahn gelöst ist.

## Notizen

