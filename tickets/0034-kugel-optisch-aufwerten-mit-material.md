---
id: 0034
title: Kugel optisch aufwerten mit passendem Material
type: Idea
priority: Medium
status: Done
area: Gameplay
created: 2026-09-04
---

# 0034 - Kugel optisch aufwerten mit passendem Material

## Beschreibung

Kugel optisch aufwerten mit entsprechendem Material

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/Marble.cs` - Marble-Komponente (Kugel).
- `Assets/Scripts/Grid/MarbleController.cs` - steuert die Bewegung der
  Kugel entlang der Bahn.

Beschreibung ist vage ("optisch aufwerten") - konkrete Materialwahl
(z.B. Glas, Metall, Stein o.ä.) und gewünschte Optik müssen noch geklärt
werden, bevor das Ticket umgesetzt werden kann.

## Notizen

Umgesetzt als Holzoptik: `Marble.CreateSphereMaterial` (in
`Assets/Scripts/Grid/Marble.cs`) setzt der 3D-Kugel eine feste
Holz-Basecolor (warmes Braun) mit mattem/satiniertem Finish
(Smoothness 0.35, Metallic 0) statt der vorherigen einfarbigen
Material. Eine erste Version mit prozeduraler Holzmaserungs-Textur
wurde auf Wunsch wieder entfernt - nur die Basecolor bleibt. Betrifft
nur die 3D-Kugel (Kinematic3D/Physics3D, `CreateSphere3D`); die
flache 2D-Murmel (Kinematic2D, `Marble.Create`) bleibt unverändert
farbig (`marbleColor` in `MarbleController`).

