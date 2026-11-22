---
id: 0068
title: Eigener Visuals-Reiter mit Trackblock-Farbflash im Grid Editor
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-11-21
---

# 0068 - Eigener Visuals-Reiter mit Trackblock-Farbflash im Grid Editor

## Beschreibung

Ich möchte gerne die visuellen Effekte von den Instrumententriggern trennen im Grid Editor. Also grundsätzlich soll es so bleiben, dass Instrumente visuelle Effekte triggern können, dies soll aber entkoppelt werden und dafür hätte ich dann gerne einen dritten Reiter im Editor, in dem ich nur die visuellen Effekte steuern kann. Für den Anfang möchte ich gerne weg davon gehen, dass festgelegte 3D Modelle (wie bei den Drums oder Xylofonen) angezeigt werden und dann auch noch kurz einen scale up bekommen. Stattdessen möchte ich lieber mit den Farben der TrackBlocks spielen und hier mehr Animationseffekte erstellen können. Beispiel: Bisher ist es so, dass ich InstrumentVisuals gepflegt habe. Für einen Drumkick-Sound wird dann ein 3D Modell an der entsprechenden Zelle angezeigt und dann beim triggern wird es kurz vergrößert. Stattdessen hätte ich gerne die Möglichkeit zunächst einmal die Basisfarbe der Trackblocks zu definieren und beim triggern der entsprechenden Zelle sollen dann alle Trackblocks vom Sublevel in einer definierten Farbe aufblitzen und mit definierbarer Animationslänge wieder zurück zur Basisfarbe lerpen. Später sollen noch weitere Animationseffekte eingestellt werden können, aber diese erste würde mir schon sehr helfen. In diesem Reiter für die Visuals würde ich dann auch die schon existierenden Funktionen zur Farbdefinition der Trackblocks platzieren und aus dem Pipe-Reiter entsprechend entfernen.

## Details

Betroffene Systeme (Stand Code):
- `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` - Layer-Toolbar aktuell `{ "Pipe", "Content" }` (`PaintLayer`); Trackblock-Farbfelder (Block Color Override, Multi-Edit Background/Block Color) liegen derzeit im Pipe-Bereich.
- `InstrumentVisual.cs`, `InstrumentBlockDecoration.cs`, `TriggerCellContent.cs` - bisherige 3D-Modell-Visualisierung mit Scale-Up beim Trigger, plus Flash-Color am Content.
- `LevelData.cs` (`BlockColorOverrides`), `TrackBlockSpawner.cs` - Blockfarben.

Akzeptanzkriterien:
- Dritter Reiter "Visuals" im Level Grid Editor neben Pipe und Content.
- Visuelle Effekte sind von den Instrumenten-/Sound-Triggern entkoppelt definierbar; Trigger einer Zelle können weiterhin einen visuellen Effekt auslösen.
- Basisfarbe der Trackblocks im Visuals-Reiter einstellbar.
- Erster Effekt "Farbflash": beim Trigger der Zelle blitzen alle Trackblocks des Sublevels in einer definierbaren Farbe auf und lerpen in definierbarer Dauer zurück zur Basisfarbe.
- Bestehende Funktionen zur Trackblock-Farbdefinition werden in den Visuals-Reiter verschoben und aus dem Pipe-Reiter entfernt.
- Fest platzierte 3D-Instrumentenmodelle mit Scale-Up werden für diesen Ansatz nicht mehr benötigt.
- Struktur erweiterbar für spätere zusätzliche Animationseffekte.

## Notizen

- 2026-11-21: Umgesetzt (noch nicht in Unity getestet, Build per dotnet fehlerfrei).
  - Neuer Reiter "Visuals" im Level Grid Editor: Toggle "Instrument-Animation", Basisfarbe pro SubLevel (+ "Ohne SubLevel"/"Ganzes Grid"), Werkzeuge Auswählen / Effekt malen / Radierer, Einzel- und Multi-Edit für Block Color und Effekt. Farbstreifen unten in jeder Zelle zeigt die aufgelöste 3D-Blockfarbe, Badge unten rechts markiert Zellen mit Effekt.
  - Block Color (Einzelzelle + Multi-Edit) aus dem Pipe-Reiter entfernt; Card Background Color bleibt dort (Kartenfarbe).
  - Daten: `CellVisualEffect`/`VisualEffectType` (erster Effekt `SubLevelColorFlash`: Farbe + Dauer), `BlockColorSettings` an `SubLevelDefinition` und `LevelData`, `LevelData.instrumentHitAnimation`.
  - Runtime: `BlockVisualEffectSystem` (wird vom TrackBlockSpawner automatisch hinzugefügt) hört auf `BlockTrigger.AnyFired`, färbt alle Blöcke des SubLevels per MaterialPropertyBlock und lerpt zurück. Zellen mit Effekt triggern auch ohne Content (außer Start/Ziel).
  - Farbpriorität: Block Color einer Zelle ohne Karte > SubLevel-Basisfarbe > Kartenfarbe > Terrain-Farbe des Spawners.
  - Instrument-Pads (Leiste/Drum/Modell) bleiben als Landefläche bestehen; nur Flash/Scale-Up/Hit-Clip sind abschaltbar.
- 2026-11-21: Nachtrag.
  - Color Flash hat die Option "Blocked Cells ausschließen" (Standard an): Füllblöcke blockierter Zellen blitzen nicht mit.
  - Content ist jetzt reiner Sound: `SoundTriggerContent.flashColor`, `TriggerCellContent.visual`, `ITriggerCellContent.Visual` und `BlockDefinition.FlashColor` entfernt. Externe 3D-Modelle (`InstrumentVisual`, Hit-Clips, Modell-Pfad in `InstrumentBlockDecoration`/`InstrumentElement`/`InstrumentPadFeedback`) gelöscht. Im Custom Sound Trigger bleibt nur Clip + "Instrument (Pad)" (die Landefläche der Murmel). Der Pad-Flash nutzt nur noch die Farbe am TrackBlock-Prefab.
  - Gelöscht: `Levels/Contents/InstrumentVisual_*.asset`, `Assets/Models/` (Hihat/Kick/Snare/Xylo.fbx) und `Assets/Prefabs/Instruments/` (Kick/Snare.prefab) - wurden nur von den InstrumentVisual-Assets verwendet.
- 2026-11-22: Nachtrag - zweiter Effekt `BlockColorFlash`: gleicher Farbflash (Farbe + Dauer), aber nur der/die Block(e) der auslösenden Zelle. Im Visuals-Reiter als Effekt wählbar (Pinsel, Einzelzelle, Multi-Edit); "Blocked Cells ausschließen" nur beim SubLevel-Flash. Badge in der Zelle: weißer Rand = Einzelblock, schwarzer Rand = SubLevel.
- 2026-11-22: Nachtrag - `SubLevelColorFlash` färbt nur noch Blöcke mit Rille (echte Tracks + Preview-Blöcke von Karten), nicht mehr das Filler-Terrain freier/blockierter Zellen (`TrackBlockSpawner.CollectBlocksInArea(..., includeFiller: false)`). Die Option "Blocked Cells ausschließen" (`excludeBlockedCells`) ist damit überflüssig und entfernt; alte Werte in Level-Assets werden von Unity ignoriert.
- 2026-11-22: Nachtrag - Instrument-Pads (Xylofon-Leiste, Trommel, Hi-Hat) sind jetzt über `TrackBlockSpawner.showInstrumentPads` abschaltbar, Standard aus. Ohne Pad fällt die Murmel direkt in die Rille (Landepunkt = Groove-Punkt, an dem sonst der Bounce endet), der Ton kommt weiterhin bei `ImpactT`; Trigger-Blöcke bekommen dann wie normale Blöcke Terrain-Deko. Mindest-FallHeight ist ohne Pad 0 (vorher Rillenradius + Pad-Höhe), dadurch können Fall-Stufen niedriger ausfallen als bisher. Der Toggle "Instrument-Animation" im Visuals-Reiter wirkt nur bei eingeschalteten Pads.
- 2026-11-22: Nachtrag - Laufbahn ohne Pad: neue `GrooveDropMarbleTrace`. Die Kugel behält in der Luft ihr Rolltempo (Position entlang der Rillen-Mittellinie = t, auch in Kurven), fällt mit Startgeschwindigkeit 0 nach unten (Wurfparabel) und hüpft danach abklingend nach - gleiche Schwerkraft wie der Fall, Hüpferhöhe = `triggerBounceRestitution` x vorherige Höhe (Standard 0.2, max. `triggerMaxBounces` = 2). Fallzeit ohne Pad über eigenes Feld `triggerDropBeatFraction` (Standard 0.35), da `triggerFallBeatFraction` in der Szene (0.144) auf das Pad abgestimmt ist. Mit Pads bleibt `TriggerFallMarbleTrace` unverändert.
