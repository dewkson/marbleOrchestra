---
id: 0071
title: Farbige Umrandung für Instrumentenfelder
type: Feature
priority: Medium
status: Done
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


- 2026-11-24: Umgesetzt (noch nicht in Unity getestet, Build per dotnet fehlerfrei).
  - `PathGrid` baut pro Instrumentenfeld (Content + Visual Effect vom Typ Color Flash) eine `InstrumentOutline_x_y` aus vier Balken in der Flash-Farbe - an die Zelle gebunden, folgt Sichtbarkeit/Animation des SubLevels wie die Content-Icons.
  - Die Umrandung liegt unter den Karten (Sorting Order -5) und ragt etwas in die Fuge zwischen den Zellen (`instrumentOutlineOutset`) - dieser Teil ist immer voll sichtbar; der innere Teil scheint durch den Kartenrahmen.
  - Kartenrahmen von Pipekarten sind leicht transparent (`cardFrameAlpha`, Standard 0.55; Auswahl-Highlight bleibt deckend, Blocked/Slots unverändert). Alles am `PathGrid` einstellbar.
  - Felder mit Instrument, aber ohne Flash-Effekt bekommen keine Umrandung.
  - 2026-11-24 Nachtrag: Statt Umrandung wird jetzt die ganze Zelle deckend in der Flash-Farbe gefärbt (ein Quadrat unter den Karten, Sorting Order -5, ragt mit `instrumentOutlineOutset` in die Fuge). Kein halbtransparenter Füllbereich über der Karte. Freie Zellen sind komplett farbig; liegt eine Karte darauf, sieht man die Farbe in der Fuge und durch den transparenten Kartenrahmen.
