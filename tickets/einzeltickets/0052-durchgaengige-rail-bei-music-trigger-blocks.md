---
id: 0052
title: Durchgängige Rail bei Music-Trigger-Blocks
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-09-28
---

# 0052 - Durchgängige Rail bei Music-Trigger-Blocks

## Beschreibung

für Music Trigger TrackBlocks sollen die Rails durchgängig angezeigt
werden, wie auch bei den normalen Blocks. Aktuell wird ein Xylophon
Objekt angezeigt und der Rail beginnt dann erst ab mitte des TrackBlocks.
Ich möchte, dass der Rail durchgängig ist und in diesem Fall auch unter
dem Xylophon-Objekt durchläuft

## Details

Betroffen: `Assets/Scripts/Grid/TrackBlockSpawner.cs`,
`Assets/Scripts/Grid/ClosedEndGrooveBlockProfile.cs`,
`Assets/Scripts/Grid/XylophoneBlockDecoration.cs`.

Ursache: In `TrackBlockSpawner.cs:809-811` bekommt ein `BlockType.Trigger`
(wie auch Start/Goal) ein `ClosedEndGrooveBlockProfile(..., closedAtEntry:
true, ...)` - laut Kommentar `:797-801` ist "Start/Goal/Trigger blocks'
entry half is always capped (IClosedEndBlockProfile), never a
through-rolling groove". Die Rille (Rail) reicht dadurch nur um
`railExtension` (`:778`) über die Blockmitte in die geschlossene Hälfte
hinein statt komplett durchzulaufen - genau das vom User beschriebene
Verhalten ("Rail beginnt erst ab Mitte des TrackBlocks"). Das
Xylophon-Objekt selbst (`XylophoneBlockDecoration.Build`) sitzt als
separates, unabhängiges Child-GameObject auf der Fall-Seite und würde von
einer durchgängigen Rille nicht behindert.

Akzeptanzkriterien (grob):
- Bei Trigger-Blocks läuft die Rail-Geometrie über den gesamten Block
  durch (wie bei Normal-Blocks), statt an der Blockmitte/dem
  `railExtension`-Punkt zu enden.
- Das Xylophon-Objekt bleibt wie bisher sichtbar und funktional
  (Fall-Trace/Pad-Position unverändert), liegt aber optisch über der
  durchlaufenden Rail statt diese zu ersetzen.
- Zu prüfen: ob `closedAtEntry` für Trigger-Blocks dafür komplett
  entfällt oder ob die geschlossene Wand (IClosedEndBlockProfile) aus
  einem anderen Grund (z.B. Wandabschluss an der Fall-Seite) weiterhin
  nötig ist und nur die sichtbare Rille verlängert werden muss.

## Notizen

- 2026-09-28: Umgesetzt. Trigger-Blocks bekommen in `TrackBlockSpawner.cs`
  jetzt dasselbe `GrooveBlockProfile` wie Normal-Blocks statt
  `ClosedEndGrooveBlockProfile(closedAtEntry: true, ...)` - die Rille
  läuft damit über den ganzen Block durch, unter dem Xylophon-Objekt
  hindurch. Fall-Trace/Pad-Position unverändert (EntryPointLocal/
  ExitPointLocal sind unabhängig vom Profil immer die echten Blockkanten).
  Betroffene Doku-Kommentare in TrackBlockSpawner.cs, TrackBlock.cs,
  TriggerFallMarbleTrace.cs, XylophoneBlockDecoration.cs und
  XylophonePadContent.cs aktualisiert. Unity-Batchmode-Kompilierung ohne
  Fehler geprüft.
