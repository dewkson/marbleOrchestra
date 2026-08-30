---
id: 0028
title: Sound und Flash-Color direkt im Level Grid Editor definieren
type: Feature
priority: Medium
status: Open
area: Level Editor
created: 2026-08-29
---

# 0028 - Sound und Flash-Color direkt im Level Grid Editor definieren

## Beschreibung

Man soll Sound und Flash-Color direkt im Level Grid Editor pro Feld
definieren können, ähnlich wie beim Pipe-System (siehe [[0003]] - dort
werden Pipes direkt per Richtungen im Editor definiert, statt separate
Assets vorher anlegen zu müssen und dann zuzuweisen).

## Details

- Aktueller Weg (Stand nach [[0023]]/[[0024]]): Für den Content-Layer
  im `LevelGridEditorWindow` muss weiterhin vorher ein
  `SoundTriggerContent`-Asset (`Assets/Scripts/Grid/
  SoundTriggerContent.cs`, `Clip` + `FlashColor`) über
  `Assets → Create → MarbleOrchestra → Cell Content → Sound Trigger`
  angelegt werden, bevor es im Content-Layer auf eine Zelle gemalt
  werden kann - ein Umweg über die Project-Assets, den das Pipe-System
  seit [[0003]] nicht mehr braucht.
- Betroffene bestehende Systeme:
  - `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` - der
    Content-Paint-Layer entdeckt Content-Assets aktuell über
    `AssetDatabase.FindAssets("t:CellContentDefinition")`
    (`LoadAllAssets<T>`) und malt per `level.SetContentAt(index, asset)`
    - müsste um eine Direkteingabe (Clip-Feld + Farbwähler pro Zelle,
      ohne vorheriges Asset) ergänzt werden.
  - `Assets/Scripts/Grid/LevelData.cs` (`Contents`-Liste,
    `SetContentAt`) - aktuell nur für Asset-Referenzen ausgelegt; für
    direkt im Editor eingegebene Werte wäre entweder ein neuer
    Datenpfad oder eine automatische Asset-Erzeugung im Hintergrund
    nötig (analog zu [[0003]]s Lösung fürs Pipe-System - dort lohnt
    sich ein Blick, wie das dort gelöst wurde).
  - `Assets/Scripts/Grid/SoundTriggerContent.cs`/
    `CellContentDefinition.cs` - Datenmodell für Clip/FlashColor bleibt
    vermutlich bestehen, nur der Autorierungsweg ändert sich.
- Keine Akzeptanzkriterien ausgearbeitet - Beschreibung liefert nur die
  Zielrichtung (Direkteingabe statt Asset-Umweg), keine konkrete
  UI-Lösung.

## Notizen

- 2026-08-29: Umgesetzt in `Assets/Scripts/Grid/Editor/
  LevelGridEditorWindow.cs`, 1:1 nach dem Muster von [[0003]]s
  "Custom Pipe"-Baukasten (`DrawCustomPipeBuilder`/`GetOrCreateCustomPipe`/
  `CreateCustomPipeAsset`): neuer "Custom Sound Trigger"-Block im
  Content-Layer mit Clip- und Flash-Color-Feld
  (`DrawCustomContentBuilder`). Beim Malen wird über
  `GetOrCreateCustomContent` zunächst nach einem bestehenden
  `SoundTriggerContent`-Asset mit identischem Clip+FlashColor gesucht
  (Wiederverwendung), sonst über `CreateCustomContentAsset` automatisch
  eines in `Assets/Levels/Contents/Sound_<ClipName>.asset` erzeugt
  (Felder via `SerializedObject`, da `SoundTriggerContent` keine
  öffentlichen Setter hat) und die Palette aktualisiert.
  `LevelData.cs`/`CellContentDefinition.cs`/`SoundTriggerContent.cs`
  unverändert - reine Editor-Fenster-Ergänzung. Noch nicht im Editor
  getestet (GUI-Code, für mich nicht automatisiert prüfbar).
