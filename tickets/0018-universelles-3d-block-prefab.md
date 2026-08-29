---
id: 0018
title: Universelles 3D-Block-Prefab als Basis
type: Feature
priority: Medium
status: Open
area: Tooling
created: 2026-08-29
---

# 0018 - Universelles 3D-Block-Prefab als Basis

## Beschreibung

Ein universell verwendbares 3D-Block-Prefab erstellen, das als Grundlage
für alle zukünftigen Terrain-, Gameplay- und Instrumentenblöcke dient.
Das Prefab soll Parameter wie Größe, Höhe, Material und Ausrichtung
unterstützen und später um weitere Eigenschaften erweitert werden können.

## Details

- Im Projekt existiert aktuell kein generisches Block-/Prefab-System
  (keine Treffer für "Block"/"Prefab" in `Assets/Scripts`) - dies wäre
  eine Neuentwicklung.
- Mögliche Berührungspunkte: [[0017]] (3D-Terrain als einzelne Blöcke pro
  Pfadabschnitt) könnte dieses Basis-Prefab als Baustein nutzen, ist aber
  ein eigenständiges Ticket.
- Akzeptanzkriterien (grobe erste Fassung):
  - Es gibt ein Unity-Prefab (inkl. zugehörigem Script/Component), das
    Größe, Höhe, Material und Ausrichtung als konfigurierbare Parameter
    besitzt.
  - Die Struktur ist so gestaltet, dass sie sich ohne Bruch um weitere
    Eigenschaften erweitern lässt (z.B. für Terrain-, Gameplay- oder
    Instrumenten-spezifische Zusatzdaten).

## Notizen

- 2026-08-29: Phase 1 aus dem Implementierungsplan für 0017-0021
  umgesetzt: `Assets/Scripts/Grid/TrackBlock.cs` (Component mit
  Size/Height/Material/Yaw/Tilt, `Rebuild()`, `EntryPointLocal`/
  `ExitPointLocal`), `IBlockProfile.cs` + `FlatBoxProfile.cs`
  (Erweiterungsachse für die Oberflächenform, statt Vererbung) und
  `GrooveProfileUtility.cs` (aus `TrackTerrainGenerator` extrahierte
  U-Rinnen-Geometrie, für [[0017]] vorbereitet). Prefab liegt unter
  `Assets/Prefabs/TrackBlock.prefab`. Noch nicht im Editor getestet -
  Status bleibt offen, bis manuell verifiziert (Größe/Höhe/Material/
  Ausrichtung im Inspector ändern, Rebuild prüfen).
