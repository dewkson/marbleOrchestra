---
id: 0012
title: Space-Umschaltung zwischen Bahnplanung und Simulation
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-08-28
---

# 0012 - Space-Umschaltung zwischen Bahnplanung und Simulation

## Beschreibung

Es braucht einen Übergang zwischen Bahnplanung und dem Durchlaufen mit
Murmeln. Das kann im einfachsten Fall über Leertaste erfolgen. Leertaste
switcht dann zwischen Planung und dem Start der Murmel in der Bahn.
Allerdings kann der Start nur erfolgen, wenn mindestens eine valide Bahn
konstruiert wurde. Dafür brauchen wir in der Screenspace UI eine einfache
Anweisung und visuellen Hinweis, dass die Bahn mit SPACE getestet werden
kann.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/GridInputHandler.cs` - zentrale Input-Verarbeitung
  im Grid/Editor-Kontext, vermutlich Anknüpfungspunkt für die neue
  Space-Eingabe.
- `Assets/Scripts/Grid/PathValidator.cs` - prüft vermutlich bereits, ob
  eine Bahn valide ist; sollte für die Start-Bedingung wiederverwendet
  werden.
- `Assets/Scripts/Grid/MarbleController.cs` / `Marble.cs` - Start/Spawn der
  Murmel(n) in der Bahn.
- Es existiert aktuell kein eigenständiges Runtime-UI-System
  (`Assets/Scripts/UI/...`) im Projekt - die Screenspace-UI-Anzeige
  (Hinweistext/visueller Indikator) muss neu angelegt werden.

Akzeptanzkriterien:
- Leertaste schaltet zwischen "Planungsmodus" und "Simulationsmodus" um.
- Start der Simulation (Space-Druck von Planung -> Simulation) ist nur
  möglich, wenn mindestens eine valide Bahn vorliegt (Prüfung über
  vorhandene Validierungslogik).
- Ist keine valide Bahn vorhanden, hat Space in diese Richtung keinen
  Effekt (bleibt im Planungsmodus).
- Screenspace-UI zeigt im Planungsmodus eine kurze Anweisung
  ("Mit SPACE testen" o.ä.) sichtbar an, sobald mindestens eine valide
  Bahn existiert; verschwindet/ändert sich, wenn keine valide Bahn
  vorhanden ist.
- Rückweg von Simulation zu Planung (Space erneut) ist jederzeit möglich.

## Notizen

