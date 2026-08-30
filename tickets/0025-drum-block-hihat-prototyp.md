---
id: 0025
title: Drum-Block / Hi-Hat-Prototyp
type: Feature
priority: Medium
status: Done
area: Audio
created: 2026-08-29
---

# 0025 - Drum-Block / Hi-Hat-Prototyp

## Beschreibung

Einen konkreten Instrumentenblock für eine Hi-Hat erstellen. Wenn die
Murmel auf den Block fällt bzw. ihn triggert, soll der entsprechende
Hi-Hat-Sound abgespielt werden. Der Block soll visuell und akustisch
deutlich auf den Trigger reagieren.

## Details

- Baut auf [[0023]] (generisches Block-Trigger-System) und [[0024]]
  (Sound-Trigger als erstes Block-Feature) auf - dieses Ticket ist der
  erste konkrete Instrumenten-Block auf dieser Grundlage.
- Akzeptanzkriterien (grobe erste Fassung):
  - Beim Murmel-Kontakt wird ein Hi-Hat-Sound abgespielt.
  - Der Block reagiert zusätzlich sichtbar (visuelles Feedback) auf den
    Trigger, nicht nur akustisch.

## Notizen

- 2026-08-29: Kein neuer Code nötig - `Assets/Levels/Contents/
  Sound_Hat.asset` existierte bereits (referenziert `Assets/Audio/
  Card Audio/Amazing Hat.wav`) und war schon auf 12 Zellen in
  `Assets/Levels/Level_Prototype.asset` gemalt. Seit [[0023]]/[[0024]]
  löst das dort automatisch Sound + Blitz aus. Einzige Ergänzung: neues
  `flashColor`-Feld auf dem Asset gesetzt (kräftiges Gelb,
  `{r: 1, g: 0.85, b: 0, a: 1}`), damit der Hi-Hat-Block sich visuell
  von anderen Trigger-Zellen (Kick/Snare, weiterhin Standard-Weiß)
  abhebt.
