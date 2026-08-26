---
id: 0003
title: Pipes direkt im Level-Editor per Richtungen definieren
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-08-25
---

# 0003 - Pipes direkt im Level-Editor per Richtungen definieren

## Beschreibung

Aktuell müssen alle Pipe-Karten vorab als eigene `PipeDefinition`
ScriptableObjects angelegt werden, damit sie im LevelEditor auswählbar sind.
Stattdessen soll man eine Pipe direkt im Editor definieren können, indem man
pro Zelle zeichnet, zu welchen Richtungen sie geöffnet ist - ohne vorher
manuell ein Asset dafür anlegen zu müssen. Wichtig: `BackgroundColor`, `Role`
und `Locked` müssen dabei ebenfalls direkt im Editor einstellbar sein.

## Details

- Betroffene Dateien: `Assets/Scripts/Grid/PipeDefinition.cs` (Felder
  `connections`, `color`, `backgroundColor`, `role`, `locked`),
  `Assets/Scripts/Grid/Direction.cs` (`[Flags] enum Direction`, 4 Werte ->
  max. 15 nicht-leere Kombinationen), `Assets/Scripts/Grid/LevelData.cs`
  (`List<PipeDefinition> pipes`, `SetPipeAt`) und
  `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` (Palette aus
  `availablePipes`, Zell-Painting).
- Idee aus der Diskussion: Im Editor pro Zelle die 4 Richtungen
  (Up/Right/Down/Left) direkt als Toggles zeichnen, plus Felder für
  `BackgroundColor`, `Role` und `Locked`. Im Hintergrund wird dafür
  automatisch eine passende `PipeDefinition` nachgeschlagen oder neu
  angelegt/gecacht - Farbe/Role/Locked bleiben damit weiterhin zentral in
  einem Asset gepflegt statt pro Zelle dupliziert, aber niemand muss die
  Assets mehr von Hand vorab erstellen.
- Zu klären beim Umsetzen: ob nach Richtungen+Role+Locked+BackgroundColor
  gemeinsam ein Asset identifiziert/wiederverwendet wird (um Duplikate zu
  vermeiden), oder ob pro Zelle ein eigenes Asset erzeugt wird.
- Akzeptanzkriterien:
  - Im Pipe-Bereich des Level-Editor-Fensters lassen sich die vier
    Richtungen einer Pipe direkt durch Zeichnen/Toggles festlegen, ohne
    vorher ein `PipeDefinition`-Asset manuell anlegen zu müssen.
  - `BackgroundColor`, `Role` und `Locked` sind für die so definierte Pipe
    ebenfalls direkt im Editor einstellbar.
  - Bestehende, manuell angelegte `PipeDefinition`-Assets funktionieren
    weiterhin unverändert (keine Breaking Changes für existierende Level).

## Notizen

Umgesetzt in `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`. Im
Pipe-Bereich der Palette gibt es jetzt einen "Custom Pipe"-Baustein mit
4 Richtungs-Toggles (U/R/D/L, kreuzförmig angeordnet), einem
BackgroundColor-Feld, einem Role-Dropdown und einem Locked-Toggle. "Use
Custom" aktiviert diesen als Pinsel; beim Malen einer Zelle sucht
`GetOrCreateCustomPipe` unter den vorhandenen `PipeDefinition`-Assets nach
einer Übereinstimmung in Connections+BackgroundColor+Role+Locked und
verwendet sie wieder, oder legt über `CreateCustomPipeAsset` (per
`SerializedObject`, da die Felder sonst privat sind) automatisch ein neues
Asset unter `Assets/Levels/Pipes/` an. Bestehende, manuell angelegte
Pipe-Assets bleiben unverändert nutzbar und tauchen weiterhin in der
normalen Palette auf.
