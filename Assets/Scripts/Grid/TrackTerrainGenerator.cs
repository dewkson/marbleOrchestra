using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Builds a 3D terrain ribbon for every currently completed
    /// Start-to-Goal track: a strip that always slopes downward in the
    /// direction of travel, with a semicircular U-shaped rail groove in the
    /// middle (radius derived from the 3D marble so it fits snugly and can
    /// roll along it), a flat, definable-width shoulder extruded out to
    /// each side (side view: ---u---), and corners rounded (Chaikin corner
    /// cutting on the centerline) instead of sharp bends. Regenerates
    /// whenever the set of completed paths changes (pipe swap, level
    /// rebuild).
    /// Start ends in a solid closed cap (no hole - a ball dropped there
    /// needs an actual floor and the slope to already be present, see
    /// 0014); Goal ends in a round hole with the shoulders closed behind
    /// it, so the marble visibly drops away at the end (see 0013).
    /// Marble movement is driven externally (MarbleController), either by
    /// sampling SampleGroovePosition (kinematic) or via Unity physics
    /// colliding against this object's MeshCollider (physics-based).
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
        [SerializeField] private float grooveRadius = 0f; // <= 0: derive from the 3D marble radius
        [SerializeField] private int grooveArcSegments = 8; // resolution of the semicircular U profile
        [SerializeField] private float heightDropPerCell = 0.25f;
        [SerializeField] private int cornerSmoothingIterations = 2; // Chaikin corner-cutting passes; 0 = sharp corners
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
                float marbleRadius = marbleController != null ? marbleController.MarbleRadius3D : 0.1f;
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
        /// e.g. 2.3 means 30% of the way from path[2] to path[3]. Follows
        /// the same corner-rounded centerline as the rendered mesh. Used
        /// for kinematic 3D movement.
        public Vector3 SampleGroovePosition(IReadOnlyList<Vector2Int> path, float pathPosition, float marbleRadius)
        {
            BuildSmoothedCenterline(path, out List<Vector3> positions, out List<float> tValues);
            Vector3 local = InterpolateByT(positions, tValues, pathPosition) + Vector3.up * (marbleRadius - grooveRadius);
            return transform.TransformPoint(local);
        }

        /// World-space point at shoulder height (no groove offset) at a
        /// fractional position along the path - used to place a physics
        /// marble right above the Start so it visibly drops in, and as the
        /// Goal reference point for arrival detection.
        public Vector3 GetShoulderWorldPosition(IReadOnlyList<Vector2Int> path, float pathPosition)
        {
            BuildSmoothedCenterline(path, out List<Vector3> positions, out List<float> tValues);
            return transform.TransformPoint(InterpolateByT(positions, tValues, pathPosition));
        }

        private static Vector3 InterpolateByT(List<Vector3> positions, List<float> tValues, float targetT)
        {
            float clamped = Mathf.Clamp(targetT, tValues[0], tValues[tValues.Count - 1]);

            for (int i = 0; i < tValues.Count - 1; i++)
            {
                if (clamped > tValues[i + 1] && i < tValues.Count - 2) continue;

                float span = tValues[i + 1] - tValues[i];
                float localT = span > 0.0001f ? (clamped - tValues[i]) / span : 0f;
                return Vector3.Lerp(positions[i], positions[i + 1], localT);
            }

            return positions[positions.Count - 1];
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
                BuildSmoothedCenterline(path, out List<Vector3> centers, out _);

                AppendRibbon(centers, vertices, triangles);

                RingBasis(centers, 0, out Vector3 startCenter, out Vector3 startRight, out Vector3 startForward);
                AppendSolidCap(startCenter, startRight, -startForward, vertices, triangles);

                int lastIndex = centers.Count - 1;
                RingBasis(centers, lastIndex, out Vector3 goalCenter, out Vector3 goalRight, out Vector3 goalForward);
                AppendHoleCap(goalCenter, goalRight, goalForward, vertices, triangles);
            }

            Mesh mesh = new Mesh { name = "TrackTerrain" };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// Builds the path's centerline (local position + original
        /// fractional path index per point) with corners rounded via
        /// repeated Chaikin corner-cutting: each interior point is replaced
        /// by two points 25%/75% along its neighboring segments, which
        /// leaves straight runs untouched but smooths actual turns into a
        /// curve. Height is carried in the position's Y and smoothed by the
        /// same linear operation, so it stays monotonically decreasing
        /// (never flattens or reverses the slope).
        private void BuildSmoothedCenterline(IReadOnlyList<Vector2Int> path, out List<Vector3> positions, out List<float> tValues)
        {
            positions = new List<Vector3>(path.Count);
            tValues = new List<float>(path.Count);

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 cellPos = grid.CellToLocalPosition(path[i]);
                positions.Add(new Vector3(cellPos.x, -heightDropPerCell * i, cellPos.y));
                tValues.Add(i);
            }

            for (int iter = 0; iter < cornerSmoothingIterations; iter++)
            {
                ChaikinSmooth(positions, tValues);
            }
        }

        private static void ChaikinSmooth(List<Vector3> positions, List<float> tValues)
        {
            if (positions.Count < 3) return;

            List<Vector3> newPositions = new List<Vector3>(positions.Count * 2);
            List<float> newT = new List<float>(tValues.Count * 2);

            newPositions.Add(positions[0]);
            newT.Add(tValues[0]);

            for (int i = 0; i < positions.Count - 1; i++)
            {
                newPositions.Add(Vector3.Lerp(positions[i], positions[i + 1], 0.25f));
                newT.Add(Mathf.Lerp(tValues[i], tValues[i + 1], 0.25f));

                newPositions.Add(Vector3.Lerp(positions[i], positions[i + 1], 0.75f));
                newT.Add(Mathf.Lerp(tValues[i], tValues[i + 1], 0.75f));
            }

            newPositions.Add(positions[positions.Count - 1]);
            newT.Add(tValues[tValues.Count - 1]);

            positions.Clear();
            positions.AddRange(newPositions);
            tValues.Clear();
            tValues.AddRange(newT);
        }

        /// One track's terrain strip: a cross-section profile (flat shoulder
        /// - U groove - flat shoulder) swept along the (corner-rounded)
        /// centerline, always descending.
        private void AppendRibbon(List<Vector3> centers, List<Vector3> vertices, List<int> triangles)
        {
            int rings = centers.Count;
            int baseIndex = vertices.Count;

            for (int i = 0; i < rings; i++)
            {
                RingBasis(centers, i, out Vector3 center, out Vector3 right, out _);

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

        private static void RingBasis(List<Vector3> centers, int index, out Vector3 center, out Vector3 right, out Vector3 forward)
        {
            center = centers[index];

            Vector3 prev = index > 0 ? centers[index - 1] : centers[index];
            Vector3 next = index < centers.Count - 1 ? centers[index + 1] : centers[index];
            Vector3 delta = next - prev;
            delta.y = 0f; // the ring's right/forward axes stay horizontal regardless of slope

            forward = delta.sqrMagnitude > 0.0000001f ? delta.normalized : Vector3.forward;
            right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right == Vector3.zero) right = Vector3.right;
        }

        /// Closes the Start end with an actual floor: the flat shoulder
        /// wings AND the U-notch itself are capped solid at the ring's own
        /// height, so a dropped ball always lands on real geometry (never
        /// falls through) - unlike a hole, which has nothing behind it.
        /// "outward" points away from the track, into the closed side.
        private void AppendSolidCap(Vector3 center, Vector3 right, Vector3 outward, List<Vector3> vertices, List<int> triangles)
        {
            int segments = grooveArcSegments;
            float platformHalfSize = grooveRadius + sideWidth;
            int baseIndex = vertices.Count;

            vertices.Add(center);

            for (int k = 0; k <= segments; k++)
            {
                float t = (float)k / segments;
                float angle = Mathf.PI + t * Mathf.PI;
                float dRight = Mathf.Cos(angle);
                float dOutward = -Mathf.Sin(angle);

                float scale = platformHalfSize / Mathf.Max(Mathf.Abs(dRight), Mathf.Abs(dOutward));
                vertices.Add(center + right * (dRight * scale) + outward * (dOutward * scale));
            }

            for (int k = 0; k < segments; k++)
            {
                int a = baseIndex;
                int b = baseIndex + 1 + k;
                int c = baseIndex + 1 + k + 1;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
            }
        }

        /// Closes the Goal end with a round hole (radius = grooveRadius,
        /// same as the groove) so the marble visibly drops away: the
        /// groove's own open end forms the near half of the hole, this
        /// builds the far half plus the flat plate out to the shoulder
        /// width, so that side reads as solid/closed around the hole.
        /// "outward" points away from the track, into the closed side.
        private void AppendHoleCap(Vector3 center, Vector3 right, Vector3 outward, List<Vector3> vertices, List<int> triangles)
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
