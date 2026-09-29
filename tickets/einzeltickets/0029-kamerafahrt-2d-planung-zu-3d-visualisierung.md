---
id: 0029
title: Kamerafahrt zwischen 2D-Planung und 3D-Visualisierung
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-08-30
---

# 0029 - Kamerafahrt zwischen 2D-Planung und 3D-Visualisierung

## Beschreibung

Kamerafahrt zwischen 2D Planung und 3D Visualisierung einführen. Die 2D
Planung ist ja quasi eine 2D Planung und beim Übergang zum 3D soll die
Kamera quasi so gelerpt werden, dass man leicht diagonal in isometrischer
Perspektive auf die 3D Murmelbahn schauen kann und dabei sollten die
Blöcke in Gänze auf dem Kamerabild zu sehen sein.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/MarbleController.cs` - `TogglePlay()`/`IsPlaying`
  schalten aktuell zwischen Planungs- und Simulationsmodus (Space-Taste,
  siehe Ticket 0012); vermutlich der Anknüpfungspunkt, um die Kamerafahrt
  beim Moduswechsel auszulösen.
- `Assets/Scripts/Grid/CameraFitter.cs` - berechnet bisher die
  orthographische 2D-Kamera (Größe/Position) passend zur Grid-Größe aus
  `PathGrid`/`LevelData`; für die 3D-Ansicht braucht es analog eine
  Berechnung, die alle generierten 3D-Blöcke vollständig im Bild hält
  (vermutlich Umstieg von orthographic auf eine isometrisch wirkende,
  leicht diagonale Perspektive bzw. Anpassung von Position/FOV oder
  orthographic size anhand der Bounds der 3D-Blöcke).
- `Assets/Scripts/Grid/TrackBlockSpawner.cs` - erzeugt die 3D-Blöcke aus
  dem 2D-Pfad; deren Bounds sind vermutlich Grundlage für die
  Kamera-Zielposition/-größe in 3D.
- Aktuell existiert keine Kamera-Interpolation/-Animation zwischen zwei
  Zuständen im Projekt - das Lerpen zwischen 2D- und 3D-Kamerapose ist neu
  zu bauen.

Akzeptanzkriterien (aus der Beschreibung abgeleitet):
- Beim Wechsel von Planung (2D) zu Simulation (3D) fährt die Kamera per
  Lerp/Interpolation von der 2D-Ansicht in eine isometrisch wirkende,
  leicht diagonale 3D-Perspektive, statt hart umzuschalten.
- In der 3D-Zielperspektive sind alle Blöcke der aktuellen Bahn vollständig
  im Kamerabild sichtbar (kein Abschneiden), unabhängig von Bahngröße/-form.
- Verhalten für den Rückweg (3D zurück zu 2D-Planung) ist aus der
  Beschreibung nicht explizit spezifiziert - zu klären, ob dort ebenfalls
  gelerpt wird oder direkt zurückgeschaltet wird.

## Notizen

Umgesetzt mit neuem `CameraModeTransition`-Script
(`Assets/Scripts/Grid/CameraModeTransition.cs`), zusätzlich an die
MainCamera in `Prototyp_Phase1.unity` gehängt (neben dem bestehenden
`CameraFitter`):
- Pollt `MarbleController.IsPlaying` pro Frame (gleiches Muster wie
  `PlaybackHintUI`) und startet bei jedem Wechsel eine Lerp-Coroutine
  zwischen aktueller und Ziel-Kamerapose (Position, Rotation,
  orthographicSize; die Kamera bleibt durchgehend orthographisch).
- 3D-Zielpose: feste isometrische Rotation (Pitch 35.264°, Yaw 45°,
  beides im Inspector einstellbar), Position/orthographicSize werden aus
  `TrackBlockSpawner.TryGetTracksWorldBounds()` (neu, kombiniert die
  `MeshRenderer.bounds` aller gespawnten `TrackBlock`s) berechnet, indem
  die 8 Bounds-Eckpunkte auf die rechte/obere/vordere Achse der
  Zielrotation projiziert werden - garantiert, dass alle Blöcke
  unabhängig von Bahnlänge/-form vollständig im Bild sind.
- 2D-Zielpose beim Rückweg: `CameraFitter.TryComputeFitPose()` (neu,
  reine Query-Version von `Fit()`s bisheriger Berechnung, ohne die Kamera
  zu bewegen) plus der beim Awake gecachten ursprünglichen Rotation.
- Nicht in der Beschreibung spezifiziert und daher offen gelassen: beim
  Rückweg (3D → 2D) wird ebenfalls gelerpt, mit derselben
  `transitionDuration`.

Noch nicht im Editor getestet/verifiziert (Unity-Instanz war beim
Umsetzen bereits vom User geöffnet, daher kein Batch-Mode-Compile-Check
möglich) - User prüft selbst im offenen Editor.

**Nachbesserung (User-Feedback):** Die erste Lösung nahm implizit eine
Front-Ansicht der 2D-Planung an (Kamera blickt entlang Welt-Z, Grid-Ebene
in lokaler X/Y). Der User wollte stattdessen eine Draufsicht (2D-Planung
von oben, dann Kamerafahrt in die isometrische 3D-Ansicht) und hat dafür
MainCamera und PathGrid in der Szene bereits per Hand um 90° um die
X-Achse gedreht (Grid-Ebene liegt jetzt bei Welt-Y ≈ 8.75, Kamera blickt
von Y ≈ 10.43 nach unten). Damit das kein reiner Editor-Hack bleibt,
wurden alle beteiligten Scripts rotationsunabhängig gemacht:
- `Assets/Scripts/Grid/BoundsCameraMath.cs` (neu) - gemeinsame Helper-
  Klasse, die die 8 Eckpunkte einer world-space `Bounds` auf die
  rechte/obere/vordere Achse einer beliebigen Rotation projiziert.
  Ersetzt sowohl den alten, Welt-X/Y-annehmenden `CameraFitter`-Code als
  auch den duplizierten Corner-Loop in `CameraModeTransition`.
- `CameraFitter.cs` - `TryComputeFitPose()`/`Fit()` arbeiten jetzt
  generisch mit der bei `Awake()` gecachten Kamerarotation/-position
  (`planRotation`/`planPosition`) statt fest verdrahteter Welt-X/Y-Achsen;
  die Grid-Bounds werden aus allen 4 Eck-Zellen über `grid.transform`
  berechnet (respektiert jetzt auch die Grid-Rotation). Wichtig: die
  Kamera-Distanz zur Grid-Ebene wird aus der bei `Awake()` gecachten
  Ausgangsposition abgeleitet, nicht aus der aktuellen - sonst hätte jede
  3D→2D-Rückfahrt die Distanz aus der letzten isometrischen Position
  fehlberechnet.
- `CameraModeTransition.cs` - fragt die 2D-Zielpose (Position, Rotation
  UND Größe) komplett bei `CameraFitter.TryComputeFitPose()` ab, statt
  selbst eine Kopie der Planungsrotation zu halten; nutzt
  `BoundsCameraMath` für die 3D-Zielpose statt eigenem Corner-Code.
- `Assets/Scripts/Grid/PathGrid.cs` - Pipe-Zellen bekommen jetzt einen 3D-
  `BoxCollider` statt `BoxCollider2D`. Physics2D-Collider berücksichtigen
  nur Position.xy und Z-Rotation; nach der Grid-Rotation wären alle Pipes
  für Physics2D auf derselben Höhe "gestapelt" gewesen.
- `Assets/Scripts/Grid/GridInputHandler.cs` - Klick-Erkennung nutzt jetzt
  `Physics.Raycast` über `Camera.ScreenPointToRay` statt
  `Physics2D.Raycast`, funktioniert dadurch unter jeder Grid-/Kamera-
  Orientierung.

Scene-seitig waren keine weiteren Änderungen an `Prototyp_Phase1.unity`
nötig - die Feldlisten von `CameraFitter`/`CameraModeTransition` sind
unverändert, die vom User bereits gesetzten Rotationen/Positionen von
MainCamera und PathGrid bleiben wie vom User konfiguriert.

**Feintuning (User-Feedback):** Yaw-Richtung der isometrischen 3D-Fahrt
umgekehrt (`yawDegrees` in `CameraModeTransition` von `45` auf `-45`, in
Script-Default und Szene). Da `BoundsCameraMath` Position/Größe generisch
aus den Bahn-Bounds für die jeweils aktuelle Rotation berechnet, bleibt
die Murmelbahn bei jedem Yaw-Vorzeichen automatisch vollständig im Bild.

Vom User im Editor geprüft und für gut befunden - Status auf Done
gesetzt.

### Follow-up: Clipping Plane schneidet Blöcke beim 2D→3D-Übergang

Nachtrag: beim Übergang tauchte wieder eine sichtbare Clipping-Plane auf,
die einige TrackBlocks anschnitt. Ursache war der knapp bemessene Abstand
der isometrischen Zielpose (`nearMargin = 2`, Szene: `2`): in der
*gesetzten* Pose reichte das, aber `LerpPose` interpolierte Position und
Rotation unabhängig voneinander, sodass die Kamera mitten in der Fahrt nah
genug ans Terrain schwenkte, dass die Near-Clip-Plane (0.3) durch Blöcke
schnitt.

Wunsch des Users: den Kameraabstand einfach sehr groß wählen - bei
orthografischer Projektion macht das für die Darstellung keinen
Unterschied.

Das stimmt, gilt aber erst nach einem zweiten Schritt. Ein reiner
Positions-Lerp zwischen zwei Endposen, die je nur unter ihrer eigenen
Rotation Sinn ergeben, erzeugt einen Fehler, der **proportional zum
Kameraabstand** wächst. Beim alten Abstand von wenigen Einheiten war das
ein kaum sichtbares Wackeln; mit einem Abstand, der groß genug ist, um
Clipping auszuschließen, hätte es die Bahn mitten im Übergang komplett aus
dem Bild geschleudert.

Deshalb umgestellt:

- `CameraPose` speichert jetzt den **gerahmten Punkt** (`Focus`) plus
  `Distance` statt einer rohen Weltposition. `LerpPose` interpoliert
  Focus/Rotation/Zoom und baut die Kameraposition in **jedem Frame aus der
  aktuellen Rotation** neu auf. Damit ist der Abstand tatsächlich
  wirkungslos für das Bild - so wie es bei orthografischer Projektion sein
  soll.
- `CameraFitter.TryComputeFitPose` liefert den gerahmten Punkt als
  zusätzlichen `out`-Parameter mit.
- `nearMargin` und `followDistance` sind zu einem einzigen
  `forwardClearance = 2000` zusammengefasst (Isometrie-Pose und
  Murmel-Follow teilen sich jetzt eine Zahl für Clipping-Headroom).
- `WidenClipRangeFor` zieht die Far-Clip-Plane passend mit - ein größerer
  Abstand hilft an der Near-Plane nur, wenn die Geometrie nicht hinten aus
  dem Sichtkörper fällt. Far-Clip in der Szene außerdem 1000 → 10000.
- `FollowMarble` und `ApplyPan` (Free Camera, 0044) laufen ebenfalls über
  den Focus, damit gerahmter Punkt und Transform nie auseinanderlaufen.

Bewusst unverändert: die 2D-Planungspose behält ihren in der Szene
gesetzten Abstand (~11.6). In 2D gab es nie ein Clipping-Problem, und
`GridInputHandler` raycastet über diese Kamera. Beim Rückweg 3D→2D ist das
unkritisch, weil `MarbleController.Stop()` über
`TrackBlockSpawner.ClearAll()` das 3D-Terrain sofort abräumt - während der
Rückfahrt steht also gar keine 3D-Geometrie mehr im Bild.

Szenenwerte mit angepasst (`Prototyp_Phase1.unity`): die alten
`nearMargin: 2` / `followDistance: 20` hätten die neuen Script-Defaults
sonst überstimmt.
