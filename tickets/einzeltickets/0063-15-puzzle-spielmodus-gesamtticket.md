---
id: 0063
title: Neuer Spielmodus "15-Puzzle" (Gesamtticket)
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-11-20
---

# 0063 - Neuer Spielmodus "15-Puzzle" (Gesamtticket)

## Beschreibung

es soll ein neuer Spielmodus implementiert werden. Bitte dafür im Level Grid Editor einen neuen Bereich/Reiter anlegen für den Spielmodus. Der bisherige soll "Card Swap" genannt werden und der neue Spielmodus soll "15-puzzle" heißen. Dieser Modus soll an das bekannte 15-Puzzle angelehnt sein. Dabei soll es neben den Pipekarten auch freie Zellen geben, damit man die entsprechenden Pipekarten (Nachbarn) auf eine freie Zelle schieben kann. Wenn nötig, unterteile dieses Ticket in kleinere Untertickets.

## Details

Übergeordnetes Ticket, aufgeteilt in Untertickets:

- 0064 - Spielmodus-Auswahl (Card Swap / 15-Puzzle) im Level Grid Editor
- 0065 - Freie Zellen im Level Grid Editor definieren (15-Puzzle)
- 0066 - Schiebe-Mechanik für Pipekarten im 15-Puzzle
- 0067 - Visuelles Feedback und Animation fürs Schieben

Der bisherige Modus (Pipe-Swap per Drag & Drop, siehe 0004) wird in "Card Swap" umbenannt.
Betroffen: `Assets/Scripts/Grid/Editor/LevelGridEditorWindow.cs`, `LevelData.cs`, `PathGrid.cs`, `GridInputHandler.cs`.

Akzeptanzkriterien: Alle Untertickets erledigt; pro Level ist der Spielmodus wählbar und spielbar.

## Notizen

- 2026-11-20: Umgesetzt über 0064-0067. Entscheidungen: Modus pro Level; freie Zelle = nicht blockierte Zelle ohne Karte; Schieben ganzer Reihen/Spalten wie im klassischen 15-Puzzle; Randomize per zufälliger gültiger Schiebezüge. Kompiliert, in Unity noch nicht getestet.
