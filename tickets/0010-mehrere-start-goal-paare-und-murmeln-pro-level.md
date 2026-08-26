---
id: 0010
title: Mehrere Start/Goal-Paare und gleichzeitige Murmeln pro Level
type: Feature
priority: Medium
status: Open
area: Gameplay
created: 2026-08-26
---

# 0010 - Mehrere Start/Goal-Paare und gleichzeitige Murmeln pro Level

## Beschreibung

Es müssen mehr als nur eine valide Murmelbahn pro Level erlaubt sein. Man
soll auch 2 Start und 2 Goal Felder haben können und dann sollen auch beide
separaten Bahnen validiert werden. Aktuell wird nur eine Bahn validiert.
Beim Starten der Murmel, sollten dann auch mehrere Murmeln loslaufen können
(bei 2 Start-Feldern entsprechend 2 Murmeln gleichzeitig).

## Details

- Betroffene Dateien:
  - `Assets/Scripts/Grid/PathValidator.cs`: `Evaluate` sucht aktuell per
    `grid.FindPipeByRole(PipeRole.Start)`/`PipeRole.Goal` jeweils genau
    eine Start- und eine Goal-Pipe und liefert genau ein
    `PathValidationResult` (ein `ConnectedCells`-Set, ein `OrderedPath`).
    Müsste auf mehrere Start/Goal-Paare erweitert werden (je Start-Pipe
    eine eigene BFS-Suche/eigenes Ergebnis).
  - `Assets/Scripts/Grid/PathGrid.cs`: `FindPipeByRole` gibt aktuell nur
    die erste gefundene Pipe einer Rolle zurück (bricht bei mehreren
    Start-/Goal-Pipes die Zuordnung); `LastValidation` ist aktuell ein
    einzelnes `PathValidationResult`.
  - `Assets/Scripts/Grid/LevelData.cs`: `OnValidate` warnt aktuell, wenn
    nicht genau 1 Start- bzw. 1 Goal-Pipe im Level vorhanden ist - müsste
    angepasst werden, um mehrere Start-/Goal-Pipes zuzulassen (ggf. mit
    Prüfung, dass Start- und Goal-Anzahl übereinstimmt).
  - `Assets/Scripts/Grid/MarbleController.cs`: hält aktuell genau eine
    `Marble`-Instanz und startet in `Play()`/`RunAlongPath` genau eine
    Murmel entlang `grid.LastValidation.OrderedPath`. Müsste pro validierter
    Bahn (bzw. pro Start-Pipe) eine eigene Murmel erzeugen und gleichzeitig
    starten können.
- Offene Fragen für die Umsetzung: wie werden Start- und Goal-Pipes einander
  zugeordnet, wenn mehrere Paare existieren (z.B. über Reihenfolge/Distanz,
  oder müssen Bahnen sich nicht überschneiden)? Wie wird mit einer nicht
  vollständig validierten Einzelbahn umgegangen (z.B. nur die validen Bahnen
  starten, oder `CanPlay` erst wenn alle Bahnen vollständig sind)?
- Akzeptanzkriterien:
  - Ein Level mit z.B. 2 Start- und 2 Goal-Pipes lässt sich anlegen, ohne
    dass die bestehende "genau 1 Start/Goal"-Validierung das verhindert.
  - Für jedes Start/Goal-Paar wird die jeweilige Bahn separat auf
    Verbindung geprüft (aktuell wird nur eine Bahn geprüft).
  - Beim Start (`Play`) laufen entsprechend viele Murmeln gleichzeitig los
    wie es valide Start/Goal-Paare gibt.

## Notizen
