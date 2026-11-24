---
id: 0070
title: Levelabschluss - Winning Screen nach allen Sublevels
type: Feature
priority: Medium
status: Done
area: UI
created: 2026-11-24
---

# 0070 - Levelabschluss - Winning Screen nach allen Sublevels

## Beschreibung

Levelabschluss: Winning Text oder Screen wenn alle Sublevels gelöst worden sind.

## Details

- Betroffene Systeme: `MarbleController` (prüft bereits gültige Bahnen pro
  SubLevel, vgl. `CanPlay` / `HasCompletedTrackInActiveSubLevel`),
  `PathGrid` (SubLevels, `ActiveSubLevelIndex`), SubLevel-Konzept aus 0046.
- Akzeptanzkriterien:
  - Sobald alle SubLevels eines Levels gelöst sind, erscheint ein
    Winning-Text bzw. -Screen.
  - Levels ohne SubLevels: Abschluss, sobald die (einzige) Bahn gelöst ist.

## Notizen


- 2026-11-24: Umgesetzt (noch nicht in Unity getestet, Build per dotnet fehlerfrei).
  - `MarbleController.IsLevelComplete`: alle SubLevels haben gleichzeitig eine gültige Bahn (`GetValidSubLevelIndices().Count == SubLevelCount`); ohne SubLevels genügt die eine Bahn.
  - Neues `LevelCompleteUI` (baut eigenes Canvas, wird von `PlaybackHintUI` automatisch hinzugefügt - kein Szenen-Setup nötig): abgedunkelter Hintergrund, "Level geschafft!" + Untertitel, Buttons "Anhören" (startet Playback, alle Bahnen loopen) und "Weiter bauen" (schließt).
  - Erscheint einmal pro Level, mit kurzer Verzögerung (`showDelay`, 0.4 s) und nur in der Planung (nicht während Play/SubLevel-Wechsel). Solange offen, ist das Grid-Editieren gesperrt; SPACE/Play schließt ihn.
  - 2026-11-24 Nachtrag: Winning Screen wieder entfernt. Stattdessen zeigt `LevelCompleteUI` unten rechts einen kleinen "Nice Beat"-Hinweis (mit Pop-in), solange das Level gelöst ist - folgt dem Live-Zustand, keine Buttons, sperrt keine Eingabe. Mobile: rechte untere Ecke des UI-Streifens; Desktop: rechte untere Ecke des Spielbereichs (die Spalte unten rechts belegt der Play-Button).
  - 2026-11-24 Nachtrag 2: Text jetzt zweizeilig "Level complete / Nice Beat" (Schrift passt sich der Höhe an). Mobile: so breit wie die Minimap und gespiegelt auf der rechten Seite (gleiche Unterkante), beginnt aber unter Kamera-Button und Bahnwechsel-Buttons und ist daher niedriger. Desktop: minimap-breit unten rechts im Spielbereich. Der Kamera-Button bleibt erhalten.
  - 2026-11-24 Nachtrag 3: Text je Modus - Planung "Level Complete", Play "Nice Beat". Mobile Play: beginnt mit dem üblichen Button-Abstand (40) unter dem Kamera-Button bzw. unter den Bahnwechsel-Buttons, wenn sichtbar. Mobile Planung: volle Breite des UI-Streifens.
