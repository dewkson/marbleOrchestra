---
id: 0026
title: Instrumenten-Events von der Blocklogik entkoppeln
type: Task
priority: Medium
status: Done
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

- 2026-08-29: Umgesetzt. `InstrumentReaction.cs` spielt nicht mehr
  selbst ab, sondern meldet nur noch die eigene `BlockDefinition` über
  ein statisches `event Action<BlockDefinition> Played` - eigene
  `AudioSource`-Pflicht entfällt (aus `TrackBlock.prefab` entfernt).
  Statisch, weil `TrackBlockSpawner` Blöcke bei Pfadänderungen komplett
  neu baut (`SyncTracks` → `Destroy`) - ein Instanz-Abo müsste sich
  ständig neu verdrahten.
  Neu: `Assets/Scripts/Audio/InstrumentAudioSystem.cs` (neuer Ordner/
  Namespace `MarbleOrchestra.Audio`) - abonniert `InstrumentReaction.
  Played`, spielt `BlockDefinition.AudioEvent` über eine eigene
  `AudioSource`. In der Szene auf dem bestehenden "MarbleController"-
  GameObject platziert, wiederverwendet dessen `AudioSource`-Komponente
  (war seit [[0023]]s Aufräumen ungenutzt).
  `MarbleController.TriggerCellContent` unverändert - kennt weiterhin
  nur `BlockTrigger.Fire()`, nichts von Audio.
