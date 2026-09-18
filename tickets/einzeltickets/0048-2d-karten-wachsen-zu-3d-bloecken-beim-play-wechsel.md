---
id: 0048
title: 2D-Karten wachsen beim Play-Wechsel zu 3D-Blöcken
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-09-18
---

# 0048 - 2D-Karten wachsen beim Play-Wechsel zu 3D-Blöcken

## Beschreibung

Der Übergang von 2D Planung zu 3D Murmelbahn soll ein visueller Hook
werden. Dafür möchte ich, dass die 2D Planung quasi auf der Höhe 0
stattfindet und sobald in den Play Modus gewechselt wird Wachsen die 2D
Karten zu den 3D Blöcken. Nachher soll es so implementiert sein, dass die
2D Karten exakt die Draufsicht der 3D Blöcke darstellt, das heißt man
würde auch die Dekorationselemente auf den 2D Karten sehen. Mit einer
angemessenen Kamerafahrt soll so ein WOW Moment vervorgerufen werden,
wenn aus 2D plötzlich 3D wird.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/PathGrid.cs` - liegt laut Notizen von Ticket 0029
  aktuell nicht auf Welt-Höhe 0 (Grid-Ebene wurde manuell auf Y ≈ 8.75
  gedreht/verschoben, damit die 2D-Planung als Draufsicht erscheint).
  Für "2D Planung auf Höhe 0" muss diese Anordnung überprüft/angepasst
  werden.
- `Assets/Scripts/Grid/PipeVisual.cs` - zeichnet die 2D-Pipe-Karten aktuell
  prozedural per SpriteRenderer (Hintergrund, Hub, Arme, Label). Für "2D
  Karte = exakte Draufsicht des 3D-Blocks inkl. Dekoration" reicht diese
  rein 2D-prozedurale Darstellung nicht mehr aus.
- `Assets/Scripts/Grid/TrackBlockSpawner.cs` - erzeugt aktuell die 3D-Blöcke
  beim Moduswechsel neu (inkl. Dekorationen wie `TerrainDecoration`,
  `XylophoneBlockDecoration`, `TunnelPortalDecoration`). Für den
  "Wachs"-Effekt wäre vermutlich ein Skalierungs-/Morph-Übergang der
  bereits gespawnten (aber flachen/2D positionierten) Blöcke nötig, statt
  eines harten Neu-Spawns.
- `Assets/Scripts/Grid/CameraModeTransition.cs` - enthält bereits eine
  Kamerafahrt zwischen 2D-Planungspose und isometrischer 3D-Pose (Ticket
  0029, Status Done). Muss vermutlich zeitlich mit der neuen
  Wachs-Animation der Blöcke synchronisiert/abgestimmt werden.
- `Assets/Scripts/Grid/MarbleController.cs` - `TogglePlay()`/`IsPlaying`
  ist der bestehende Auslösepunkt für den Moduswechsel (Space, Ticket
  0012) und vermutlich Anknüpfungspunkt für den Start der Wachs-Animation.
- Verwandt, aber nicht identisch: Ticket 0030 ("2D-Karten optisch
  aufwerten", offen/vage) zielt auf generelle optische Aufwertung der 2D-
  Karten ab, nicht auf exakte Draufsicht-Korrespondenz zu den 3D-Blöcken
  inkl. Dekoration wie hier gefordert.

Akzeptanzkriterien (aus der Beschreibung abgeleitet):
- Die 2D-Planungsebene liegt auf Welt-Höhe 0 (statt wie aktuell manuell
  versetzt).
- Beim Wechsel in den Play-Modus wachsen die 2D-Karten sichtbar zu den
  3D-Blöcken heran (Höhenwachstum/Skalierung), statt dass die 3D-Blöcke
  ohne Übergang erscheinen.
- Die 2D-Kartendarstellung entspricht exakt der Draufsicht des
  zugehörigen 3D-Blocks, inklusive Dekorationselementen (z.B. Xylophon-
  Deko, Terrain-Deko, Tunnelportale) - die 2D-Karte ist also keine
  eigenständige, abstrahierte Darstellung mehr, sondern eine echte
  Draufsicht-Projektion des 3D-Contents.
- Die bestehende Kamerafahrt (Ticket 0029) wird so abgestimmt, dass sie
  zusammen mit dem Wachs-Effekt einen zusammenhängenden "WOW-Moment"
  ergibt.
- Konkrete Details zur Umsetzung (z.B. ob die 2D-Karte technisch eine
  Top-Down-Kamera-Ansicht des 3D-Blocks ist, ein gemeinsames Mesh/Material
  für beide Zustände genutzt wird, oder die Wachs-Animation eine reine
  Skalierung entlang der Höhenachse ist) sind aus der Beschreibung nicht
  spezifiziert und müssen vor der Umsetzung geklärt werden.

## Notizen

