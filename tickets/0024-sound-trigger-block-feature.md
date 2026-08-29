---
id: 0024
title: Sound-Trigger als erstes Block-Feature
type: Feature
priority: Medium
status: Open
area: Audio
created: 2026-08-29
---

# 0024 - Sound-Trigger als erstes Block-Feature

## Beschreibung

Einen ersten Blocktyp implementieren, der beim Kontakt mit einer Murmel
einen Sound abspielt. Sound und weitere Parameter sollen über das
Block-Prefab konfigurierbar sein, sodass später verschiedene
Instrumente auf derselben technischen Grundlage umgesetzt werden
können.

## Details

- Baut auf [[0023]] (generisches Block-Trigger-System) auf - dieses
  Ticket ist der erste konkrete Verbraucher des dort entstehenden
  Events.
- Existierendes Vorbild (2D-Grid-gebunden, kein direkt wiederverwendbarer
  Code, aber gleiche Grundidee): `Assets/Scripts/Grid/
  SoundTriggerContent.cs` - spielt beim Erreichen einer Zelle einen
  `AudioClip` über eine `AudioSource` ab (`PlayOneShot`).
- Akzeptanzkriterien (grobe erste Fassung):
  - Ein Block spielt bei Murmel-Kontakt (über [[0023]]s Trigger-Event)
    einen konfigurierten `AudioClip` ab.
  - Sound-Clip und weitere Parameter sind pro Block-/Prefab-Instanz im
    Inspector einstellbar, nicht hart codiert.

## Notizen
