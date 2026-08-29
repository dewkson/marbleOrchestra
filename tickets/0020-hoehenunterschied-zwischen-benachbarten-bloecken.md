---
id: 0020
title: Höhenunterschied zwischen benachbarten Blöcken aus Pfad ableiten
type: Feature
priority: Medium
status: Open
area: Physics
created: 2026-08-29
---

# 0020 - Höhenunterschied zwischen benachbarten Blöcken aus Pfad ableiten

## Beschreibung

Benachbarte Blöcke sollen unterschiedliche Höhen besitzen können.
Dadurch entsteht zwischen den Blöcken ein kontrollierter
Höhenunterschied, über den die Murmel vom höheren auf den niedrigeren
Block fällt bzw. rollt. Der Höhenverlauf muss aus dem geplanten 2D-Pfad
abgeleitet werden.

## Details

- Baut auf [[0017]] (3D-Terrain als einzelne Blöcke pro Pfadabschnitt),
  [[0018]] (universelles 3D-Block-Prefab mit Höhen-Parameter) und
  [[0019]] (Block-Ausrichtung/Gefälle aus 2D-Pfadrichtung) auf - dieses
  Ticket betrifft konkret die Höhe jedes Blocks relativ zu seinen
  Nachbarn entlang des Pfades.
- Akzeptanzkriterien (grobe erste Fassung):
  - Aus dem geplanten 2D-Pfad wird automatisch ein Höhenverlauf
    abgeleitet, der jedem Block entlang der Bahn eine Höhe zuweist.
  - Zwischen benachbarten Blöcken kann ein Höhenunterschied bestehen,
    der einen kontrollierten Übergang (Fallen/Rollen) der Murmel vom
    höheren zum niedrigeren Block ermöglicht.

## Notizen

- 2026-08-29: Erste Umsetzung ließ alle Blöcke gleich stark kippen
  (konstantes Tilt), um nahtlose Übergänge zu erreichen. Nach
  User-Feedback grundlegend korrigiert: Übergänge sollen KEIN nahtloses
  Rollen sein, sondern ein kurzer Fall der Murmel an jeder Blockgrenze
  (Grund: spätere Soundtrigger sollen den Eindruck erzeugen, die Murmel
  lande auf einem Xylophon-Element und erzeuge dadurch einen Ton, siehe
  [[0015]]). Umgesetzt als Treppen-Modell: alle Blöcke sitzen auf
  derselben Bodenebene (lokal Y 0) und sind flach (`TiltDegrees = 0`);
  pro Block wird stattdessen `TrackBlock.Height` kleiner (erster/höchster
  Block bei `startHeight`, dann `-heightDropPerCell` pro Zelle, geclampt
  über `maxStepHeight` und nach unten über `minBlockHeight`).
- 2026-08-29: Nach weiterem Feedback korrigiert - Blöcke sollen NICHT
  komplett flach sein, sondern weiterhin sichtbar geneigt, UND trotzdem
  soll ein kleiner Fall am Übergang bleiben. Gelöst über neuen Faktor
  `tiltFraction` (0-1, Default 0.5): die Neigung schließt nur einen Teil
  der Höhenstufe (`seamlessTiltDegrees * tiltFraction`), der Rest bleibt
  als Fall übrig. `tiltFraction = 0` → wie zuvor (flach, reiner Fall),
  `tiltFraction = 1` → nahtlos (kein Fall mehr). Details siehe
  Plan-Datei.
- 2026-08-29: `maxStepHeight` (Sicherheits-Clamp auf `heightDropPerCell`)
  wieder entfernt - war eine verfrühte Absicherung für ein noch nicht
  existierendes Feature (variable Höhenprofile pro Zelle); aktuell gibt
  es keine Variation, gegen die geclampt werden müsste. `heightDropPerCell`
  wird jetzt direkt verwendet.

