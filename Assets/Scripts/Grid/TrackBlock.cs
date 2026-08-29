using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Universal building block for the marble track: a solid, closed box
    /// whose size, height, material and orientation are configurable, and
    /// whose rollable top-surface shape comes from a pluggable Profile
    /// (flat by default, see IBlockProfile). Terrain, gameplay and
    /// instrument blocks are meant to reuse this same prefab/component and
    /// only swap the Profile or add sibling components (e.g. a future
    /// instrument trigger reading EntryPointLocal/ExitPointLocal) - never
    /// subclass it, since a Unity prefab can't swap a component's type per
    /// instance.
    /// Orientation is split in two: Yaw rotates the whole transform around
    /// the vertical axis (the block's footprint stays a normal, level box -
    /// flat bottom, vertical sides - at any yaw), while Tilt is baked
    /// directly into the mesh (the entry/exit rings of the top surface are
    /// shifted up/down by half the tilt-implied drop each), so only the
    /// rollable top surface itself slopes - never the block's bounding box.
    /// Rebuild() regenerates the mesh from the current Size/Height/Tilt/
    /// Profile; call it after changing any of those at runtime. Yaw alone
    /// is a cheap transform op and doesn't require a rebuild.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class TrackBlock : MonoBehaviour
    {
        [SerializeField] private Vector2 size = Vector2.one; // x = width (lateral), y = length (along travel direction)
        [SerializeField] private float height = 0.2f; // solid body thickness below the rollable top surface
        [SerializeField] private Material material;
        [SerializeField] private float yawDegrees; // rotation around world/local Y - facing direction
        [SerializeField] private float tiltDegrees; // downhill slope of the top surface, entry (higher) to exit (lower)

        public Vector2 Size { get => size; set { size = value; Rebuild(); } }
        public float Height { get => height; set { height = value; Rebuild(); } }

        public Material Material
        {
            get => material;
            set { material = value; ApplyMaterial(); }
        }

        public float YawDegrees { get => yawDegrees; set { yawDegrees = value; ApplyOrientation(); } }

        /// Changing Tilt reshapes the mesh (see class remarks), so it
        /// triggers a Rebuild rather than a cheap transform update.
        public float TiltDegrees { get => tiltDegrees; set { tiltDegrees = value; Rebuild(); } }

        private IBlockProfile profile = FlatBoxProfile.Instance;
        public IBlockProfile Profile
        {
            get => profile;
            set { profile = value ?? FlatBoxProfile.Instance; Rebuild(); }
        }

        /// Total vertical descent of the top surface from entry to exit,
        /// implied by TiltDegrees over the block's own length.
        private float Drop => size.y * Mathf.Tan(tiltDegrees * Mathf.Deg2Rad);

        /// Local-space point on the rollable surface at the entry (higher)
        /// edge, used to chain this block to the previous one's exit point.
        public Vector3 EntryPointLocal => profile.EntryPoint(size) + Vector3.up * (Drop * 0.5f);

        /// Local-space point on the rollable surface at the exit (lower)
        /// edge, used to chain this block to the next one's entry point.
        public Vector3 ExitPointLocal => profile.ExitPoint(size) - Vector3.up * (Drop * 0.5f);

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        private void OnEnable()
        {
            Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            CacheComponents();

            Vector2[] baseCrossSection = profile.BuildCrossSection(size);
            float halfDrop = Drop * 0.5f;
            Vector2[] entryCrossSection = OffsetY(baseCrossSection, halfDrop);
            Vector2[] exitCrossSection = OffsetY(baseCrossSection, -halfDrop);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            AppendTopSurface(entryCrossSection, exitCrossSection, vertices, triangles);
            AppendSideWalls(entryCrossSection, exitCrossSection, vertices, triangles);
            AppendEndCaps(entryCrossSection, exitCrossSection, vertices, triangles);
            AppendBottom(entryCrossSection, vertices, triangles);

            Mesh mesh = new Mesh { name = "TrackBlock" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            ApplyMaterial();
            ApplyOrientation();
        }

        private static Vector2[] OffsetY(Vector2[] source, float dy)
        {
            Vector2[] result = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++) result[i] = new Vector2(source[i].x, source[i].y + dy);
            return result;
        }

        private void CacheComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        }

        private void ApplyMaterial()
        {
            CacheComponents();
            if (material != null) meshRenderer.sharedMaterial = material;
        }

        /// Yaw only - rotates the whole block around the vertical axis so
        /// it faces the travel direction. Tilt lives in the mesh itself
        /// (see Rebuild), not in the transform, so the block's footprint
        /// stays a normal, level box (flat bottom, vertical sides) at any
        /// yaw, and yaw/tilt never need to be composed into one rotation.
        private void ApplyOrientation()
        {
            transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        private float HalfLength => size.y * 0.5f;
        private float BottomY => -height;

        private void AppendTopSurface(Vector2[] entryCrossSection, Vector2[] exitCrossSection, List<Vector3> vertices, List<int> triangles)
        {
            int entryRing = vertices.Count;
            for (int j = 0; j < entryCrossSection.Length; j++)
                vertices.Add(new Vector3(entryCrossSection[j].x, entryCrossSection[j].y, -HalfLength));

            int exitRing = vertices.Count;
            for (int j = 0; j < exitCrossSection.Length; j++)
                vertices.Add(new Vector3(exitCrossSection[j].x, exitCrossSection[j].y, HalfLength));

            for (int j = 0; j < entryCrossSection.Length - 1; j++)
            {
                int a = entryRing + j;
                int b = entryRing + j + 1;
                int c = exitRing + j;
                int d = exitRing + j + 1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        /// Flat side walls at the leftmost/rightmost cross-section point,
        /// dropping from the (possibly tilted) top surface down to the
        /// flat bottom, spanning the full entry-to-exit length. Each wall
        /// stays exactly in its X = const plane (tilt only ever changes Y),
        /// so it remains a vertical wall even when the top surface slopes.
        private void AppendSideWalls(Vector2[] entryCrossSection, Vector2[] exitCrossSection, List<Vector3> vertices, List<int> triangles)
        {
            Vector2 entryLeft = entryCrossSection[0];
            Vector2 exitLeft = exitCrossSection[0];
            Vector2 entryRight = entryCrossSection[entryCrossSection.Length - 1];
            Vector2 exitRight = exitCrossSection[exitCrossSection.Length - 1];

            // Note: flip is inverted relative to the Z-constant skirts below -
            // for an X-constant plane, the same topLeft/topRight/bottomLeft/
            // bottomRight winding produces the opposite outward direction.
            AppendQuad(
                new Vector3(entryLeft.x, entryLeft.y, -HalfLength), new Vector3(exitLeft.x, exitLeft.y, HalfLength),
                new Vector3(entryLeft.x, BottomY, -HalfLength), new Vector3(exitLeft.x, BottomY, HalfLength),
                vertices, triangles, flip: false);

            AppendQuad(
                new Vector3(entryRight.x, entryRight.y, -HalfLength), new Vector3(exitRight.x, exitRight.y, HalfLength),
                new Vector3(entryRight.x, BottomY, -HalfLength), new Vector3(exitRight.x, BottomY, HalfLength),
                vertices, triangles, flip: true);
        }

        /// Entry/exit skirts: trace each ring's own silhouette down to the
        /// flat bottom, closing the block off there (so a standalone block
        /// is watertight even where a neighbor isn't chained on yet).
        private void AppendEndCaps(Vector2[] entryCrossSection, Vector2[] exitCrossSection, List<Vector3> vertices, List<int> triangles)
        {
            AppendSkirt(entryCrossSection, -HalfLength, vertices, triangles, flip: true);
            AppendSkirt(exitCrossSection, HalfLength, vertices, triangles, flip: false);
        }

        private void AppendSkirt(Vector2[] crossSection, float z, List<Vector3> vertices, List<int> triangles, bool flip)
        {
            for (int j = 0; j < crossSection.Length - 1; j++)
            {
                Vector2 a = crossSection[j];
                Vector2 b = crossSection[j + 1];
                AppendQuad(
                    new Vector3(a.x, a.y, z), new Vector3(b.x, b.y, z),
                    new Vector3(a.x, BottomY, z), new Vector3(b.x, BottomY, z),
                    vertices, triangles, flip);
            }
        }

        /// Flat bottom rectangle. X range is identical for the entry and
        /// exit rings (tilt only ever changes Y), so either can be used.
        private void AppendBottom(Vector2[] crossSection, List<Vector3> vertices, List<int> triangles)
        {
            float left = crossSection[0].x;
            float right = crossSection[crossSection.Length - 1].x;

            AppendQuad(
                new Vector3(left, BottomY, -HalfLength), new Vector3(right, BottomY, -HalfLength),
                new Vector3(left, BottomY, HalfLength), new Vector3(right, BottomY, HalfLength),
                vertices, triangles, flip: true);
        }

        private static void AppendQuad(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight,
            List<Vector3> vertices, List<int> triangles, bool flip)
        {
            int baseIndex = vertices.Count;
            vertices.Add(topLeft);
            vertices.Add(topRight);
            vertices.Add(bottomLeft);
            vertices.Add(bottomRight);

            if (!flip)
            {
                triangles.Add(baseIndex); triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 3);
            }
            else
            {
                triangles.Add(baseIndex); triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 3); triangles.Add(baseIndex + 2);
            }
        }
    }
}
