---
id: 0032
title: Erstes Default-Terrain (Gras/Moos) definieren
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-04
---

# 0032 - Erstes Default-Terrain (Gras/Moos) definieren

## Beschreibung

Erstes default Terrain definieren. Zum Beispiel eine Gras- oder
Mooslandschaft mit einigen dekorativen Elementen. Im späteren Verlauf
sollen diese Terrains auch schon auf durch das Muster und die Farbe auf
den Karten zu erkennen sein und so erstellt der Nutzer je nach
Platzierung ein einzigartiges Terrain.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/BlockDefinition.cs` - hat bereits ein `Biome`-Feld
  (`string`, Konstante `DefaultBiome = "Default"`), das laut Kommentar
  ("placeholder - no biome system exists yet, always DefaultBiome today")
  bislang nie einen echten Effekt hat. Dieses Ticket wäre der erste
  Schritt, der diesem Platzhalter tatsächlich eine sichtbare
  Terrain-Darstellung (Gras/Moos + Dekor-Elemente) zuordnet.
- `Assets/Scripts/Grid/TrackBlock.cs` / `TrackBlockSpawner.cs` (siehe
  [[0017]]/[[0018]]) - erzeugen die 3D-Blöcke prozedural; hier müsste die
  Biom-abhängige visuelle Darstellung (Material/Textur/Dekor-Prefabs)
  andocken.
- Der im Beschreibungstext genannte zweite Teil (Terrain-Muster/-Farbe
  auch auf den 2D-Karten erkennbar machen, siehe [[0030]]
  `Assets/Scripts/Grid/PipeVisual.cs`) ist explizit als "im späteren
  Verlauf" markiert - vermutlich ein Folgeschritt auf demselben
  Biom-Datenmodell, hier nur als Zielrichtung mit aufgenommen, nicht als
  Akzeptanzkriterium für diesen ersten Schritt.

Keine Akzeptanzkriterien ausgearbeitet - Beschreibung liefert nur ein
Beispiel (Gras-/Moos-Terrain mit Dekor-Elementen) und die grobe
Zielrichtung, keine konkrete Asset-/Prefab-Liste.

## Notizen

Umgesetzt für die `DefaultBiome`: `TrackBlockSpawner.terrainColor` von
Erdbraun auf ein Gras-/Moosgrün geändert (Default sowohl im Code als
auch im Szenen-Wert in `Prototyp_Phase1.unity`) - betrifft die
Material-Basecolor aller Blöcke. Zusätzlich neue statische Klasse
`TerrainDecoration.cs`: streut pro Block einige kleine, flach
gestauchte "Moosklumpen" (abgeflachte, eingefärbte Kugeln aus Unitys
eingebauter Sphere-Mesh, kein Import-Asset) auf die flachen Schultern
links/rechts der Rille (`grooveRadius`/`SideWidth`), damit sie nicht
in die Rollbahn der Murmel hineinragen. Deterministisch pro
Zellkoordinate geseedet, damit sich ein Track bei einem
`SyncTracks`-Rebuild nicht sichtbar neu würfelt. Aufruf aus
`TrackBlockSpawner.BuildTrack` pro Block, geschützt über
`BlockDefinition.Biome` (aktuell immer `DefaultBiome`) - für
zukünftige Biome bereits als Erweiterungspunkt angelegt, aber keine
Biome-Registry o.ä. eingeführt, da bislang nur ein Biom existiert.

Auf Nutzerfeedback nachjustiert:
- Moosklumpen-Dichte erhöht (`MinClumps`/`MaxClumps` von 2-5 auf 4-9).
- Die Rille selbst (wo die Murmel tatsächlich rollt) bekommt jetzt
  einen eigenen erdbraunen Farbton statt des Gras-/Moosgrüns der
  Schultern: `TrackBlock`s Mesh hat dafür zwei Submeshes bekommen
  (Schultern/Wände/Boden vs. Rille), `IBlockProfile` um
  `IsGrooveSegment(int segmentIndex, Vector2 size)` erweitert (in
  `GrooveBlockProfile` anhand der Rillen-Bogenpunkte, in
  `FlatBoxProfile` immer `false`, da kein Rillen-Profil), `TrackBlock`
  bekommt eine neue `GrooveMaterial`-Property, `TrackBlockSpawner`
  erzeugt dafür ein zweites Material aus neuem `grooveColor`-Feld.
  `BlockFlashFeedback` (0023) musste angepasst werden, da
  `SetPropertyBlock` ohne Index nur Submesh 0 trifft - blinkt jetzt
  über beide Submeshes, damit die Rille beim Triggern weiterhin
  mitblinkt.

Nicht Teil dieses Tickets: die im Beschreibungstext genannte
Sichtbarkeit des Terrain-Musters/-Farbe auf den 2D-Karten (siehe
[[0030]]) - explizit als späterer Folgeschritt markiert.