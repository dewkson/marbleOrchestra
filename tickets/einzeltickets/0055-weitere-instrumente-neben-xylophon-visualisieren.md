---
id: 0055
title: Weitere Instrumente neben Xylophon in 3D visualisieren
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-09-30
---

# 0055 - Weitere Instrumente neben Xylophon in 3D visualisieren

## Beschreibung

weitere Instrumente neben Xylophon ermöglichen: Aktuell ist es so, dass wenn auf einer Zelle ein AudioTrigger definiert ist, ein 3D Objekt von einem Xylophon Element visualisiert wird unabhängig davon ob der Sound tatsächlich ein Xylophon ist. Ich möchte gerne die Möglichkeit haben auch andere 3D Objekte wie Pauken, Snares, Hats und weitere Instrumente zu visualisieren

## Details

- Betroffene Stellen: `Assets/Scripts/Grid/TrackBlockSpawner.cs` (baut für Trigger-Zellen immer `XylophoneBlockDecoration.Build(...)`, ca. Zeile 1205; `MinTriggerFallHeight`/Pad-Geometrie ebenfalls Xylophon-spezifisch), `XylophoneBlockDecoration.cs`, `InstrumentPadFeedback.cs`, `SoundTriggerContent.cs`, `XylophonePadContent.cs`, `TriggerFallMarbleTrace.cs`.
- Akzeptanzkriterien:
  - Die visualisierte 3D-Geometrie einer Trigger-Zelle richtet sich nach dem Instrument (nicht mehr immer Xylophon).
  - Mindestens weitere Instrumente wie Pauke, Snare, Hi-Hat sind auswählbar/darstellbar; das System ist für weitere Instrumente erweiterbar.
  - Die Instrumentenwahl ist im Level-Daten-Modell/Level Grid Editor definierbar und passt zum jeweiligen Sound.
  - Trigger-Feedback (Flash/Reaktion) und Murmel-Fall-Trace funktionieren mit den neuen Geometrien.

## Notizen

