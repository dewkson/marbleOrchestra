---
id: 0007
title: Zelltrennlinien im Level Grid Editor
type: Feature
priority: Low
status: Done
area: Level Editor
created: 2026-08-26
---

# 0007 - Zelltrennlinien im Level Grid Editor

## Beschreibung

Der LevelGridEditor sollte Linien visualisieren, die die Zellen voneinander
trennen.

## Details

- Betroffen: `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`,
  Methode `DrawCell` - zeichnet aktuell pro Zelle `EditorGUI.DrawRect(rect,
  background)` für den Hintergrund und ruft am Ende `GUI.Box(rect,
  GUIContent.none)` auf, was je nach Editor-Skin nur einen schwachen/keinen
  sichtbaren Rahmen ergibt.
- Akzeptanzkriterien:
  - Zwischen benachbarten Zellen im Grid ist eine klar sichtbare Trennlinie
    zu erkennen, unabhängig vom Editor-Skin (Light/Dark).
  - Betrifft nur die Darstellung im Level-Grid-Editor-Fenster, keine
    Laufzeit-/Gameplay-Logik.

## Notizen

Umgesetzt in `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`: das
bisherige `GUI.Box(rect, GUIContent.none)` (Skin-abhängiger, "geboxter"
Look pro Zelle) wurde durch `DrawGridLines` ersetzt - zeichnet nur dünne
(1px), halbtransparente weiße Linien an den vier Kanten jeder Zelle. Da
Nachbarzellen sich Kanten teilen, ergibt das ein durchgehendes Gitter statt
einzelner Zell-Umrandungen.
