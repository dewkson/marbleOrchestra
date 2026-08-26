# Ticket-System

Einfaches, dateibasiertes Ticket-System für dieses Repo. Kein externer Dienst
nötig - jedes Ticket ist eine Markdown-Datei, die zusammen mit dem Code
versioniert wird.

## Struktur

- `INDEX.md` - Tabellarische Übersicht aller Tickets (Status, Priorität, Typ).
- `TEMPLATE.md` - Vorlage für neue Tickets.
- `NNNN-slug.md` - Ein Ticket pro Datei, fortlaufend nummeriert (`0001`, `0002`, ...).

## Ticket anlegen

Am einfachsten über den Skill:

```
/ticket <Beschreibung des Problems oder Features>
```

Der Skill vergibt automatisch die nächste ID, legt die Ticket-Datei aus
`TEMPLATE.md` an und trägt sie in `INDEX.md` ein. Details siehe
`.claude/skills/ticket/SKILL.md`.

## Status-Werte

- `Open` (⚪) - noch nicht begonnen
- `In Progress` (🟡) - in Arbeit
- `Done` (✅) - erledigt
- `Wontfix` (🚫) - bewusst nicht umgesetzt

## Prioritäts-Werte

- `Low` (🔵), `Medium` (🟡), `High` (🟠), `Critical` (🔴)

## Typ-Werte

- `Bug`, `Feature`, `Task`, `Idea`

Im Frontmatter der einzelnen Ticket-Dateien bleiben Status und Priorität
reiner Text (maschinenlesbar). Nur in der Tabelle von `INDEX.md` werden sie
zusätzlich mit dem jeweiligen Icon vorangestellt (z.B. `✅ Done`, `🔴 Critical`).

Tickets manuell zu bearbeiten (z.B. Status auf `Done` setzen) ist jederzeit
erlaubt - einfach das Frontmatter und `INDEX.md` konsistent halten.
