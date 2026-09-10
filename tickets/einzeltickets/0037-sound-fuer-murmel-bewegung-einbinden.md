---
id: 0037
title: Sound für Murmel-Bewegung einbinden
type: Feature
priority: Medium
status: Open
area: Audio
created: 2026-09-04
---

# 0037 - Sound für Murmel-Bewegung einbinden

## Beschreibung

Sound für die generelle Bewegung der Murmel einbinden

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/MarbleController.cs` - steuert die Bewegung der
  Murmel entlang der Bahn (Kinematic2D/Kinematic3D/Physics3D), hat
  aktuell bewusst kein eigenes `AudioSource` (siehe Kommentar am
  Klassenkopf) - Sound-Reaktionen laufen bisher nur pro Block über
  `SoundTriggerContent`/`BlockTrigger` (siehe 0023/0024).
- `Assets/Scripts/Grid/Marble.cs` - Marble-Komponente (Kugel).
- `Assets/Scripts/Audio/InstrumentAudioSystem.cs` - bestehendes
  Audio-System für Instrumenten-Events.

Bisher existiert kein durchgängiges Bewegungsgeräusch (z.B. Rollen),
sondern nur diskrete Trigger-Sounds an einzelnen Blöcken. Konkrete
Ausgestaltung (z.B. Loop-Sound während der Fahrt, lautstärke-/
tempoabhängig, pro Bewegungsmodus unterschiedlich) muss noch geklärt
werden.

## Notizen
