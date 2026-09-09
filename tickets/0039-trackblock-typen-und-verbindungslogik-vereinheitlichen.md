---
id: 0039
title: TrackBlock-Typen und Verbindungslogik vereinheitlichen
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-09
---

# 0039 - TrackBlock-Typen und Verbindungslogik vereinheitlichen

## Beschreibung

### Ziel

Die TrackBlock-Komponente soll überarbeitet und vereinheitlicht werden, sodass unterschiedliche Blocktypen definiert und mehrere aufeinanderfolgende TrackBlocks konsistent miteinander verbunden werden können. Insbesondere soll sichergestellt werden, dass normale TrackBlocks ohne Höhenversatz aneinander anschließen, während TriggerBlocks gezielt Höhenunterschiede für Fall- und Trigger-Mechaniken unterstützen.

### Blocktypen

Ein TrackBlock besitzt einen **Typ**, der sein Verhalten und seine benötigten Attribute bestimmt:

* **Start**
  * Startpunkt der Kugel liegt in der Mitte des Blocks.
  * Die Kugel kann sich in eine der vier Richtungen bewegen.
  * Keine Fallhöhe zum vorherigen Block erforderlich.

* **Goal**
  * Enthält eine Zielschale in der Mitte des Blocks.
  * Die Kugel kann aus einer der vier Richtungen in das Goal einlaufen.
  * Keine Fallhöhe zum vorherigen Block erforderlich.

* **Normal**
  * Besitzt eine definierte **Input-/Eingangsrichtung** und **Output-/Ausgangsrichtung**.
  * Aus der Kombination beider Richtungen ergibt sich die Geometrie des Blocks, z. B.:
    * gerade Strecke
    * 90°-Kurve
  * Der Block soll höhengleich an den vorherigen Block anschließen.
  * Die Geometrie bzw. Höhe des Blocks muss sich daher anhand der Verbindung zum vorherigen Block entsprechend ausrichten.

* **Trigger**
  * Dient als Basis für Sound- und Visual-Trigger, z. B. ein Xylophon-Element.
  * Besitzt eine definierte **Input-/Eingangsrichtung**, über die bestimmt wird, von welcher Seite die Kugel auf den Block gelangt.
  * Besitzt eine definierte **Output-/Ausgangsrichtung**, über die die Kugel den Block wieder verlässt.
  * Zusätzlich besitzt der Block eine **Fallhöhe zum vorherigen Block**.
  * Die Kugel fällt aus dem vorherigen Track-Level auf das Trigger-Element (z. B. Xylophon) und gelangt anschließend wieder auf den Groove, um ihre Bewegung in der definierten Ausgangsrichtung fortzusetzen.
  * Die Eingangsrichtung ist insbesondere für die korrekte Positionierung und Ausrichtung des Trigger-Elements relevant.

### TrackBlock-Attribute

Ein TrackBlock soll grundsätzlich folgende Informationen abbilden können:

* **Type** – Start, Goal, Normal oder Trigger
* **Input Direction** – Richtung, aus der die Kugel auf den Block gelangt
* **Output Direction** – Richtung, in der die Kugel den Block verlässt
* **Fall Height** – Höhenunterschied zum vorherigen Block; insbesondere für TriggerBlocks relevant
* **Surface Inclination** – Neigung der Track-Oberfläche zur visuellen und physikalisch plausiblen Anpassung der Kugelgeschwindigkeit

### Verbindungslogik

Das System soll ermöglichen, mehrere TrackBlocks zu einer durchgängigen Strecke zu verbinden.

Für **Start-, Goal- und Normal-Blocks** wird zunächst von einer höhengleichen Verbindung ausgegangen. Bei NormalBlocks muss anhand der Input-Richtung und des vorherigen Blocks gewährleistet werden, dass die beiden Trackflächen ohne sichtbaren Höhenversatz ineinander übergehen.

Bei einem **TriggerBlock** darf dagegen bewusst ein Höhenunterschied zum vorherigen Block definiert werden. Dieser Höhenunterschied bildet die Fallhöhe der Kugel auf das jeweilige Trigger-Element. Anschließend wird die Kugel wieder auf die Track-Oberfläche geführt und bewegt sich entsprechend der definierten Output-Richtung weiter.

Die Blockdefinition soll damit als Grundlage für ein **generisches Track-System** dienen, in dem beliebig viele unterschiedliche TrackBlocks hintereinander angeordnet und geometrisch konsistent miteinander verbunden werden können.

## Details

Betrifft voraussichtlich `Assets/Scripts/Grid/TrackBlock.cs`, `TrackBlockSpawner.cs`, `BlockDefinition.cs` sowie die Profile (`IBlockProfile.cs`, `GrooveBlockProfile.cs`, `FlatBoxProfile.cs`, `ClosedEndGrooveBlockProfile.cs`, `GrooveProfileUtility.cs`) und den Xylophon-Trigger (`XylophonePadContent.cs`, `BlockTrigger.cs`). Baut auf bereits umgesetzten Tickets [[0018]] (universelles Block-Prefab), [[0019]] (Gefälle aus Pfadrichtung), [[0020]] (Höhenunterschied aus Pfad), [[0022]] (Rollen-Prefabs) und [[0023]]/[[0024]] (Trigger-System) auf und vereinheitlicht/erweitert deren Ergebnisse um explizite Blocktypen (Start/Goal/Normal/Trigger) mit Input-/Output-Richtung, Fall Height und Surface Inclination als zentrale Attribute.

## Notizen

**2026-09-09:** Umgesetzt gemäß Implementierungsplan (siehe
`C:\Users\flori\.claude\plans\zazzy-zooming-sphinx.md`):
`BlockType`-Enum, `ITriggerCellContent`/`TriggerCellContent`
(`SoundTriggerContent`/`XylophonePadContent` vereinheitlicht),
`BlockDefinition` um `InputDirection`/`OutputDirection`/`FallHeight`/
`SurfaceInclination` erweitert, `TrackBlockSpawner` auf eine laufende
Höhen-Verkettung umgestellt (Start/Goal/Normal höhengleich, nur
Trigger-Blocks mit expliziter Fallhöhe). Die 90°-Kurven-Mesh-Geometrie
wurde bewusst ausgeklammert und als eigenes Ticket [[0040]] angelegt.
Noch offen: manuelle Verifikation im Unity Editor (Play-Mode-Test mit
und ohne Trigger-Content) - Status bleibt bis dahin Open.

**2026-09-09 (Nachbesserung nach User-Feedback):** Vier Punkte
umgesetzt:
1. **Stetige negative Steigung:** neues Feld `normalInclinationDegrees`
   (Default 3°) - jeder gerade Normal-Block bekommt jetzt eine leichte
   Dauer-Neigung in Rollrichtung statt `SurfaceInclination = 0`.
   Kurven-Normal-Blocks bleiben explizit flach (`isTurn`-Ausnahme in
   `ComputeSurfaceInclination`), wie gewünscht.
2. **Gemeinsame Bodenhöhe:** `TrackBlockSpawner.BuildTrack` löst jetzt
   `height = entryY - localEntryY` (mit `minBlockHeight`-Sicherheitsnetz,
   Feld wieder eingeführt) und setzt DENSELBEN Wert für
   `transform.position.y` UND `block.Height` - dadurch landet der
   Boden jedes Blocks (`TrackBlock.BottomY = -height`) immer exakt bei
   Welt-Y 0, nur die Körperdicke variiert. Ersetzt den vorherigen
   `bodyHeight`-Ansatz (konstante Dicke, wandernder Boden), der dem in
   einem früheren Ticket festgelegten Verhalten widersprach.
3. **Kinematic3D folgt der Kurve:** `TrackBlock.SampleGroovePointLocal(t)`
   liefert für kurvige Blocks den tatsächlichen Punkt auf dem Bogen
   (nicht nur die Höhe); `TrackBlockSpawner.SampleTrackPosition` nutzt
   das für jeden `IsCurved`-Block (X/Z/Y zusammen), für gerade Blocks
   bleibt die bisherige, günstigere Zellmitten-Interpolation
   unverändert (kein Regressionsrisiko dort).
4. **Xylophon-Geometrie vereinfacht:** `XylophonePillowDecoration`
   (rundes Hufeisen-Element) entfernt, ersetzt durch
   `XylophoneBlockDecoration` - ein einfacher quaderförmiger Block,
   positioniert auf der tatsächlichen Eingangsseite
   (`InputDirection`, Yaw-bereinigt). Rillen-Geometrie
   (`ClosedEndGrooveBlockProfile`) unverändert.

Beim Gegenprüfen des Arbeitsverzeichnisses fiel auf, dass
`Assets/Levels/Level_Prototype.asset` durch eine offenbar im
Hintergrund laufende Unity-Instanz verändert worden war (mehrere
`Sound_Hat`-Content-Referenzen auf `{fileID: 0}` zurückgesetzt,
vermutlich durch einen Reload während der Skript-Änderungen) - wieder
auf den committeten Stand zurückgesetzt (`git checkout`), da das nicht
beabsichtigt war.

Weiterhin nicht in dieser Umgebung testbar (kein Unity-Editor) - bitte
im Editor verifizieren, insbesondere: Neigung auf geraden Strecken
sichtbar aber dezent, Blockböden optisch auf einer Ebene, Kugel folgt
im Kinematic3D-Modus sichtbar der Kurve, Xylophon-Block sitzt auf der
richtigen (Eingangs-)Seite.

**2026-09-09 (zweite Nachbesserung):** Drei weitere Punkte nach
erneutem User-Feedback:
1. **Xylophon-Pad auf falscher Seite + Form zu klobig:** Ursache
   gefunden - die Positionierung nutzte `InputDirection` direkt statt
   deren Gegenrichtung; bei Geradeausfahrt (der einzige heute
   unterstützte Fall, Trigger-Blocks biegen nie ab) landete das Pad
   dadurch systematisch auf der Ausgangs- statt der Eingangsseite. Da
   die geschlossene Rillen-Hälfte (`ClosedEndGrooveBlockProfile`)
   ohnehin immer fix auf lokal -Z liegt, wird die Richtung jetzt gar
   nicht mehr berechnet, sondern das Pad direkt dort verankert -
   robuster und behebt den Seiten-Bug an der Wurzel.
   `XylophoneBlockDecoration` komplett neu: statt Quader jetzt eine
   Kapsel-Form (länglich entlang der Fahrtrichtung/lokal Z, an beiden
   Enden abgerundet), eingepasst in die geschlossene Rillen-Hälfte
   zwischen Blockkante und Wand.
2. **Zu wenig Fallhöhe:** Default von `TriggerCellContent.fallHeight`
   von 0.15 auf 0.3 verdoppelt - betraf noch keine gespeicherten
   Assets (siehe erste Nachbesserung), Änderung wirkt daher überall.
3. **Kugel "setzt zurück" in Kurven:** Ursache gefunden - die Kurve
   startet/endet geometrisch eine halbe Zelle VOR/NACH der
   Zellmitte (an der wahren Kanten-Eintritts-/Austrittsstelle), während
   gerade Blocks weiterhin Zellmitte-zu-Zellmitte interpolieren (siehe
   vorherige Nachbesserung) - an der Nahtstelle sprang die Position
   dadurch um eine halbe Zelle. Fix: `TrackBlock.SampleGroovePointLocal`
   verankert die Kurve jetzt an beiden Enden stetig auf die
   Zellmitten-Konvention zurück (linear ausgeblendeter Versatz,
   `Lerp(radius*curveInVec, radius*curveOutVec, t)`), sodass Start/Ende
   exakt mit den Nachbarblöcken übereinstimmen und die Kugel trotzdem
   sichtbar dem Bogen folgt.

Beim Speichern erneut aufgefallen: `Assets/Levels/Level_Prototype.asset`
wurde wieder von der Hintergrund-Unity-Instanz verändert (gleiches
Muster wie zuvor) - wieder zurückgesetzt. Das scheint bei jeder
Skript-Neukompilierung zu passieren; ggf. lohnt es sich, das separat
zu untersuchen (z.B. ob Unity beim Domain-Reload ungespeicherte
Level-Änderungen verwirft/überschreibt).

Weiterhin nicht in dieser Umgebung testbar - bitte im Editor
verifizieren.

**2026-09-09 (dritte Nachbesserung):** Alle drei Punkte der zweiten
Nachbesserung waren noch nicht korrekt behoben:

1. **Xylophon-Pad immer noch falsche Seite:** der "einfach fix auf
   lokal -Z"-Ansatz ging davon aus, dass Trigger-Blocks nie abbiegen -
   trifft aber nicht auf den Test-Fall zu (Eingangs- ≠
   Ausgangsrichtung). Fix: `InputDirection` wird jetzt tatsächlich
   ausgewertet, aber mit korrigiertem Vorzeichen - die Kugel bewegt
   sich IN `InputDirection` HIER AN, kommt also von der
   ENTGEGENGESETZTEN Seite; das vorherige Vorzeichen war genau
   verkehrt (positionierte auf der Ausgangs- statt Eingangsseite -
   exakt der gemeldete Bug). `fallSideLocal = -InputDirection`,
   Yaw-bereinigt, wählt jetzt die tatsächlich passende lokale Achse
   (X oder Z, je nach Eingangsrichtung) statt fix Z anzunehmen.
2. **Form wieder verschlechtert:** Kapsel-Form zurückgenommen, wieder
   ein einfacher Quader (wie ursprünglich gewünscht), jetzt aber
   länglicher (Tiefe/Breite-Verhältnis von 0.5/0.9 auf 1.05/0.55
   gedreht und verstärkt) und entlang der tatsächlichen Fallrichtung
   ausgerichtet statt fix entlang Z.
3. **Kugel fährt nur bis zur Mitte, dann 90°-Knick:** Ursache des
   Blend-Ansatzes aus der zweiten Nachbesserung gefunden - das
   Verschieben der Kurven-Endpunkte auf die Zellmitten hat die Kurve
   auf die doppelte Länge gestreckt (echte Sehne Eingang→Ausgang ist
   bei 90° nur ca. 71% der Zellgröße, Zellmitte-zu-Zellmitte ist eine
   volle Zelle) - der lineare Ausgleichsterm dominierte dadurch
   sichtbar über die eigentliche Bogenform. Kompletter Kurswechsel:
   `SampleGroovePointLocal` liefert jetzt wieder den unveränderten,
   echten Bogen (kein Blend mehr). Stattdessen zielen die
   GERADEN Nachbarblöcke jetzt direkt auf den echten Kurven-Ein-/
   Ausgang statt auf die Zellmitte (neue Methode
   `TrackBlockSpawner.JunctionWorldPoint`) - so bleibt die Übergabe
   nahtlos, ohne die Bogenform selbst zu verzerren. Start/Goal bleiben
   bewusst zellmitten-basiert (passend zur Tunnel-Deko); eine Kurve
   direkt neben Start/Goal wäre davon noch betroffen, kommt aber im
   aktuellen Design nicht vor (Kurven nur auf inneren Normal-Blocks).

Weiterhin nicht in dieser Umgebung testbar - bitte im Editor
verifizieren, insbesondere mit einem Trigger-Block, dessen Eingangs-
und Ausgangsrichtung sich unterscheiden (der tatsächliche Test-Fall).

**2026-09-09 (vierte Nachbesserung):**
1. **Fallhöhe erneut verdoppelt:** `TriggerCellContent.fallHeight`
   Default von 0.3 auf 0.6.
2. **Kugel nimmt "sehr komischen Pfad" in der Kurve:** konkreten Bug
   in `JunctionWorldPoint` gefunden - für einen gekrümmten Block wurde
   `EntryPointLocal`/`ExitPointLocal` verwendet, die IMMER die gerade
   -Z/+Z-Formel liefern, unabhängig davon, ob der Block gekrümmt ist.
   Der echte Kurven-Ein-/Ausgang liegt aber bei einer Abbiegung oft
   entlang lokal X statt Z - dadurch zielte der gerade Nachbarblock auf
   einen komplett falschen Punkt (nicht nur leicht verschoben). Fix:
   `atK.SampleGroovePointLocal(0f)` /
   `beforeK.SampleGroovePointLocal(1f)` statt
   `EntryPointLocal`/`ExitPointLocal` - diese Methode ist bereits
   kurven-bewusst (nutzt `curveInVec`/`curveOutVec` korrekt).
3. **Xylophon-Pad falsch herum skaliert:** X/Z-Zuordnung in
   `XylophoneBlockDecoration` vertauscht (Element liegt jetzt QUER zur
   Fallrichtung, wie ein echter Xylophon-Klangstab, statt längs dazu)
   und das Verhältnis von 1.05/0.55 (≈1.9) auf 1.15/0.35 (≈3.3)
   verstärkt für ein schmaleres Element.

Weiterhin nicht in dieser Umgebung testbar - bitte im Editor
verifizieren.

**2026-09-09 (fünfte Nachbesserung):**
1. **Fallhöhe erneut verdoppelt:** `TriggerCellContent.fallHeight`
   Default von 0.6 auf 1.2.
2. **Xylophon-Pads: komische, lineare Murmelbewegung ("Abkürzung
   Eingangs-/Ausgangskante"):** Ursache war eine echte Regression aus
   der vierten Nachbesserung - `JunctionWorldPoint` hat die Höhe (Y)
   des Übergangspunkts NEU aus dem NACHBAR-Block berechnet statt aus
   dem eigenen Block; dadurch wurde der bewusste, abrupte Fallhöhen-
   Sprung eines Trigger-Blocks über das GESAMTE vorherige Segment
   verschmiert, statt exakt an der Blockgrenze zu passieren - je
   größer die Fallhöhe (nach den Verdopplungen), desto sichtbarer der
   Effekt ("gleitet linear ab" statt "rollt normal, fällt dann ab").
   Fix: X/Z und Y jetzt strikt getrennt behandelt - `JunctionLocalXZ`
   bleibt für die Kurven-Anschluss-Korrektur zuständig (X/Z), aber die
   Höhe kommt wieder ausschließlich aus dem eigenen Block
   (`SampleFloorY`, unverändert zum ursprünglichen Verhalten vor allen
   Kurven-Anpassungen) - der Fallhöhen-Sprung ist dadurch wieder exakt
   an der Blockgrenze, nicht mehr verschmiert. `JunctionWorldPoint`
   dabei in `JunctionLocalXZ` umbenannt und arbeitet jetzt komplett in
   Spawner-lokalem Raum (kein World-Roundtrip mehr nötig).
3. **Xylophon-Pad-Geometrie:** Y-Skalierung (Höhe) halbiert
   (`grooveRadius * 1.1` → `* 0.55`); Abstand zur Blockmitte von
   `halfCell * 0.5` auf `* 0.65` erhöht (näher am äußeren Rand).

Weiterhin nicht in dieser Umgebung testbar - bitte im Editor
verifizieren, insbesondere den Trigger-Block-Fall (sollte jetzt
wieder ein sichtbar abrupter Fall statt eines Gleitens sein) und die
normale Kurve (sollte unverändert korrekt bleiben, da hier
ausschließlich die X/Z-Trennung, nicht die Kurven-Logik selbst,
angepasst wurde).

**2026-09-09 (Abschluss):** User hat den Kern (Blocktypen,
Verbindungslogik, Steigung, Bodenhöhe) abgenommen - Status auf Done
gesetzt. Feinschliff an der Kinematic3D-Bewegung der Kugel (Kurven,
Trigger-Fall) wird als eigenes Ticket [[0041]] weitergeführt.
