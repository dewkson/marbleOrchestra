---
id: 0043
title: Globaler Beat-Takt und Loop-Länge pro Level
type: Feature
priority: Medium
status: In Progress
area: Audio
created: 2026-09-11
---

# 0043 - Globaler Beat-Takt und Loop-Länge pro Level

## Beschreibung

Mit den SoundTriggern soll am Ende ein Beat gebaut werden, dafür wird ein
loopbarer Takt von 16 Steps (= 16 TrackBlocks) gebraucht. Aktuell hängt die
Loop-Länge direkt an der Pfadlänge, wodurch man beim Grid-Aufbau stark
eingeschränkt ist (z.B. nur bestimmte Grid-Größen für 16/32 Steps). Das soll
eleganter gelöst werden: globaler Takt, Loop-Länge entkoppelt von der
Pfadlänge.

## Details

- Klarstellung: In Kinematic3D bekommt jeder Block (inkl. Start und Goal)
  genau einen Beat - 16 Blocks = 16 Beats, Start und Goal teilen sich
  keinen Beat. Nur der alte Kinematic2D-Modus hatte N-1 Übergänge.
- Probleme vorher:
  - Jede Bahn loopte mit ihrer eigenen Pfadlänge, Bahnen unterschiedlicher
    Länge liefen gegeneinander.
  - `RunAlongPath3D` hat `elapsed` pro Block auf `Duration` gekappt und den
    Frame-Überhang verworfen - Drift von bis zu einem Frame pro Block.
- Umsetzung:
  - Neu: `Assets/Scripts/Grid/BeatClock.cs` - gemeinsame Zeitbasis pro
    Simulationslauf, absolute Zeit (`Time.timeAsDouble`) statt
    aufsummiertem `Time.deltaTime`.
  - `LevelData.loopLengthSteps` (Default 16, 0 = Pfadlänge wie bisher).
  - `PathGrid.Level` gibt das LevelData heraus.
  - Start und Goal sind stumm und zählen nicht zum Takt: 16 Steps =
    18 TrackBlocks. `TrackBlockSpawner` vergibt auf Start/Goal nie einen
    Trigger, `LevelData.OnValidate` warnt bei Trigger-Inhalt dort.
  - `MarbleController`: Lap-Länge = Anzahl Blocks zwischen Start und Goal,
    aufgerundet auf ganze Loops. Die nächste Murmel spawnt auf Start, sobald
    die aktuelle den Block direkt vor Goal erreicht - bei passender Länge
    ist sie auf Block 1, wenn die alte auf Goal ist (Start/Goal überlappen,
    kurz zwei Murmeln pro Bahn, jede Runde eigene Coroutine `RunLap`).
    Kürzere Bahn: neue Murmel wartet auf Start bis zum nächsten Downbeat.
    Längere Bahn: Lap über mehrere Loops (z.B. 32). Position und Trigger
    werden pro Frame aus der Clock abgeleitet; nach einem Frame-Hänger
    werden übersprungene Trigger nachgeholt.
  - Kinematic2D: Goal hält jetzt ebenfalls einen Beat (Lap = N Beats wie
    in 3D). Physics3D: Murmel wird kinematisch auf Start gehalten und erst
    auf dem Downbeat fallen gelassen.
- Akzeptanzkriterien:
  - 18-Block-Bahn (Start + 16 Steps + Goal) loopt nahtlos in 16 Beats,
    ohne Drift über viele Loops.
  - Sound-Inhalt auf Start/Goal löst nichts aus.
  - Bahn mit weniger als 16 Steps: Pause bis zum nächsten Downbeat.
  - Mehrere Bahnen im selben Level laufen auf demselben Downbeat.

## Notizen

- Bewusst `Time.timeAsDouble` statt `AudioSettings.dspTime`: dspTime springt
  pro Audio-Buffer (Murmel würde ruckeln), und Sounds laufen weiterhin über
  `PlayOneShot` im Frame. Sample-genaues Audio wäre ein Folgeschritt mit
  `PlayScheduled`.
- Entscheidung: Start/Goal sind stumm und liegen außerhalb des Taktes, die
  "1" ist der erste Block nach Start. Anlass: Sounds auf Start/Goal kamen
  vorher ~72 ms zu früh (Straight-Trace mit `ImpactT = 0` statt Fall-Phase
  0.144 der Trigger-Blöcke). Eigene Trigger-Geometrie für Start/Goal wäre
  ein separates Feature.
- Noch nicht im Unity-Editor getestet.
