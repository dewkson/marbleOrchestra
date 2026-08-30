using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Spawns one TrackBlock instance per cell of every currently completed
    /// Start-to-Goal track, instead of the old single continuous ribbon
    /// mesh (see 0013/0017) - each block is a separate, self-contained
    /// GameObject (Instantiate from trackBlockPrefab, a new, deliberate
    /// departure from this codebase's otherwise 100% procedural convention
    /// - see PipeDefinition.cs's comment on staying prefab-free for DATA;
    /// this is about the visual/mesh shell instead). Diffs per track (by
    /// its Start cell) against the previous frame's paths so an unrelated
    /// pipe swap on one track doesn't tear down and rebuild every other
    /// track's blocks.
    /// Each block's Yaw faces the direction of its own outgoing path
    /// segment (incoming for the very last/Goal block, which has no
    /// outgoing one) - always an exact grid step, so always a multiple of
    /// 90°, never a diagonal bisector at turns (see 0019).
    /// Blocks all share the same floor plane (local Y 0): each one is
    /// simply THINNER than the previous by a constant per-cell step
    /// (TrackBlock.Height decreases, position.y = that same Height so the
    /// bottom stays put) - a staircase of decreasing blocks, tallest/first
    /// at Start (see 0020). Each block ALSO has its own small downhill Tilt
    /// (see 0019) - but that tilt only ever closes a configurable FRACTION
    /// (tiltFraction) of the per-cell height step, never all of it, so a
    /// deliberate residual height mismatch - a short fall - always remains
    /// at the boundary to the next, lower block (e.g. for a future "lands
    /// on a xylophone key" sound trigger) - see 0021. tiltFraction 0 = flat
    /// blocks, pure fall; 1 = tilt fully closes the step, seamless, no
    /// fall; the current default sits in between.
    /// SampleGroovePosition/GetShoulderWorldPosition sample each block's
    /// own real EntryPointLocal/ExitPointLocal for height (so the fall is
    /// reflected exactly, see 0021), but always use plain grid cell centers
    /// for X/Z - not the block's own rotated Entry/ExitPoint - since a turn
    /// block's entry face (per the "outgoing direction only" yaw above)
    /// generally does NOT line up with where the path actually enters it;
    /// grid cell centers avoid that misalignment entirely.
    /// Lives on its own GameObject; grid, marbleController and
    /// trackBlockPrefab are wired in the Inspector or auto-found at Awake.
    /// </summary>
    public class TrackBlockSpawner : MonoBehaviour
    {
        [SerializeField] private PathGrid grid;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private TrackBlock trackBlockPrefab;
        [SerializeField] private float grooveRadius = 0f; // <= 0: derive from the 3D marble radius; always clamped to at most half the cell size (see Awake)
        [SerializeField] private int grooveArcSegments = 8; // resolution of the semicircular U profile
        [SerializeField] private float startHeight = 1f; // TrackBlock.Height of the first (tallest) block; each next block is thinner by heightDropPerCell
        [SerializeField] private float heightDropPerCell = 0.25f;
        [SerializeField] private float minBlockHeight = 0.05f; // safety floor so a long track's last blocks never become degenerate/near-zero thickness
        [SerializeField, Range(0f, 1f)] private float tiltFraction = 0.5f; // how much of the per-cell height step the block's own Tilt closes; the rest is the fall at the boundary. 0 = flat blocks (pure fall), 1 = seamless (no fall)
        [SerializeField] private Color terrainColor = new Color(0.45f, 0.32f, 0.22f);

        public float GrooveRadius => grooveRadius;

        private class TrackInstance
        {
            public IReadOnlyList<Vector2Int> Path;
            public Transform Root;
            public List<TrackBlock> Blocks;
        }

        private readonly List<TrackInstance> tracks = new List<TrackInstance>();
        private Material sharedMaterial;

        private void Awake()
        {
            if (grid == null) grid = FindAnyObjectByType<PathGrid>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();

            float marbleRadius = marbleController != null ? marbleController.MarbleRadius3D : 0.1f;
            if (grooveRadius <= 0f) grooveRadius = marbleRadius * 1.15f;
            grooveRadius = Mathf.Min(grooveRadius, grid.CellSize * 0.5f); // leaves room for a non-negative shoulder, see SideWidth

            sharedMaterial = CreateMaterial(terrainColor);
        }

        private void Update()
        {
            SyncTracks(FindCompletedPaths());
        }

        /// World-space point at the groove floor, raised by marbleRadius so
        /// a marble of that size rests on it, at a fractional position
        /// along the path - e.g. 2.3 means 30% of the way from path[2]'s
        /// own entry to its own exit. X/Z come from the grid cell centers
        /// (always correct, even at a turn where a block's own rotated
        /// Entry/ExitPoint would be offset to the wrong side - see 0019's
        /// note on why turn blocks face only their outgoing direction);
        /// only the height (Y) comes from the actual spawned block's own
        /// EntryPointLocal/ExitPointLocal, so it reflects that block's real
        /// tilt. The deliberate fall at each block boundary (see 0020) is
        /// exactly the jump between one index's exit height and the next
        /// index's entry height - not smoothed away. Used for kinematic 3D
        /// movement.
        public Vector3 SampleGroovePosition(IReadOnlyList<Vector2Int> path, float pathPosition, float marbleRadius)
        {
            return SampleTrackPosition(path, pathPosition, marbleRadius);
        }

        /// Same chained sampling as SampleGroovePosition, but at the floor
        /// itself (no marble-radius offset) - used to place a physics
        /// marble right above Start (MarbleController adds its own
        /// marbleRadius3D + physicsDropHeight clearance on top), and as the
        /// Goal reference point for arrival detection.
        public Vector3 GetShoulderWorldPosition(IReadOnlyList<Vector2Int> path, float pathPosition)
        {
            return SampleTrackPosition(path, pathPosition, 0f);
        }

        private Vector3 SampleTrackPosition(IReadOnlyList<Vector2Int> path, float pathPosition, float verticalOffset)
        {
            TrackInstance track = FindTrackByStart(path[0]);

            float clamped = Mathf.Clamp(pathPosition, 0f, path.Count - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(clamped), 0, path.Count - 1);
            int nextIndex = Mathf.Clamp(index + 1, 0, path.Count - 1);
            float f = clamped - index;

            Vector3 a = grid.CellToLocalPosition(path[index]);
            Vector3 b = grid.CellToLocalPosition(path[nextIndex]);
            Vector3 xz = Vector3.Lerp(a, b, f);

            float floorY = SampleFloorY(track, index, f);
            return transform.TransformPoint(new Vector3(xz.x, floorY + verticalOffset, xz.y));
        }

        /// Groove-floor height at fractional position f (0-1) between the
        /// block at index's own entry and exit - i.e. within that ONE
        /// block's real, possibly tilted surface. Falls back to the
        /// pre-tilt formula if that track's blocks aren't spawned yet this
        /// frame (e.g. the very first frame after a path just completed).
        private float SampleFloorY(TrackInstance track, int index, float f)
        {
            if (track != null && index < track.Blocks.Count)
            {
                TrackBlock block = track.Blocks[index];
                float entryY = block.transform.localPosition.y + block.EntryPointLocal.y;
                float exitY = block.transform.localPosition.y + block.ExitPointLocal.y;
                return Mathf.Lerp(entryY, exitY, f);
            }

            return BlockHeightAt(index) - grooveRadius;
        }

        /// TrackBlock.Height (= top-surface height above the shared floor
        /// at local Y 0) for the block at this path index: startHeight at
        /// Start, shrinking by heightDropPerCell per cell, never below
        /// minBlockHeight.
        private float BlockHeightAt(int index) => Mathf.Max(minBlockHeight, startHeight - heightDropPerCell * index);

        /// The block's own Tilt, in degrees: the angle that would close
        /// heightDropPerCell entirely over one block's length (travelLength),
        /// scaled down by tiltFraction so only part of the step is ramped
        /// away - the rest is the intentional fall at the boundary.
        private float ComputeTiltDegrees(float travelLength)
        {
            float seamlessTiltDegrees = Mathf.Atan2(heightDropPerCell, travelLength) * Mathf.Rad2Deg;
            return seamlessTiltDegrees * tiltFraction;
        }

        /// Flat shoulder width to each side of the groove, derived so the
        /// block's total width (2*grooveRadius + 2*SideWidth) exactly
        /// equals the cell size - i.e. a square footprint (see BuildTrack).
        /// Non-negative because grooveRadius is clamped to at most half the
        /// cell size in Awake.
        private float SideWidth => grid.CellSize * 0.5f - grooveRadius;

        /// Yaw faces this block's own outgoing path segment (incoming for
        /// the last/Goal block, which has none) - always an exact grid
        /// step, so always a multiple of 90°. Deliberately NOT a bisector
        /// between incoming and outgoing direction at turns (see 0019):
        /// a turn cell simply faces its exit direction instead of pointing
        /// diagonally through the corner.
        private static float ComputeYawDegrees(IReadOnlyList<Vector2Int> path, int index)
        {
            Vector2Int from = index < path.Count - 1 ? path[index] : path[index - 1];
            Vector2Int to = index < path.Count - 1 ? path[index + 1] : path[index];
            Vector3 forward = new Vector3(to.x - from.x, 0f, to.y - from.y);
            return Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;
        }

        /// Cardinal Direction for the same from/to delta ComputeYawDegrees
        /// uses (deliberately duplicated rather than refactoring that
        /// already-tested method) - feeds BlockDefinition.PathDirection.
        private static Direction ComputePathDirection(IReadOnlyList<Vector2Int> path, int index)
        {
            Vector2Int from = index < path.Count - 1 ? path[index] : path[index - 1];
            Vector2Int to = index < path.Count - 1 ? path[index + 1] : path[index];
            Vector2Int delta = to - from;

            foreach (Direction dir in DirectionExtensions.All)
                if (dir.ToGridOffset() == delta) return dir;
            return Direction.None;
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

        /// Keeps each track's blocks alive across frames where its path
        /// hasn't changed. Only tracks whose path actually changed (or
        /// disappeared, or newly completed) are torn down/rebuilt - an
        /// unrelated pipe swap elsewhere in the grid leaves other tracks'
        /// blocks (and any marble currently running on them) untouched.
        private void SyncTracks(List<IReadOnlyList<Vector2Int>> paths)
        {
            for (int i = tracks.Count - 1; i >= 0; i--)
            {
                TrackInstance track = tracks[i];
                IReadOnlyList<Vector2Int> match = FindPathByStart(paths, track.Path[0]);
                if (match != null && PathEquals(match, track.Path)) continue;

                Destroy(track.Root.gameObject);
                tracks.RemoveAt(i);
            }

            foreach (IReadOnlyList<Vector2Int> path in paths)
            {
                if (FindTrackByStart(path[0]) != null) continue;
                tracks.Add(BuildTrack(path));
            }
        }

        private TrackInstance BuildTrack(IReadOnlyList<Vector2Int> path)
        {
            Vector2Int start = path[0];
            GameObject root = new GameObject($"Track_{start.x}_{start.y}");
            root.transform.SetParent(transform, false);

            List<TrackBlock> blocks = new List<TrackBlock>(path.Count);
            Vector2 blockSize = new Vector2(grid.CellSize, grid.CellSize); // square footprint - see SideWidth
            float tiltDegrees = ComputeTiltDegrees(blockSize.y);

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int cell = path[i];
                TrackBlock block = Instantiate(trackBlockPrefab, root.transform);
                block.name = $"Block_{i:00}_{cell.x}_{cell.y}";

                float blockHeight = BlockHeightAt(i);
                Vector3 cellPos = grid.CellToLocalPosition(cell);
                block.transform.localPosition = new Vector3(cellPos.x, blockHeight, cellPos.y);

                block.Profile = new GrooveBlockProfile(grooveRadius, SideWidth, grooveArcSegments);
                block.Size = blockSize;
                block.Height = blockHeight; // bottom stays at the shared floor (local Y 0); only this block's own thickness shrinks
                block.Material = sharedMaterial;
                block.YawDegrees = ComputeYawDegrees(path, i);
                block.TiltDegrees = tiltDegrees; // ramps part of the step away; the rest is the fall to the next block (see class remarks)

                PipeRole role = grid.GetPipe(cell)?.Role ?? PipeRole.Normal;
                CellContentDefinition content = grid.GetContent(cell);
                SoundTriggerContent soundContent = content as SoundTriggerContent;
                TriggerBehavior trigger = content != null ? TriggerBehavior.OnEnter : TriggerBehavior.None;
                Color flashColor = soundContent != null ? soundContent.FlashColor : Color.white;
                block.SetDefinition(new BlockDefinition(cell, ComputePathDirection(path, i), blockHeight, role,
                    trigger, soundContent?.Clip, BlockDefinition.DefaultBiome, flashColor));

                blocks.Add(block);
            }

            return new TrackInstance { Path = path, Root = root.transform, Blocks = blocks };
        }

        /// The TrackBlock spawned at this grid cell, if any track
        /// currently covers it (linear scan - tracks change rarely, this
        /// is only called from MarbleController's per-cell trigger, not
        /// per-frame).
        public TrackBlock GetBlockAt(Vector2Int coord)
        {
            foreach (TrackInstance track in tracks)
            {
                for (int i = 0; i < track.Path.Count; i++)
                {
                    if (track.Path[i] == coord) return i < track.Blocks.Count ? track.Blocks[i] : null;
                }
            }
            return null;
        }

        /// World-space bounds encapsulating every currently spawned block
        /// across all tracks, from their MeshRenderer.bounds (already
        /// reflecting each block's real position, yaw and staircase
        /// height). Used by CameraModeTransition (see 0029) to frame the
        /// 3D view so the whole track fits on screen. False (bounds left
        /// at default) if no track is currently spawned.
        public bool TryGetTracksWorldBounds(out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (TrackInstance track in tracks)
            {
                foreach (TrackBlock block in track.Blocks)
                {
                    if (block == null) continue;
                    Renderer renderer = block.GetComponent<Renderer>();
                    if (renderer == null) continue;

                    if (!any) { bounds = renderer.bounds; any = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
            }

            return any;
        }

        private TrackInstance FindTrackByStart(Vector2Int start)
        {
            foreach (TrackInstance track in tracks)
            {
                if (track.Path[0] == start) return track;
            }
            return null;
        }

        private static IReadOnlyList<Vector2Int> FindPathByStart(List<IReadOnlyList<Vector2Int>> paths, Vector2Int start)
        {
            foreach (IReadOnlyList<Vector2Int> path in paths)
            {
                if (path[0] == start) return path;
            }
            return null;
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
