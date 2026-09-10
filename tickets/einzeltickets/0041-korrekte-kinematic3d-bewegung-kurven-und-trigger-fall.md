---
id: 0041
title: Korrekte Kinematic3D-Bewegung bei Kurven und Trigger-Fall verifizieren
type: Task
priority: Medium
status: Done
area: Physics
created: 2026-09-09
---

# 0041 - Korrekte Kinematic3D-Bewegung bei Kurven und Trigger-Fall verifizieren

## Beschreibung

Folge-Ticket zu [[0039]] (TrackBlock-Typen und Verbindungslogik
vereinheitlichen), aus dessen Umsetzung heraus entstanden: die
Kinematic3D-Bewegung der Kugel (`TrackBlockSpawner.SampleTrackPosition`/
`JunctionLocalXZ`/`SampleFloorY`, `TrackBlock.SampleGroovePointLocal`)
wurde im Rahmen von Ticket 39 mehrfach nachgebessert (Kurven-Verfolgung,
Trigger-Fallhöhe), konnte in der Bearbeitungs-Umgebung aber nie visuell
im Unity-Editor getestet werden. Dieses Ticket verfolgt die
abschließende Verifikation und ggf. weitere Feinjustierung dieser
Bewegung, getrennt von Ticket 39 (das inhaltlich abgenommen und
geschlossen ist).

## Details

Betroffene Bereiche, die im Editor gegengeprüft werden sollten:

- **Normale 90°-Kurven (Normal-Blocks):** Kugel soll im Kinematic3D-
  Modus sichtbar dem gebogenen Groove folgen (`TrackBlock.IsCurved`,
  `SampleGroovePointLocal`), nicht linear abkürzen oder springen. Zuletzt
  über `TrackBlockSpawner.JunctionLocalXZ` gelöst (Kurven-Anschluss statt
  Zellmitte an Kurven-Grenzen) - mehrere frühere Ansätze (Endpunkt-Blend
  auf Zellmitten, falsche Achsen-Zuordnung) waren fehlerhaft und wurden
  verworfen, siehe Notizen in [[0039]].
- **Trigger-Blocks (Xylophon/Sound) mit abweichender Ein-/
  Ausgangsrichtung:** Kugel soll erkennbar "abrupt" auf das
  Trigger-Element fallen (`FallHeight`, zuletzt auf 1.2 erhöht) statt zu
  gleiten - zuletzt behoben durch strikte Trennung von X/Z- und
  Höhen-Interpolation (`SampleFloorY` nutzt nur den eigenen Block, nicht
  den Nachbarn).
- **Zusammenspiel Kurve + Trigger unmittelbar benachbart:** in
  `JunctionLocalXZ`/`SampleFloorY` bisher nicht speziell behandelt -
  sollte bei Gelegenheit mitgeprüft werden, falls ein Level so etwas
  enthält.
- **Start/Goal-Sonderfall:** bleiben bewusst zellmitten-basiert (passend
  zur Tunnel-Deko) - eine Kurve direkt neben Start/Goal wäre laut
  aktuellem Design-Kommentar noch nicht korrekt abgedeckt, kommt aber in
  bestehenden Leveln nicht vor.
- Falls sich bei der Verifikation zeigt, dass die aktuelle Lösung
  strukturell nicht ausreicht (z.B. Kurve direkt neben Start/Goal doch
  benötigt wird), ggf. Bezug zu [[0038]] (generischer, pro Block-Variante
  definierbarer Kinematic-Trace) prüfen - dort wäre ein grundsätzlicherer
  Ansatz denkbar, der die hier einzeln gepatchten Fälle vereinheitlicht.

## Notizen

**2026-09-10 (Editor-Rückmeldung + struktureller Fix):** User hat im
Editor geprüft: Kurven werden jetzt korrekt genommen, an den
Trigger-Blocks nimmt die Kugel aber "eine komische Abkürzung" - beim
XylophonePad unauffällig, bei den Hat-Blocks bewegt sie sich einfach
diagonal vom Eingangs- zum Ausgangspunkt.

Ursachenanalyse (drei unabhängige Gründe, alle in der alten,
rückgerechneten Bewegung):

1. Y war eine lineare Funktion von X/Z: `SampleFloorY` lerpte
   `entryY -> exitY` über dasselbe `f` wie die X/Z-Interpolation, und
   `ComputeSurfaceInclination` verteilte per `fallTiltFraction` die
   halbe FallHeight als Neigung über die ganze Zelle. Das IST per
   Konstruktion die schräge Gerade von Eingangs- zu Ausgangspunkt; der
   Rest der FallHeight war kein Fall, sondern ein Teleport in einem
   einzigen Frame.
2. Halbzellen-Versatz: `JunctionLocalXZ` lieferte Zellmitten,
   `SampleFloorY` dagegen Blockkanten - der Höhensprung lag damit
   geometrisch in der Mitte des Trigger-Blocks statt an dessen
   Eingangskante.
3. Die Pad-Geometrie kam in der Bewegung überhaupt nicht vor:
   `ClosedEndGrooveBlockProfile.EntryPoint` liefert Rillentiefe, obwohl
   die Eingangshälfte eines Trigger-Blocks flach zugemauert ist - die
   Kugel lief die erste Blockhälfte durch das Vollmaterial und mitten
   durch die Xylophon-Leiste.

Gelöst nicht durch weitere Einzelkorrekturen, sondern durch Umsetzung
von [[0038]] (Trace pro Block-Variante) - Details dort. Für die hier
gelisteten Punkte heißt das:

- **Kurven:** folgen ihrem echten Bogen (`CurvedMarbleTrace`);
  `JunctionLocalXZ` und seine Sonderfälle entfallen ersatzlos.
- **Trigger-Blocks:** echte Fall-Parabel auf die Leiste, kurzer
  Absprung in die Rille, dann Ausrollen (`TriggerFallMarbleTrace`).
  Landepunkt kommt aus `XylophoneBlockDecoration.PadTopY`/
  `PadCenterOffset`, kann also nicht von der sichtbaren Leiste
  abweichen. Der Sound feuert am Aufprall (`ImpactTime`), nicht mehr
  beim Überschreiten der Zellmitte.
- **Kurve + Trigger benachbart / Start+Goal neben Kurve:** durch die
  Kante-zu-Kante-Parametrisierung automatisch erledigt - jeder Trace
  endet exakt dort, wo der nächste beginnt; die alte Sonderbehandlung
  für Nachbarn existiert nicht mehr.
- **Start/Goal:** nicht mehr zellmitten-, sondern rillenende-basiert
  (`WallZ`), d.h. die Kugel rollt aus dem Tunnelmaul heraus bzw. dort
  hinein.

Zusätzlich dabei gefunden und behoben: die Höhenkette rechnete mit dem
ANGEFORDERTEN `entryY` weiter, auch wenn `minBlockHeight` die
Blockdicke geklammert hatte - der Block lag dann höher als die Kette
annahm (bei FallHeight 1.2 ab `startHeight` 1.0 z.B. 0.19 Einheiten
Versatz). Sie chained jetzt über `actualEntryY`, und wenn die Klammerung
greift, gibt es eine Debug-Warnung mit Hinweis auf startHeight/
FallHeight statt eines stillen Versatzes.

Hinweis zu den Asset-Daten: `Sound_Hat` und `XylophonePad_` stehen auf
`fallHeight` 0.3, `Sound_Snare` auf 0 - `Sound_Amazing Hat` und
`Sound_Kick` haben (weil älter als das Feld) gar keinen serialisierten
Wert und laufen auf den Initializer 1.2. Das erklärt, warum dieselbe
Mechanik pro Blocktyp unterschiedlich stark auffiel; die Werte sind
jetzt der einzige Hebel für die Fallhöhe.

Weiterhin nicht in dieser Umgebung visuell prüfbar - die Anschlüsse
wurden numerisch gegengerechnet (Kette Start -> Gerade -> Kurve ->
Trigger -> Gerade -> Goal, alle Übergänge lückenlos). Bitte im Editor
gegenprüfen: springt die Kugel sichtbar auf die Leiste, bevor sie in
die Rille fällt und weiterrollt; passt das Tempo (ein Trigger-Block
dauert jetzt real länger als eine Gerade - Fallzeit); sitzen
Ein-/Ausgang bei Start/Goal im Tunnelmaul. Feintuning über
`fallGravity`/`triggerBounceHeight` am TrackBlockSpawner und die
`fallHeight` der Content-Assets.

**2026-09-10 (Takt-Vorgabe + Minimum-FallHeight):** User-Vorgabe: jeder
Block muss zeitlich gleich lang sein, damit die Musik im Takt bleibt -
Umsetzung siehe [[0038]]. Für dieses Ticket heißt das: ein
Trigger-Block dauert nicht mehr länger als eine Gerade, der Fall ist
ein fester Anteil (`triggerFallBeatFraction`, Default 0.5) des Taktes,
und alle Trigger-Blocks klingen an derselben Stelle ihres Taktes.

Beim Nachrechnen der neuen Traces dabei gefunden: bei `FallHeight` 0
(`Sound_Snare`) endet die Rille des Vorgängerblocks 11,5 cm UNTER der
zugemauerten Eingangshälfte des Trigger-Blocks - die Kugel startete
also im Vollmaterial und musste sich auf die Leiste hocharbeiten. Ein
Trigger-Block braucht mindestens `grooveRadius + PadTopY` Fallhöhe,
damit die Kugel überhaupt auf der Leiste landen kann; darunter wird
jetzt auf dieses Minimum aufgerundet (mit Debug-Warnung). Betrifft im
aktuellen Level nur `Sound_Snare` (0 -> 0.178); `Sound_Hat` und
`XylophonePad_` liegen mit 0.3 darüber.

Numerisch gegengeprüft (feine Abtastung der Trigger-Traces, gerade und
abbiegend, FallHeight 0.3/1.2/Minimum): Phasen stoßen lückenlos
aneinander, der Aufprall liegt in allen Fällen exakt bei t=0.50, und
die Kugel schneidet weder die Leiste noch die Deckfläche der
Eingangshälfte.

**2026-09-10 (Abschluss):** User hat die Umstellung abgenommen ("passt
erstmal") - Status auf Done gesetzt. Die verbliebene reine Sichtprüfung
im Editor (Sprung auf die Leiste, Takt-Gefühl bei
`triggerFallBeatFraction` 0.5, Tunnelmaul an Start/Goal) bleibt
Feintuning über die Inspector-Werte und braucht kein eigenes Ticket
mehr.

