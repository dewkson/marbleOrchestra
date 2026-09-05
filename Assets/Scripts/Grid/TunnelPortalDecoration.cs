using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Purely cosmetic add-on for a Start/Goal TrackBlock (see
    /// ClosedEndGrooveBlockProfile): a small hill straddling the block's
    /// closed half with an arched tunnel mouth facing the groove, and a
    /// dark disc set back inside it, so the groove visually reads as
    /// emerging from (Start) or disappearing into (Goal) a tunnel instead
    /// of just dead-ending against a flat wall - see 0022.
    /// Built the same way as TerrainDecoration's moss clumps: a separate
    /// child GameObject with its own MeshFilter/MeshRenderer, no collider
    /// (the closed half is never actually traveled by the marble - see
    /// TrackBlock's own EntryPointLocal/ExitPointLocal remarks), parented
    /// to the block so it's destroyed along with it.
    /// Geometry: an annular arch frame at the tunnel mouth (sized just
    /// large enough to clear the groove/marble), opening onto a dark disc
    /// set back behind it, with a mound roof that tapers smoothly from the
    /// frame's own radius down to a point at the back - so it reads as the
    /// tunnel being let into a small hill rather than a capped pipe.
    /// </summary>
    public static class TunnelPortalDecoration
    {
        private const int ArcSegments = 10;
        private const int RoofRingCount = 7; // rings along the taper, including the frame ring itself

        /// railExtension: how far TrackBlock's own groove now reaches past
        /// the block's center (see ClosedEndGrooveBlockProfile) - the dark
        /// interior disc is placed at least that far back so it hides the
        /// groove's own transition wall instead of leaving a visible strip
        /// of plain ground between the tunnel mouth and the darkness.
        public static void Build(TrackBlock block, bool closedAtEntry, float grooveRadius, float sideWidth, Vector2 size, float railExtension, Material terrainMaterial, Material tunnelMaterial)
        {
            float tunnelRadius = grooveRadius * 1.3f; // just enough clearance for the groove/marble to pass through
            float hillRadius = Mathf.Min(tunnelRadius * 1.6f, (grooveRadius + sideWidth) * 0.9f); // frame thickness around the opening
            float hillLength = Mathf.Min(hillRadius * 2.2f, size.y * 0.5f * 0.85f);
            float tunnelDepth = Mathf.Min(Mathf.Max(tunnelRadius * 0.6f, railExtension + tunnelRadius * 0.4f), hillLength * 0.85f);

            // The hill sits on the block's closed half; its tunnel mouth
            // faces the open/groove half - towards +Z for Start (closed at
            // entry, groove runs from Z=0 to +HalfLength) and towards -Z
            // for Goal (closed at exit, groove runs from -HalfLength to
            // Z=0) - see ClosedEndGrooveBlockProfile/TrackBlock.Rebuild.
            float dir = closedAtEntry ? -1f : 1f;

            Vector2[] frameArc = BuildArc(hillRadius, ArcSegments);
            Vector2[] innerArc = BuildArc(tunnelRadius, ArcSegments);

            List<Vector3> vertices = new List<Vector3>();
            List<int> mainTriangles = new List<int>();
            List<int> tunnelTriangles = new List<int>();

            // Front arch frame at Z=0: full hill radius down to the smaller
            // tunnel arc - the tunnel arc's own area is left open, which is
            // what reveals the dark disc behind it.
            AppendRingBand(frameArc, 0f, innerArc, 0f, vertices, mainTriangles, flip: dir < 0f);

            // Mound roof: tapers the frame's radius smoothly down to zero
            // (a point) at the hill's back instead of ending in a flat
            // disc, so it blends into the ground like a real mound rather
            // than looking like a capped-off pipe.
            Vector2[] previousRing = frameArc;
            float previousZ = 0f;
            for (int i = 1; i < RoofRingCount; i++)
            {
                float t = (float)i / (RoofRingCount - 1);
                float scale = Mathf.Cos(t * Mathf.PI * 0.5f); // 1 at the frame, easing down to 0 at the back
                Vector2[] ring = BuildArc(hillRadius * scale, ArcSegments);
                float z = dir * hillLength * t;

                AppendRingBand(previousRing, previousZ, ring, z, vertices, mainTriangles, flip: dir > 0f);

                previousRing = ring;
                previousZ = z;
            }

            // Dark tunnel interior, set back behind the frame's opening.
            AppendFanCap(innerArc, dir * tunnelDepth, vertices, tunnelTriangles, flip: dir < 0f);

            Mesh mesh = new Mesh { name = "TunnelPortal" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2; // 0 = hill (terrainMaterial), 1 = dark tunnel interior (tunnelMaterial)
            mesh.SetTriangles(mainTriangles, 0);
            mesh.SetTriangles(tunnelTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject portal = new GameObject("TunnelPortal");
            portal.transform.SetParent(block.transform, false);

            MeshFilter filter = portal.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = portal.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { terrainMaterial, tunnelMaterial };
        }

        /// Half-circle arc (upper semicircle, resting on Y=0 at both ends),
        /// left to right - same sweep principle as
        /// GrooveProfileUtility.BuildProfile, just the top half instead of
        /// the bottom.
        private static Vector2[] BuildArc(float radius, int segments)
        {
            Vector2[] points = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.PI * (1f - t);
                points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            return points;
        }

        /// Connects two same-length arcs (near/far - possibly at different Z
        /// and/or different radius) point for point, like a lofted band.
        /// flip picks between the two possible winding orders so the
        /// visible side faces the direction the caller needs - see Build's
        /// call sites for how each is derived.
        private static void AppendRingBand(Vector2[] nearArc, float nearZ, Vector2[] farArc, float farZ, List<Vector3> vertices, List<int> triangles, bool flip)
        {
            int nearRing = vertices.Count;
            for (int j = 0; j < nearArc.Length; j++) vertices.Add(new Vector3(nearArc[j].x, nearArc[j].y, nearZ));

            int farRing = vertices.Count;
            for (int j = 0; j < farArc.Length; j++) vertices.Add(new Vector3(farArc[j].x, farArc[j].y, farZ));

            for (int j = 0; j < nearArc.Length - 1; j++)
            {
                int a = nearRing + j, b = nearRing + j + 1, c = farRing + j, d = farRing + j + 1;
                if (!flip)
                {
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(b); triangles.Add(d); triangles.Add(c);
                }
                else
                {
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }
        }

        /// Solid fan cap sealing one end of an arc.
        private static void AppendFanCap(Vector2[] arc, float z, List<Vector3> vertices, List<int> triangles, bool flip)
        {
            int ring = vertices.Count;
            for (int j = 0; j < arc.Length; j++) vertices.Add(new Vector3(arc[j].x, arc[j].y, z));

            for (int j = 1; j < arc.Length - 1; j++)
            {
                if (!flip)
                {
                    triangles.Add(ring); triangles.Add(ring + j); triangles.Add(ring + j + 1);
                }
                else
                {
                    triangles.Add(ring); triangles.Add(ring + j + 1); triangles.Add(ring + j);
                }
            }
        }
    }
}
