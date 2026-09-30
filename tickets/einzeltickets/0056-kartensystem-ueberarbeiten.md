---
id: 0056
title: Kartensystem überarbeiten (weniger ScriptableObjects)
type: Task
priority: Medium
status: Open
area: Tooling
created: 2026-09-30
---

# 0056 - Kartensystem überarbeiten (weniger ScriptableObjects)

## Beschreibung

Kartensystem überarbeiten. Aktuell ist es so, dass für jede mögliche Kombination von Attributen ein einzelnes Karten scriptable Objekt generiert wird. Gerade in Testphasen kann das zu einer unübersichtlichen Anzahl von Objekten führen. Wir sollten dieses System überdenken und eventuell mehr mit Prefabs und Variants arbeiten (nur ein Vorschlag)

## Details

- Vermutlich betroffen: `Assets/Scripts/Grid/PipeDefinition.cs` (ScriptableObject pro Karte/Pipe) sowie `CellContentDefinition.cs` und `LevelData.cs`, die darauf verweisen.
- Ziel: Anzahl der Karten-Assets reduzieren bzw. übersichtlicher halten. Prefabs/Variants sind nur ein Lösungsvorschlag; Alternativen (z.B. Karten zur Laufzeit aus Attributen generieren) sind zu prüfen.
- Vor der Umsetzung: Ist-Zustand analysieren (wo und wie werden die Karten-Assets erzeugt und genutzt) und Lösungsansatz festlegen.

## Notizen

