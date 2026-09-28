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
- 2026-09-28 (Korrektur): Erste Umsetzung ließ Trigger-Blocks nie eine
  Kurve zeigen - `isTurn` in `TrackBlockSpawner.cs` erlaubte nur
  `BlockType.Normal`, weil das vor der Änderung irrelevant war (die Rille
  war ja gekappt). Mit durchgängiger Rille zeigte ein abbiegender
  Trigger-Block (z.B. Eingang von links, Ausgang nach oben) dadurch eine
  falsch ausgerichtete gerade Röhre statt der echten Kurve. Fix: `isTurn`
  gilt jetzt auch für `BlockType.Trigger`; `TrackBlockSpawner.CreateTrace`
  prüft `Trigger` jetzt vor `isTurn`/`CurvedMarbleTrace`, damit ein
  kurvender Trigger-Block weiterhin `TriggerFallMarbleTrace` bekommt (statt
  eines reinen Roll-Traces) - dessen Bounce/Roll-Phase folgt jetzt über
  `TrackBlock.SampleGroovePointLocal` der echten (ggf. gekrümmten)
  Rillen-Mittellinie statt einer geraden Linie zum Blockausgang. Damit
  zeigt ein kurvender Trigger-Block jetzt die echte Kurve, mit dem
  Xylophon-Element darüber. Unity-Editor war beim Nutzer bereits offen,
  daher konnte kein frischer Batchmode-Kompilierungslauf gemacht werden -
  Änderungen manuell auf Typkorrektheit und Aufrufreihenfolge
  (Profile/Size/Yaw/SetCurve vor CreateTrace) geprüft.
- 2026-09-28 (Follow-up): Xylophon-Element auf Wunsch näher an die
  Blockkante gerückt. `XylophoneBlockDecoration.PadCenterOffset` (vorher
  fixer Faktor 0.65 * halfCell) rückt die Bar jetzt so nah wie möglich an
  die echte Blockkante (halfCell), abzüglich der halben Bar-Dicke und
  eines kleinen 3%-Sicherheitsabstands (`EdgeMarginFraction`), damit sie
  nicht optisch in die Außenwand clippt. Bar-Dicke dafür in eine geteilte
  `BoxThickness`-Hilfsfunktion ausgelagert, damit `Build` und
  `PadCenterOffset` nie auseinanderlaufen können. Der Murmel-Trace ist
  automatisch mitgezogen: `TriggerFallMarbleTrace.padLanding` liest
  denselben `PadCenterOffset`-Wert, die Murmel landet also weiterhin exakt
  auf der (jetzt weiter außen liegenden) Bar; Fall-Start bleibt an der
  echten Blockkante (unverändert), nur die Fallstrecke wird dadurch
  entsprechend kürzer. Auch hier war der Unity-Editor beim Nutzer bereits
  offen, daher nur manuelle Prüfung möglich.
