using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Builds a 3D terrain ribbon for every currently completed
    /// Start-to-Goal track: a strip that always slopes downward in the
    /// direction of travel, with a semicircular U-shaped rail groove in the
    /// middle (radius derived from the marble so it fits snugly and can roll
    /// along it) and a flat, definable-width shoulder extruded out to each
    /// side (side view: ---u---). Regenerates whenever the set of completed
    /// paths changes (pipe swap, level rebuild).
    /// Purely geometric - marble movement is driven externally
    /// (MarbleController), either by sampling SampleGroovePosition
    /// (kinematic) or via Unity physics colliding against this object's
    /// MeshCollider (physics-based), see 0014.
    /// Lives on its own GameObject (with its own MeshFilter/MeshRenderer/
    /// MeshCollider); grid and marbleController are wired in the Inspector
    /// or auto-found at Awake.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class TrackTerrainGenerator : MonoBehaviour
    {
        [SerializeField] private PathGrid grid;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private float sideWidth = 0.4f; // flat shoulder extruded to each side of the groove
        [SerializeField] private float grooveRadius = 0f; // <= 0: derive from marble radius
        [SerializeField] private int grooveArcSegments = 8; // resolution of the semicircular U profile
        [SerializeField] private float heightDropPerCell = 0.25f;
        [SerializeField] private Color terrainColor = new Color(0.45f, 0.32f, 0.22f);

        public float GrooveRadius => grooveRadius;

        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private Vector2[] profile;
        private List<IReadOnlyList<Vector2Int>> lastPaths = new List<IReadOnlyList<Vector2Int>>();

        private void Awake()
        {
            if (grid == null) grid = FindFirstObjectByType<PathGrid>();
            if (marbleController == null) marbleController = FindFirstObjectByType<MarbleController>();
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();

            if (grooveRadius <= 0f)
            {
                float marbleRadius = marbleController != null ? marbleController.MarbleRadius : 0.15f;
                grooveRadius = marbleRadius * 1.15f;
            }

            profile = BuildProfile();
            GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(terrainColor);
        }

        private void Update()
        {
            List<IReadOnlyList<Vector2Int>> paths = FindCompletedPaths();
            if (PathListsEqual(paths, lastPaths)) return;

            lastPaths = paths;
            Mesh mesh = paths.Count > 0 ? BuildMesh(paths) : null;
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        /// World-space point on the groove's floor (where a marble of the
        /// given radius rests) at a fractional position along the path -
        /// e.g. 2.3 means 30% of the way from path[2] to path[3]. Used for
        /// kinematic 3D movement so the marble follows this exact geometry.
        public Vector3 SampleGroovePosition(IReadOnlyList<Vector2Int> path, float pathPosition, float marbleRadius)
        {
            int count = path.Count;
            float floorOffset = marbleRadius - grooveRadius;

            if (count < 2)
            {
                RingTransform(path, 0, out Vector3 onlyCenter, out _, out _);
                return transform.TransformPoint(onlyCenter + Vector3.up * floorOffset);
            }

            float clamped = Mathf.Clamp(pathPosition, 0f, count - 1);
            int index = Mathf.Min(Mathf.FloorToInt(clamped), count - 2);
            float t = clamped - index;

            RingTransform(path, index, out Vector3 centerA, out _, out _);
            RingTransform(path, index + 1, out Vector3 centerB, out _, out _);

            Vector3 local = Vector3.Lerp(centerA, centerB, t) + Vector3.up * floorOffset;
            return transform.TransformPoint(local);
        }

        /// World-space point at shoulder height (y offset 0) at one path
        /// index - used to place a physics marble right above the Start
        /// hole so it visibly drops in.
        public Vector3 GetShoulderWorldPosition(IReadOnlyList<Vector2Int> path, int index)
        {
            RingTransform(path, index, out Vector3 center, out _, out _);
            return transform.TransformPoint(center);
        }

        private List<IReadOnlyList<Vector2Int>> FindCompletedPaths()
        {
            List<IReadOnlyList<Vector2Int>> paths = new List<IReadOnlyList<Vector2Int>>();
            foreach (PathValidationResult result in grid.LastValidations)
            {
                if (result.GoalReached && result.OrderedPath.Count >= 2) paths.Add(result.OrderedPath);
            }
            return paths;
        }

        private Mesh BuildMesh(List<IReadOnlyList<Vector2Int>> paths)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            foreach (IReadOnlyList<Vector2Int> path in paths)
            {
                AppendRibbon(path, vertices, triangles);

                RingTransform(path, 0, out Vector3 startCenter, out Vector3 startRight, out Vector3 startForward);
                AppendEndCap(startCenter, startRight, -startForward, vertices, triangles);

                int lastIndex = path.Count - 1;
                RingTransform(path, lastIndex, out Vector3 goalCenter, out Vector3 goalRight, out Vector3 goalForward);
                AppendEndCap(goalCenter, goalRight, goalForward, vertices, triangles);
            }

            Mesh mesh = new Mesh { name = "TrackTerrain" };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// One track's terrain strip: a cross-section profile (flat shoulder
        /// - U groove - flat shoulder) swept along the path, always
        /// descending (height strictly decreases with path index).
        private void AppendRibbon(IReadOnlyList<Vector2Int> path, List<Vector3> vertices, List<int> triangles)
        {
            int rings = path.Count;
            int baseIndex = vertices.Count;

            for (int i = 0; i < rings; i++)
            {
                RingTransform(path, i, out Vector3 center, out Vector3 right, out _);

                for (int j = 0; j < profile.Length; j++)
                {
                    vertices.Add(center + right * profile[j].x + Vector3.up * profile[j].y);
                }
            }

            for (int i = 0; i < rings - 1; i++)
            {
                int ringStart = baseIndex + i * profile.Length;
                int nextRingStart = baseIndex + (i + 1) * profile.Length;

                for (int j = 0; j < profile.Length - 1; j++)
                {
                    int a = ringStart + j;
                    int b = ringStart + j + 1;
                    int c = nextRingStart + j;
                    int d = nextRingStart + j + 1;

                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }
        }

        private void RingTransform(IReadOnlyList<Vector2Int> path, int index, out Vector3 center, out Vector3 right, out Vector3 forward)
        {
            Vector3 cellPos = grid.CellToLocalPosition(path[index]);
            center = new Vector3(cellPos.x, -heightDropPerCell * index, cellPos.y);

            forward = ComputeForward(path, index);
            right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right == Vector3.zero) right = Vector3.right;
        }

        /// Closes off the end of a track where it has no neighbor (Start's
        /// approach side, Goal's departure side): a flat plate at shoulder
        /// height around a round hole (radius = grooveRadius, same as the
        /// groove) - the hole's near half is simply the ribbon's own end
        /// ring (already open there), this only builds the far half plus
        /// the flat plate out to the shoulder width, so that side reads as
        /// solid/closed instead of an open channel.
        /// "outward" points away from the track, into the closed side.
        private void AppendEndCap(Vector3 center, Vector3 right, Vector3 outward, List<Vector3> vertices, List<int> triangles)
        {
            int segments = grooveArcSegments;
            float platformHalfSize = grooveRadius + sideWidth;
            int baseIndex = vertices.Count;

            for (int k = 0; k <= segments; k++)
            {
                float t = (float)k / segments;
                float angle = Mathf.PI + t * Mathf.PI;
                float dRight = Mathf.Cos(angle);
                float dOutward = -Mathf.Sin(angle);

                Vector3 inner = center + right * (dRight * grooveRadius) + outward * (dOutward * grooveRadius);

                float scale = platformHalfSize / Mathf.Max(Mathf.Abs(dRight), Mathf.Abs(dOutward));
                Vector3 outer = center + right * (dRight * scale) + outward * (dOutward * scale);

                vertices.Add(inner);
                vertices.Add(outer);
            }

            for (int k = 0; k < segments; k++)
            {
                int a = baseIndex + k * 2;
                int b = baseIndex + k * 2 + 1;
                int c = baseIndex + (k + 1) * 2;
                int d = baseIndex + (k + 1) * 2 + 1;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        /// Cross-section, left to right: flat outer shoulder, a semicircular
        /// U dipping down by grooveRadius and back up (the rail the marble
        /// sits in), flat outer shoulder. Side view: ---u---
        private Vector2[] BuildProfile()
        {
            int arcPoints = grooveArcSegments + 1;
            Vector2[] points = new Vector2[arcPoints + 2];

            points[0] = new Vector2(-(grooveRadius + sideWidth), 0f);

            for (int k = 0; k <= grooveArcSegments; k++)
            {
                float t = (float)k / grooveArcSegments;
                float angle = Mathf.PI + t * Mathf.PI; // sweeps the bottom half-circle, left rim to right rim
                points[1 + k] = new Vector2(Mathf.Cos(angle) * grooveRadius, Mathf.Sin(angle) * grooveRadius);
            }

            points[arcPoints + 1] = new Vector2(grooveRadius + sideWidth, 0f);
            return points;
        }

        private static Vector3 ComputeForward(IReadOnlyList<Vector2Int> path, int index)
        {
            Vector2Int prev = index > 0 ? path[index - 1] : path[index];
            Vector2Int next = index < path.Count - 1 ? path[index + 1] : path[index];
            Vector2Int delta = next - prev;
            Vector3 forward = new Vector3(delta.x, 0f, delta.y);
            return forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;
        }

        private static bool PathListsEqual(List<IReadOnlyList<Vector2Int>> a, List<IReadOnlyList<Vector2Int>> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!PathEquals(a[i], b[i])) return false;
            }
            return true;
        }

        private static bool PathEquals(IReadOnlyList<Vector2Int> a, IReadOnlyList<Vector2Int> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            return material;
        }
    }
}
