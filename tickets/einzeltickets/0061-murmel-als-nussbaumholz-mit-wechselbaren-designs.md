---
id: 0061
title: Murmel als Nussbaumholz mit wechselbaren Designs
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-10-19
---

# 0061 - Murmel als Nussbaumholz mit wechselbaren Designs

## Beschreibung

ich möchte die Optik der Murmel ändern: Es soll aussehen wie eine nussbaum hölzerne Murmel mit einer sehr leichten Maserung. Generell sollen unterschiedliche Murmel-Designs möglich sein

## Details

- Betroffen: `Assets/Scripts/Grid/Marble.cs` (`CreateSphereMaterial`, `GetMarbleTexture` - aktuell pinke prozedurale Marmoroptik, siehe Ticket 0034) und `Assets/Scripts/Grid/MarbleController.cs`.
- Akzeptanzkriterien:
  - Standard-Optik: Nussbaumholz (dunkles Braun) mit sehr leichter, dezenter Maserung.
  - Murmel-Designs sind austauschbar/erweiterbar (z.B. als datengetriebene Definition statt fest verdrahtetem Material), sodass weitere Designs ohne Codeänderung am Material ergänzt werden können.

## Notizen

Umgesetzt: neues ScriptableObject `MarbleDesign` (Create > MarbleOrchestra > Marble Design) mit Pattern (Wood/Veined), Farben, Frequenz, Turbulenz, Stärke, Glanz oder komplettem Material-Override. `MarbleController.marbleDesign` wählt das Design; leer = eingebautes Nussbaum-Default. `MarbleDesign.CreatePinkMarble()` bewahrt die alte pinke Optik als Alternative. Marble.cs enthält keine Material-Logik mehr.
