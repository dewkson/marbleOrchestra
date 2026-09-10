---
id: 0036
title: Instrumentenreaktionen in 3D mit Partikeleffekt anreichern
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-09-04
---

# 0036 - Instrumentenreaktionen in 3D mit Partikeleffekt anreichern

## Beschreibung

Instrumentenreaktionen in 3D. Eventuell mit einfachen Partikelsystem für
das "Feel" anreichern.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/BlockFlashFeedback.cs` - aktuelle
  Instrumentenreaktion eines Blocks beim Triggern: kurzes Aufblitzen der
  Blockfarbe (`FlashColor` aus `BlockDefinition`, siehe [[0027]]) über ein
  `MaterialPropertyBlock`, sonst keine visuelle Reaktion.
- `Assets/Scripts/Grid/InstrumentReaction.cs` / `BlockTrigger.cs` - lösen
  das Trigger-Event aus, an das ein Partikeleffekt analog zum Flash
  andocken könnte (`trigger.Triggered`-Event).
- Verwandt, aber inhaltlich anders: [[0015]] ("Soundtrigger auf der
  3D-Bahn visualisieren", statischer Marker/Kreisfläche an der
  Trigger-Position) wurde vom User auf "Wont Do" gesetzt. Dieses Ticket
  hier ist keine statische Markierung, sondern eine reaktive Verstärkung
  des bestehenden Flash-Feedbacks im Trigger-Moment (z.B. Partikelstoß
  beim Auslösen).

Keine Akzeptanzkriterien ausgearbeitet - Beschreibung liefert nur die
Zielrichtung ("Feel" anreichern, eventuell per einfachem Partikelsystem),
keine konkrete Vorgabe zu Partikelform/-farbe oder ob je Instrument
unterschiedlich.

## Notizen

