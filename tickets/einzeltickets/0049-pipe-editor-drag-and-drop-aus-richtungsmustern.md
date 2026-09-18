---
id: 0049
title: Pipe-Editor per Drag & Drop aus Richtungsmustern statt Kartenliste
type: Feature
priority: Medium
status: In Progress
area: Level Editor
created: 2026-09-18
---

# 0049 - Pipe-Editor per Drag & Drop aus Richtungsmustern statt Kartenliste

## Beschreibung

Editor für Pipes so verbessern, dass nicht eine Liste mit allen bekannten
Karten zur Auswahl steht, sondern geordnet alle möglichen
Richtungsausprägungen. Dann soll man über Drag and Drop aus den Mustern
die Karten auf das Grid ziehen können und Attribute wie locked, start
oder stop löst man indem man eine Zelle auswählt und dann über toggles
das jeweilige Attribut an oder ausschaltet.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` - `DrawPalette()`
  (ca. Zeile 309-381) zeigt aktuell im Pipe-Layer eine scrollbare Liste
  aller bekannten `PipeDefinition`-Assets (`availablePipes`) als
  Klick-Auswahl (`DrawPaletteEntry`), die per anschließendem Klick auf
  eine Zelle gemalt wird (kein Drag & Drop).
- `DrawCustomPipeBuilder()` (Zeile 383+) erlaubt bereits das
  Zusammenstellen einer Pipe aus Richtungs-Toggles
  (`DrawDirectionToggles()`), plus Background-Color, `Role`
  (Normal/Start/Goal, siehe Ticket 0009) und `Locked` (Ticket 0008) - die
  Attribute werden hier aber direkt beim Brush-Bauen gesetzt, nicht
  nachträglich pro ausgewählter Zelle.
- `GetOrCreateCustomPipe`/`CreateCustomPipeAsset`/`BuildPipeId` (ab Zeile
  767) erzeugen bei Bedarf neue `PipeDefinition`-Assets aus
  Richtungskombination + Rolle + Locked - dieser Mechanismus wäre
  vermutlich weiter nutzbar, wenn Attribute nachträglich per Zellauswahl
  geändert werden.
- `Assets/Scripts/Grid/PipeDefinition.cs` - definiert `Connections`
  (Richtungs-Flags), `Role`, `Locked`, `Color` als Datenmodell einer Pipe.

Akzeptanzkriterien (aus der Beschreibung abgeleitet):
- Die Palette zeigt statt einer Liste vorhandener `PipeDefinition`-Assets
  geordnet alle möglichen Richtungsausprägungen (Kombinationen der
  Connection-Richtungen) als Muster an.
- Karten werden per Drag & Drop aus der Palette auf eine Grid-Zelle
  gezogen, statt wie bisher "Palette-Eintrag anklicken, dann Zelle
  anklicken".
- Attribute wie `Locked`, `Start`/`Goal`-Rolle werden nicht mehr beim
  Bauen der Palette-Karte festgelegt, sondern nachträglich: Zelle
  auswählen, dann per Toggles das jeweilige Attribut an-/ausschalten.
- Offen/zu klären vor Umsetzung: ob die bisherige Liste bekannter
  `PipeDefinition`-Assets (`availablePipes`) dadurch komplett ersetzt wird
  oder weiterhin parallel existiert; wie das Zellauswahl-Attribut-Panel
  UI-seitig in `LevelGridEditorWindow` eingebunden wird (z.B. analog zum
  bestehenden SubLevel-Panel).

## Notizen

- 2026-09-18: Umgesetzt in `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`:
  - `AllDirectionPatterns` (neu, `BuildOrderedDirectionPatterns()`/`CountBits()`)
    listet alle 16 Richtungskombinationen, sortiert nach Anzahl offener
    Seiten (Sackgasse → Gerade/Ecke → T-Stück → Kreuz).
  - `DrawPipePalette()`/`DrawPatternGrid()`/`DrawPatternEntry()` ersetzen die
    bisherige Liste vorhandener `PipeDefinition`-Assets im Pipe-Layer durch
    ein 3-spaltiges Raster dieser 16 Muster-Vorschauen (per
    `DrawConnectionArms`, jetzt generisch auf `Direction`+`Color` statt
    `PipeDefinition` umgestellt).
  - Drag & Drop: `HandlePatternDragSource()` startet bei MouseDown auf einer
    Muster-Kachel einen `DragAndDrop`-Vorgang mit dem `Direction`-Wert als
    Generic Data; `HandlePipeDrag()` auf der Zielzelle nimmt ihn per
    `DragUpdated`/`DragPerform` an (`Rejected`-Cursor über blockierten
    Zellen) und ruft `PlacePipePattern()` auf, die per bestehendem
    `GetOrCreateCustomPipe()` eine Normal/unlocked Pipe mit den
    Connections erzeugt/wiederverwendet.
  - Attribute (Locked/Start/Goal) werden nicht mehr vor dem Malen im Brush
    festgelegt: Klick auf eine Pipe-Zelle setzt `selectedCellIndex`
    (magenta Rahmen zur Kennzeichnung), `DrawCellAttributesPanel()` zeigt
    darunter Toggles, die bei Änderung ebenfalls über
    `GetOrCreateCustomPipe()` eine passende Asset-Variante
    holen/erzeugen und der Zelle zuweisen.
  - Entfernt: alte "Custom Pipe"-Baumeister-UI (`DrawCustomPipeBuilder`,
    `DrawDirectionToggles`) sowie die zugehörigen Felder
    (`useCustomBrush`, `customConnections`, `customRole`, `customLocked`) -
    ihre Funktion (beliebige Connections wählen) übernimmt jetzt die
    vollständige Muster-Liste, ihre Rolle/Locked-Vorbelegung übernimmt das
    neue Attribut-Panel.
  - Erneutes Draufziehen eines Musters auf eine bereits belegte Zelle setzt
    deren Attribute zurück auf Normal/unlocked (bewusst - "frische Karte"
    platzieren); nicht separat mit dem User abgestimmt, da aus der
    Beschreibung nicht spezifiziert.
  - Content/Blocked/Swap-Layer unverändert (weiterhin Klick-Paint aus
    Asset-Liste) - Ticket betraf laut Beschreibung nur den Pipe-Editor.
  - Noch nicht im Editor getestet (kein Unity-Batchmode-Compile-Check in
    dieser Umgebung verfügbar) - User bitte im offenen Editor prüfen,
    insbesondere ob das In-Window-Drag&Drop zwischen Palette und Grid wie
    erwartet funktioniert.
- 2026-09-18 (Nachbesserung 1): Auf User-Feedback drei Compile-Fehler durch
  `Object`/`Random`-Mehrdeutigkeit (durch das für `Array.Sort` ergänzte
  `using System;`) behoben, indem die betroffenen Stellen explizit auf
  `UnityEngine.Object`/`UnityEngine.Random` qualifiziert wurden.
- 2026-09-18 (Nachbesserung 2): Auf User-Feedback die Muster-Reihenfolge
  von der automatisch sortierten (`BuildOrderedDirectionPatterns`/
  `CountBits`, entfernt) auf eine vom User exakt vorgegebene, fest
  kodierte 5x3-Anordnung umgestellt (`AllDirectionPatterns` jetzt ein
  Array-Literal). Das "None"-Muster (keine Richtung) wurde entfernt, da es
  keine gültige Pipe ist - damit sind es 15 statt 16 Muster. Das dafür
  ergänzte `using System;` wurde wieder entfernt (verursachte die
  Mehrdeutigkeits-Fehler aus Nachbesserung 1).
- 2026-09-18 (Nachbesserung 3): Auf User-Feedback den separaten "Swap"-Reiter
  entfernt (`PaintLayer.Swap`, `pendingSwapIndex`, `DrawSwapPendingBorder`).
  Swappen geht jetzt per Drag & Drop direkt auf dem Grid: eine bereits
  belegte Pipe-Zelle auf eine andere ziehen tauscht beide (`SwapPipes()`,
  unverändert). Neu dafür: `pipeDragCandidateIndex` (in `HandleCellClick`
  gesetzt, wenn die angeklickte Zelle eine Pipe hat) und
  `HandlePipeCellDragStart()` (einmal pro `OnGUI`, nicht pro Zelle
  aufgerufen - siehe Kommentar dort für die Begründung), die bei
  `MouseDrag` den `DragAndDrop`-Vorgang mit `PipeCellDragKey`+Quellindex
  startet. `HandlePipeDrag` wurde zu `HandlePipeDrop` umbenannt und um
  diesen zweiten Generic-Data-Zweig ergänzt.
- 2026-09-18 (Nachbesserung 4): Auf User-Feedback den separaten "Blocked"-
  Reiter ebenfalls entfernt (`PaintLayer.Blocked` raus, Enum jetzt nur noch
  Pipe/Content). Blocken geht jetzt wie die Richtungsmuster per Drag & Drop:
  eine eigene "Blocked"-Kachel (`DrawBlockedPatternRow`/
  `DrawBlockedPatternEntry`, rotes X wie die bisherige
  `DrawBlockedOverlay`) unterhalb des 5x3-Rasters, separat davon (nicht
  Teil der vom User vorgegebenen Reihenfolge, da kein Richtungs-Pattern).
  Rechtsklick auf eine blockierte Zelle entsperrt sie jetzt unabhängig vom
  aktiven Layer (`ClearCell`), da es keinen eigenen Blocked-Layer mehr
  gibt, der das sonst übernehmen würde. Zusätzlich neuer Button
  "Block Empty Cells" (`FillEmptyCellsWithBlocked`) - blockiert alle
  Zellen ohne Pipe und ohne Content über das gesamte Grid (nicht auf ein
  SubLevel beschränkt, anders als Randomize).

