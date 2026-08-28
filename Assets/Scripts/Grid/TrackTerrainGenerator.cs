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
    /// Purely geometric/visual - marble movement itself still runs on the
    /// 2D grid (see MarbleController); this does not drive physics.
    /// Lives on its own GameObject (with its own MeshFilter/MeshRenderer);
    /// grid and marbleController are wired in the Inspector or auto-found
    /// at Awake.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TrackTerrainGenerator : MonoBehaviour
    {
        [SerializeField] private PathGrid grid;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private float sideWidth = 0.4f; // flat shoulder extruded to each side of the groove
        [SerializeField] private float grooveRadius = 0f; // <= 0: derive from marble radius
        [SerializeField] private int grooveArcSegments = 8; // resolution of the semicircular U profile
        [SerializeField] private float heightDropPerCell = 0.25f;
        [SerializeField] private Color terrainColor = new Color(0.45f, 0.32f, 0.22f);

        private MeshFilter meshFilter;
        private Vector2[] profile;
        private List<IReadOnlyList<Vector2Int>> lastPaths = new List<IReadOnlyList<Vector2Int>>();

        private void Awake()
        {
            if (grid == null) grid = FindFirstObjectByType<PathGrid>();
            if (marbleController == null) marbleController = FindFirstObjectByType<MarbleController>();
            meshFilter = GetComponent<MeshFilter>();

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
            meshFilter.mesh = paths.Count > 0 ? BuildMesh(paths) : null;
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
                Vector3 cellPos = grid.CellToLocalPosition(path[i]);
                Vector3 center = new Vector3(cellPos.x, -heightDropPerCell * i, cellPos.y);

                Vector3 forward = ComputeForward(path, i);
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                if (right == Vector3.zero) right = Vector3.right;

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
