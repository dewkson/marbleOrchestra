---
id: 0002
title: Randomize-Funktion für Pipes im Level-Editor
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-08-25
---

# 0002 - Randomize-Funktion für Pipes im Level-Editor

## Beschreibung

Der LevelEditor braucht im PipeBereich eine "Randomize" Funktion, die die
aktuell definierten Pipes über die vorhandenen Zellen zufällig verteilt.
Wichtig ist dabei, dass fixierte Pipes nicht randomisiert werden.

## Details

- Betroffen ist vermutlich `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`,
  der Tilemap-Editor für `LevelData` (Toolbar mit "Pipe"/"Content"-Layer,
  Palette aus `availablePipes`, Zellen werden über `level.SetPipeAt(index, ...)`
  gesetzt).
- "Fixiert" entspricht dem bestehenden `locked`-Flag auf `PipeDefinition`
  (`Assets/Scripts/Grid/PipeDefinition.cs:26`, Property `Locked`), das auch
  zur Laufzeit über `PathPipe.IsLocked` ausgewertet wird
  (`Assets/Scripts/Grid/GridInputHandler.cs:29`).
- Akzeptanzkriterien:
  - Neuer Button "Randomize" im Pipe-Bereich des Level-Editor-Fensters.
  - Verteilt die aktuell im Level vorhandenen (nicht-fixierten) Pipes
    zufällig neu auf die Zellen, die aktuell eine nicht-fixierte Pipe
    enthalten.
  - Zellen mit einer Pipe, deren `Locked == true` ist, bleiben unverändert
    (Position und Inhalt).
  - Änderung markiert das Level-Asset als dirty, damit sie gespeichert wird.

## Notizen

Umgesetzt in `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`: Button
"Randomize" neben dem Pipe/Content-Toolbar (nur sichtbar im Pipe-Layer). Die
Methode `RandomizePipes()` sammelt alle nicht-fixierten Zellen (mit Pipe
*oder* leer) als Zielslots und die darin enthaltenen nicht-fixierten Pipes
als zu verteilende Werte, mischt die Zielslots per Fisher-Yates und schreibt
die Pipes über `SetPipeAt` zurück - überzählige Slots werden geleert. Zellen
mit `Locked == true`-Pipe bleiben unangetastet. Änderung wird per
`Undo.RecordObject` erfasst und das Asset über `EditorUtility.SetDirty` als
dirty markiert.

Korrektur: Erste Version hat nur belegte Zellen untereinander getauscht,
freie Zellen wurden nicht als Ziel berücksichtigt - jetzt werden die Pipes
über alle nicht-fixierten Zellen (belegt + leer) verteilt.
