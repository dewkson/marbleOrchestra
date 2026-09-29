---
id: 0053
title: Grooves und voreingestellte Höhen auch bei unvollständigen Bahnen sichtbar
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-29
---

# 0053 - Grooves und voreingestellte Höhen auch bei unvollständigen Bahnen sichtbar

## Beschreibung

Ich möchte, dass die Grooves in 3D auch sichtbar sind, selbst wenn sie zu
keiner validen Bahn gehören. Aktuell ist es so, dass nur valide Bahnen
auch in 3D dargestellt werden, alle anderen Blocks werden ohne Rails
dargestellt. Außerdem möchte ich, dass die voreingestellten Höhen auch
schon eingehalten werden, selbst wenn dazu noch keine validen Bahnen
gebaut wurden.

## Details

Betroffen: `Assets/Scripts/Grid/TrackBlockSpawner.cs`,
`Assets/Scripts/Grid/PathValidator.cs`.

Ursache: `TrackBlockSpawner.FindCompletedPaths()` liest nur
`PathValidationResult`e mit `GoalReached == true` und baut auch nur für
diese über `BuildTrack` echte, mit Rille versehene Blöcke. Jede Pipe-Zelle,
die nicht Teil eines vollständigen Start→Ziel-Pfads ist - ein noch nicht
fertig verlegter Bahnabschnitt, oder ein Seitenzweig an einer
T/Kreuz-Pipe, der nicht zum Ziel führt - fiel bisher komplett auf die
generische, flache Filler-Behandlung (`SyncFillerBlocks`) zurück: keine
Rille, keine Berücksichtigung der jeweiligen Pipe-Form. Aus demselben
Grund wirkte eine an einem Start (oder SubLevel) gesetzte Höhe
(`LevelData.HeightOverrides`, siehe 0050) erst, sobald ihr Start-Pipe
tatsächlich Teil einer vollständigen Bahn war - `ResolveStartHeight` wurde
nur für `BuildTrack`s Start-Block bzw. für vollständig isolierte
Filler-Inseln aufgerufen, nie für einen unfertigen, aber bereits
angeschlossenen Bahnabschnitt.

## Notizen

Umgesetzt über einen dritten, rein dekorativen Rendering-Pass
(`TrackBlockSpawner.SyncPreviewBlocks`, aufgerufen in `RebuildNow()`
zwischen `SyncTracks` und `SyncFillerBlocks`), der `PathValidator`s
bereits vorhandene BFS-Traversierung ab jedem Start weiterverwendet statt
sie zu verwerfen:

- `PathValidator.PathValidationResult` gibt jetzt zusätzlich
  `ConnectedOrder` (jede von einem Start aus erreichbare Zelle, in
  BFS-Reihenfolge, Start zuerst) und `CameFrom` (Eltern-Zuordnung) zurück -
  beide waren als `visited`/`cameFrom` in `EvaluateFrom` schon vorhanden,
  wurden aber bisher verworfen, sobald kein Ziel erreicht wurde.
  `OrderedPath`/`GoalReached` unverändert.
- `SyncPreviewBlocks` baut für jede Zelle in `ConnectedOrder`, die nicht
  bereits Teil einer echten, spielbaren Bahn ist (`SyncTracks`s eigene
  `tracks`-Liste), einen dekorativen `TrackBlock` mit echter Rille - Form
  aus Eltern-/Kind-Richtung im BFS-Baum, bei einer Verzweigung (T/Kreuz)
  wird nur der erste Zweig (Priorität Up/Right/Down/Left) gezeigt, dieselbe
  Einschränkung, die eine echte fertige Bahn für ungenutzte Pipe-Öffnungen
  ohnehin schon hat. Diese Blöcke tragen keine `SetTrace`/Trigger-Wirkung
  und liegen außerhalb der `tracks`-Liste - für `MarbleController` (der nur
  über `GetBlockAt`/`tracks` auf Blöcke zugreift) unsichtbar, es rollt dort
  also nie eine Murmel.
- Höhe wird entlang derselben BFS-Kette vom Start aus verkettet wie bei
  einer echten Bahn (`ResolveStartHeight` am Start, je Zelle abzüglich
  `FallHeight` bei Trigger-Inhalt) - eine an einem Start oder SubLevel
  gesetzte Höhe wirkt dadurch sofort auf jede daran angeschlossene
  Pipe-Zelle, auch wenn die Bahn noch kein Ziel erreicht. Eine Zelle, die
  von keinem Start aus erreichbar ist (isoliertes Pipe-Fragment), fällt wie
  gehabt auf `ResolveStartHeight(cell)` direkt zurück - deren eigenen
  Height-Override, falls gesetzt.
- Ein Zweig, der von einem bereits ECHTEN Bahn-Block abzweigt (T/Kreuz auf
  dem Gewinnerpfad), verkettet seine Höhe von dessen tatsächlich gebauter
  Höhe (`CollectTrackHeights()`), nicht erneut vom Start aus.
- `SyncFillerBlocks` bekommt die neuen Preview-Höhen
  (`previewHeights`-Feld) zusätzlich zu den echten Bahn-Höhen als Anker
  gereicht, damit umgebendes Filler-Terrain weiterhin sauber an einen
  Preview-Block anschließt, ohne selbst einen zu überschreiben
  (`SolveFillerHeights` schließt jeden Key seines Eingabe-Dictionarys
  ohnehin von seinem eigenen Ergebnis aus).
- `ClearAll()`/`CollectAllBlocks()` (2D→3D-Grow-Animation, siehe 0048)
  räumen bzw. erfassen die Preview-Blöcke genauso wie Filler.

Bewusst nicht mitgemacht:
- Kein Xylophon-Pad/Trigger-Feedback auf einer Preview-Zelle mit
  Trigger-Inhalt - nur schlichte `TerrainDecoration`, damit eine noch
  nicht spielbare Zelle nicht suggeriert, sie reagiere bereits auf die
  Murmel. Die `FallHeight` fließt trotzdem in die Höhenkette ein, damit
  die Vorschau zur späteren echten Bahn passt.
- Eine Zelle mit mehr als zwei genutzten Pipe-Öffnungen (T/Kreuz) zeigt
  weiterhin nur eine Rille mit zwei Enden - dieselbe Grenze, die das
  bestehende Rendering für eine echte, vollständige Bahn schon hatte.
- Kameraframing (`TryGetTracksWorldBounds`) bezieht Preview-Blöcke bewusst
  nicht ein, unverändert wie bisher bei Filler-Blöcken.

Der Unity-Editor war beim Nutzer bereits offen, daher kein frischer
Batchmode-Kompilierungslauf möglich - Änderungen manuell auf
Klammer-/Typkorrektheit und Aufrufreihenfolge geprüft.

### Korrektur: Preview hing an der BFS ab einem Start

Erste Umsetzung griff zu kurz - beim Test waren neben der validen Bahn nur
5 Blöcke mit Rille zu sehen statt der erwarteten ~25. Ursache: der
Preview-Pass lief ausschließlich über `PathValidationResult.ConnectedOrder`,
also über die Zellen, die PathValidator **von einem Start aus** erreichen
kann. Eine Karte, die (noch) keine durchgehende Verbindung zu einem Start
hat - im Testlevel die große Mehrheit - wurde davon nie berührt und fiel
weiter auf flaches Filler-Terrain zurück.

Umgestellt: `SyncPreviewBlocks` iteriert jetzt das ganze Grid und baut für
**jede** Zelle mit Karte einen Block, unabhängig von jeder Erreichbarkeit.
Die Form kommt dabei ausschließlich aus den eigenen Öffnungen der Karte
(`PipeDefinition.Connections`), nicht mehr aus Eltern/Kind-Richtungen im
BFS-Baum - das ist auch inhaltlich richtiger: gezeigt wird die Schiene, die
auf der Karte gezeichnet ist. Die BFS dient nur noch der Höhenkette
(`ResolvePreviewHeights`), Zellen ohne Start-Anbindung fallen auf ihr
eigenes `ResolveStartHeight(cell)` zurück.

Zwei Umrechnungen, die dabei nötig wurden:

- `Connections` beschreibt die **Seiten**, an denen eine Pipe offen ist, ein
  `TrackBlock` dagegen die **Fahrtrichtungen** der Murmel. Eintritt durch
  eine Seite heißt Fahrt in die Gegenrichtung, daher das `Opposite()`. Zwei
  Öffnungen auf derselben Achse ergeben so eine gerade Schiene, zwei auf
  verschiedenen Achsen eine 90-Grad-Kurve. Ohne diese Umrechnung hätte eine
  gerade Up/Down-Pipe ein `SetCurve(Up, Down)` bekommen - eine
  180-Grad-"Kurve" ohne gültige Geometrie.
- Von den 40 Karten im Testlevel haben 8 nur **eine** Öffnung: das sind die
  Start-/Ziel-Karten. Für die bedeutet eine einzelne Öffnung nicht
  "Sackgasse", sondern Mund bzw. Einlauf. Sie bekommen deshalb dasselbe
  `ClosedEndGrooveBlockProfile` + `TunnelPortalDecoration` wie eine echte
  Bahn (`closedAtEntry: isStart`). Über die generische Regel wäre die
  Schiene jedes Starts in die falsche Richtung seiner eigenen Achse
  gezeigt worden.

Eine Normal-Karte mit nur einer Öffnung (echte Sackgasse) läuft weiterhin
gerade durch ihre eigene Achse - für eine halbe, an der Blockmitte endende
Rille gibt es kein Profil außer dem Start/Ziel-Tunnel, und der wäre dort
inhaltlich falsch.
