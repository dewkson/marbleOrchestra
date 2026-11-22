---
id: 0069
title: Instrument-Sprites auf Trigger-Zellen in 2D-Planung
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-11-21
---

# 0069 - Instrument-Sprites auf Trigger-Zellen in 2D-Planung

## Beschreibung

In der 2D Planungsphase sollen auf denjenigen Zellen, wo Instrumente getriggert werden, 2D Sprites der jeweiligen Instrumente abgebildet werden.

## Details

- Betroffene Systeme: `Assets/Scripts/Grid/PipeVisual.cs` (2D-Kartendarstellung zur Laufzeit), `Assets/Scripts/Grid/SoundTriggerContent.cs` / `CellContentDefinition.cs` (haben aktuell nur `Label`, kein Sprite pro Instrument).
- Verwandt: [[0031]] (abstrakte Instrument-Visualisierung im 2D-Grid, vage Idee).
- Akzeptanzkriterien:
  - Zellen mit Instrument-Trigger zeigen in der 2D-Planungsphase ein Sprite des jeweiligen Instruments.
  - Zellen ohne Trigger bleiben unverändert.

## Notizen


- 2026-11-22: Umgesetzt (noch nicht in Unity getestet, Build per dotnet fehlerfrei).
  - Nutzt das `Icon` aus [[0031]] (`CellContentDefinition.Icon`). `PathGrid` baut pro Content-Zelle mit Icon einen eigenen `ContentIcon_x_y`-SpriteRenderer - an die Zelle gebunden wie der Content selbst, nicht an die Karte, bleibt also beim Tauschen/Schieben stehen.
  - Einstellbar am `PathGrid`: Größe relativ zur Karte (Standard 0.55), Versatz von der Kartenmitte, Tint, Sorting Order (Standard 10: über allen Karten-Renderern, unter einer gezogenen Karte).
  - Sichtbarkeit folgt dem aktiven SubLevel wie die Karten; beim Play-Wechsel in 3D verschwindet es mit dem ganzen Grid.
  - Nachtrag: Icon ist nur auf Zellen ohne Pipekarte voll sichtbar; liegt eine Karte darauf, wird es mit `coveredContentIconAlpha` (Standard 0.85, vorher 0.3 bzw. 0.6) leicht transparent gezeigt. Aktualisiert nach jedem Tauschen/Schieben (über `Revalidate`). Eine leere Karte ohne PipeDefinition zählt nicht als Abdeckung.
