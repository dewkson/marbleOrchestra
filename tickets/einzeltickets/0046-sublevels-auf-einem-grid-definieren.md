---
id: 0046
title: Sublevels auf einem Grid definieren
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-09-16
---

# 0046 - Sublevels auf einem Grid definieren

## Beschreibung

in Zukunft soll man im Grid Editor mehrere Sublevels bauen können. Also zum
Beispiel ist ein Sublevel das Xylophon Level. Wenn der Spieler das valide
gebaut hat, kann er fortschreiten zum nächsten Sublevel "Percussion"
wechseln. Mehrere Sublevels sollen dabei auf einem großen Grid definiert
werden können. Hierfür ist eine Definition vom Sublevel sinnvoll.

## Details

Betroffene Dateien/Systeme:

- `Assets/Scripts/Grid/LevelData.cs` - bisher beschreibt ein
  ScriptableObject genau ein Level (`width`/`height` plus die row-major
  Listen `pipes`/`contents`). Für Sublevels braucht es eine eigene
  Definition, die einen Bereich des großen Grids mit Metadaten (z.B. Name
  wie "Xylophon"/"Percussion", Reihenfolge) zusammenfasst.
- `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` - Tilemap-Painter
  mit Layer-Auswahl; hier müssen Sublevel-Bereiche angelegt, benannt und
  im Grid sichtbar abgegrenzt werden.
- `Assets/Scripts/Grid/PathGrid.cs` - baut aktuell genau ein LevelData in
  2D-Arrays auf (`Build(LevelData)`); Validierung und Spielbetrieb müssen
  sich auf das aktive Sublevel beziehen können.
- `Assets/Scripts/Grid/PathValidator.cs` - liefert über
  `PathValidationResult` bereits die "gültig gebaut"-Information, die als
  Fortschrittsbedingung zum nächsten Sublevel dient.
- `Assets/Scripts/Grid/MarbleController.cs` - entscheidet, wann gespielt
  wird; der Wechsel ins nächste Sublevel muss sich hier oder in einer
  übergeordneten Fortschrittslogik einhängen.
- `Assets/Scripts/Grid/CameraModeTransition.cs` /
  `Assets/Scripts/Grid/CameraFitter.cs` - rahmen bisher das gesamte Grid;
  bei Sublevels ist zu klären, ob auf den aktiven Bereich gerahmt wird.
- Bestehende Assets: `Assets/Levels/Level_Xylophon.asset`,
  `Assets/Levels/Level_Prototype.asset`.

Hängt inhaltlich mit 0045 zusammen (nicht bebaubare Zellen / variable
Grid-Formen), da beide das große Grid in kleinere nutzbare Bereiche
unterteilen.

Akzeptanzkriterien:

- Es gibt eine Sublevel-Definition, die einen Bereich des großen Grids
  samt Bezeichnung beschreibt und persistiert wird.
- Im Grid Editor lassen sich mehrere Sublevels auf einem Grid anlegen,
  benennen und voneinander abgegrenzt erkennen.
- Zur Laufzeit ist immer ein Sublevel aktiv; die Pfadvalidierung bezieht
  sich auf dieses Sublevel.
- Ist das aktive Sublevel valide gebaut, kann der Spieler zum nächsten
  Sublevel fortschreiten.

## Notizen

Umsetzung: `LevelData` bekommt eine neue `SubLevelDefinition` (Name +
`RectInt`-Bereich auf dem großen Grid) und eine gleichnamige, geordnete
`subLevels`-Liste - Listenreihenfolge IST die Fortschritts-Reihenfolge, es
gibt kein separates Index-Feld. CRUD-Methoden (`AddSubLevel`,
`RemoveSubLevelAt`, `SetSubLevelName`, `SetSubLevelArea`, `MoveSubLevel`)
analog zum bestehenden Pipe/Content/Blocked-Muster. Eine leere Liste (der
Stand aller bisherigen Level-Assets) bedeutet "das ganze Grid ist ein
einziges implizites Sublevel" - vollständig abwärtskompatibel, keine
Migration nötig.

`PathGrid` hält `ActiveSubLevelIndex`/`ActiveSubLevelArea` (Fallback: das
volle Grid ohne Sublevels) und `AdvanceToNextSubLevel()`. `PathValidator`
berücksichtigt für Start-Suche, Nachbar-Traversierung und Goal-Suche nur
noch Zellen innerhalb `ActiveSubLevelArea` - dadurch sind
`TrackBlockSpawner`/`MarbleController` automatisch mitskaliert, da beide
nur `grid.LastValidations` konsumieren, ohne selbst geändert werden zu
müssen (`MarbleController.CanPlay` heißt jetzt effektiv "aktives Sublevel
ist valide gebaut").

`MarbleController` bekommt `TryAdvanceSubLevel()` (Taste **N**, analog zum
bestehenden Space/S/R-Prototyp-Schema): nur möglich, wenn `CanPlay` true
ist (= aktives Sublevel hat eine valide Bahn), stoppt danach eine laufende
Simulation und ruft `CameraFitter.Fit()` direkt auf, damit die 2D-Planungs-
kamera sofort auf den neuen aktiven Bereich zoomt (nicht erst beim nächsten
Play/Stop-Wechsel).

Die im Ticket offene Frage bei `CameraFitter`/`CameraModeTransition` ist
damit beantwortet: `CameraFitter` rahmt in der 2D-Planungsansicht nur noch
den aktiven Sublevel-Bereich (`PathGrid.ActiveSubLevelArea`) statt immer
das volle Grid; die isometrische 3D-Ansicht (`CameraModeTransition`)
brauchte keine Änderung, da sie ohnehin schon die tatsächlich gespawnten
Track-Bounds rahmt, die durch die PathValidator-Einschränkung automatisch
sublevel-scoped sind.

Im Level Grid Editor gibt es ein neues "SubLevels"-Panel oberhalb der
Pipe/Content/Blocked-Toolbar: pro Sublevel Farb-Swatch, Name, Bereich
(x/y/w/h), Auf/Ab zum Umsortieren und Entfernen, plus "Add SubLevel". Jeder
Sublevel-Bereich wird als farbiger Rahmen mit Namens-Label über dem Grid
eingezeichnet (dickerer Rahmen für den aktuell in der Liste ausgewählten).

Bewusst nicht umgesetzt: eine tatsächliche Zwei-Sublevel-Demo auf
`Level_Xylophon.asset`/`Level_Prototype.asset` (z.B. "Xylophon" +
"Percussion" wie im Beispiel) - das ist reine Content-Autoring-Arbeit, die
jetzt über das neue Editor-Panel gemacht werden kann, aber keine
Codeänderung mehr braucht.

### Follow-up 1: Sichtbarkeit über Sublevel-Wechsel hinweg

Nachtrag nach erstem Test mit N: `PathGrid` deaktiviert in
`UpdateActiveSubLevelVisibility()` das `PathPipe`-GameObject jeder Zelle
außerhalb des aktiven Sublevel-Bereichs (aufgerufen von `Build()` und
`AdvanceToNextSubLevel()`). Dadurch ist in der 2D-Ansicht immer nur das
aktive Sublevel sichtbar - als Nebeneffekt verschwindet auch der Collider
deaktivierter Pipes aus `GridInputHandler`s Raycasts, ein bereits
verlassenes Sublevel lässt sich also nicht mehr versehentlich
weiterbearbeiten.

### Follow-up 2: alte Sublevels bleiben aktiv (mehrere gleichzeitige Bahnen)

Zweiter Nachtrag: "aktives Sublevel" hieß bis hierhin fälschlich auch
"einziges gerade spielendes Sublevel" - PathValidator hat nur den
Bereich des gerade bearbeiteten Sublevels ausgewertet, alle vorherigen
fielen komplett aus `grid.LastValidations` heraus. Für das musikalische
Ziel (mehrere gleichzeitig laufende Spuren/Loops, eine pro erreichtem
Sublevel) muss aber jedes bereits erreichte Sublevel weiter validieren
und seine Kugel(n) weiter spawnen/loopen, nicht nur das zuletzt aktive.

Dafür zwei getrennte Konzepte in `PathGrid`:

- `ActiveSubLevelArea`/`IsInActiveSubLevel` (unverändert): das EINE gerade
  im 2D-Grid-Editor/Planungsview sichtbare und bearbeitbare Sublevel.
- Neu: `IsInUnlockedSubLevel(coord)` - wahr für jede Zelle, deren Sublevel-
  Index <= `ActiveSubLevelIndex` ist, also für JEDES bisher erreichte
  Sublevel. `PathValidator.EvaluateAll()` filtert Start-Pipes jetzt danach
  statt nach `IsInActiveSubLevel` - dadurch laufen alle bisher gebauten
  Bahnen dauerhaft weiter mit, nicht nur die des aktuell bearbeiteten
  Sublevels.
- Neu: `GetOwnSubLevelArea(coord)` liefert den Bereich GENAU des Sublevels,
  zu dem eine Zelle gehört (nicht die Vereinigung aller entsperrten
  Bereiche). `PathValidator` begrenzt die Nachbarschafts-Traversierung
  jedes Starts jetzt auf dessen EIGENEN Sublevel-Bereich statt auf den
  aktiven - so kann eine Bahn nie über die Grenze in ein benachbartes
  (auch entsperrtes) Sublevel hineinwachsen, obwohl mehrere gleichzeitig
  ausgewertet werden.

`MarbleController.Play()`/`TrackBlockSpawner` brauchten dafür keine
Änderung: beide iterieren ohnehin schon über alle Einträge in
`grid.LastValidations` (das unterstützt mehrere gleichzeitige Bahnen seit
0010) und behandeln jede davon unabhängig.

Als Aufräumarbeit dabei entfernt: der eigens für Follow-up 1 eingebaute
Sonderfall in `TrackBlockSpawner.SyncTracks()` ("Track nicht abreißen,
wenn sein Start außerhalb des aktiven Bereichs liegt"). Der ist jetzt
überflüssig, weil ein entsperrtes Sublevel ohnehin für immer weiter
validiert wird und `paths` seinen Track weiterhin ganz normal jeden Frame
enthält - `SyncTracks` erkennt "Pfad unverändert" also wieder über den
normalen Vergleich, ganz ohne Sonderfall.

### Follow-up 3: geführte Kamera folgt nur dem aktuell bearbeiteten Sublevel

Dritter Nachtrag: durch Follow-up 2 laufen jetzt mehrere Sublevels
gleichzeitig, aber die geführte isometrische 3D-Kamera
(`CameraModeTransition`) kannte dieses Konzept noch nicht - sie hat beim
Rahmen und beim Kamera-Follow einfach irgendeine/alle Bahnen genommen.
Jetzt bezieht sie sich konsequent nur noch auf das gerade bearbeitete
(aktive) Sublevel:

- `TrackBlockSpawner.TryGetTracksWorldBounds()` (verwendet für den
  Einstiegs-Sprung in die 3D-Ansicht) rahmt nur noch Tracks, deren Start im
  aktiven Sublevel liegt (`grid.IsInActiveSubLevel`) - nicht mehr alle
  jemals gebauten Tracks.
- `Marble` bekommt ein neues `StartCoord`-Feld, das `MarbleController`
  beim Erzeugen in `RunTrack()` setzt. `PrimaryMarbleTransform` (worauf
  `CameraModeTransition.FollowMarble()` einrastet) sucht darüber jetzt
  gezielt eine Kugel, deren Start im aktiven Sublevel liegt, statt einfach
  die erste in der Liste zu nehmen - bei mehreren gleichzeitig laufenden
  Sublevels wäre das sonst eine zufällige, möglicherweise schon fertige
  frühere Bahn gewesen. Liefert `null`, solange im aktiven Sublevel gerade
  keine Kugel unterwegs ist - `FollowMarble()` behandelte einen Null-Target
  bereits vorher als No-op.

### Follow-up 4: Randomize im Level Grid Editor auf das jeweilige Sublevel begrenzt

Vierter Nachtrag: der "Randomize"-Button im Level Grid Editor (neben der
Pipe/Content/Blocked-Toolbar) hat bisher Pipes über das GESAMTE Grid
gemischt, ohne Rücksicht auf Sublevel-Grenzen - dadurch konnte eine Pipe
aus Sublevel A im Bereich von Sublevel B landen, obwohl Sublevels
unabhängige Rätsel sein sollen.

Behoben: `RandomizePipes()` wurde zu `RandomizePipesInArea(RectInt area)`
und mischt jetzt nur noch innerhalb eines einzelnen Bereichs. Der Button
ist dafür aus der Toolbar ins neue SubLevels-Panel gewandert: jede
Sublevel-Zeile hat jetzt ihren eigenen "Randomize"-Button, der nur deren
eigenen Bereich mischt. Für Level-Assets ganz ohne Sublevels (z.B.
`Level_Prototype.asset`) gibt es dort zusätzlich einen
"Randomize (whole grid)"-Button, der wie bisher das gesamte Grid mischt -
dieselbe Konvention wie zur Laufzeit (kein Sublevel definiert = das ganze
Grid ist ein einziges implizites Sublevel).

### Follow-up 5: schneller Pipe-Swap direkt im Level Grid Editor

Neue Funktion (kein Bug-Fix): ein vierter Toolbar-Layer "Swap" neben
Pipe/Content/Blocked. Klick auf eine Zelle markiert sie (cyanfarbener
Rahmen) als ersten Swap-Partner, Klick auf eine zweite Zelle tauscht
beider Pipes direkt (Undo-fähig); Rechtsklick bricht die Auswahl ab. Das
ist das Editor-Pendant zum bestehenden Klick-Klick-Tausch von
`GridInputHandler` zur Laufzeit, aber nutzbar ohne in den Play-Modus zu
wechseln.
