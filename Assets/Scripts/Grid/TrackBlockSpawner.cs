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
    /// Each straight-through block's Yaw faces the direction of its own
    /// outgoing path segment (incoming for the very last/Goal block, which
    /// has no outgoing one) - always an exact grid step, so always a
    /// multiple of 90° (see 0019). A Normal block whose path actually turns
    /// 90° instead gets Yaw 0 and a curved groove built directly in
    /// grid-axis-aligned local coordinates (see TrackBlock.SetCurve/0040) -
    /// Start/Goal/Trigger blocks never turn (see the isTurn check in
    /// BuildTrack).
    /// Height chaining (see 0039): each block gets a BlockType (Start/Goal/
    /// Normal/Trigger - see ComputeBlockType) derived from its position in
    /// the path and its CellContentDefinition. Start/Goal/Normal blocks
    /// ALWAYS connect at the exact same height as the previous block's own
    /// exit (no jump) - only a Trigger block (an ITriggerCellContent, e.g.
    /// SoundTriggerContent or XylophonePadContent) is allowed a deliberate
    /// FallHeight, read straight from that content, so the marble visibly
    /// falls from the previous block onto the trigger element before
    /// continuing in its OutputDirection. A straight Normal block's own
    /// Tilt is always a small constant downhill slope
    /// (normalInclinationDegrees); a Trigger block's own Tilt instead ramps
    /// away a FRACTION (fallTiltFraction) of ITS OWN FallHeight, the rest
    /// remaining as the deliberate fall at its entry boundary; a turning
    /// Normal block and Start/Goal stay flat (0).
    /// Every block shares the exact same floor plane (local Y 0, seen from
    /// below) - per an earlier ticket's explicit requirement - by solving
    /// each block's own Height (body thickness) from its required top
    /// height and using that SAME value as both TrackBlock.Height and the
    /// block's own transform.position.y (see BuildTrack); only the body
    /// thickness varies from block to block, never the shared floor.
    /// SampleGroovePosition/GetShoulderWorldPosition sample a CURVED block
    /// (see TrackBlock.IsCurved/0040) from its own real, unmodified
    /// SampleGroovePointLocal (X/Z/Y together), so kinematic movement
    /// actually hugs the turn instead of cutting across it. A STRAIGHT
    /// block instead handles X/Z and Y separately: X/Z lerps between two
    /// JunctionLocalXZ values - usually the plain grid cell centers on
    /// either side (equivalent to that block's own centerline), except
    /// right next to a curve, where the junction is that curve's own true
    /// entry/exit instead of the cell center half a grid step further away
    /// (see JunctionLocalXZ's own remarks). Y instead always comes from
    /// THIS block's own entry/exit only (SampleFloorY, never blended
    /// toward a neighbor) - deliberately, so a Trigger's own FallHeight
    /// (see 0039) still reads as a sudden drop right at its boundary
    /// instead of smearing across the whole previous block's travel.
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
        [SerializeField] private float startHeight = 1f; // world/spawner-local Y of the groove floor at Start's own entry point - the baseline the whole height chain hangs from (see 0039)
        [SerializeField] private float minBlockHeight = 0.05f; // safety floor for TrackBlock.Height, so a long chain of Trigger falls can never shrink a block to a degenerate/near-zero or negative thickness
        [SerializeField, Range(0f, 15f)] private float normalInclinationDegrees = 3f; // continuous downhill slope every straight Normal block's own surface has, in its travel direction - see 0039 follow-up. Curved Normal blocks stay flat (0) - a sloped curve is out of scope for now, see TrackBlock.SetCurve/0040
        [SerializeField, Range(0f, 1f)] private float fallTiltFraction = 0.5f; // how much of a Trigger block's OWN FallHeight its Tilt closes; the rest is the actual fall onto the trigger element. 0 = flat pad (pure fall), 1 = seamless (no visible fall) - see 0039
        [SerializeField] private Color terrainColor = new Color(0.30f, 0.45f, 0.20f); // grass/moss green - the "Default" biome's first look (see 0032)
        [SerializeField] private Color grooveColor = new Color(0.40f, 0.27f, 0.15f); // earthy brown for the rollable groove itself, distinct from the grass shoulders (see 0032)
        [SerializeField] private Color tunnelColor = new Color(0.05f, 0.05f, 0.06f); // near-black interior of the Start/Goal tunnel portal (see TunnelPortalDecoration)

        public float GrooveRadius => grooveRadius;

        private class TrackInstance
        {
            public IReadOnlyList<Vector2Int> Path;
            public Transform Root;
            public List<TrackBlock> Blocks;
        }

        private readonly List<TrackInstance> tracks = new List<TrackInstance>();
        private Material sharedMaterial;
        private Material sharedGrooveMaterial;
        private Material sharedTunnelMaterial;

        private void Awake()
        {
            if (grid == null) grid = FindAnyObjectByType<PathGrid>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();

            float marbleRadius = marbleController != null ? marbleController.MarbleRadius3D : 0.1f;
            if (grooveRadius <= 0f) grooveRadius = marbleRadius * 1.15f;
            grooveRadius = Mathf.Min(grooveRadius, grid.CellSize * 0.5f); // leaves room for a non-negative shoulder, see SideWidth

            sharedMaterial = CreateMaterial(terrainColor);
            sharedGrooveMaterial = CreateMaterial(grooveColor);
            sharedTunnelMaterial = CreateMaterial(tunnelColor);
        }

        private void Update()
        {
            SyncTracks(FindCompletedPaths());
        }

        /// World-space point at the groove floor, raised by marbleRadius so
        /// a marble of that size rests on it, at a fractional position
        /// along the path - e.g. 2.3 means 30% of the way from path[2]'s
        /// own entry to its own exit. For a curved block (see
        /// TrackBlock.IsCurved/0040) this follows the block's own real,
        /// UNMODIFIED curved centerline (X/Z included, not just height) so
        /// the marble visibly hugs the turn instead of cutting across it -
        /// straight neighbors on either side aim their own X/Z at that same
        /// curve's own true entry/exit instead of the plain cell center
        /// they'd otherwise use (see JunctionLocalXZ), so nothing has to
        /// snap OR distort to stay continuous. Used for kinematic 3D
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
            float f = clamped - index;

            TrackBlock block = track != null && index < track.Blocks.Count ? track.Blocks[index] : null;

            if (block != null && block.IsCurved)
            {
                Vector3 localPoint = block.SampleGroovePointLocal(f);
                return block.transform.TransformPoint(localPoint) + Vector3.up * verticalOffset;
            }

            // X/Z and Y are deliberately handled separately here: X/Z needs
            // to be curve-aware at a turn's boundary (see JunctionLocalXZ),
            // but Y must NOT blend across a block boundary that isn't
            // continuous on purpose - a Trigger's own FallHeight (see 0039)
            // is exactly such a deliberate jump, which SampleFloorY (using
            // only block's own entry/exit) preserves as a sudden drop right
            // at the boundary. Blending Y toward a neighbor too (an earlier
            // attempt) smeared that drop across the WHOLE previous block's
            // travel instead, looking like the marble just glides down in
            // a straight line rather than rolling normally and then
            // falling - see 0039 follow-up.
            int nextIndex = Mathf.Clamp(index + 1, 0, path.Count - 1);
            Vector3 a = JunctionLocalXZ(path, track, index);
            Vector3 b = JunctionLocalXZ(path, track, nextIndex);
            Vector3 xz = Vector3.Lerp(a, b, f);

            float floorY = SampleFloorY(block, f);
            return transform.TransformPoint(new Vector3(xz.x, floorY, xz.y)) + Vector3.up * verticalOffset;
        }

        /// Groove-floor height at fractional position f (0-1) between
        /// block's own entry and exit - i.e. within that ONE block's real,
        /// possibly tilted surface (or, for a Trigger, its own fall - see
        /// 0039). Falls back to startHeight if that block isn't spawned
        /// yet this frame (e.g. the very first frame after a path just
        /// completed) - self-corrects the very next frame once BuildTrack
        /// has actually run, so exactness here doesn't matter. Only used
        /// for a non-curved block - see SampleTrackPosition.
        private float SampleFloorY(TrackBlock block, float f)
        {
            if (block != null)
            {
                float entryY = block.transform.localPosition.y + block.EntryPointLocal.y;
                float exitY = block.transform.localPosition.y + block.ExitPointLocal.y;
                return Mathf.Lerp(entryY, exitY, f);
            }

            return startHeight;
        }

        /// Spawner-local X/Z the marble should be at when pathPosition
        /// equals exactly k - normally the grid cell's own center (as
        /// before), EXCEPT right at the boundary of a curved block (see
        /// TrackBlock.IsCurved/0040), where it's that block's own true
        /// entry/exit instead. Without this, a straight neighbor's plain
        /// cell-center target would sit half a cell away from where the
        /// curve actually starts/ends, so approaching/leaving it either
        /// snapped (an earlier attempt at fixing this left the curve's own
        /// endpoints alone) or - blending the curve's own endpoints toward
        /// the cell centers instead (a second attempt) - stretched its
        /// true chord out of shape instead (see 0039 follow-ups for both).
        /// Start (k=0) and Goal (the last index) always stay center-based,
        /// matching their own tunnel decoration; a curve directly adjacent
        /// to either would still see this mismatch, but curves only ever
        /// occur on interior Normal blocks today anyway.
        private Vector3 JunctionLocalXZ(IReadOnlyList<Vector2Int> path, TrackInstance track, int k)
        {
            if (k > 0 && k < path.Count - 1)
            {
                // EntryPointLocal/ExitPointLocal always use the straight
                // -Z/+Z formula, even on a curved block (see TrackBlock's
                // own remarks) - SampleGroovePointLocal(0)/(1) is the
                // curve-aware equivalent, which for a turn can sit along
                // local X instead of Z.
                TrackBlock atK = track != null && k < track.Blocks.Count ? track.Blocks[k] : null;
                if (atK != null && atK.IsCurved) return SpawnerLocalXZ(atK, atK.SampleGroovePointLocal(0f));

                TrackBlock beforeK = track != null && k - 1 < track.Blocks.Count ? track.Blocks[k - 1] : null;
                if (beforeK != null && beforeK.IsCurved) return SpawnerLocalXZ(beforeK, beforeK.SampleGroovePointLocal(1f));
            }

            return grid.CellToLocalPosition(path[k]);
        }

        /// Converts a point in some block's own local space into
        /// spawner-local space (x, z-as-y, matching grid.CellToLocalPosition's
        /// own convention) WITHOUT a full world-space round-trip - valid
        /// because every block is a direct child of a root GameObject that
        /// itself sits at zero offset from this spawner's own transform
        /// (see BuildTrack), so composing the block's own localPosition and
        /// Yaw is exactly equivalent to (and cheaper than) TransformPoint
        /// followed by InverseTransformPoint back through this spawner.
        private static Vector3 SpawnerLocalXZ(TrackBlock block, Vector3 blockLocalPoint)
        {
            Vector3 rotated = Quaternion.Euler(0f, block.YawDegrees, 0f) * blockLocalPoint;
            Vector3 spawnerLocal = block.transform.localPosition + rotated;
            return new Vector3(spawnerLocal.x, spawnerLocal.z, 0f);
        }

        /// Flat shoulder width to each side of the groove, derived so the
        /// block's total width (2*grooveRadius + 2*SideWidth) exactly
        /// equals the cell size - i.e. a square footprint (see BuildTrack).
        /// Non-negative because grooveRadius is clamped to at most half the
        /// cell size in Awake.
        private float SideWidth => grid.CellSize * 0.5f - grooveRadius;

        /// Yaw faces this block's own outgoing path segment (incoming for
        /// the last/Goal block, which has none) - always an exact grid
        /// step, so always a multiple of 90°. Only used for blocks that
        /// DON'T turn (see BuildTrack's isTurn check) - a turning Normal
        /// block gets Yaw 0 instead, its curve built directly from the real
        /// Input/Output directions (see TrackBlock.SetCurve/0040) rather
        /// than a diagonal bisector through the corner.
        private static float ComputeYawDegrees(IReadOnlyList<Vector2Int> path, int index)
        {
            Vector2Int from = index < path.Count - 1 ? path[index] : path[index - 1];
            Vector2Int to = index < path.Count - 1 ? path[index + 1] : path[index];
            Vector3 forward = new Vector3(to.x - from.x, 0f, to.y - from.y);
            return Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;
        }

        /// What this block IS (see BlockType/0039): Start/Goal by position
        /// in the path, Trigger for any cell whose content opts in via
        /// ITriggerCellContent (SoundTriggerContent, XylophonePadContent,
        /// and any future trigger content - no per-type special-casing
        /// needed here), Normal otherwise.
        private static BlockType ComputeBlockType(bool isStart, bool isGoal, ITriggerCellContent triggerContent)
        {
            if (isStart) return BlockType.Start;
            if (isGoal) return BlockType.Goal;
            if (triggerContent != null) return BlockType.Trigger;
            return BlockType.Normal;
        }

        /// Direction the marble arrives from - the travel direction of the
        /// incoming segment (path[index-1] -> path[index]). None at Start,
        /// which has no incoming segment.
        private static Direction ComputeInputDirection(IReadOnlyList<Vector2Int> path, int index)
        {
            if (index == 0) return Direction.None;
            return DirectionFromDelta(path[index] - path[index - 1]);
        }

        /// Direction the marble leaves in - the travel direction of the
        /// outgoing segment (path[index] -> path[index+1]). None at Goal,
        /// which has no outgoing segment.
        private static Direction ComputeOutputDirection(IReadOnlyList<Vector2Int> path, int index)
        {
            if (index == path.Count - 1) return Direction.None;
            return DirectionFromDelta(path[index + 1] - path[index]);
        }

        private static Direction DirectionFromDelta(Vector2Int delta)
        {
            foreach (Direction dir in DirectionExtensions.All)
                if (dir.ToGridOffset() == delta) return dir;
            return Direction.None;
        }

        /// This block's own Tilt, in degrees. A Trigger block ramps part of
        /// ITS OWN FallHeight away (scaled by fallTiltFraction) - the rest
        /// is the intentional fall at its entry boundary (see class
        /// remarks). A straight Normal block always gets a small constant
        /// downhill slope (normalInclinationDegrees) so the marble keeps a
        /// gentle, continuous pace even across a long flat-looking stretch -
        /// see 0039 follow-up. A turning Normal block stays flat (a sloped
        /// curve is out of scope for now, see TrackBlock.SetCurve/0040), and
        /// so do Start/Goal.
        private float ComputeSurfaceInclination(BlockType type, bool isTurn, float travelLength, float fallHeight)
        {
            if (type == BlockType.Trigger)
            {
                float seamlessTiltDegrees = Mathf.Atan2(fallHeight, travelLength) * Mathf.Rad2Deg;
                return seamlessTiltDegrees * fallTiltFraction;
            }
            if (type == BlockType.Normal && !isTurn) return normalInclinationDegrees;
            return 0f;
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

            // How far the Start/Goal/Trigger groove reaches past the block's
            // center into its closed half, so it visibly runs into a
            // TunnelPortalDecoration's/pad's mouth instead of dead-ending
            // right at the frame - clamped well inside the closed half so
            // it never collides with the block's own true edge.
            float railExtension = Mathf.Min(grooveRadius * 1.5f, blockSize.y * 0.5f * 0.6f);

            float runningExitY = startHeight; // world/spawner-local Y of the groove floor the next block's entry must match, unless it's a Trigger block

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int cell = path[i];
                TrackBlock block = Instantiate(trackBlockPrefab, root.transform);
                block.name = $"Block_{i:00}_{cell.x}_{cell.y}";

                bool isStart = i == 0;
                bool isGoal = i == path.Count - 1;
                CellContentDefinition content = grid.GetContent(cell);
                ITriggerCellContent triggerContent = content as ITriggerCellContent;

                BlockType type = ComputeBlockType(isStart, isGoal, triggerContent);
                Direction inputDir = ComputeInputDirection(path, i);
                Direction outputDir = ComputeOutputDirection(path, i);

                // A curved groove (see 0040) only ever makes sense for a
                // Normal block whose path actually turns 90° - Start/Goal/
                // Trigger blocks' entry half is always capped
                // (IClosedEndBlockProfile), never a through-rolling groove
                // that could turn (see TrackBlock.SetCurve's remarks).
                // Straight-through means InputDirection == OutputDirection
                // (the marble keeps travelling the same way, e.g. Up->Up) -
                // NOT Opposite() (that would be a 180° reversal, which
                // doesn't occur on a valid grid path in the first place).
                bool isTurn = type == BlockType.Normal && inputDir != Direction.None && outputDir != Direction.None
                    && inputDir != outputDir;

                IBlockProfile profile = type == BlockType.Start ? new ClosedEndGrooveBlockProfile(grooveRadius, SideWidth, grooveArcSegments, closedAtEntry: true, railExtension)
                    : type == BlockType.Goal ? new ClosedEndGrooveBlockProfile(grooveRadius, SideWidth, grooveArcSegments, closedAtEntry: false, railExtension)
                    : type == BlockType.Trigger ? new ClosedEndGrooveBlockProfile(grooveRadius, SideWidth, grooveArcSegments, closedAtEntry: true, railExtension)
                    : new GrooveBlockProfile(grooveRadius, SideWidth, grooveArcSegments);

                float fallHeight = type == BlockType.Trigger ? triggerContent.FallHeight : 0f;
                float entryY = runningExitY - fallHeight; // Start/Goal/Normal: fallHeight is always 0 -> perfectly height-matched to the previous block's exit
                float inclinationDegrees = ComputeSurfaceInclination(type, isTurn, blockSize.y, fallHeight);
                float drop = blockSize.y * Mathf.Tan(inclinationDegrees * Mathf.Deg2Rad);
                float halfDrop = drop * 0.5f;

                // Solve this block's own Height (body thickness) so that its
                // EntryPointLocal (== profile.EntryPoint + halfDrop, see
                // TrackBlock) lands exactly at entryY WHILE its bottom stays
                // at the shared floor (local Y 0, same for every block, per
                // an earlier ticket's explicit requirement) - i.e. position
                // and Height are set to the SAME value on purpose: since
                // TrackBlock.BottomY is -height in the block's own local
                // space, a transform.localPosition.y of exactly `height`
                // always puts the world-space bottom at 0, regardless of
                // how tall/short this particular block ends up being.
                float localEntryY = profile.EntryPoint(blockSize).y + halfDrop;
                float height = Mathf.Max(entryY - localEntryY, minBlockHeight);

                Vector3 cellPos = grid.CellToLocalPosition(cell);
                block.transform.localPosition = new Vector3(cellPos.x, height, cellPos.y);

                block.Profile = profile;
                block.Size = blockSize;
                block.Height = height;
                block.Material = sharedMaterial;
                block.GrooveMaterial = sharedGrooveMaterial;

                if (isTurn)
                {
                    block.YawDegrees = 0f; // curved geometry is built directly in grid-axis-aligned local coordinates
                    block.SetCurve(inputDir.ToLocalVector3(), outputDir.ToLocalVector3());
                }
                else
                {
                    block.ClearCurve();
                    block.YawDegrees = ComputeYawDegrees(path, i);
                }

                block.TiltDegrees = inclinationDegrees;

                SoundTriggerContent soundContent = content as SoundTriggerContent;
                TriggerBehavior trigger = soundContent != null ? TriggerBehavior.OnEnter : TriggerBehavior.None; // XylophonePadContent is visual-only, no trigger
                Color flashColor = soundContent != null ? soundContent.FlashColor : Color.white;
                block.SetDefinition(new BlockDefinition(cell, inputDir, outputDir, entryY, type, fallHeight,
                    inclinationDegrees, trigger, soundContent?.Clip, BlockDefinition.DefaultBiome, flashColor));

                if (isStart || isGoal)
                {
                    TunnelPortalDecoration.Build(block, closedAtEntry: isStart, grooveRadius, SideWidth, blockSize, railExtension, sharedMaterial, sharedTunnelMaterial);
                }
                else if (type == BlockType.Trigger)
                {
                    // The marble travels IN InputDirection to arrive here,
                    // so it arrives FROM the opposite side - undo this
                    // block's own Yaw to get that side in local space,
                    // regardless of which way the block itself faces.
                    Vector3 fallSideLocal = Quaternion.Euler(0f, -block.YawDegrees, 0f) * (-inputDir.ToLocalVector3());
                    XylophoneBlockDecoration.Build(block, fallSideLocal, grooveRadius, SideWidth, sharedMaterial);
                }
                else
                {
                    TerrainDecoration.Scatter(block, cell, block.Definition.Biome, grooveRadius, SideWidth, blockSize);
                }

                blocks.Add(block);
                runningExitY = entryY - drop; // baseline the next block's entry must match, unless it's a Trigger block
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
