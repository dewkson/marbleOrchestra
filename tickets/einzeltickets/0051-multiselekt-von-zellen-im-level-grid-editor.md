---
id: 0051
title: Multiselekt von Zellen im Level Grid Editor
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-09-28
---

# 0051 - Multiselekt von Zellen im Level Grid Editor

## Beschreibung

multiselekt von Zellen / Karten im GridEditor. Ihc möchte einige
Eigenschaften, wie die TrackBlock-Farbe, Rahmenfarbe, Rahmendicke oder auch
das Sprite direkt für mehrere Zellen gleichzeitig einstellen können,
aktuell muss ich das für jede Zelle / Karte einzeln machen.

## Details

Betroffen: `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`. Aktuell
existiert nur eine Einzelauswahl (`selectedCellIndex: int?`, siehe
`LevelGridEditorWindow.cs:77`) - ein Klick auf eine Zelle wählt genau eine
Zelle, deren Attribute dann im "Selected Cell"-Panel einzeln bearbeitet
werden (`LevelGridEditorWindow.cs:609ff`). Es gibt keinen Mechanismus, um
mehrere Zellen gleichzeitig auszuwählen und Eigenschaften in einem
Rutsch zu setzen.

Hängt inhaltlich mit [0050](0050-separate-farbe-pro-trackblock-im-leveleditor.md)
zusammen (Per-Block-Farbe), da Multiselekt dort besonders beim Setzen der
Farbe für mehrere Blöcke auf einmal nützlich wäre.

Akzeptanzkriterien (grob):
- Mehrere Zellen im Grid-Editor lassen sich gleichzeitig auswählen (z.B.
  per Ctrl/Shift-Klick oder Rahmen-Auswahl).
- Für die Selektion lassen sich Eigenschaften wie TrackBlock-Farbe,
  Rahmenfarbe, Rahmendicke und Sprite gemeinsam auf alle ausgewählten
  Zellen anwenden, statt jede Zelle einzeln bearbeiten zu müssen.

## Notizen

Umgesetzt in `LevelGridEditorWindow.cs`: Shift/Ctrl/Cmd-Klick fügt Zellen
zu einer Multi-Selektion (`selectedCellIndices`) hinzu/entfernt sie; ein
normaler Klick selektiert wie bisher nur eine Zelle. Ab zwei
selektierten Zellen zeigt das "Selected Cell"-Panel ein "Multi
Edit"-Panel mit Apply-Buttons für Background Color, Sprite (Bild) und
Locked - alles Eigenschaften, die schon pro Zelle im Datenmodell
existieren (`PipeDefinition.BackgroundColor`/`CardImage`/`Locked`, sowie
`CardLook` für blockierte Zellen).

Rahmenfarbe und Rahmendicke sind bewusst nicht Teil des Multi-Edit-Panels:
die existieren aktuell nur level-weit (`LevelData.CardBorderColor`/
`CardBorderThickness`, siehe `DrawCardStylePanel`), nicht pro Zelle. Das
würde eine Datenmodell-Erweiterung analog zu
[0050](0050-separate-farbe-pro-trackblock-im-leveleditor.md) voraussetzen
- Nutzer hat sich bewusst dagegen entschieden, das im Rahmen dieses
Tickets mit umzusetzen.
