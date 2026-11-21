---
id: 0069
title: Instrument-Sprites auf Trigger-Zellen in 2D-Planung
type: Feature
priority: Medium
status: Open
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

