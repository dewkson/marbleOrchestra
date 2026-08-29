---
id: 0014
title: Murmelbewegung entlang der 3D-Bahn (kinematisch und Physics)
type: Feature
priority: Medium
status: Done
area: Gameplay
created: 2026-08-28
---

# 0014 - Murmelbewegung entlang der 3D-Bahn (kinematisch und Physics)

## Beschreibung

Wir brauchen die Murmelbewegung im gleichen Tempo in 3D. Ich würde davon
absehen, das mit echten Physics zu machen, sondern die Kugel kinematisch
durch die 3D Bahn zu bewegen, so als ob es scheint, dass sie aus dem Loch
am Start herauskommt und im Loch beim Ende verschwindet. Eigentlich würde
ich sogar gerne beide Wege ausprobieren: kinematisch und physics-based.

## Details

Betroffene/relevante bestehende Systeme:
- `Assets/Scripts/Grid/MarbleController.cs` - bewegt die Murmel(n) aktuell
  rein 2D per `Vector3.Lerp` zwischen `PathGrid.CellToLocalPosition`-Punkten
  (XY-Ebene, Z=0), mit `cellsPerSecond` als Tempo-Vorgabe.
- `Assets/Scripts/Grid/Marble.cs` - rein visuelle 2D-Sprite-Repräsentation
  der Murmel; müsste für eine 3D-Darstellung ergänzt/ersetzt werden.
- `Assets/Scripts/Grid/TrackTerrainGenerator.cs` - erzeugt die 3D-Bahn
  (Rinne mit Gefälle, siehe 0013) inkl. der Loch-Öffnungen an Start/Ziel
  (siehe Ergänzung dort) - die 3D-Bewegung soll dieser Geometrie folgen
  und optisch aus dem Start-Loch kommen bzw. im Ziel-Loch verschwinden.
- Aktuell existiert keine 3D-Physik-Interaktion (Rigidbody/Collider) für
  die Murmel - für den Physics-basierten Ansatz müsste das neu aufgebaut
  werden.

Akzeptanzkriterien (grobe erste Fassung):
- Murmel bewegt sich in 3D entlang des von TrackTerrainGenerator erzeugten
  Bahnverlaufs (inkl. Gefälle), im gleichen Tempo wie die bisherige 2D-Logik
  (`cellsPerSecond`).
- Am Start erscheint die Murmel sichtbar aus dem Loch kommend, am Ziel
  verschwindet sie sichtbar im Loch.
- Zwei Bewegungsarten sollen ausprobiert/verglichen werden:
  1. Kinematisch: Position wird direkt (ohne Physik-Engine) entlang der
     Bahnkurve interpoliert, analog zur bisherigen 2D-Lerp-Logik.
  2. Physics-based: Murmel bewegt sich über Unity-Physik (Rigidbody +
     Collider/Rail-Geometrie) durch die Bahn.
- Offen: wie beide Varianten koexistieren/umschaltbar sein sollen (z.B. per
  Einstellung), oder ob es sich um einen reinen Vergleichs-Spike handelt.

## Notizen

- 2026-08-28: Umgesetzt als `MarbleController.MovementMode`
  (Kinematic2D/Kinematic3D/Physics3D, im Inspector umschaltbar,
  Default weiterhin Kinematic2D - bestehendes Verhalten unverändert):
  - Kinematic3D: neue `TrackTerrainGenerator.SampleGroovePosition()` liefert
    die Weltposition auf dem Rinnenboden für eine fraktionale Bahn-Position;
    `MarbleController.RunAlongPath3D()` bewegt die Murmel darauf mit
    demselben `cellsPerSecond`-Tempo wie die 2D-Variante.
  - Physics3D: `TrackTerrainGenerator` bekommt einen `MeshCollider` (nicht
    konvex, exakte Rinnengeometrie); die Murmel bekommt Rigidbody +
    SphereCollider und wird kurz über dem Start-Loch fallen gelassen
    (`RunAlongPathPhysics()`), Ziel-Erkennung über Abstand zum Ziel-Loch
    plus Timeout als Sicherheitsnetz.
  - Neue 3D-Murmel-Darstellung `Marble.CreateSphere3D()` (Kugel-Primitive
    statt 2D-Sprite).
  - Ungetestet in der Unity-Physik-Engine selbst (kein Editor-Zugriff) -
    besonders Reibung/Rollverhalten und die Kollisions-Feinheiten an den
    Loch-Übergängen (0013) sollten im Editor geprüft/getunt werden.
- 2026-08-28: Nachbesserungen am Physics3D-Modus nach erstem Test:
  1. Separater, kleinerer Radius für die 3D-Murmel (`marbleRadius3D`,
     Default 0.1 statt 0.15) - unabhängig vom 2D-Radius, um Steckenbleiben
     in der Rinne zu vermeiden. Bestimmt jetzt auch die Rinnenbreite
     (`TrackTerrainGenerator.grooveRadius`-Herleitung).
  2. Ecken werden per Chaikin Corner-Cutting auf der Mittellinie
     abgerundet (`cornerSmoothingIterations`, Default 2) statt als scharfer
     Knick - betrifft Mesh UND kinematische Bewegung (beide nutzen dieselbe
     geglättete Mittellinie), Physics3D profitiert automatisch über den
     MeshCollider.
  3. Start hat kein Loch mehr (`AppendSolidCap`) - Ursache des
     Durchfallens war vermutlich, dass der Lochbereich keinen Boden hatte.
     Zusätzlich spawnt die Physics-Murmel jetzt `physicsSpawnOffset`
     (Default 30%) hinter dem Start, wo das Gefälle schon greift, statt
     exakt auf der Kappe.
  4. Ziel zeigt jetzt das (vorher für Start genutzte) Loch-Design
     (`AppendHoleCap`) - Rinne mündet nur von einer Seite hinein, dahinter
     schließen die Schultern ab.
