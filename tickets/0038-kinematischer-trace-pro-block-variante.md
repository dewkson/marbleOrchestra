---
id: 0038
title: Kinematischer Bewegungs-Trace pro Block-Variante definierbar
type: Feature
priority: Medium
status: In Progress
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

- 2026-09-04: Umgesetzt als neuer Erweiterungspunkt `IBlockMotionTrace`
  (`Assets/Scripts/Grid/IBlockMotionTrace.cs`), analog zu `IBlockProfile`
  (das nur die Mesh-Form definiert) - eine Methode
  `float SampleOffset(float f)`, die bei Fortschritt `f` (0 = Entry,
  1 = Exit) einen zusätzlichen vertikalen Offset auf die bisherige
  lineare Entry-zu-Exit-Höhen-Interpolation addiert (muss bei 0/1 exakt
  0 zurückgeben, damit Blöcke an ihren Grenzen weiterhin nahtlos
  aneinander anschließen - der bewusste Sprung an der Blockgrenze aus
  [[0020]]/[[0021]] bleibt dadurch unverändert, nur das INNERE eines
  Blocks kann jetzt vom linearen Verlauf abweichen).
  `LinearMotionTrace` (Default, Offset immer 0 - bisheriges Verhalten
  unverändert) und `JumpMotionTrace` (Parabel `4*apexHeight*f*(1-f)`,
  peakt bei f=0.5) als konkrete Implementierungen. Bewusst NUR vertikal
  (kein voller `Vector3`-Pfad): X/Z kommen laut Klassenkommentar von
  `TrackBlockSpawner` bewusst aus Grid-Zellmitten statt der
  Block-eigenen, rotierten Entry/Exit-Punkte (Yaw-Mismatch an Kurven,
  siehe [[0019]]/[[0021]]) - ein lateraler Pfad-Offset müsste durch die
  Block-Rotation transformiert werden und hätte dieses bereits gelöste
  Problem wieder aufgerissen. Die "Kurve macht tatsächlich eine Kurve"-
  Anforderung aus [[0022]] bleibt daher bewusst dessen Aufgabe, nicht
  Teil dieses Tickets.
  Kein separater Geschwindigkeits-Parameter: da `f` weiterhin mit
  konstanter Real-Zeit-Rate fortschreitet, erzeugt jeder nichtlineare
  `SampleOffset`-Verlauf automatisch ein Geschwindigkeitsprofil als
  Nebeneffekt (steil an den Rändern, flach am Scheitelpunkt) - eine
  zweite, unabhängige Speed-Achse wurde als unnötige zusätzliche
  Abstraktion verworfen.
  `TrackBlock` bekommt eine neue `MotionTrace`-Property (Default
  `LinearMotionTrace.Instance`, kein `Rebuild()` nötig - reine
  Sampling-Angelegenheit, keine Mesh-Änderung).
  `TrackBlockSpawner.SampleFloorY` addiert `block.MotionTrace.
  SampleOffset(f)` auf die bestehende Lerp-Höhe.
  Da die eigentliche Block-Varianten-Auswahl (welcher Block/welche
  Verbindung einen Sprung bekommt) erst mit [[0022]] existiert, gibt es
  vorerst einen Test-Hook: neues Inspector-Feld `testJumpApexHeight` auf
  `TrackBlockSpawner` (Default 0 = deaktiviert/linear); ist der Wert > 0,
  bekommt JEDER gespawnte Block statt des Default-Traces einen
  `JumpMotionTrace` mit dieser Apex-Höhe - zum Ausprobieren/Tunen im
  Editor, nicht für den Produktivbetrieb gedacht. Noch nicht im Editor
  getestet (kein Editor-Zugriff in dieser Session).
  Kein Rewiring von `MarbleController` nötig - `RunAlongPath3D` läuft
  unverändert über `SampleGroovePosition`, das intern bereits durch
  `SampleFloorY` geht.
