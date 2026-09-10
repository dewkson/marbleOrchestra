---
id: 0021
title: Murmel-Übergang zwischen Blöcken überprüfen und optimieren
type: Task
priority: Medium
status: Done
area: Physics
created: 2026-08-29
---

# 0021 - Murmel-Übergang zwischen Blöcken überprüfen und optimieren

## Beschreibung

Überprüfen und optimieren, wie sich die Murmel beim Übergang von einem
Block auf den nächsten verhält. Ziel ist ein kontrollierter,
zuverlässiger Übergang ohne unerwartetes Springen, Steckenbleiben oder
Verlassen des vorgesehenen Pfades.

## Details

- Setzt voraus, dass die Bahn aus einzelnen Blöcken besteht (siehe
  [[0017]], [[0018]]) mit ggf. Neigung ([[0019]]) und Höhenunterschieden
  zwischen Nachbarblöcken ([[0020]]) - dieses Ticket betrifft das
  physikalische Verhalten der Murmel genau an den Nahtstellen zwischen
  zwei Blöcken.
- Relevante bestehende Systeme: `Assets/Scripts/Grid/Marble.cs`,
  `Assets/Scripts/Grid/MarbleController.cs` - steuern aktuell die
  Murmelbewegung, vermutlich Ansatzpunkt für Anpassungen am
  Übergangsverhalten.
- Akzeptanzkriterien (grobe erste Fassung):
  - Die Murmel wechselt an Blockgrenzen zuverlässig auf den Folgeblock,
    ohne zu springen, hängenzubleiben oder vom vorgesehenen Pfad
    abzukommen.
  - Verhalten ist auch bei Höhenunterschieden zwischen Blöcken (siehe
    [[0020]]) und unterschiedlichen Neigungswinkeln (siehe [[0019]])
    konsistent.

## Notizen

- 2026-08-29: Umgesetzt in `TrackBlockSpawner.SampleTrackPosition`/
  `SampleFloorY`: X/Z kommen aus den Zellmitten (`grid.
  CellToLocalPosition`), da ein Kurven-Block wegen seiner Yaw-Regel
  (siehe [[0019]]-Notiz) mit seiner eigenen Entry-Seite nicht dort
  liegt, wo der Pfad tatsächlich eintritt. Die Höhe kommt aus dem
  tatsächlich gespawnten Block (`EntryPointLocal`/`ExitPointLocal`),
  der Sprung zwischen Block-Exit und nächstem Block-Entry ist der
  gewollte Fall (siehe [[0020]]) und wird nicht geglättet.
- 2026-08-29: `TrackBlock` und `Marble` erhalten je ein prozedural
  erzeugtes `PhysicsMaterial` (niedrige Reibung, keine Bounciness).
  `Marble.CreateSphere3D` nutzt jetzt
  `CollisionDetectionMode.ContinuousDynamic` gegen Tunneling an den
  jetzt dünneren, mehrteiligen Collidern.
- Zurückgestellt: Fase/Chamfer an Höhenstufen-Kanten - erst im
  Playtesting prüfen, ob nötig.

