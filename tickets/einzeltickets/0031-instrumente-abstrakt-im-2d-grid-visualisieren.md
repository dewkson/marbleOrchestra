---
id: 0031
title: Instrumente abstrakt im 2D-Grid visualisieren
type: Idea
priority: Medium
status: Done
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


- 2026-11-22: Umgesetzt im Level Grid Editor (noch nicht in Unity getestet, Build per dotnet fehlerfrei).
  - `CellContentDefinition` hat ein optionales `icon` (Sprite). Zellen mit Content zeigen das Icon zentriert (70 % der Zellgröße); das Buchstaben-Label bleibt oben links, damit Contents mit gleichem Icon (z.B. verschiedene Snare-Clips) unterscheidbar sind. Ohne Icon bleibt alles wie bisher (nur Label).
  - Die Content-Palette zeigt das Icon statt des blauen Farbfelds. Der "Custom Sound Trigger" hat ein Feld "Icon", das neu erzeugte Contents übernehmen.
  - Icons: `Assets/Sprites/Instruments/Instrument_Hihat|Kick|Snare.png` (256x256, transparent), gerendert per Blender aus den in 0068 gelöschten FBX-Modellen (aus der Git-Historie wiederhergestellt, Modelle selbst nicht wieder ins Projekt aufgenommen).
  - Zugewiesen an alle Hat-/Kick-/Snare-Contents unter `Assets/Levels/Contents/`. Xylophon-Contents haben noch kein Icon (kein Bild vorhanden).
  - Laufzeit-2D-Darstellung (`PipeVisual`) ist nicht Teil dieses Tickets, siehe [[0069]] - das neue `Icon` kann dort direkt wiederverwendet werden.
