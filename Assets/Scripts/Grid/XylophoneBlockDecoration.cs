using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Purely cosmetic add-on for a Trigger TrackBlock (see BlockType.
    /// Trigger/0039): a simple, narrow block sitting on the side the
    /// marble actually falls in from - fallSideLocal, the block's own local
    /// representation of the WORLD direction back toward the previous
    /// TrackBlock (i.e. the opposite of InputDirection - the marble travels
    /// IN that direction to arrive here, so it arrives FROM the opposite
    /// side). The marble lands here, then rolls out through the groove in
    /// the block's OutputDirection. Deliberately simple - an axis-aligned
    /// box, elongated ACROSS whichever local axis (X or Z) fallSideLocal
    /// points along (like a real xylophone bar, laid crosswise to the
    /// direction the marble falls in), not the original horseshoe/capsule
    /// shapes (see 0039 follow-ups). The groove geometry itself
    /// (ClosedEndGrooveBlockProfile) is unaffected by this.
    /// Built the same way as TerrainDecoration/TunnelPortalDecoration: a
    /// separate child GameObject with its own MeshFilter/MeshRenderer, no
    /// collider, parented to the block.
    /// </summary>
    public static class XylophoneBlockDecoration
    {
        public static void Build(TrackBlock block, Vector3 fallSideLocal, float grooveRadius, float sideWidth, Material material)
        {
            float halfCell = grooveRadius + sideWidth; // == half the (square) block's own width/length
            float boxHeight = grooveRadius * 0.55f;
            float boxLength = halfCell * 1.15f; // ACROSS the fall direction - elongated
            float boxThickness = halfCell * 0.35f; // along the fall direction - narrow
            float offset = halfCell * 0.65f; // how far the box's own center sits from the block's center, toward the fall side (close to the outer edge)

            // Direction is always cardinal, and this block's own Yaw is
            // always a multiple of 90 degrees (see TrackBlockSpawner), so
            // fallSideLocal is always exactly along local X or local Z -
            // no arbitrary rotation needed, just pick which axis is
            // "thickness" (along the fall direction) vs. "length" (across
            // it) for the box.
            bool fallAlongX = Mathf.Abs(fallSideLocal.x) >= Mathf.Abs(fallSideLocal.z);
            float sign = fallAlongX ? Mathf.Sign(fallSideLocal.x) : Mathf.Sign(fallSideLocal.z);
            if (sign == 0f) sign = -1f;

            Vector3 center = fallAlongX ? new Vector3(sign * offset, 0f, 0f) : new Vector3(0f, 0f, sign * offset);
            float halfX = (fallAlongX ? boxThickness : boxLength) * 0.5f;
            float halfZ = (fallAlongX ? boxLength : boxThickness) * 0.5f;

            Mesh mesh = BuildBoxMesh(center, halfX, halfZ, boxHeight);

            GameObject xylophoneBlock = new GameObject("XylophoneBlock");
            xylophoneBlock.transform.SetParent(block.transform, false);

            MeshFilter filter = xylophoneBlock.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = xylophoneBlock.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        /// Simple axis-aligned box resting on Y=0 (the shoulder height),
        /// centered laterally at `center` (X/Z), extending up by `height`.
        /// Each face's winding was hand-verified (Cross(edge1,edge2) at
        /// each of the 6 faces) to give an outward-facing normal.
        private static Mesh BuildBoxMesh(Vector3 center, float halfX, float halfZ, float height)
        {
            Vector3 b0 = center + new Vector3(-halfX, 0f, -halfZ);
            Vector3 b1 = center + new Vector3(halfX, 0f, -halfZ);
            Vector3 b2 = center + new Vector3(halfX, 0f, halfZ);
            Vector3 b3 = center + new Vector3(-halfX, 0f, halfZ);
            Vector3 t0 = b0 + Vector3.up * height;
            Vector3 t1 = b1 + Vector3.up * height;
            Vector3 t2 = b2 + Vector3.up * height;
            Vector3 t3 = b3 + Vector3.up * height;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            AppendQuad(t0, t3, t2, t1, vertices, triangles); // top (+Y)
            AppendQuad(b0, b1, b2, b3, vertices, triangles); // bottom (-Y)
            AppendQuad(b1, b0, t0, t1, vertices, triangles); // -Z side
            AppendQuad(b3, b2, t2, t3, vertices, triangles); // +Z side
            AppendQuad(b0, b3, t3, t0, vertices, triangles); // -X side
            AppendQuad(b2, b1, t1, t2, vertices, triangles); // +X side

            Mesh mesh = new Mesh { name = "XylophoneBlock" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AppendQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, List<Vector3> vertices, List<int> triangles)
        {
            int baseIndex = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.Add(baseIndex); triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex); triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 3);
        }
    }
}
