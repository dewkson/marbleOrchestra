---
id: 0038
title: Kinematischer Bewegungs-Trace pro Block-Variante definierbar
type: Feature
priority: Medium
status: Done
area: Physics
created: 2026-09-04
---

# 0038 - Kinematischer Bewegungs-Trace pro Block-Variante definierbar

## Beschreibung

Für die einzelnen Block-Varianten soll definierbar sein, welchen Pfad
die Kugel im kinematischen 3D-Modus tatsächlich nimmt, und wo sie wie
schnell ist, um auch Sprünge simulieren zu können. Da die Blöcke
unterschiedlich sind und die Kugel sich jeweils anders verhält, wäre es
sinnvoll, wenn man den Trace pro Block definieren kann.

## Details

- Betrifft `MovementMode.Kinematic3D` aus [[0014]]: aktuell liefert
  `TrackTerrainGenerator.SampleGroovePosition()` für eine fraktionale
  Bahn-Position eine Weltposition auf dem Rinnenboden, mit konstantem
  Tempo (`cellsPerSecond`) über `MarbleController.RunAlongPath3D()` -
  ein einheitlicher Bewegungsverlauf für alle Blöcke.
- Hängt eng mit [[0020]]/[[0021]] zusammen (Höhensprung zwischen
  Blöcken, dort bewusst nicht geglättet) und mit der in [[0022]]
  ergänzten Möglichkeit, Übergänge als Sprung oder nahtlos zu
  definieren - ein Sprung-Übergang bräuchte vermutlich einen eigenen
  Trace (z.B. Wurfparabel mit Geschwindigkeitsänderung), während ein
  nahtloser Übergang dem bisherigen Rinnenverlauf folgt.
- Passt konzeptionell zu [[0027]] (`BlockDefinition`, datengetriebenes
  Modell pro Block) - der Trace (Pfad + Geschwindigkeitsprofil) wäre ein
  weiterer, pro Block-Variante konfigurierbarer Aspekt in diesem Sinne.
- Akzeptanzkriterien (grobe erste Fassung):
  - Jede Block-Variante kann ihren eigenen Bewegungs-Trace für die
    Kugel im Kinematic3D-Modus definieren (Positionsverlauf über die
    Blocklänge, nicht nur die bisherige lineare Interpolation).
  - Der Trace kann zusätzlich ein Geschwindigkeitsprofil festlegen
    (Kugel muss nicht mit konstantem Tempo durch den Block laufen).
  - Damit lassen sich Sprünge (z.B. kurze Flugphase mit Beschleunigung/
    Verzögerung) pro Block-Variante simulieren, ohne die Physics3D-Engine
    zu verwenden.

## Notizen

**2026-09-10 (umgesetzt):** Ausgelöst durch das Feedback aus [[0041]]
(Kugel gleitet an Trigger-Blocks diagonal von Eingangs- zu Ausgangspunkt,
statt zu fallen). Statt einer weiteren Einzelkorrektur an der alten,
aus der Nachbargeometrie rückgerechneten Bewegung ist der Trace jetzt
wie hier beschrieben ein eigenes, pro Block-Variante definierbares
Konzept:

- Neues Interface `IMarbleTrace` (`Duration`/`ImpactTime`/`SampleLocal`),
  parametrisiert über die ZEIT statt über die Strecke - dadurch sind
  Beschleunigung, Sprünge und Pausen pro Block möglich
  (Akzeptanzkriterium "Geschwindigkeitsprofil"). `speed` ist das
  Basistempo in lokalen Einheiten/Sekunde
  (`MarbleController.CellsPerSecond * PathGrid.CellSize`); rollende
  Anteile skalieren damit, ein echter Fall folgt dagegen der Gravitation.
- Jeder Trace deckt exakt die EIGENE Ausdehnung seines Blocks ab (Kante
  zu Kante), nicht mehr Zellmitte zu Zellmitte. Aufeinanderfolgende
  Traces stoßen dadurch exakt aneinander; der halbzellen-Versatz
  zwischen X/Z- und Höhen-Interpolation, der die alte Lösung überhaupt
  erst nötig machte, existiert nicht mehr (siehe [[0041]]).
- Implementierungen: `StraightMarbleTrace` (gerade Blocks sowie
  Start/Goal, dort verkürzt auf das eigene Rillen-Ende `WallZ`, sodass
  die Kugel im Tunnelmaul erscheint/verschwindet), `CurvedMarbleTrace`
  (folgt `TrackBlock.SampleGroovePointLocal`, Kurve dauert entsprechend
  ihrer echten Bogenlänge etwas kürzer als eine Gerade) und
  `TriggerFallMarbleTrace` (Fall auf die Xylophon-Leiste, kurzer
  Absprung in die Rille, Ausrollen).
- `TrackBlock.Trace`/`SetTrace` als Gegenstück zu `Profile` (Form vs.
  Bewegung); gesetzt von `TrackBlockSpawner.CreateTrace` - die eine
  Stelle, an der pro Block-Variante entschieden wird, wie sich die Kugel
  bewegt. `TrackTraceSegment` bündelt Block + Trace + Tempo für
  `MarbleController`.
- `MarbleController.RunAlongPath3D` spielt nur noch Trace für Trace ab
  und feuert den Block-Trigger an dessen `ImpactTime` (echter Aufprall
  auf der Leiste) statt beim Überschreiten der Zellmitte.
- Entfallen sind damit `SampleGroovePosition`, `JunctionLocalXZ`,
  `SpawnerLocalXZ`, `SampleFloorY` und das kosmetische
  `fallTiltFraction` (ein Trigger-Block ist jetzt einfach flach, der
  Fall steckt vollständig im Trace).

Nicht in dieser Umgebung visuell prüfbar (kein Unity-Editor) - die
Geometrie wurde stattdessen numerisch gegengerechnet (Weltposition von
Trace-Anfang/-Ende je Block über eine Kette Start -> Gerade -> Kurve ->
Trigger -> Gerade -> Goal: alle Übergänge lückenlos). Sichtprüfung im
Editor läuft über [[0041]].

**2026-09-10 (Nachschärfung: Takt):** User-Vorgabe - jeder Block-Trace
muss GENAU GLEICH LANG dauern, sonst funktioniert die musikalische
Komponente nicht. Die erste Fassung leitete die Dauer aus der Geometrie
ab (echte Fallzeit über `fallGravity`, Bogenlänge/Tempo bei Kurven), ein
Trigger-Block dauerte damit bis zu 3x so lang wie eine Gerade. Das
Zeitmodell ist deshalb umgestellt:

- `IMarbleTrace` ist jetzt über den eigenen TAKT des Blocks
  parametrisiert: `SampleLocal(float t)` mit t = 0..1, und
  `TrackTraceSegment.Duration` ist für JEDEN Block `1 / cellsPerSecond`.
  Eine Zelle ist ein Takt - Punkt. `speed`/`Duration(speed)` sind aus
  dem Interface verschwunden.
- Variieren darf ein Trace nur noch, WIE er seinen Takt verbraucht:
  beschleunigender Fall, Sprungbogen, gleichmäßiges Rollen. Eine Kurve
  fährt entsprechend etwas langsamer als eine Gerade (kürzerer Bogen,
  gleicher Takt) - genau das hält den Rhythmus gleichmäßig.
- Die Fallzeit auf einem Trigger-Block ist ein fester Anteil des Taktes
  (`triggerFallBeatFraction`, Default 0.5) - unabhängig von der
  FallHeight, ein tieferer Fall fällt einfach schneller. Damit klingt
  JEDER Trigger-Block an derselben Stelle seines Taktes; da nur
  Trigger-Blocks überhaupt Ton machen, ist der Abstand zwischen zwei
  Noten immer eine ganze Zahl von Takten. Mit physikalischer Fallzeit
  hing der Versatz dagegen an der jeweiligen FallHeight, also hörbar
  daneben.
- Der Rest des Taktes wird zwischen Absprung und Ausrollen nach
  zurückgelegter Strecke aufgeteilt, damit die Kugel nach der Landung
  gleichmäßig weiterläuft.
- Die Wurfparabeln lösen "Gravitation" und Absprunggeschwindigkeit aus
  Start-/Zielpunkt und Scheitelhöhe (`TriggerFallMarbleTrace.Arc`)
  statt aus einer festen Erdbeschleunigung - dieselbe Formel bedient
  damit den reinen Fall (Scheitel 0 -> Absprunggeschwindigkeit 0),
  den Absprung von der Leiste und den Sonderfall einer Leiste, die
  höher liegt als der Block, von dem die Kugel kommt.
- `fallGravity` ist damit entfallen, `triggerFallBeatFraction` neu.
