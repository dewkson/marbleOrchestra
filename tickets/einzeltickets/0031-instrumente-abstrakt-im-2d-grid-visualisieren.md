---
id: 0031
title: Instrumente abstrakt im 2D-Grid visualisieren
type: Idea
priority: Medium
status: Open
area: Level Editor
created: 2026-09-04
---

# 0031 - Instrumente abstrakt im 2D-Grid visualisieren

## Beschreibung

Instrumente abstrakt in 2D Grid visualisieren

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` (`DrawCell`) -
  zeigt Content-Marker (Sound-Trigger-Instrumente wie Kick/Snare/Hat)
  aktuell nur als einzelner Buchstabe oben links in der Zelle an
  (siehe [[0006]]).
- `Assets/Scripts/Grid/CellContentDefinition.cs` - Basisklasse aller
  Content-Typen, hat aktuell nur `Label` (Buchstabe/Kurz-Label), keine
  Form/Icon/Farbe pro Instrument über die reine Textdarstellung hinaus.
- `Assets/Scripts/Grid/SoundTriggerContent.cs` - konkrete Instrument-Assets
  (Clip + FlashColor), siehe auch [[0028]] (Direkteingabe im Editor statt
  Asset-Umweg).

Beschreibung ist vage ("abstrakt visualisieren") - unklar ist noch, ob
damit eine Erweiterung der bestehenden Buchstaben-Marker (z.B. um Form/Icon
pro Instrument) gemeint ist, ob das auch die Laufzeit-2D-Darstellung
(vgl. [[0030]], `PipeVisual.cs`) betreffen soll, oder ob eine komplett neue
Darstellungsart gewünscht ist. Konkrete Gestaltungswünsche müssen noch
geklärt werden.

## Notizen

