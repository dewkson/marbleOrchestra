---
id: 0011
title: Murmelbahnen loopbar machen
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-08-26
---

# 0011 - Murmelbahnen loopbar machen

## Beschreibung

Murmelbahnen sollen loopbar sein, das heißt wenn eine valide Murmelbahn von
Start bis Goal geschlossen ist, soll bei Erreichen des Goals eine neue
Murmel von Start loslaufen. Das soll synchron so passieren, dass instantly
die Murmel beim Ankommen in der Mitte vom Goal verschwindet und eine neue
in der Mitte vom Start losläuft.

## Details

- Betroffen: `Assets/Scripts/Grid/MarbleController.cs`, Methoden `RunTrack`
  und `RunAlongPath` - `RunAlongPath` läuft den `OrderedPath` einer
  `PathValidationResult` aktuell genau einmal ab und `RunTrack` beendet
  danach den Coroutine-Durchlauf (`activeRunCount--`), die Murmel bleibt
  auf der letzten Position (Goal) stehen.
- Für das Looping müsste `RunTrack` pro Track wiederholt von vorn starten
  (Murmel zurück auf `path[0]`, erneut `RunAlongPath`), statt nach einem
  Durchlauf zu enden - inklusive erneutem Triggern des Contents am
  Start-Feld.
- "Synchron/instantly" deutet darauf hin, dass beim letzten Schritt zum
  Goal keine sichtbare Pause/Verzögerung entstehen soll, bevor die nächste
  Murmel am Start erscheint (kein Warten, direkter Übergang).
- Akzeptanzkriterien:
  - Erreicht eine Murmel das Goal einer vollständig validierten Bahn,
    verschwindet sie dort und im selben Moment startet eine neue Murmel
    am Start derselben Bahn.
  - Das Looping läuft weiter, bis `Stop()`/`ResetMarble()` aufgerufen wird
    oder die Bahn durch ein Pipe-Swap ungültig wird.
  - Betrifft nur Bahnen, die tatsächlich `GoalReached == true` sind.

## Notizen

Umgesetzt in `Assets/Scripts/Grid/MarbleController.cs`: `RunTrack` läuft
jetzt in einer `while`-Schleife statt nur einmal. Nach jedem abgeschlossenen
Durchlauf (`RunAlongPath`) wird die alte Murmel deaktiviert und zerstört
und eine neue am selben Start erzeugt - alles ohne `yield` dazwischen, also
im selben Frame/Schritt, dadurch wirkt der Übergang instantan (kein
Teleportieren derselben Instanz, sondern "verschwindet am Goal, neue
erscheint am Start" wie gewünscht).

Vor jeder neuen Runde wird der aktuelle Pfad frisch über die neue
Hilfsmethode `FindCurrentPath(startCoord)` aus `grid.LastValidations`
aufgelöst (Vergleich über den Start-Koordinatenpunkt) statt den einmal zu
Spielbeginn ermittelten `OrderedPath` weiterzuverwenden. Wird die Bahn
durch einen Pipe-Swap ungültig (kein Ergebnis mit `GoalReached == true` und
diesem Start mehr vorhanden), bricht die Schleife an der nächsten
Runden-Grenze ab und `activeRunCount` wird dekrementiert. `Stop()`/
`ResetMarble()` funktionieren unverändert (brechen die Coroutine hart ab,
`ClearMarbles()` räumt die zu diesem Zeitpunkt aktiven Murmeln auf).
