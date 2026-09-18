---
id: 0047
title: Umgebende Blöcke mit interpolierter Höhe
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-16
---

# 0047 - Umgebende Blöcke mit interpolierter Höhe

## Beschreibung

Surrounding Blocks. Es sollen nicht nur die relevanten Murmelbahnen als
Blöcke dargestellt werden, sondern diese sollen auch von benachbarten
Blöcken umgeben sein (Ohne Murmelbahn). Sprich: Diejenigen Blöcke, die um
die Sublevels drumherum liegen und auch die nicht "bebaubaren" (Ticket 45)
Blöcke sollen visualisiert werden und unterschiedliche Höhen abbilden.
Dabei soll sich die Höhe dieser Blöcke, die keine Murmelbahn enthalten,
daran orientieren wie hoch die benachbarten Blöcke sind. Wenn zum Beispiel
ein nicht Murmelbahn Block einen rechten und linken Nachbarn hat, dann soll
er sich von der Höhe genau dazwischen befinden. Wenn solch ein Block nur
einen direkten Nachbarn mit Murmelbahn hat, dann soll er auch den nächsten
Nachbarn in die gleiche Richtung berücksichtigen. Ist ein stetiges Gefälle
in Richtung der Nachbarn vorhanden, soll dieser Block entsprechend auch
steigen. Mit diesem Vorgehen soll die Murmelbahn in eine Umgebung gesetzt
werden, die die Höhenunterschiede realistisch fortsetzt und so das Terrain
definiert.

## Details

Betroffene Dateien/Systeme:

- `Assets/Scripts/Grid/TrackBlockSpawner.cs` - spawnt aktuell ausschließlich
  Blöcke entlang der validierten Murmelbahn (Start→Goal, siehe
  `runningExitY`/`height`-Kette ab ca. Zeile 348). Höhe wird nur pro
  Track-Block aus dem vorherigen Bahn-Block abgeleitet (0039). Für
  umgebende Blöcke ohne Murmelbahn braucht es eine neue Spawn- und
  Höhenlogik, die stattdessen von den Höhen der Nachbar-Zellen im Grid
  ausgeht statt von einer Bahn-Kette.
- `Assets/Scripts/Grid/TrackBlock.cs` - hält `Height`/`BottomY` pro Block;
  die gleiche Struktur müsste auch für Nicht-Bahn-Blöcke nutzbar sein.
- `Assets/Scripts/Grid/PathGrid.cs` - kennt Grid-Ausdehnung
  (`Width`/`Height`) und belegte/unbelegte Zellen; liefert die Grundlage,
  um zu bestimmen, welche Zellen "umgebend" (kein Pipe/keine Bahn) sind.
- Ticket [[0045-nicht-bebaubare-zellen-fuer-variable-grid-formen]]: die
  dort eingeführte Markierung nicht bebaubarer Zellen ist laut
  Beschreibung Teil der hier zu visualisierenden Umgebung.
- Ticket [[0046-sublevels-auf-einem-grid-definieren]]: "Blöcke, die um die
  Sublevels drumherum liegen" setzt voraus, dass Sublevel-Bereiche im Grid
  bekannt sind.
- `Assets/Scripts/Grid/TerrainDecoration.cs` /
  `TerrainDecorationSettings.cs` - bestehendes Muster für rein kosmetische,
  pro Block deterministische Zusatz-Geometrie; ggf. Vorbild für die
  visuelle Gestaltung der umgebenden Blöcke.

Akzeptanzkriterien (aus der Beschreibung abgeleitet):

- Zellen ohne Murmelbahn (umliegend oder nicht bebaubar) werden ebenfalls
  als Block dargestellt, nicht nur die Bahn-Zellen.
- Die Höhe eines solchen Blocks orientiert sich an den Höhen seiner
  direkten Nachbarn: bei zwei gegenüberliegenden Nachbarn liegt er genau
  dazwischen.
- Hat ein solcher Block nur in eine Richtung einen direkten
  Bahn-Nachbarn, wird zusätzlich dessen übernächster Nachbar in derselben
  Richtung einbezogen, um ein Gefälle fortzusetzen statt abzuflachen.
- Bei einem durchgehenden Gefälle über mehrere Nachbarn hinweg setzt sich
  dieses Gefälle in den umgebenden Blöcken sichtbar fort (steigt/fällt
  entsprechend), statt abrupt auf eine mittlere Höhe zu springen.

Offene Frage für die Umsetzung: das genaue Interpolations-/Extrapolations-
verfahren für Fälle mit mehr als zwei relevanten Nachbarn oder ganz ohne
direkten Bahn-Nachbarn ist aus der Beschreibung nicht eindeutig ableitbar
und müsste beim Umsetzen festgelegt werden.

## Notizen

Umsetzung in `TrackBlockSpawner.cs` (`SyncFillerBlocks()`, aufgerufen aus
`Update()` neben dem bestehenden `SyncTracks()`):

- `CollectTrackHeights()` sammelt für jede aktuell gespawnte Bahn-Zelle
  (über alle Tracks/Sublevels hinweg) ihre tatsächliche Höhe
  (`TrackBlock.transform.localPosition.y`, identisch zu dem, was
  `BuildTrack` als `height` auch für `TrackBlock.Height` verwendet).
- `SolveFillerHeights()` löst für jede der übrigen Zellen im ganzen
  `Width`x`Height`-Grid (nicht bebaubare, unbenutzte und Zellen außerhalb
  jedes Sublevel-Bereichs) eine Höhe: pro Achse (Links/Rechts,
  Oben/Unten) exakter Mittelwert bei zwei gegenüberliegenden bekannten
  Nachbarn; bei nur einem direkten Nachbarn wird dessen übernächster
  Nachbar in derselben Richtung herangezogen, um das Gefälle per linearer
  Extrapolation (`2*nah - fern`) fortzusetzen. Löst in mehreren
  Durchgängen von außen nach innen auf (eine in Durchgang N gelöste Zelle
  wird in Durchgang N+1 selbst zum gültigen Nachbarn), damit sich ein
  Gefälle über mehrere Blöcke hinweg fortsetzt statt abrupt zu wechseln.
  Zwei Prioritätsstufen: echte Zwei-Punkt-Ableitungen (Mittelwert/
  Extrapolation) werden zuerst über das ganze Grid ausgeschöpft, bevor
  eine Zelle mit nur einem einzelnen bekannten Nachbarn (keine zweite
  Referenz für eine Steigung) diesen einfach flach übernimmt - damit
  gewinnt eine echte Steigung immer gegen eine willkürliche flache
  Fortsetzung, unabhängig von der Verarbeitungsreihenfolge. Für die von
  der Beschreibung offen gelassene Frage (siehe unten) war das die
  gewählte Festlegung. Komplett isolierte Zellen ohne jeden Bezug zu
  einer Bahn-Zelle fallen auf `startHeight` zurück (dieselbe Baseline, an
  der auch die Bahn-Höhenkette hängt).
- `BuildFillerBlock()` instanziiert dafür ein normales `TrackBlock` mit
  `FlatBoxProfile` (flache Planke, keine Rille) statt eines
  Groove-Profils, `BlockType.Filler` (neu in `BlockType.cs`) als
  Kennzeichnung, und dekoriert es wie einen Normal-Block über
  `TerrainDecoration.Scatter` (mit `grooveRadius = 0`, da kein Rillen-
  Bereich freigehalten werden muss).
- Alle Filler-Blöcke hängen unter einem einzigen `FillerBlocks`-Root, der
  komplett neu gebaut wird, sobald sich die Menge/Höhe der Bahn-Zellen
  ändert (Vergleich gegen `lastTrackHeights`) - anders als bei
  `SyncTracks` gibt es hier kein laufendes Murmel-/Zustands-Objekt, das
  über einen Rebuild hinweg erhalten bleiben müsste.

Die Blöcke stehen unabhängig vom Spielzustand immer (auch schon während
der 2D-Planung, bevor überhaupt zum ersten Mal simuliert wird) - sichtbar
werden sie ohnehin erst, wenn die isometrische 3D-Ansicht aktiv ist, genau
wie die echten Bahn-Blöcke auch.

### Follow-up: sanftere Höhenübergänge zu Bahn-Blöcken

Nachtrag nach erstem Test: die reine Extrapolationsregel
(`2*nah - fern`) übernimmt einen großen Höhensprung zwischen zwei
Bahn-Zellen (z.B. die `FallHeight` eines Trigger-Blocks) mit voller
Stärke in die direkt angrenzende umgebende Zelle - das wirkte wie eine
künstliche Klippe statt sanftem Gelände.

Neu in `TrackBlockSpawner.SolveFillerHeights()`: nach der bestehenden
Auflösung (Mittelwert/Extrapolation/Fallback) läuft jetzt
`SmoothFillerHeights()` - eine klassische Laplace-Glättung, die jede
umgebende Zelle über mehrere Durchgänge hinweg schrittweise Richtung
Mittelwert ihrer aufgelösten Nachbarn zieht. Echte Bahn-Zellen werden
dabei nie verändert (bleiben fixe Anker), nur die umgebenden Zellen
"entspannen" sich dazwischen. Zwei neue Inspector-Felder an
`TrackBlockSpawner` steuern die Stärke:

- `fillerSmoothingIterations` (0-20, Default 8) - Anzahl der Glättungs-
  Durchgänge; 0 schaltet die Glättung komplett ab (altes Verhalten).
- `fillerSmoothingStrength` (0-1, Default 0.5) - wie stark jeder Durchgang
  eine Zelle Richtung Nachbar-Mittelwert zieht.

Beide Werte sind bewusst als Inspector-Parameter exponiert, falls sich
"sanfter" je nach Level/Gefälle unterschiedlich anfühlen soll.

### Follow-up 2: 3D-Blöcke existierten schon während der 2D-Planung

Nachtrag: `TrackBlockSpawner.Update()` hat `SyncTracks()`/`SyncFillerBlocks()`
schon immer unbedingt jeden Frame aufgerufen - auch während der 2D-Planung,
bevor überhaupt zum ersten Mal simuliert wurde. Das war vorher kaum
sichtbar (nur die paar tatsächlichen Bahn-Blöcke existierten dann im
Hintergrund), aber seit die Filler-Blöcke praktisch das GANZE Grid
abdecken, war das im Scene-View deutlich auffällig und widerspricht dem
eigentlich beabsichtigten Verhalten "die 3D-Bahn baut sich erst beim
2D-zu-3D-Wechsel auf".

Behoben: `TrackBlockSpawner.Update()` wurde entfernt. Stattdessen:

- `TrackBlockSpawner.RebuildNow()` (neu, public) führt `SyncTracks()` +
  `SyncFillerBlocks()` einmalig und synchron aus. `MarbleController.Play()`
  ruft das jetzt direkt zu Beginn auf, noch bevor Marble-Coroutinen
  gestartet werden - dadurch existieren die Blöcke garantiert rechtzeitig
  für `CameraModeTransition`s Bounds-Berechnung und die Murmel's eigenen
  `HasTrackFor`-Wartelogik, unabhängig von der (laut bestehendem
  Kommentar ohnehin undefinierten) Update()-Reihenfolge zwischen den
  Komponenten.
- `TrackBlockSpawner.ClearAll()` (neu, public) räumt alle Bahn- und
  Filler-Blöcke wieder ab. `MarbleController.Stop()` ruft das auf - `Stop()`
  wird sowohl von der S-Taste als auch (über `ResetMarble()`) von R und
  vom Sublevel-Wechsel (N) erreicht, deckt also jeden Weg zurück in den
  "nicht spielend"-Zustand ab.

Damit existiert die gesamte 3D-Visualisierung (Bahn + Umgebung) nur noch
genau zwischen einem Play und dem nächsten Stop/Reset/Sublevel-Wechsel -
nie während der reinen 2D-Planung.

### Follow-up 3: Filler-Terrain sollte progressiv mit den Sublevels aufgedeckt werden

Nachtrag: `SyncFillerBlocks()` hat bisher das GESAMTE Grid mit Filler-
Blöcken gefüllt, unabhängig davon, welche Sublevels bereits erreicht
waren (siehe [[0046-sublevels-auf-einem-grid-definieren]]). Im ersten
Sublevel waren dadurch schon die Umgebungs-Blöcke des zweiten (noch gar
nicht erreichten) Sublevels sichtbar - gewünscht war stattdessen, dass im
ersten Sublevel wirklich nur dessen eigene 3D-Blöcke stehen, und im
zweiten Sublevel die Blöcke von beiden (kumulativ, passend zum
"mehrere Sublevels laufen gleichzeitig"-Verhalten aus Follow-up 2 des
Tickets 0046).

Behoben: `SolveFillerHeights()` nimmt eine Zelle nur noch in die
Berechnung auf, wenn `grid.IsInUnlockedSubLevel(coord)` wahr ist (Index
ihres Sublevels <= `ActiveSubLevelIndex`) - alles andere bleibt komplett
unberührt, bekommt also weder Bahn- noch Filler-Block. Nebeneffekt bei
Sublevel-definierten Leveln: eine Zelle außerhalb JEDES Sublevel-Bereichs
("Lücke" im großen Grid) wird jetzt gar nicht mehr befüllt, statt wie
ursprünglich in diesem Ticket vorgesehen immer als Umgebung mitzulaufen -
das war nötig, um "noch nicht erreicht" sauber von "gehört zu keinem
Sublevel" unterscheidbar zu halten; bei Leveln ganz ohne Sublevels ändert
sich nichts (weiterhin das komplette Grid).

### Follow-up 4: einstellbare Start-Höhe pro Sublevel

Weitere Anforderung: die Starthöhe der Bahn (`TrackBlockSpawner.startHeight`,
bisher ein einzelner globaler Wert für den ganzen Spawner) sollte sich im
Level Grid Editor pro Sublevel einstellen lassen - sinnvoll, weil mehrere
Sublevels jetzt gleichzeitig sichtbar sein können (siehe Follow-up 3) und
dafür unterschiedliche Höhenniveaus brauchen können.

- `SubLevelDefinition` (siehe 0046, `LevelData.cs`) bekommt ein neues
  Feld `startHeight` (Default 1, wie der bisherige globale Wert) mit
  Property `StartHeight` und Setter `SetStartHeight` - plus
  `LevelData.SetSubLevelStartHeight(index, value)` als öffentliche
  Mutator-Methode (analog zu `SetSubLevelArea`/`SetSubLevelName`).
- `PathGrid.TryGetSubLevelIndexAt(coord, out index)` (vorher `private`,
  intern schon für `IsInUnlockedSubLevel`/`GetOwnSubLevelArea` genutzt)
  ist jetzt `public`, damit `TrackBlockSpawner` das Sublevel einer Zelle
  nachschlagen kann.
- Neue `TrackBlockSpawner.ResolveStartHeight(coord)`: liefert die
  `StartHeight` des Sublevels, zu dem die Zelle gehört, oder den
  bisherigen globalen `startHeight`-Wert als Fallback (Zelle gehört zu
  keinem Sublevel, oder es gibt gar keine Sublevels). Wird sowohl in
  `BuildTrack()` (statt des bisherigen globalen Werts) als auch in
  `SolveFillerHeights()`s Fallback für komplett isolierte Filler-Zellen
  verwendet.
- Im Level Grid Editor: jede Sublevel-Zeile im SubLevels-Panel hat jetzt
  ein "Y0"-Feld (mit Tooltip) zum direkten Einstellen der Start-Höhe.
