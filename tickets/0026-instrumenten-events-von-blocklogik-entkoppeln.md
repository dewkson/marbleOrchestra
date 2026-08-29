---
id: 0026
title: Instrumenten-Events von der Blocklogik entkoppeln
type: Task
priority: Medium
status: Open
area: Gameplay
created: 2026-08-29
---

# 0026 - Instrumenten-Events von der Blocklogik entkoppeln

## Beschreibung

Die Blocklogik soll nicht direkt an einzelne Sounds gekoppelt sein.
Stattdessen soll ein Block ein generisches Event auslösen, das von
einem Audio-/Music-System verarbeitet wird. Dadurch können später
Hi-Hat, Kick, Snare, Bass, Piano etc. dieselbe Block-Infrastruktur
verwenden.

## Details

- Baut auf [[0023]] (generisches Block-Trigger-System), [[0024]]
  (Sound-Trigger als erstes Block-Feature) und [[0025]] (Hi-Hat-
  Prototyp) auf - dieses Ticket entkoppelt/verallgemeinert die dort
  entstandene, noch direkt an Sound-Wiedergabe gekoppelte Umsetzung.
- Akzeptanzkriterien (grobe erste Fassung):
  - Ein Block löst beim Trigger ein generisches Event aus, ohne selbst
    Kenntnis von AudioClip/AudioSource o.ä. zu haben.
  - Ein separates Audio-/Music-System verarbeitet dieses Event und
    entscheidet, welcher Sound/welches Instrument abgespielt wird.
  - Die bestehenden Blöcke aus [[0024]]/[[0025]] funktionieren nach der
    Umstellung unverändert weiter.

## Notizen
