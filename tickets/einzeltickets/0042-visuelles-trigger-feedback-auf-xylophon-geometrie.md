---
id: 0042
title: Visuelles Trigger-Feedback auf Xylophon-Geometrie
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-10
---

# 0042 - Visuelles Trigger-Feedback auf Xylophon-Geometrie

## Beschreibung

Visual Trigger auf Xylophon Geometrie anwenden. Aktuell flasht der
gesamte TrackBlock, allerdings soll nur das Xylophon Element visuelles
Feedback geben und dabei auch die Option haben eine kleine scale up and
back animation anzuzeigen.

## Details

Betroffen:

- `Assets/Scripts/Grid/BlockFlashFeedback.cs` - setzt den Flash aktuell
  per `MaterialPropertyBlock` auf dem `MeshRenderer` des TrackBlocks
  selbst (über alle Submeshes, also Schultern UND Rille), reagiert auf
  `BlockTrigger.Triggered` und liest die Farbe aus
  `TrackBlock.Definition.FlashColor` (siehe [[0023]]/[[0027]]).
- `Assets/Scripts/Grid/XylophoneBlockDecoration.cs` - baut die Leiste
  als eigenes Kind-GameObject ("XylophoneBlock") mit eigenem
  MeshFilter/MeshRenderer, gibt aktuell aber keine Referenz darauf
  zurück; `TrackBlockSpawner.BuildTrack` verwirft sie entsprechend.
- `Assets/Scripts/Grid/BlockTrigger.cs` - bleibt unverändert die
  Ereignisquelle; Reaktions-Komponenten hängen sich dort ein (siehe
  [[0023]]).

Akzeptanzkriterien (grobe erste Fassung):

- Bei einem Trigger flasht nur noch die Xylophon-Leiste, nicht mehr der
  ganze Block (Schultern/Rille bleiben unverändert).
- Zusätzlich optional (an-/abschaltbar) eine kurze Scale-Up-and-Back-
  Animation der Leiste; Stärke/Dauer konfigurierbar.
- Blocks ohne Xylophon-Geometrie (Normal/Start/Goal) behalten ein
  sinnvolles Verhalten - entweder weiterhin Block-Flash oder gar keins,
  ist zu entscheiden.
- Die Farbe kommt weiterhin aus `BlockDefinition.FlashColor`, es bleibt
  bei einer Quelle der Wahrheit.

Entscheidung des Users: visuelles Feedback gilt erstmal nur für
Trigger-Blocks, normale Blocks brauchen keins. Eigene Komponente, in
der Grundfarbe des Instrument-Elements (später auch andere Instrumente
als Xylophon), Stärke der Scale-Animation und Flash-Farbe einstellbar
sind.

## Notizen

**2026-09-10 (umgesetzt):** Neue Komponente
`Assets/Scripts/Grid/InstrumentPadFeedback.cs`, auf dem TrackBlock-
Prefab an der Stelle des bisherigen `BlockFlashFeedback` (das damit
nirgends mehr verwendet wurde und entfernt ist - Doku-Verweise in
`BlockTrigger`/`SoundTriggerContent`/`BlockDefinition` zeigen jetzt auf
die neue Komponente).

- Sie reagiert auf `BlockTrigger.Triggered` wie bisher, färbt und
  skaliert aber ausschließlich das Instrument-Element. Blocks ohne
  solches Element (Normal/Start/Goal) bleiben untätig - die Komponente
  tut nichts, bis ihr über `Attach(Renderer)` eins übergeben wird.
- `TrackBlockSpawner` reicht das Element durch:
  `XylophoneBlockDecoration.Build` liefert jetzt seinen MeshRenderer
  zurück, der Spawner gibt ihn an die Komponente des Blocks weiter.
  Damit bleibt es bei der Regel aus [[0023]] - der Block entscheidet
  selbst, wie er reagiert; der Spawner stellt nur die Verbindung her.
- Einstellbar im Prefab: `padColor` (Normalfarbe des Elements, wird
  beim Attach gesetzt), `flashColor` + `useContentFlashColor` (letzteres
  lässt einer `SoundTriggerContent.FlashColor` den Vortritt, siehe
  [[0027]], damit die instrumentenspezifischen Farben erhalten
  bleiben), `scaleAmount` und `pulseDuration`.
- Verlauf ist eine Halbwelle Sinus über die Dauer (0 -> 1 -> 0), sodass
  Farbe und Skalierung ohne Sprung hoch und wieder zurück gehen.
- Farbe läuft weiterhin über einen `MaterialPropertyBlock`, nicht über
  das geteilte Material - sonst würde die ganze Bahn mitfärben.
- `XylophoneBlockDecoration` baut die Leiste jetzt um den EIGENEN
  Ursprung und setzt stattdessen die `localPosition` des Kind-Objekts.
  Vorher steckte der Versatz in den Vertices, wodurch ein Skalieren die
  Leiste von der Blockmitte weggeschoben statt vergrößert hätte; jetzt
  sitzt der Pivot mittig auf ihrer Unterseite und sie wächst an Ort und
  Stelle nach oben/zur Seite.

Bekannter Randfall, bewusst nicht mitgeändert: `XylophonePadContent`
ist rein visuell und bekommt in `TrackBlockSpawner` weiterhin
`TriggerBehavior.None`, feuert also gar keinen Trigger - so eine Zelle
bekommt ihre `padColor`, pulst aber nie. Falls das Feedback dort auch
gewünscht ist, reicht es, diesen Block-Typ ebenfalls auf
`TriggerBehavior.OnEnter` zu setzen (der Audio-Pfad ist gegen einen
fehlenden Clip abgesichert).

