---
id: 0054
title: Blockierte Zellen auch ohne Bild in der 2D-Planung anzeigen
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-09-29
---

# 0054 - Blockierte Zellen auch ohne Bild in der 2D-Planung anzeigen

## Beschreibung

Wieso werden die Blocked-Zellen in der 2D-Planung gar nicht erst
angezeigt? Ich möchte hier auch eine Darstellung, nur eben ohne
Interaktivität. Ansonsten wirkt der Übergang 2D zu 3D nicht vollständig.

## Details

Betroffen: `Assets/Scripts/Grid/PathGrid.cs`,
`Assets/Scripts/Grid/PipeVisual.cs`.

Ursache war eine Kette aus zwei Stellen:

1. `LevelData.GetBlockedLookAt(index)` liefert bewusst `null`, sobald kein
   Sprite gesetzt ist - nicht nur wenn gar kein `CardLook` existiert:
   `return look != null && look.Image != null ? look : null;`. Das ist
   nötig, weil Unity `null`-Elemente in einer `List<CardLook>` beim
   Deserialisieren durch default-konstruierte Instanzen ersetzt - im
   gespeicherten Level steht für jede blockierte Zelle tatsächlich
   `- image: {fileID: 0}`, also ein nicht-nulles `CardLook` ohne Bild.
2. `PathGrid.CreateBlockedVisual` stieg bei `look == null` sofort aus
   (`if (look == null) return null;`).

Zusammen hieß das: eine blockierte Zelle war in 2D nur dann überhaupt
sichtbar, wenn ihr im Level-Editor ein Bild zugewiesen wurde (die
ursprüngliche Absicht aus 0030). Im Testlevel haben alle 9 blockierten
Zellen kein Bild - in der Planung klaffte dort also ein Loch, während der
3D-Pass über genau dieselben Zellen solides Filler-Terrain baut. Genau
dieser Bruch ließ den Übergang unvollständig wirken.

## Notizen

Umgesetzt:

- `PathGrid.CreateBlockedVisual` baut die Karte jetzt für **jede**
  blockierte Zelle, mit oder ohne Bild - der frühe `null`-Ausstieg ist
  weg.
- `PipeVisual.RefreshBlocked` akzeptiert ein `null`-`look` und zeichnet
  dann die schlichte gerahmte Karte ohne Bild
  (`ApplyFrameAndImage(look != null ? look.Image : null)`; die Methode
  konnte einen `null`-Sprite ohnehin schon, es wurde nur nie einer
  durchgereicht).

Nicht-interaktiv bleibt es dadurch, dass das von `CreateBlockedVisual`
gebaute GameObject - anders als `CreatePipe` - **keinen** `BoxCollider`
bekommt: `GridInputHandler` findet eine Zelle ausschließlich per
`Physics.Raycast` gegen einen solchen Collider, die Raycasts gehen also
durch. Zusätzlich lehnt `PathGrid.SwapCards` eine blockierte Koordinate
ohnehin ab. An beidem war nichts zu ändern.

Die blockierte Karte zeigt weiterhin die dunkle Fläche (0.18 Grau) im
Rahmen des Levels, wie sie 0030 für bebilderte blockierte Zellen
eingeführt hat - nur eben jetzt auch ohne Bild. Bewusst NICHT mitgemacht:
die 2D-Fläche an die tatsächliche 3D-Blockfarbe der Zelle anzugleichen
(`blockColorOverrides`, siehe 0050 - im Testlevel ein helles Grau). Das
wäre eine gestalterische Entscheidung, keine Fehlerbehebung.

Gegen einen echten Roslyn-Build geprüft (Unity-eigenes csc gegen die
generierte `Assembly-CSharp.csproj`, Ausgabe außerhalb des Projekts):
Exit 0, keine neuen Warnungen.
