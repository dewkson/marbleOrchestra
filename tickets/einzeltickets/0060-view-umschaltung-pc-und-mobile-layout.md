---
id: 0060
title: View-Umschaltung zwischen PC- und Mobile-Layout
type: Feature
priority: Medium
status: Done
area: UI
created: 2026-10-19
---

# 0060 - View-Umschaltung zwischen PC- und Mobile-Layout

## Beschreibung

ich hätte gerne die Option den View zwischen PC und Mobile zu switchen. Damit meine ich nicht einfach nur die Editor-EInstellung der AUflösung, sondern auch, dass die Trennung des Gameviews nicht links rechts, sondern oben unten ist

## Details

- Aktuell ist das Layout fest als Links/Rechts-Split verdrahtet: `GameViewLayout` (`Assets/Scripts/Grid/GameViewLayout.cs`) setzt `MainViewFraction = 3/4`, `mainCamera.rect = (0, 0, 0.75, 1)` und `IsInMainView` prüft nur `screenPos.x`. Die Side-Column (480 Referenzpixel) liegt rechts.
- Betroffen sind vermutlich auch `MinimapCamera.cs`, `CameraFitter.cs` und `CameraModeTransition.cs` (nutzen Aspect/Camera-Rect bzw. Layout).
- Akzeptanzkriterien:
  - Umschaltbare Layout-Option PC / Mobile (nicht nur Editor-Auflösung).
  - PC: bisheriger Split links (Hauptansicht) / rechts (Side-Column).
  - Mobile: Split oben / unten (Hauptansicht und Side-Column/Minimap/Buttons vertikal getrennt).
  - Kamera-Rects, Input-Hit-Tests (`IsInMainView`), Canvas-Skalierung und Kamera-Fitting passen sich an das gewählte Layout an.

## Notizen

