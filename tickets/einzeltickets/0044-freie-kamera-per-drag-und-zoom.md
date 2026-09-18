---
id: 0044
title: Freie Kamera per Drag und Zoom
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-16
---

# 0044 - Freie Kamera per Drag und Zoom

## Beschreibung

Kamera Option ergänzen, die wenn man mit linksclick oder Touch draggt oder
zoomt in einen "freie Kamera" Modus wechselt. Die Isometrische Perspektive,
die aktuell schon beim Follow Script vorhanden ist sollte dabei aber
beibehalten werden, nur zoom und eine auf Orthogonale Bewegung sollte
möglich sein. Wenn freie Kamera aktiviert wurde, muss der Spieler über einen
Button wieder in den geführten Modus wechseln können.

## Details

Betroffene Dateien/Systeme:

- `Assets/Scripts/Grid/CameraModeTransition.cs` - hält den isometrischen
  Winkel (`pitchDegrees`/`yawDegrees`) und macht in `FollowMarble()` das
  geführte Nachziehen der Kamera (SmoothDamp auf Position und
  `orthographicSize`). Hier muss der geführte Modus unterbrechbar werden.
- `Assets/Scripts/Grid/BoundsCameraMath.cs` - rechnet bereits in den
  kameraeigenen Right/Up/Forward-Achsen; passend für das Panning entlang
  der Bildschirmachsen.
- `Assets/Scripts/Grid/GridInputHandler.cs` - nutzt bereits
  `UnityEngine.InputSystem` mit Press/Drag-Unterscheidung über
  `dragThresholdPixels`; das Drag-Gesture-Handling für die Kamera darf sich
  nicht mit dem Pipe-Swap-Drag während der Planung beißen.
- `Assets/Scripts/Grid/PlaybackHintUI.cs` - Vorbild für zur Laufzeit
  gebaute Screenspace-UI (Canvas/Text ohne Scene-Setup) für den
  Rückkehr-Button.

Akzeptanzkriterien:

- Linksklick-Drag bzw. Touch-Drag sowie Zoom (Mausrad / Pinch) schalten die
  Kamera automatisch in den freien Modus, das geführte Nachziehen der
  Murmel stoppt dabei.
- Die Kamera bleibt orthographisch und behält die bestehende isometrische
  Rotation (`pitchDegrees`/`yawDegrees`) unverändert bei - keine Drehung
  durch den Spieler.
- Drag verschiebt die Kamera nur entlang ihrer eigenen Right/Up-Achsen,
  Zoom verändert nur `orthographicSize`.
- Ein Button blendet sich im freien Modus ein und schaltet zurück in den
  geführten Modus; die Kamera findet dabei wieder sanft zur Murmel zurück.

## Notizen

- 2026-09-16: Umgesetzt in `CameraModeTransition.cs` (Free-Cam-State,
  Drag-Pan über Mouse/Touch, Zoom über Mausrad/Pinch, `ReturnToGuidedCamera()`)
  und neuem `FreeCameraReturnButtonUI.cs` (Rückkehr-Button, baut bei Bedarf
  auch ein EventSystem + InputSystemUIInputModule zur Laufzeit auf, da das
  Projekt nur das neue Input System nutzt). In `Prototyp_Phase1.unity` als
  neues GameObject verdrahtet.
- 2026-09-18: User-Feedback - freie Kamera klippte an einigen Blöcken (v.a.
  vorderste Ecke), da sie beim Wechsel in den freien Modus von der engen
  `followDistance` (3) aus startet und nur seitlich pannt, nie in der Tiefe
  nachjustiert. `followDistance` in `CameraModeTransition.cs` und im
  serialisierten Wert in `Prototyp_Phase1.unity` schrittweise auf 6, dann auf
  20 erhöht (Far Clip Plane liegt bei 1000, also reichlich Puffer) - da die
  Kamera orthographisch ist, ändert der Abstand nichts an Zoom/Framing,
  nur am Clipping-Spielraum.
