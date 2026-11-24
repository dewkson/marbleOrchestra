---
id: 0071
title: Farbige Umrandung für Instrumentenfelder
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-11-24
---

# 0071 - Farbige Umrandung für Instrumentenfelder

## Beschreibung

Instrumentenfelder mit entsprechender farblichen Umrandung (entsprechend der jeweils eingestellten flash Farbe) anzeigen. Wenn Karten darüberliegen, muss die farbliche Umrandung noch erkennbar sein. Daher die Ränder der Pipekarte mit leichter Transparenz versehen

## Details

- Betroffene Systeme: `Assets/Scripts/Grid/PathGrid.cs` (baut pro Content-Zelle bereits die `ContentIcon_x_y`-Sprites aus [[0069]], an die Zelle gebunden), `Assets/Scripts/Grid/CellVisualEffect.cs` (`FlashColor` pro Zelle), `Assets/Scripts/Grid/PipeVisual.cs` (2D-Kartendarstellung).
- Verwandt: [[0068]] (Visuals-Reiter / Farbflash), [[0069]] (Instrument-Sprites).
- Akzeptanzkriterien:
  - Instrumentenfelder zeigen in der 2D-Planung eine Umrandung in der jeweils eingestellten Flash-Farbe.
  - Die Umrandung bleibt an der Zelle (nicht an der Karte), also auch beim Tauschen/Schieben.
  - Liegt eine Pipekarte auf dem Feld, ist die Umrandung weiterhin erkennbar - die Ränder der Pipekarte sind dafür leicht transparent.

## Notizen

