---
id: 0067
title: Visuelles Feedback und Animation fürs Schieben
type: Feature
priority: Low
status: Open
area: UI
created: 2026-11-20
---

# 0067 - Visuelles Feedback und Animation fürs Schieben

## Beschreibung

Das Schieben von Pipekarten im 15-Puzzle-Modus soll in 2D (inkl. Mobile-Layout) sichtbar und verständlich dargestellt werden. (Teil von 0063)

## Details

- Freie Zellen im Spiel erkennbar darstellen (z.B. leerer Slot).
- Karte gleitet animiert auf die freie Zelle; ggf. Hervorhebung verschiebbarer Karten.
- Abhängig von 0066.

## Notizen

- 2026-11-20: Leerer Slot unter jeder bebaubaren Zelle (`PipeVisual.RefreshFreeSlot`), Karten gleiten animiert (`PathPipe.SlideTo`, `PathGrid.slideDuration`), beim Hover/Drag werden alle Karten hervorgehoben, die sich mitbewegen würden. Swap-Sound wird auch beim Schieben abgespielt.
- 2026-11-20: Feedback umgesetzt: kein freies Draggen mehr. Die Wischrichtung wird erkannt (achsenfest, Schwelle `slideDragThresholdCells`) und löst dieselbe kontrollierte Schiebeanimation wie ein Klick aus. Hover-Hervorhebung nur noch bei eindeutiger Klickrichtung.
