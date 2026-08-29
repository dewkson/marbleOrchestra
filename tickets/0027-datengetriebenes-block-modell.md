---
id: 0027
title: Datengetriebenes Block-Modell unabhängig von Darstellung
type: Feature
priority: Medium
status: Open
area: Tooling
created: 2026-08-29
---

# 0027 - Datengetriebenes Block-Modell unabhängig von Darstellung

## Beschreibung

Ein datengetriebenes Modell für einen Block entwickeln, das seine
grundlegenden Eigenschaften unabhängig von seiner visuellen Darstellung
beschreibt. Dazu gehören beispielsweise Pfadrichtung, Höhe, Blocktyp,
Trigger-Verhalten, Audio-Event und Biom. Das Modell soll später sowohl
von der 2D-Planung als auch von der 3D-Generierung verwendet werden
können.

Der Block weiß, WAS er ist.
Die 3D-Darstellung weiß, WIE er aussieht.
Das Music-System weiß, WAS er musikalisch auslöst.

## Details

- Berührungspunkte mit bestehenden/geplanten Systemen:
  - [[0018]] `TrackBlock` (`Assets/Scripts/Grid/TrackBlock.cs`) trägt
    aktuell sowohl Daten (Size, Height, Yaw/Tilt) als auch die
    Mesh-Erzeugung selbst (WIE) in einer Komponente - ein separates
    Datenmodell würde diese Vermischung auflösen.
  - [[0017]] `TrackBlockSpawner` berechnet Pfadrichtung/Höhe pro Zelle
    bereits (`ComputeYawDegrees`, `BlockHeightAt`) direkt beim Spawnen -
    wäre ein potenzieller Ort, aus dem das neue Modell diese Werte
    bezieht bzw. an das Modell übergibt.
  - [[0023]]-[[0026]] (Block-Trigger-System, gerade in Umsetzung)
    betreffen "Trigger-Verhalten" und "Audio-Event" - deren Ergebnis
    (`BlockTrigger`/`InstrumentReaction`) könnte auf dieses Modell
    aufbauen bzw. mit ihm abgeglichen werden, sobald es existiert.
  - "Biom" ist ein neues Konzept, taucht in keinem bisherigen Ticket
    auf - nicht näher spezifiziert.
  - `PipeDefinition.cs` ist als bestehendes Vorbild für "Daten ohne
    Visual/Prefab-Referenz" relevant (explizit im Kommentar dort
    begründet).
- Keine Akzeptanzkriterien ausgearbeitet - Beschreibung liefert nur die
  Zielrichtung (WAS/WIE/musikalisch getrennt), keine konkrete
  Datenstruktur oder Dateien.

## Notizen

- 2026-08-29: Scope mit User abgestimmt: reine Datenklasse, im Code
  zusammengesetzt (keine neue `ScriptableObject`-Autorierung, keine
  Änderung an `LevelData`/`LevelGridEditorWindow`); soll
  `CellContentDefinition`/`SoundTriggerContent` als Quelle für Trigger/
  Audio langfristig ablösen - das tatsächliche Umverdrahten von
  `MarbleController` ist aber nicht Teil dieses Tickets.
- 2026-08-29: Umgesetzt - neue `BlockDefinition.cs` (readonly struct:
  `Coord`, `PathDirection` (`Direction`-Enum), `Height`, `Type`
  (wiederverwendet `PipeRole`), `Trigger` (neues `TriggerBehavior`-Enum:
  `None`/`OnEnter`), `AudioEvent` (`AudioClip`, pragmatischer erster
  Schritt statt eines Instrument-Ids), `Biome` (`string`, aktuell immer
  `DefaultBiome`, da kein Biom-System existiert). `TrackBlock.Definition`
  (Property, rein datenhaltend, kein Einfluss auf Mesh-Erzeugung).
  `TrackBlockSpawner.BuildTrack` befüllt es pro Block aus
  `PathGrid.GetPipe(cell)?.Role`, `PathGrid.GetContent(cell)` und den
  bereits berechneten Yaw/Height-Werten (neue `ComputePathDirection`,
  dupliziert bewusst `ComputeYawDegrees`s Delta-Logik statt die
  bestehende, getestete Methode umzubauen). Keine Änderungen an
  `LevelData`, `LevelGridEditorWindow`, `CellContentDefinition`,
  `SoundTriggerContent`, `MarbleController`. Noch nicht im Editor
  getestet.
- Nächster Schritt (separat, nicht Teil dieses Tickets): Phase A des
  pausierten Gameplay-Trigger-Plans ([[0023]]) sollte jetzt
  `TrackBlock.Definition.Trigger`/`.AudioEvent` als Quelle nutzen statt
  eigenständig `PathGrid.GetContent` abzufragen.
