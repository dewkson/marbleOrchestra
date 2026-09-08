using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Purely cosmetic add-on for a "xylophone pad" TrackBlock variant (see
    /// XylophonePadContent/0022): a rounded, horseshoe-shaped pillow (like
    /// a travel neck pillow) wrapping the 3 sides of the block that aren't
    /// the groove's single exit direction, open only where the groove
    /// itself passes through - so a marble arriving from any of those 3
    /// sides visually lands on the pillow before dropping into the groove
    /// at the block's center. Pairs with ClosedEndGrooveBlockProfile (the
    /// same groove-starts-near-center, closed-on-one-side mechanism
    /// already used for Start/Goal).
    /// Built the same way as TerrainDecoration/TunnelPortalDecoration: a
    /// separate child GameObject with its own MeshFilter/MeshRenderer, no
    /// collider, parented to the block.
    /// Geometry: a tube whose cross-section is a half-circle resting on
    /// Y=0 (the same "ridge" shape as TunnelPortalDecoration's roof), swept
    /// along a circular arc around the block's center (in the X-Z plane)
    /// instead of a straight line, covering all but a gap centered on the
    /// groove's exit direction (local +Z) - which is also where the actual
    /// groove dips below Y=0, so the two never overlap.
    /// </summary>
    public static class XylophonePillowDecoration
    {
        private const int TubeSegments = 10; // cross-section resolution
        private const int PathSteps = 24; // rings around the sweep - higher than TunnelPortalDecoration's since this covers ~270 degrees
        private const float GapDegrees = 100f; // opening centered on the groove's exit direction (+Z, 90 degrees in our X-Z angle convention)

        public static void Build(TrackBlock block, float grooveRadius, float sideWidth, Material terrainMaterial)
        {
            float halfWidth = grooveRadius + sideWidth;
            float pathRadius = halfWidth * 0.55f;
            float tubeRadius = halfWidth * 0.35f;

            float startDeg = 90f + GapDegrees * 0.5f;
            float sweepDeg = 360f - GapDegrees;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            Vector3[] previousRing = null;
            Vector3[] firstRing = null;
            for (int i = 0; i <= PathSteps; i++)
            {
                float t = (float)i / PathSteps;
                float angle = (startDeg + sweepDeg * t) * Mathf.Deg2Rad;
                Vector3[] ring = BuildTubeRing(angle, pathRadius, tubeRadius);

                if (i == 0) firstRing = ring;
                else AppendRingBand(previousRing, ring, vertices, triangles); // angle increases near->far throughout - see AppendRingBand's own remark on winding

                previousRing = ring;
            }

            // The two open ends of the horseshoe, both facing into the gap
            // - see AppendFanCap's remark on why they use opposite flips.
            AppendFanCap(firstRing, vertices, triangles, flip: false);
            AppendFanCap(previousRing, vertices, triangles, flip: true);

            Mesh mesh = new Mesh { name = "XylophonePillow" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject pillow = new GameObject("XylophonePillow");
            pillow.transform.SetParent(block.transform, false);

            MeshFilter filter = pillow.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = pillow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = terrainMaterial;
        }

        /// One ring of the tube's cross-section at the given angle around
        /// the block's center (angle=0 -> +X, angle=90deg -> +Z, matching
        /// TrackBlock's own local axes) - a half-circle resting on Y=0
        /// (same shape as TunnelPortalDecoration's roof cross-section),
        /// oriented so it lies in the plane spanned by the outward radial
        /// direction at this angle and the vertical (Y) axis - i.e. it
        /// always touches the ground at its inner and outer edges,
        /// regardless of where around the sweep it sits.
        private static Vector3[] BuildTubeRing(float angle, float pathRadius, float tubeRadius)
        {
            Vector3 radialDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 pathCenter = radialDir * pathRadius;

            Vector3[] ring = new Vector3[TubeSegments + 1];
            for (int j = 0; j <= TubeSegments; j++)
            {
                float t = (float)j / TubeSegments;
                float crossAngle = Mathf.PI * (1f - t);
                float x = Mathf.Cos(crossAngle) * tubeRadius;
                float y = Mathf.Sin(crossAngle) * tubeRadius;
                ring[j] = pathCenter + radialDir * x + Vector3.up * y;
            }
            return ring;
        }

        /// Connects two consecutive rings point for point, like a lofted
        /// band - always in the direction of increasing sweep angle (near
        /// = earlier/smaller angle, far = later/larger angle), so a single
        /// fixed winding order gives the correct outward-facing normal
        /// throughout (verified numerically against BuildTubeRing's own
        /// point layout - unlike TrackBlock/TunnelPortalDecoration's
        /// straight sweeps, there's no direction to flip here since the
        /// pillow only ever sweeps one way around).
        private static void AppendRingBand(Vector3[] nearRing, Vector3[] farRing, List<Vector3> vertices, List<int> triangles)
        {
            int nearBase = vertices.Count;
            vertices.AddRange(nearRing);
            int farBase = vertices.Count;
            vertices.AddRange(farRing);

            for (int j = 0; j < nearRing.Length - 1; j++)
            {
                int a = nearBase + j, b = nearBase + j + 1, c = farBase + j, d = farBase + j + 1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        /// Solid fan cap sealing one open end of the horseshoe. The two
        /// ends face opposite ways (both into the gap, from either side of
        /// it), so they need opposite winding - flip=false for the sweep's
        /// starting ring (faces "backwards", against the sweep direction),
        /// flip=true for its ending ring (faces "forwards", continuing the
        /// sweep direction) - same convention as
        /// TunnelPortalDecoration.AppendFanCap's own end-of-sweep cap.
        private static void AppendFanCap(Vector3[] ring, List<Vector3> vertices, List<int> triangles, bool flip)
        {
            int baseIndex = vertices.Count;
            vertices.AddRange(ring);

            for (int j = 1; j < ring.Length - 1; j++)
            {
                if (!flip)
                {
                    triangles.Add(baseIndex); triangles.Add(baseIndex + j); triangles.Add(baseIndex + j + 1);
                }
                else
                {
                    triangles.Add(baseIndex); triangles.Add(baseIndex + j + 1); triangles.Add(baseIndex + j);
                }
            }
        }
    }
}
