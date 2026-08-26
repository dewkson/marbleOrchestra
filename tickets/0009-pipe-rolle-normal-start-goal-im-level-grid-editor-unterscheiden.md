---
id: 0009
title: Pipe-Rolle (Normal/Start/Goal) im Level Grid Editor unterscheiden
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-08-26
---

# 0009 - Pipe-Rolle (Normal/Start/Goal) im Level Grid Editor unterscheiden

## Beschreibung

Der LevelEditor sollte auch unterscheiden können, ob eine Pipe-Karte eine
normale Karte ist oder Start oder Goal.

## Details

- Betroffen: `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`,
  Methode `DrawCell` - zeichnet aktuell keine Information zu
  `PipeDefinition.Role`.
- `Assets/Scripts/Grid/PipeDefinition.cs` hat das bestehende `Role`-Feld
  (`enum PipeRole { Normal, Start, Goal }`, Property `Role`), das u.a. in
  `LevelData.OnValidate` bereits zur Prüfung auf genau 1 Start-/Goal-Pipe
  verwendet wird.
- Akzeptanzkriterien:
  - Zellen mit einer Pipe, deren `Role == Start` bzw. `Role == Goal` ist,
    sind im Level-Grid-Editor-Fenster optisch klar erkennbar und von
    `Role == Normal` unterscheidbar (z.B. Label/Icon/Rahmenfarbe).
  - `Role == Normal` zeigt keinen zusätzlichen Hinweis.

## Notizen

Umgesetzt in `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`: neue
Methode `DrawRoleBadge`, aufgerufen aus `DrawCell` wenn `pipe.Role !=
PipeRole.Normal`. Zeigt oben rechts in der Zelle ein farbiges Badge mit
Buchstabe - "S" (grün) für Start, "G" (gold) für Goal. `Role == Normal`
zeigt weiterhin kein zusätzliches Badge.
