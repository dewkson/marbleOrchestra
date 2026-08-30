---
id: 0023
title: Generisches Block-Trigger-System
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-08-29
---

# 0023 - Generisches Block-Trigger-System

## Beschreibung

Ein generisches Trigger-System für Blöcke implementieren. Ein Block soll
erkennen können, wenn eine Murmel ihn betritt bzw. auf ihm landet, und
darauf ein konfigurierbares Event auslösen. Das System soll unabhängig
vom konkreten Inhalt des Blocks funktionieren.

## Details

- Baut auf [[0018]] (universelles `TrackBlock`-Prefab) auf - dessen
  Notiz sieht bereits "Daten/Verhalten über Sibling-Components auf
  demselben GameObject" als Erweiterungsachse vor (analog zu
  `CellContentDefinition` bei 2D-Zellen); dieses Ticket wäre die
  konkrete Umsetzung dieser Achse.
- Klarstellung (User-Feedback, 2026-08-29): Der Trigger soll **nicht**
  auf echtem 3D-Kontakt zwischen Murmel und `TrackBlock` beruhen.
  Stattdessen kann er sich an das bestehende 2D-Grid-System hängen:
  `Assets/Scripts/Grid/CellContentDefinition.cs` (abstrakte
  `ScriptableObject`-Basis mit `Activate(CellContentContext)`) und
  `SoundTriggerContent.cs` als Beispiel-Implementierung, ausgelöst über
  `MarbleController.TriggerCellContent` anhand der 2D-Zellkoordinate,
  die die Murmel gerade durchquert - das Projekt arbeitet ohnehin
  voraussichtlich nicht durchgängig physikbasiert. Kernanforderung ist,
  dass der jeweilige `TrackBlock` auf den Trigger sichtbar/visuell
  reagieren kann, unabhängig davon, wie der Trigger technisch ausgelöst
  wurde.
- Betroffene bestehende Systeme: `CellContentDefinition.cs`/
  `SoundTriggerContent.cs` (bestehender Trigger-Mechanismus, Vorbild/
  Ausgangspunkt), `TrackBlock.cs` (soll auf ein Trigger-Event reagieren
  können), `MarbleController.cs` (`TriggerCellContent`, bestehende
  Aufrufstelle).
- Akzeptanzkriterien (grobe erste Fassung):
  - Ein über das bestehende 2D-Grid-System ausgelöstes Trigger-Event
    kann dem `TrackBlock` an derselben Zelle zugeordnet und
    weitergereicht werden.
  - `TrackBlock` (bzw. eine Sibling-Component darauf) kann auf dieses
    Event reagieren und dabei sichtbares/visuelles Feedback liefern -
    das ist die Kernanforderung dieses Tickets.
  - Das System löst ein konfigurierbares Event aus, ohne selbst zu
    wissen, was der Block inhaltlich tut (Sound, Gameplay-Effekt, etc.)
    - siehe [[0024]] als ersten konkreten Verbraucher.

## Notizen
