---
id: 0021
title: Murmel-Übergang zwischen Blöcken überprüfen und optimieren
type: Task
priority: Medium
status: Open
area: Physics
created: 2026-08-29
---

# 0021 - Murmel-Übergang zwischen Blöcken überprüfen und optimieren

## Beschreibung

Überprüfen und optimieren, wie sich die Murmel beim Übergang von einem
Block auf den nächsten verhält. Ziel ist ein kontrollierter,
zuverlässiger Übergang ohne unerwartetes Springen, Steckenbleiben oder
Verlassen des vorgesehenen Pfades.

## Details

- Setzt voraus, dass die Bahn aus einzelnen Blöcken besteht (siehe
  [[0017]], [[0018]]) mit ggf. Neigung ([[0019]]) und Höhenunterschieden
  zwischen Nachbarblöcken ([[0020]]) - dieses Ticket betrifft das
  physikalische Verhalten der Murmel genau an den Nahtstellen zwischen
  zwei Blöcken.
- Relevante bestehende Systeme: `Assets/Scripts/Grid/Marble.cs`,
  `Assets/Scripts/Grid/MarbleController.cs` - steuern aktuell die
  Murmelbewegung, vermutlich Ansatzpunkt für Anpassungen am
  Übergangsverhalten.
- Akzeptanzkriterien (grobe erste Fassung):
  - Die Murmel wechselt an Blockgrenzen zuverlässig auf den Folgeblock,
    ohne zu springen, hängenzubleiben oder vom vorgesehenen Pfad
    abzukommen.
  - Verhalten ist auch bei Höhenunterschieden zwischen Blöcken (siehe
    [[0020]]) und unterschiedlichen Neigungswinkeln (siehe [[0019]])
    konsistent.

## Notizen

