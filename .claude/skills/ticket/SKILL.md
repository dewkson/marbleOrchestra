---
name: ticket
description: Nimmt anhand einer Freitext-Beschreibung ein neues Ticket auf und dokumentiert es im dateibasierten Ticket-System unter tickets/. Nutzen bei "leg ein Ticket an", "notier den Bug", "das sollten wir tracken" o.ä.
argument-hint: <Beschreibung des Bugs/Features/Tasks>
---

Du legst ein neues Ticket im Ticket-System dieses Repos an (`tickets/`). Die
Beschreibung des Users kommt als Argument. Falls kein Argument übergeben
wurde, frage kurz danach.

## Ablauf

1. **Kontext laden**: Lies `tickets/INDEX.md` und `tickets/TEMPLATE.md`. Falls
   `tickets/` noch nicht existiert, lege es inkl. `INDEX.md` (leere Tabelle)
   und `TEMPLATE.md` neu an, orientiert an diesem Skill.

2. **Nächste ID bestimmen**: Schau in `tickets/` nach vorhandenen Dateien
   `NNNN-*.md` und nimm die höchste Nummer + 1, vierstellig mit führenden
   Nullen (`0001`, `0002`, ...). Bei leerem Verzeichnis: `0001`.

3. **Beschreibung analysieren** und daraus ableiten (nichts erfinden, was
   nicht aus der Beschreibung oder dem Code hervorgeht):
   - **Titel**: kurz und prägnant (max. ~8 Wörter), aus der Beschreibung
     destilliert.
   - **Typ**: `Bug` (Fehlverhalten, Crash, "funktioniert nicht"), `Feature`
     (neue Funktionalität), `Task` (Aufräumarbeit, Refactor, Setup) oder
     `Idea` (vage/unausgereift). Im Zweifel `Task`.
   - **Priorität**: `Medium` als Default. `High`/`Critical` nur wenn die
     Beschreibung Dringlichkeit/Blocker signalisiert (z.B. "blockiert",
     "crash", "kaputt"). `Low` bei "nice to have", "irgendwann".
   - **Area**: grob passendes Feld (`Gameplay`, `Level Editor`, `Audio`,
     `UI`, `Physics`, `Tooling`, `Other`) - bei Bedarf kurz in `Assets/`
     nachsehen (z.B. via Grep/Glob), welche Scripts/Systeme betroffen sind.
   - Beschreibt der User erkennbar **mehrere unabhängige** Probleme/Features
     in einer Nachricht, lege für jedes ein eigenes Ticket mit eigener ID an,
     statt sie zu vermischen.

4. **Ticket-Datei anlegen** unter `tickets/NNNN-<slug>.md` (Slug = Titel,
   klein geschrieben, Bindestriche statt Leerzeichen) nach dem Schema aus
   `TEMPLATE.md`:
   - Frontmatter (`id`, `title`, `type`, `priority`, `status: Open`, `area`,
     `created`: heutiges Datum `YYYY-MM-DD`).
   - Abschnitt "Beschreibung": die Original-Beschreibung des Users,
     möglichst wortgetreu.
   - Abschnitt "Details": deine Einordnung - betroffene Dateien/Systeme
     (falls recherchiert), bei Bugs eine vermutete Ursache/Repro-Hinweise
     falls ersichtlich, bei Features grobe Akzeptanzkriterien. Wenn dazu
     nichts Konkretes bekannt ist, diesen Abschnitt kurz halten statt Text
     zu füllen.
   - Abschnitt "Notizen": leer lassen (Platz für spätere Updates).

5. **Index aktualisieren**: Neue Zeile in der Tabelle von `tickets/INDEX.md`
   ergänzen (ID, Titel, Typ, Priorität, Status, Area, Erstellt-Datum). In den
   Spalten Priorität und Status jeweils das passende Icon aus der Legende am
   Ende von `INDEX.md` voranstellen (z.B. `🟡 Medium`, `⚪ Open`) - im
   Frontmatter der Ticket-Datei selbst bleiben beide Werte reiner Text.

6. **Kurz bestätigen**: Antworte dem User knapp mit Ticket-ID, Titel und
   Pfad der angelegten Datei. Keine langen Zusammenfassungen.

## Nicht tun

- Keine Tickets für triviale, bereits erledigte Dinge anlegen - im Zweifel
  nachfragen statt zu raten.
- Keine Status-Änderungen an bestehenden Tickets vornehmen, außer der User
  bittet explizit darum (z.B. "schließ Ticket 0003").
- Keine Zusatzfelder oder Abschnitte über das Template hinaus einführen.
