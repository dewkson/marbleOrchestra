---
id: 0040
title: Gebogene Rillen-Geometrie für 90°-Kurven-Blocks
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-09
---

# 0040 - Gebogene Rillen-Geometrie für 90°-Kurven-Blocks

## Beschreibung

Folge-Ticket zu [[0039]] (TrackBlock-Typen und Verbindungslogik
vereinheitlichen), aus dessen Implementierungsplan bewusst ausgeklammert:

Normal- und Trigger-Blocks sollen bei einer 90°-Abbiegung im Pfad
(Input-Richtung ≠ Gegenrichtung der Output-Richtung) eine tatsächlich
gebogene Rillen-Geometrie bekommen, statt wie bisher einen geraden
Groove-Block, der nur an der Output-Richtung ausgerichtet wird. Dadurch
entsteht aktuell ein sichtbarer lateraler Versatz zwischen der
tatsächlichen Eingangsseite des Pfades und dem geraden Entry-Punkt des
Blocks (siehe 0019).

## Details

Betrifft primär `Assets/Scripts/Grid/TrackBlock.cs`
(`Rebuild()`/`AppendTopSurface`/`AppendSideWalls`/`AppendEndCaps`/
`AppendBottom` sweepen aktuell strikt linear entlang der lokalen
Z-Achse - Entry bei `-HalfLength`, Exit bei `+HalfLength`) sowie
`Assets/Scripts/Grid/IBlockProfile.cs` und die vorhandenen Profile
(`GrooveBlockProfile.cs`, `ClosedEndGrooveBlockProfile.cs`).

Aus dem 0039-Plan übernommener technischer Ansatzpunkt: ein neues
Profil-Konzept (analog zu `IClosedEndBlockProfile`), das statt eines
einzelnen linearen Sweeps eine Serie von Ringen entlang eines
beliebigen Pfades liefert (Position + Rotation pro Ring, ähnlich dem
bereits vorhandenen Ring-Sweep-Muster in `TunnelPortalDecoration.cs`/
`XylophonePillowDecoration.cs`, aber mit begehbarer Groove-Fläche,
Collider und korrekten Seitenwänden/Boden für einen nicht-rechteckigen
Footprint - ein Kurven-Block verläuft von der Mitte einer Kantenmitte
zur Mitte einer angrenzenden Kante desselben quadratischen
Zellen-Footprints).

Mit [[0039]] liegt das Datenmodell dafür bereits vollständig vor
(`BlockDefinition.InputDirection`/`OutputDirection` pro Block), sodass
dieses Ticket rein auf der Geometrie-/Meshing-Seite ansetzt, ohne
Typsystem oder Höhenlogik erneut anzufassen.

Akzeptanzkriterien (grobe erste Fassung):
- Ein Normal- oder Trigger-Block mit Input- und Output-Richtung im
  90°-Winkel zueinander zeigt eine sichtbar gebogene Rille, die
  passgenau (kein lateraler Versatz) an die Nachbarblöcke anschließt.
- Der erzeugte Mesh-Collider bleibt wasserdicht und überlappt nicht mit
  Nachbarzellen.
- Gerade Strecken (Input/Output entgegengesetzt) bleiben unverändert
  gegenüber dem heutigen Verhalten.

## Notizen

**2026-09-09:** Umgesetzt gemäß Implementierungsplan (siehe
`C:\Users\flori\.claude\plans\zazzy-zooming-sphinx.md`):
`TrackBlock` bekommt einen komplett separaten Kurven-Pfad
(`SetCurve`/`ClearCurve`, `BuildCurvedMesh` mit 9 Ringen entlang eines
90°-Kreisbogens, hergeleitet aus `InputDirection`/`OutputDirection` als
achsen-parallele Vektoren) - `BuildStraightMesh` (bisheriger Code)
bleibt dabei unverändert, keine Regression für Start/Goal/Trigger/
Geradeaus-Normal-Blocks. `TrackBlockSpawner.BuildTrack` erkennt eine
90°-Abbiegung bei Normal-Blocks (`InputDirection != OutputDirection`,
beide gesetzt) und aktiviert dafür `SetCurve` statt Yaw-Rotation.
Scope-Erkenntnis: Trigger-Blocks turnen nie geometrisch (ihre
Eingangsseite ist ohnehin gekappt, siehe 0039) - nur Normal-Blocks
betroffen. Kinematic3D-Bewegungssampling folgt der Kurve weiterhin
nicht exakt (X/Z bleiben linear zwischen Zellenmitten) - das ist
Sache von Ticket [[0038]], nicht dieses Tickets.

Beim Selbst-Review wurde ein kritischer Bug in der
Abbiege-Erkennung gefunden und korrigiert (`!= outputDir.Opposite()`
hätte JEDE gerade Strecke fälschlich als Kurve behandelt; korrekt ist
`!= outputDir`).

**Nicht möglich in dieser Umgebung:** kein Unity-Editor verfügbar, daher
keine visuelle/Play-Mode-Verifikation der Mesh-/Collider-Geometrie
(Normalen-Richtung, Winding, Wasserdichtheit). Die Geometrie wurde
rechnerisch hergeleitet und mehrfach von Hand nachgerechnet, muss aber
im Editor angeschaut werden, bevor der Status auf Done wechselt. Falls
die Oberfläche invertiert erscheint oder die Kugel durchfällt: zuerst
die `flip`-Parameter in `AppendCurvedTopSurfaceSegment`/
`AppendCurvedSideWallSegment`/`AppendCurvedBottomSegment` testweise
umdrehen. Status bleibt bis dahin Open.

**2026-09-09 (Nachbesserung):** User-Feedback: Grundfläche der
Kurven-Blocks war nicht mehr quadratisch, sondern an der äußeren Ecke
der Kurve abgerundet. Ursache: die Rillen-"Röhre" (Channel-Sweep) war
exakt so breit wie die ganze Zelle (Halbbreite = `HalfLength`), wodurch
ihre äußere Randlinie selbst zu einem Bogen wurde und die der
Kurve gegenüberliegende Ecke des Quadrats (die "ferne" Ecke) komplett
ausgelassen wurde, statt mit Material gefüllt zu sein - das erzeugte
die gerundete Silhouette.
Fix: neue Methode `AppendCurvedFillerSegment` (plus `ringFillPoints` in
`BuildCurvedMesh`) füllt pro Ring-Segment den Bereich zwischen der
Rillen-Röhre-Außenkante und der tatsächlichen quadratischen
Blockgrenze auf (Top/Bottom/Außenwand), berechnet über die Distanz von
`center` entlang derselben Radial-Richtung bis zur echten Kante
(`fillDistance = 2*radius / max(sin, cos)` - schneidet die beiden
"fernen" Kantenebenen exakt an der Diagonale gegenüber dem
Kurven-Drehpunkt). Ergebnis: Grundfläche bleibt für jede Kombination
von Input-/Output-Richtung exakt quadratisch, unabhängig von der
inneren Rillenform.
Einziger verbleibender Unsicherheitsfaktor (ebenfalls nur im Editor
prüfbar, aber bewusst geringes Risiko, da rein kosmetisch): die
Wicklungsrichtung (`flip`) der neuen Außenwand in
`AppendCurvedFillerSegment` wurde per Analogie zur bestehenden
Geradeaus-Logik gewählt, nicht durch explizite Kreuzprodukt-Probe
verifiziert - falls die Außenwand der fernen Ecke von außen
unsichtbar wirkt (Backface Culling), dort zuerst `flip: false` auf
`true` für den dritten `AppendQuad`-Aufruf testen.

**2026-09-09 (zweite Nachbesserung):** User-Feedback: bei einigen
Kurven war die obere Fläche bis zur Ecke invertiert, bei anderen war
die Eckenfüllung korrekt, aber die Rille selbst nicht mehr erkennbar.
Ursache gefunden: welcher Rillen-Querschnitts-Index (0 oder der
letzte) die tatsächliche AUSSEN-Kante ist (die, die zur fernen Ecke
gefüllt werden muss) - statt der Innen-Kante, die exakt auf den
Kurven-Drehpunkt kollabiert - hängt vom **Drehsinn** der jeweiligen
Kurve ab (im Uhrzeigersinn vs. gegen den Uhrzeigersinn, je nach
Input-/Output-Richtungs-Kombination). Der ursprüngliche Fix hat
IMMER Index 0 als Außenkante angenommen; für die Hälfte aller
Abbiege-Kombinationen war das genau falsch - dort wurde die Füllung
an der bereits-am-Drehpunkt-liegenden Seite angesetzt und zog sich
quer über die Rille, statt die leere ferne Ecke zu füllen (erklärt
beide beobachteten Symptome je nach Drehsinn).
Fix: `BuildCurvedMesh` bestimmt jetzt den Drehsinn rechnerisch
(`Vector3.Dot(Vector3.Cross(Vector3.up, curveInVec), curveOutVec) > 0`
= im Uhrzeigersinn) und wählt daraus den korrekten Rillen-Index
(`railIndex`) sowie die für korrekte Normalen nötige Punkt-Reihenfolge
(`railFirst`) in `AppendCurvedFillerSegment`. Für zwei konkrete
Richtungs-Kombinationen (Up→Right als "im Uhrzeigersinn" und
Right→Up als "gegen den Uhrzeigersinn") von Hand durchgerechnet und
bestätigt, dass beide jetzt den korrekten Index/die korrekte
Reihenfolge liefern - die übrigen 6 Kombinationen folgen aus der
gleichen, in beiden Fällen bestätigten Formel.
Weiterhin nicht in dieser Umgebung überprüfbar (kein Unity-Editor) -
bitte im Editor an mehreren/allen 8 möglichen Kurven-Kombinationen
gegenprüfen, dass sowohl Eckenfüllung als auch Rille durchgängig
korrekt aussehen.

**2026-09-09 (dritte Nachbesserung):** User-Feedback: obere Flächen
sehen jetzt korrekt aus, aber bei einigen (nicht allen) Kurven passen
die seitlichen Wände nicht. Ursache: die neue Außenwand der
Eckenfüllung (`AppendCurvedFillerSegment`, dritter `AppendQuad`-Aufruf)
war fest auf `flip: false` gesetzt, ohne den gleichen Drehsinn-Faktor
zu berücksichtigen, der schon die Ecke selbst betraf. Per
Kreuzprodukt-Probe (`Cross(sweepRichtung, up)`, hergeleitet und
gegengeprüft an der bereits im Originalcode korrekten
`AppendSideWalls`-Konvention) für beide Drehsinne von Hand
durchgerechnet: für "im Uhrzeigersinn" ist `flip: false` korrekt
(Normale zeigt nach außen), für "gegen den Uhrzeigersinn" zeigt
`flip: false` dagegen nach INNEN - dort muss es `flip: true` sein.
Fix: dritter `AppendQuad`-Aufruf nutzt jetzt `flip: railFirst` (das
bereits vorhandene Drehsinn-Flag, das genau diese beiden Fälle
richtig unterscheidet) statt einem festen `false`.
Die beiden anderen Wand-Quellen (`AppendCurvedSideWallSegment`s
eigene Rillen-Wände an Index 0/letzter Index) wurden gegengeprüft und
brauchen KEINEN Drehsinn-Fix: je nach Drehsinn ist eine davon eine
verdeckte, mit der Füllung zusammenfallende Nahtstelle (Wicklung
egal) und die andere entartet zu einer Nulldreiecks-Fläche am
Drehpunkt (unsichtbar) - in beiden Fällen ohne visuelle Auswirkung.
Weiterhin nicht in dieser Umgebung überprüfbar - bitte im Editor
erneut an mehreren Kurven-Kombinationen (insbesondere an vorher
betroffenen) gegenprüfen.

**2026-09-09 (Abschluss):** User hat die Korrektur im Editor bestätigt
("sieht gut aus") - Status auf Done gesetzt.
