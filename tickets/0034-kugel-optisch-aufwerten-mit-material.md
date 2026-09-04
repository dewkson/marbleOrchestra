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

Nachträglich auf Wunsch zu einer pinken Marmoroptik mit feiner
Maserung geändert: `CreateSphereMaterial` nutzt jetzt wieder eine
prozedurale Textur (`GetMarbleTexture` - klassisches
Perlin-Marmor-Rezept: turbulenzverzerrte Sinus-Bänder zwischen einem
hellen und einem kräftigen Pink), diesmal deutlich feiner/dichter
abgestimmt als die frühere, wieder entfernte Holzmaserung. Passender
Wortwitz nebenbei: die "Murmel" bekommt eine echte "Marmor"-Optik.

