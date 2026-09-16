---
id: 0045
title: Nicht bebaubare Zellen für variable Grid-Formen
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-09-16
---

# 0045 - Nicht bebaubare Zellen für variable Grid-Formen

## Beschreibung

Grid variabler aufbauen: ein einzelnes Level muss nicht zwangsläufig
rechteckig aufgebaut sein, solange jede Zelle immer einen direkten Nachbar
hat. Im Editor soll man künftig also beispielsweise ein 6x6 Grid einstellen
können, auch wenn man nur einen kleineren Bereich für sein Level benötigt.
Dafür muss man im Editor Zellen so markieren können, dass sie nicht
"bebaut" werden dürfen

## Details

Betroffene Dateien/Systeme:

- `Assets/Scripts/Grid/LevelData.cs` - hält `width`/`height` und die beiden
  row-major Listen `pipes`/`contents` (Index = `y * width + x`). Die
  Blockier-Markierung braucht eine dritte, gleich indizierte Ebene, die
  `EnsureListSizes()` und `ResizeGrid()`/`RemapGrid()` mitführen müssen.
- `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` - Tilemap-Painter
  mit Layer-Auswahl (`PaintLayer.Pipe`/`Content`) und Palette; hier kommt
  das Markieren nicht bebaubarer Zellen hinzu, inkl. eigener Darstellung
  im Grid.
- `Assets/Scripts/Grid/PathGrid.cs` - baut `pipes`/`contents` als
  2D-Arrays über `Width`/`Height` auf; blockierte Zellen dürfen weder eine
  Pipe bekommen noch per Swap belegt werden (`SwapCards`).
- `Assets/Scripts/Grid/GridInputHandler.cs` - Pipe-Swap per Klick/Drag darf
  blockierte Zellen nicht als Quelle oder Drop-Ziel akzeptieren.
- `Assets/Scripts/Grid/PathValidator.cs` - Konnektivitätsprüfung von Start
  nach Goal muss blockierte Zellen als nicht passierbar behandeln.
- `Assets/Scripts/Grid/CameraFitter.cs` - rahmt aktuell über die vier
  Eckzellen des vollen `Width`/`Height`-Rechtecks; bei kleinerem bebaubarem
  Bereich ist zu klären, ob weiter das volle Grid oder nur der genutzte
  Bereich gerahmt wird.

Akzeptanzkriterien:

- Im Level Grid Editor lassen sich einzelne Zellen als nicht bebaubar
  markieren und wieder freigeben; die Markierung ist im Grid sichtbar.
- Die Markierung wird in `LevelData` persistiert und übersteht ein
  Resize des Grids (analog zum bestehenden Remapping der Pipes/Contents).
- Blockierte Zellen können im Spiel weder bepipet noch von der Murmel
  durchlaufen werden; die Pfadvalidierung ignoriert sie.
- Ein Level mit z.B. 6x6 Grid, von dem nur ein kleinerer zusammenhängender
  Bereich bebaubar ist, lässt sich anlegen und spielen.

## Notizen

Umsetzung: `LevelData` führt jetzt eine dritte, gleich indizierte
`blocked`-Liste (analog zu `pipes`/`contents`), die `EnsureListSizes()`,
`ResizeGrid()`/`RemapGrid()` mitführt. `SetBlockedAt()` löscht Pipe/Content
der Zelle beim Blockieren. `PathGrid.Build()` legt für blockierte Zellen
gar kein `PathPipe`-GameObject (und damit keinen Collider) an - dadurch
können Klick/Drag in `GridInputHandler` blockierte Zellen von sich aus
weder als Quelle noch als Drop-Ziel treffen, ohne dass dort etwas geändert
werden musste. `PathGrid.SwapCards()` lehnt blockierte Koordinaten
zusätzlich defensiv ab (`IsBlocked()`). `PathValidator` brauchte keine
Änderung: er überspringt Zellen ohne Pipe (`neighbor == null`) bereits,
was für blockierte Zellen jetzt automatisch zutrifft.

Im Level Grid Editor gibt es einen dritten Toolbar-Layer "Blocked":
Klick blockiert eine Zelle (räumt Pipe/Content ab), Rechtsklick hebt die
Blockierung auf; blockierte Zellen werden dort mit dunklem Overlay + rotem
X markiert, und Pipe/Content-Layer lassen sich auf ihnen nicht bemalen.

`CameraFitter` rahmt weiterhin unverändert das volle `Width`/`Height`-
Rechteck (nicht nur den bebaubaren Bereich) - das war im Ticket als offene
Frage markiert, aber keine harte Akzeptanzkriterium, und die einfachere
Variante reicht für ein Level mit z.B. 6x6 Grid und kleinerem bebaubarem
Bereich.
