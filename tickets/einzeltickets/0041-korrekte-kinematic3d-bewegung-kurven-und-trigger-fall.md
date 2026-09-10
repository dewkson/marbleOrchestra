---
id: 0041
title: Korrekte Kinematic3D-Bewegung bei Kurven und Trigger-Fall verifizieren
type: Task
priority: Medium
status: Open
area: Physics
created: 2026-09-09
---

# 0041 - Korrekte Kinematic3D-Bewegung bei Kurven und Trigger-Fall verifizieren

## Beschreibung

Folge-Ticket zu [[0039]] (TrackBlock-Typen und Verbindungslogik
vereinheitlichen), aus dessen Umsetzung heraus entstanden: die
Kinematic3D-Bewegung der Kugel (`TrackBlockSpawner.SampleTrackPosition`/
`JunctionLocalXZ`/`SampleFloorY`, `TrackBlock.SampleGroovePointLocal`)
wurde im Rahmen von Ticket 39 mehrfach nachgebessert (Kurven-Verfolgung,
Trigger-Fallhöhe), konnte in der Bearbeitungs-Umgebung aber nie visuell
im Unity-Editor getestet werden. Dieses Ticket verfolgt die
abschließende Verifikation und ggf. weitere Feinjustierung dieser
Bewegung, getrennt von Ticket 39 (das inhaltlich abgenommen und
geschlossen ist).

## Details

Betroffene Bereiche, die im Editor gegengeprüft werden sollten:

- **Normale 90°-Kurven (Normal-Blocks):** Kugel soll im Kinematic3D-
  Modus sichtbar dem gebogenen Groove folgen (`TrackBlock.IsCurved`,
  `SampleGroovePointLocal`), nicht linear abkürzen oder springen. Zuletzt
  über `TrackBlockSpawner.JunctionLocalXZ` gelöst (Kurven-Anschluss statt
  Zellmitte an Kurven-Grenzen) - mehrere frühere Ansätze (Endpunkt-Blend
  auf Zellmitten, falsche Achsen-Zuordnung) waren fehlerhaft und wurden
  verworfen, siehe Notizen in [[0039]].
- **Trigger-Blocks (Xylophon/Sound) mit abweichender Ein-/
  Ausgangsrichtung:** Kugel soll erkennbar "abrupt" auf das
  Trigger-Element fallen (`FallHeight`, zuletzt auf 1.2 erhöht) statt zu
  gleiten - zuletzt behoben durch strikte Trennung von X/Z- und
  Höhen-Interpolation (`SampleFloorY` nutzt nur den eigenen Block, nicht
  den Nachbarn).
- **Zusammenspiel Kurve + Trigger unmittelbar benachbart:** in
  `JunctionLocalXZ`/`SampleFloorY` bisher nicht speziell behandelt -
  sollte bei Gelegenheit mitgeprüft werden, falls ein Level so etwas
  enthält.
- **Start/Goal-Sonderfall:** bleiben bewusst zellmitten-basiert (passend
  zur Tunnel-Deko) - eine Kurve direkt neben Start/Goal wäre laut
  aktuellem Design-Kommentar noch nicht korrekt abgedeckt, kommt aber in
  bestehenden Leveln nicht vor.
- Falls sich bei der Verifikation zeigt, dass die aktuelle Lösung
  strukturell nicht ausreicht (z.B. Kurve direkt neben Start/Goal doch
  benötigt wird), ggf. Bezug zu [[0038]] (generischer, pro Block-Variante
  definierbarer Kinematic-Trace) prüfen - dort wäre ein grundsätzlicherer
  Ansatz denkbar, der die hier einzeln gepatchten Fälle vereinheitlicht.

## Notizen
