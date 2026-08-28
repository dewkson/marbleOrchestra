---
id: 0015
title: Soundtrigger auf der 3D-Bahn visualisieren
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-08-28
---

# 0015 - Soundtrigger auf der 3D-Bahn visualisieren

## Beschreibung

Die 3D Marble Bahn sollte eine Visualisierung bekommen, an den Stellen wo
ein Soundtrigger vorhanden ist. Diese Visualisierungen sollten die
unterschiedlichen Sounds unterschiedlich darstellen. Im aktuellen Beispiel
haben wir Hat, Snare und Kick. Ich könnte mir eine halbtransparente
Kreisfläche orthogonal zur Bahnrichtung vorstellen und wenn die Kugel
durchfährt wird der Sound getriggert. Diese runde Fläche könnte eventuell
auch ein Image von einer Snare, einem Kick oder einer Hihat zeigen.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/SoundTriggerContent.cs` - aktuell nur ein
  `AudioClip`-Feld, kein Konzept von "Sound-Typ" (Hat/Snare/Kick) - für eine
  typabhängige Visualisierung braucht es vermutlich eine zusätzliche
  Kategorisierung (z.B. Enum oder Sprite-Referenz pro Trigger), da sich der
  Typ nicht zuverlässig aus dem `AudioClip` ableiten lässt.
- `Assets/Scripts/Grid/CellContentDefinition.cs` /
  `CellContentContext` - `Activate()` wird aktuell beim Erreichen der Zelle
  aufgerufen (Trigger-Zeitpunkt bereits vorhanden); die Visualisierung
  müsste zusätzlich an der Zellposition auf der 3D-Bahn platziert werden,
  vermutlich analog zu `TrackTerrainGenerator`s Ring-Berechnung (Position +
  Rechtsvektor orthogonal zur Bahnrichtung).
- `Assets/Scripts/Grid/TrackTerrainGenerator.cs` - erzeugt die 3D-Bahn samt
  Ringen/Richtung pro Zelle; liefert vermutlich die Referenzgeometrie
  (Position, Rechtsvektor) für die Platzierung der Kreisflächen.
- `Assets/Scripts/Grid/MarbleController.cs` - triggert `Activate()` beim
  Durchfahren; ob/wie die Visualisierung auf Kugel-Ankunft reagiert (z.B.
  kurzer Puls-Effekt beim Triggern) ist offen.

Akzeptanzkriterien (grobe erste Fassung):
- Jede Zelle mit `SoundTriggerContent` zeigt auf der 3D-Bahn eine
  halbtransparente, kreisförmige Fläche orthogonal zur Bahnrichtung.
- Die Darstellung unterscheidet sich je nach Sound-Typ (Hat/Snare/Kick) -
  z.B. per Farbe oder per Bild/Icon.
- Die Visualisierung ist an der Stelle sichtbar, an der die Kugel den Sound
  auslöst (Zellposition), unabhängig vom gewählten Bewegungsmodus
  (Kinematic3D/Physics3D, siehe 0014).

## Notizen

