---
id: 0006
title: Content-Marker im Level Grid Editor als Buchstabe statt Farbkasten
type: Feature
priority: Medium
status: Done
area: Level Editor
created: 2026-08-26
---

# 0006 - Content-Marker im Level Grid Editor als Buchstabe statt Farbkasten

## Beschreibung

Für die SoundTriggerContents wäre es hilfreich in den Zellen im Editor nicht
nur einen einfarbigen Kasten oben links anzuzeigen, sondern einen
Buchstaben. Ich habe aktuell z.B. Kick, Snare und Hat. Ich will zu den
einzelnen SoundTriggerContents dann die Buchstaben K, S und H definieren
können und diese sollen im Grid auch so oben links angezeigt werden.

## Details

- Betroffen: `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`,
  Methode `DrawCell` - zeichnet aktuell für jede Zelle mit `content != null`
  einen festen gelben 8x8-Marker oben links (`new Color(1f, 0.85f, 0.2f)`),
  unabhängig vom konkreten Content-Typ.
- `Assets/Scripts/Grid/CellContentDefinition.cs` ist die gemeinsame
  Basisklasse aller Content-Typen (u.a.
  `Assets/Scripts/Grid/SoundTriggerContent.cs`) und hat bisher nur
  `ContentId` (Name), aber kein konfigurierbares Anzeige-Label - müsste um
  ein Buchstaben/Label-Feld ergänzt werden, damit der Marker generisch für
  alle Content-Typen bleibt (nicht nur SoundTrigger-spezifisch).
- Bestehende Assets: `Assets/Levels/Contents/Sound_Kick.asset`,
  `Sound_Snare.asset`, `Sound_Hat.asset`.
- Akzeptanzkriterien:
  - Pro `CellContentDefinition`-Asset lässt sich ein Buchstabe/Kurz-Label
    definieren (z.B. K, S, H für Kick/Snare/Hat).
  - Im Level-Grid-Editor-Fenster wird in der Zelle oben links statt des
    einfarbigen Kastens dieser Buchstabe angezeigt.
  - Zellen ohne Content zeigen weiterhin keinen Marker.

## Notizen

Umgesetzt: `Assets/Scripts/Grid/CellContentDefinition.cs` hat jetzt ein
`label`-Feld (`string`, Default `"?"`) mit öffentlichem `Label`-Getter -
generisch auf der Basisklasse, damit es für alle Content-Typen funktioniert.
Da es ein normales `[SerializeField]` ist, taucht es automatisch im
Standard-Inspector jedes Content-Assets auf und ist dort editierbar, ohne
dass die Level-Grid-Editor-UI dafür erweitert werden musste. Die drei
bestehenden Assets `Sound_Kick.asset`, `Sound_Snare.asset`, `Sound_Hat.asset`
haben `label: K` / `S` / `H` erhalten.

In `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs` zeigt `DrawCell`
jetzt statt des einfarbigen Kastens oben links ein kleines dunkles
Label-Feld mit `content.Label` als Text (neue `GetContentLabelStyle()`).
