---
id: 0050
title: Separate Farbe pro Trackblock im Level-Editor
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-09-28
---

# 0050 - Separate Farbe pro Trackblock im Level-Editor

## Beschreibung

separate Trackblock Farben: Ich möchte im LevelEditor gerne die Möglichkeit
haben, die Trackblock farbe für spezifische Trackblocks einzustellen.
Aktuell gibt es eine global eingestellte Farbe für alle Trackblocks.

## Details

Betroffen: `Assets/Scripts/Grid/TrackBlockSpawner.cs`. Aktuell sind
`terrainColor` und `grooveColor` (sowie `tunnelColor`) globale
`[SerializeField]`-Werte auf dem Spawner, aus denen in `Awake()` je ein
einziges Shared Material (`sharedMaterial`, `sharedGrooveMaterial`,
`sharedTunnelMaterial`) erzeugt und für alle Blöcke gleichermaßen verwendet
wird (`TrackBlockSpawner.cs:80-82`, `:116-118`, `:1004`). Es gibt keinen
Mechanismus, einem einzelnen Block eine abweichende Farbe zuzuweisen.

Akzeptanzkriterien (grob):
- Im Level-Editor lässt sich für einzelne Trackblocks eine individuelle
  Farbe setzen, die von der globalen Default-Farbe abweicht.
- Blöcke ohne explizite Einstellung nutzen weiterhin die globale
  Default-Farbe.
- Offen/zu klären in der Umsetzung: ob dafür weiterhin Shared Materials
  pro Farbe sinnvoll sind oder auf MaterialPropertyBlocks pro Block
  umgestellt werden muss, und ob die Farbe pro Block im Leveldaten-Modell
  (analog zu Sound/FlashColor, siehe 0028) persistiert wird.

## Notizen

Umgesetzt: neuer per-Zelle Farb-Override, persistiert im Leveldaten-Modell
(analog zu HeightOverrides/BlockedLooks): `LevelData.blockColorOverrides`
(`List<ColorOverride>`, null = kein Override), erreichbar über
`GetBlockColorOverrideAt`/`SetBlockColorOverrideAt` und `PathGrid.
GetBlockColorOverride(coord)`.

`TrackBlockSpawner.ResolveBlockMaterial(cell)` liefert für eine Zelle mit
Override ein gecachtes Material für genau diese Farbe (ein Material pro
distinkter Override-Farbe statt pro Zelle, damit Zellen mit derselben
Farbe weiterhin dasselbe Material/Batching teilen - siehe
`overrideMaterialCache`), sonst das bisherige globale `sharedMaterial`.
Angewendet auf den Block-Körper selbst (Track- und Filler-Blöcke), die
Start/Goal-Tunnelwand und die Basis des Xylophon-Pads bei Trigger-Blöcken.
Rillenfarbe (`grooveColor`) und Tunnel-Innenfarbe (`tunnelColor`) bleiben
bewusst global/nicht überschreibbar - Ticket verlangte nur "eine
individuelle Farbe" pro Block.

Im Level-Editor einstellbar über ein neues "Custom Block Color"-Feld im
"Selected Cell"-Panel (für Pipe- und blockierte Zellen) sowie im
Multi-Edit-Panel aus [0051](0051-multiselekt-von-zellen-im-level-grid-editor.md)
("Block Color (3D)" mit Apply/Clear-Button, gilt für alle ausgewählten
Zellen unabhängig davon ob Pipe oder blockiert).

### Follow-up 1: Blockfarbe kommt von der Karte statt von der Zelle

Nachtrag: die Farbe war bis hierhin rein an die Zellposition gebunden
(`LevelData.blockColorOverrides` per Grid-Index, direkt abgefragt in
`PathGrid.GetBlockColorOverride`). Damit lag sie in einer Liste, die
parallel zu den Karten gepflegt werden musste - und lief zwangsläufig
auseinander:

- Verschiebt der Spieler in der 2D-Planung eine Karte, blieb die Farbe an
  der alten Zelle liegen.
- `LevelGridEditorWindow.RandomizePipesInArea` mischte die Karten, ließ die
  Overrides aber unberührt.
- Auch ohne jede Bewegung: im gespeicherten `Level_MarbleOrchestraV2` wichen
  34 von 49 Zellen ab, weil Kartenfarbe und Override getrennt eingestellt
  worden waren.

Entscheidend war der Blick in die Daten: die sichtbare Kartenfarbe ist
`PipeDefinition.BackgroundColor`, und es existieren bewusst Farbvarianten
jeder Pipe-Form (`Pipe_UpDown 1` blau, `2` rot, `4` grün, `5` gold, ...).
Die Karte trägt ihre Farbe also ohnehin schon mit sich - die Override-Liste
war eine zweite, redundante Quelle derselben Information.

Umgestellt: `PathGrid.GetBlockColorOverride(coord)` leitet die Blockfarbe
für jede Zelle mit Karte direkt aus `PipeDefinition.BackgroundColor` ab.
Damit stimmen 2D und 3D per Konstruktion überein - Swap, Randomize und jede
künftige Kartenbewegung sind automatisch korrekt, ohne dass irgendeine
Operation etwas mitführen muss. `TrackBlockSpawner.ResolveBlockMaterial`
bleibt unverändert.

Der Per-Zell-Override bleibt bestehen, gilt aber nur noch für Zellen OHNE
Karte (blockierte Zellen und leere Zellen mit Filler-Block aus
[0047](0047-umgebende-bloecke-mit-interpolierter-hoehe.md)) - dort gibt es
keine Karte, von der eine Farbe kommen könnte, und das Terrain bewegt sich
ohnehin nicht.

Im Level-Editor entsprechend angepasst: das "Custom Block Color"-Feld
erscheint nur noch für Zellen ohne Karte. Bei einer Kartenzelle zeigt das
"Selected Cell"-Panel die Blockfarbe stattdessen read-only mit dem Hinweis,
dass sie von der Karte kommt. Im Multi-Edit-Panel überspringt "Block Color
(3D)" Kartenzellen; "Background Color" färbt dort Karte und 3D-Block in
einem Zug.

Konsequenz, bewusst in Kauf genommen: die globale `terrainColor` am
`TrackBlockSpawner` greift jetzt nur noch für Zellen ohne Karte. Eine Karte
ohne eigene Farbe (die ungefärbten Basis-Pipes mit
`backgroundColor` 0.15/0.15/0.15) färbt ihren Block entsprechend dunkelgrau.

Bewusst NICHT mitgezogen: der Höhen-Override
(`LevelData.HeightOverrides`) und die Content-Ebene (Sound-Trigger) bleiben
an der Zellposition - beide beschreiben das Terrain bzw. den Takt an dieser
Stelle, nicht die Karte.

Die veralteten Overrides auf Kartenzellen im bestehenden Level sind
wirkungslos, aber noch im Asset. Sie werden erst wieder sichtbar, wenn eine
solche Zelle später blockiert wird.
