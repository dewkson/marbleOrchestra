using System.Collections;
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
    /// (normalInclinationDegrees); a turning Normal block, a Trigger block
    /// and Start/Goal stay flat (0) - a Trigger's FallHeight is a real
    /// fall in the marble's own trace (see below), so its surface no
    /// longer ramps any part of it away.
    /// Every block shares the exact same floor plane (local Y 0, seen from
    /// below) - per an earlier ticket's explicit requirement - by solving
    /// each block's own Height (body thickness) from its required top
    /// height and using that SAME value as both TrackBlock.Height and the
    /// block's own transform.position.y (see BuildTrack); only the body
    /// thickness varies from block to block, never the shared floor.
    /// Kinematic marble movement (see 0038/0041) is NOT reconstructed from
    /// this geometry any more: every block carries its own IMarbleTrace
    /// instead (see CreateTrace), covering exactly its own extent, and
    /// MarbleController just plays those back end to end, one beat each.
    /// That's what lets each block variant move the marble its own way
    /// within an otherwise strictly even musical grid - a curve hugs its
    /// real arc, a Trigger block drops the marble onto its pad, bounces it
    /// into the groove and rolls it out, Start/Goal appear/vanish at their
    /// own tunnel wall. The earlier approach (X/Z lerped between grid cell
    /// centers, Y lerped from the current block's own entry to its exit)
    /// mixed two parametrizations half a cell apart, which turned every
    /// fall into a diagonal glide across the block - see 0041.
    /// Lives on its own GameObject; grid, marbleController and
    /// trackBlockPrefab are wired in the Inspector or auto-found at Awake.
    /// Surrounding, non-rollable filler terrain (see 0047,
    /// SyncFillerBlocks) covers every OTHER grid cell - non-buildable
    /// ones (0045), simply unused ones, and anything outside every
    /// SubLevel's own Area (0046) - so the track reads as carved into a
    /// continuous landscape instead of floating over an empty plane.
    /// </summary>
    public class TrackBlockSpawner : MonoBehaviour
    {
        [SerializeField] private PathGrid grid;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private TrackBlock trackBlockPrefab;
        [SerializeField] private float grooveRadius = 0f; // <= 0: derive from the 3D marble radius; always clamped to at most half the cell size (see Awake)
        [SerializeField] private int grooveArcSegments = 8; // resolution of the semicircular U profile
        [SerializeField] private float startHeight = 1f; // world/spawner-local Y of the groove floor at Start's own entry point - the baseline the whole height chain hangs from (see 0039); only used as a fallback for a cell that belongs to no SubLevel at all - each SubLevel has its own configurable StartHeight instead (see ResolveStartHeight/0047 follow-up)
        [SerializeField] private float minBlockHeight = 0.05f; // safety floor for TrackBlock.Height, so a long chain of Trigger falls can never shrink a block to a degenerate/near-zero or negative thickness
        [SerializeField] private float flatStartHeight = 0.1f; // world/spawner-local Y every block's own top surface starts at for the 2D->3D grow transition (see 0048, simplified scope) - should roughly match the 2D PathGrid's own scene height so the 3D track visually rises from about where the flat 2D view left off
        [SerializeField] private float growDuration = 1.1f; // seconds the grow-to-gameplay lerp takes - matches CameraModeTransition's own transitionDuration default so the height animation and the camera's 2D->isometric swing read as one synchronized moment, even though the two aren't code-coupled
        [SerializeField, Range(0f, 15f)] private float normalInclinationDegrees = 3f; // continuous downhill slope every straight Normal block's own surface has, in its travel direction - see 0039 follow-up. Curved Normal blocks stay flat (0) - a sloped curve is out of scope for now, see TrackBlock.SetCurve/0040
        [SerializeField, Range(0.05f, 0.9f)] private float triggerFallBeatFraction = 0.5f; // how much of a Trigger block's own beat the marble's fall onto its pad takes - the SAME for every Trigger block regardless of FallHeight, so all notes sound at the same phase and the music stays on the grid (see TriggerFallMarbleTrace/0038)
        [SerializeField] private float triggerBounceHeight = 0.06f; // how high the marble hops off a Trigger block's pad before dropping into the groove; 0 = no bounce, it just drops on
        [SerializeField] private Color terrainColor = new Color(0.30f, 0.45f, 0.20f); // grass/moss green - the "Default" biome's first look (see 0032)
        [SerializeField] private Color grooveColor = new Color(0.40f, 0.27f, 0.15f); // earthy brown for the rollable groove itself, distinct from the grass shoulders (see 0032)
        [SerializeField] private Color tunnelColor = new Color(0.05f, 0.05f, 0.06f); // near-black interior of the Start/Goal tunnel portal (see TunnelPortalDecoration)
        [SerializeField] private TerrainDecorationSettings terrainDecorationSettings; // moss-clump count/size/color/clustering - see TerrainDecoration.Scatter; null falls back to TerrainDecorationSettings.Default
        [SerializeField, Range(0, 20)] private int fillerSmoothingIterations = 8; // see 0047 follow-up/SmoothFillerHeights - softens sharp local steps (e.g. right beside a Trigger's own big FallHeight drop) left over from SolveFillerHeights' per-axis rules; 0 disables smoothing entirely
        [SerializeField, Range(0f, 1f)] private float fillerSmoothingStrength = 0.5f; // how far each smoothing iteration nudges a filler cell toward its neighbours' average - 0 disables smoothing, 1 snaps straight to it (can oscillate); actual track cell heights are never touched by this

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

        // See 0047/SyncFillerBlocks: the surrounding, non-rollable terrain
        // that fills every grid cell NOT covered by a track block.
        private readonly Dictionary<Vector2Int, float> lastTrackHeights = new Dictionary<Vector2Int, float>();
        private GameObject fillerRoot;
        private static readonly Vector2Int[] FillerAxisOffsets = { Direction.Right.ToGridOffset(), Direction.Up.ToGridOffset() };

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

        /// Builds (or refreshes) the whole 3D visualization - every
        /// currently completed track's blocks plus the surrounding filler
        /// terrain (see 0047) - in one synchronous call, rather than
        /// continuously in Update() as it used to: the 3D representation
        /// should only ever exist from the moment the player actually
        /// switches into the 3D view, not throughout 2D planning too (see
        /// 0047 follow-up - filler terrain made the old always-on
        /// building far more visible than the odd track block ever was).
        /// MarbleController.Play() calls this synchronously right before
        /// starting any track coroutines, so both the marble's own
        /// HasTrackFor wait (see RunAlongPath3D) and CameraModeTransition's
        /// bounds-based framing always find the blocks already built -
        /// no per-frame Update() ordering to race against any more.
        public void RebuildNow()
        {
            SyncTracks(FindCompletedPaths());
            SyncFillerBlocks();
        }

        /// The 2D-to-3D "grow" transition (see 0048, simplified scope):
        /// every currently spawned block - track blocks AND filler terrain
        /// alike - is first snapped to the SAME flat surface height
        /// (flatStartHeight), then their Y position is lerped from there up
        /// to each block's own real, already-solved height (TrackBlock.Height -
        /// always exactly equal to the Y position BuildTrack/BuildFillerBlock
        /// gave it, see their own comments on the shared-floor invariant, so
        /// no separate bookkeeping of "final height" is needed here). Pure
        /// Transform.position changes - no Rebuild()/re-meshing at any point
        /// during the animation, since TrackBlock.Rebuild() allocates a new
        /// Mesh and re-cooks a MeshCollider every call and is not meant to
        /// run every frame. Call this right after RebuildNow() has built
        /// the final track (see MarbleController.Play) - the blocks it
        /// finds already reflect their true target shape/height, this only
        /// re-animates their starting position.
        public IEnumerator GrowToGameplay()
        {
            List<TrackBlock> blocks = CollectAllBlocks();

            foreach (TrackBlock block in blocks)
            {
                Vector3 p = block.transform.localPosition;
                p.y = flatStartHeight;
                block.transform.localPosition = p;
            }

            float elapsed = 0f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / growDuration));

                foreach (TrackBlock block in blocks)
                {
                    Vector3 p = block.transform.localPosition;
                    p.y = Mathf.Lerp(flatStartHeight, block.Height, f);
                    block.transform.localPosition = p;
                }

                yield return null;
            }

            foreach (TrackBlock block in blocks)
            {
                Vector3 p = block.transform.localPosition;
                p.y = block.Height;
                block.transform.localPosition = p;
            }
        }

        private List<TrackBlock> CollectAllBlocks()
        {
            List<TrackBlock> result = new List<TrackBlock>();

            foreach (TrackInstance track in tracks)
            {
                foreach (TrackBlock block in track.Blocks)
                {
                    if (block != null) result.Add(block);
                }
            }

            if (fillerRoot != null)
            {
                foreach (Transform child in fillerRoot.transform)
                {
                    TrackBlock block = child.GetComponent<TrackBlock>();
                    if (block != null) result.Add(block);
                }
            }

            return result;
        }

        /// Tears down every currently spawned 3D block - real track
        /// blocks and filler terrain alike - so nothing of the 3D
        /// visualization lingers once back in 2D planning (see 0047
        /// follow-up). MarbleController.Stop() calls this whenever
        /// IsPlaying flips back to false. Safe to call repeatedly/when
        /// already empty.
        public void ClearAll()
        {
            foreach (TrackInstance track in tracks)
            {
                if (track.Root != null) Destroy(track.Root.gameObject);
            }
            tracks.Clear();

            if (fillerRoot != null) Destroy(fillerRoot);
            fillerRoot = null;
            lastTrackHeights.Clear();
        }

        /// True once this exact path's blocks actually exist - false in
        /// the frame a track just completed but SyncTracks hasn't run yet
        /// (MarbleController's and this component's Update order is
        /// undefined), and false again once the path changed underneath a
        /// marble that's still running on it.
        public bool HasTrackFor(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count == 0) return false;
            TrackInstance track = FindTrackByStart(path[0]);
            return track != null && PathEquals(track.Path, path);
        }

        /// The marble's own movement across ONE block of this path (see
        /// IMarbleTrace/0038): its trace, resolved against that block's
        /// transform and the given tempo. False if this track isn't
        /// spawned (any more) - the caller should then stop rather than
        /// keep a marble running on blocks that no longer exist.
        public bool TryGetTraceSegment(IReadOnlyList<Vector2Int> path, int index, float cellsPerSecond, out TrackTraceSegment segment)
        {
            segment = default;
            if (!HasTrackFor(path)) return false;

            TrackInstance track = FindTrackByStart(path[0]);
            if (index < 0 || index >= track.Blocks.Count) return false;

            TrackBlock block = track.Blocks[index];
            if (block == null) return false;

            segment = new TrackTraceSegment(block, block.Trace, cellsPerSecond);
            return true;
        }

        /// World-space point on the marble's own path at a fractional
        /// position along it - e.g. 2.3 means 30% of the way through
        /// path[2]'s own beat (see IMarbleTrace), and path.Count means the
        /// very end of the last block's. At the groove floor itself,
        /// no marble-radius offset: used to place a physics marble right
        /// above Start (MarbleController adds its own marbleRadius3D +
        /// physicsDropHeight clearance on top), and as the Goal reference
        /// point for arrival detection. Kinematic movement doesn't go
        /// through here - it plays whole trace segments back instead (see
        /// TryGetTraceSegment).
        public Vector3 GetShoulderWorldPosition(IReadOnlyList<Vector2Int> path, float pathPosition)
        {
            return SampleTrackPosition(path, pathPosition, 0f);
        }

        private Vector3 SampleTrackPosition(IReadOnlyList<Vector2Int> path, float pathPosition, float verticalOffset)
        {
            TrackInstance track = FindTrackByStart(path[0]);

            float clamped = Mathf.Clamp(pathPosition, 0f, path.Count);
            int index = Mathf.Clamp(Mathf.FloorToInt(clamped), 0, path.Count - 1);
            float f = Mathf.Clamp01(clamped - index);

            TrackBlock block = track != null && index < track.Blocks.Count ? track.Blocks[index] : null;

            if (block == null)
            {
                // That block isn't spawned yet this frame (e.g. the very
                // first frame after a path just completed) - the plain
                // cell center at the baseline height is close enough, and
                // self-corrects the very next frame once BuildTrack has
                // actually run.
                Vector3 cell = grid.CellToLocalPosition(path[index]);
                return transform.TransformPoint(new Vector3(cell.x, startHeight, cell.y)) + Vector3.up * verticalOffset;
            }

            return block.transform.TransformPoint(block.Trace.SampleLocal(f)) + Vector3.up * verticalOffset;
        }

        /// Smallest FallHeight a Trigger block can actually be built with:
        /// its entry half is sealed off and carries the pad bar on top
        /// (see BuildTrack's profile choice and XylophoneBlockDecoration),
        /// so the previous block's groove must end at least at that bar's
        /// own top for the marble to land ON the pad instead of starting
        /// inside solid geometry. Anything smaller (e.g. a content asset
        /// left at 0) is raised to this, with a warning.
        private float MinTriggerFallHeight => grooveRadius + XylophoneBlockDecoration.PadTopY(grooveRadius);

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

        /// This block's own Tilt, in degrees. A straight Normal block
        /// always gets a small constant downhill slope
        /// (normalInclinationDegrees) so the marble keeps a gentle,
        /// continuous pace even across a long flat-looking stretch - see
        /// 0039 follow-up. Everything else stays flat: a turning Normal
        /// block (a sloped curve is out of scope for now, see
        /// TrackBlock.SetCurve/0040), Start/Goal, and a Trigger block -
        /// the latter because its FallHeight is a REAL fall in the
        /// marble's own trace now (see TriggerFallMarbleTrace/0041), so
        /// its pad no longer ramps part of that height away.
        private float ComputeSurfaceInclination(BlockType type, bool isTurn)
        {
            if (type == BlockType.Normal && !isTurn) return normalInclinationDegrees;
            return 0f;
        }

        /// The path the marble itself takes through this block (see
        /// IMarbleTrace/0038) - the one place each block variant's own
        /// movement is defined, as opposed to its shape (IBlockProfile).
        /// Start/Goal are shortened to their own sealed groove end
        /// (IClosedEndBlockProfile.WallZ), so the marble rolls out of /
        /// into the tunnel portal's mouth rather than appearing at the
        /// block's outer edge; a turn follows its real arc; a Trigger
        /// block falls onto its pad, bounces into the groove and rolls
        /// out. Called once per block at spawn time, after Size/Profile/
        /// Yaw/Tilt are final (the traces read those).
        private IMarbleTrace CreateTrace(TrackBlock block, BlockType type, bool isTurn, float fallHeight, Vector3 fallSideLocal, float railExtension)
        {
            if (isTurn) return new CurvedMarbleTrace(block);

            float halfLength = block.Size.y * 0.5f;

            switch (type)
            {
                case BlockType.Start:
                    return StraightMarbleTrace.BetweenZ(block, -railExtension, halfLength);
                case BlockType.Goal:
                    return StraightMarbleTrace.BetweenZ(block, -halfLength, railExtension);
                case BlockType.Trigger:
                    return new TriggerFallMarbleTrace(block, fallSideLocal, fallHeight,
                        XylophoneBlockDecoration.PadTopY(grooveRadius),
                        XylophoneBlockDecoration.PadCenterOffset(grooveRadius, SideWidth),
                        -railExtension + grooveRadius, triggerFallBeatFraction, triggerBounceHeight);
                default:
                    return StraightMarbleTrace.BetweenZ(block, -halfLength, halfLength);
            }
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
        /// Every SubLevel reached so far keeps validating and appearing in
        /// `paths` for as long as it's unlocked (see 0046 follow-up,
        /// PathGrid.IsInUnlockedSubLevel) - once reached, its pipes are
        /// also hidden/non-interactive (PathGrid's own visibility toggle),
        /// so its path can't actually change any more either. That's what
        /// keeps several SubLevels' tracks spawning marbles side by side
        /// without any special-casing here: an earlier SubLevel's track
        /// just keeps matching every frame like any other stable track.
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

        /// Surrounds the actual marble track with flat, non-rollable
        /// filler blocks (see 0047) for every grid cell NOT covered by a
        /// track block right now - permanently non-buildable cells (see
        /// 0045), simply unused ones, and any area outside every
        /// SubLevel's own Area if SubLevels don't tile the whole grid (see
        /// 0046). Each filler's own height comes from its neighbours (see
        /// SolveFillerHeights) rather than a track's own height chain, so
        /// the terrain reads as one continuous landscape the track is
        /// carved into rather than an island floating in a flat plane.
        /// Rebuilt in one shot whenever the set of track cells (or any of
        /// their heights) actually changes - cheap enough at
        /// grid-prototype scale, and simpler than diffing individual
        /// filler blocks the way SyncTracks diffs individual tracks
        /// (fillers carry no marble/running state to preserve across a
        /// rebuild).
        private void SyncFillerBlocks()
        {
            Dictionary<Vector2Int, float> trackHeights = CollectTrackHeights();
            if (FillerInputUnchanged(trackHeights)) return;

            lastTrackHeights.Clear();
            foreach (KeyValuePair<Vector2Int, float> entry in trackHeights) lastTrackHeights[entry.Key] = entry.Value;

            if (fillerRoot != null) Destroy(fillerRoot);
            fillerRoot = new GameObject("FillerBlocks");
            fillerRoot.transform.SetParent(transform, false);

            Vector2 blockSize = new Vector2(grid.CellSize, grid.CellSize);
            foreach (KeyValuePair<Vector2Int, float> entry in SolveFillerHeights(trackHeights))
            {
                BuildFillerBlock(entry.Key, entry.Value, blockSize);
            }
        }

        private Dictionary<Vector2Int, float> CollectTrackHeights()
        {
            Dictionary<Vector2Int, float> result = new Dictionary<Vector2Int, float>();
            foreach (TrackInstance track in tracks)
            {
                for (int i = 0; i < track.Path.Count && i < track.Blocks.Count; i++)
                {
                    TrackBlock block = track.Blocks[i];
                    if (block != null) result[track.Path[i]] = block.transform.localPosition.y;
                }
            }
            return result;
        }

        private bool FillerInputUnchanged(Dictionary<Vector2Int, float> current)
        {
            if (fillerRoot == null) return false;
            if (current.Count != lastTrackHeights.Count) return false;

            foreach (KeyValuePair<Vector2Int, float> entry in current)
            {
                if (!lastTrackHeights.TryGetValue(entry.Key, out float previous) || !Mathf.Approximately(previous, entry.Value))
                    return false;
            }
            return true;
        }

        /// Fills in a height for every grid cell not already covered by a
        /// track (see SyncFillerBlocks), so the surrounding terrain reads
        /// as one continuous landscape rather than a flat plane the track
        /// floats over. Two opposite direct neighbours (known, or already
        /// solved in an earlier pass) average straight to the height
        /// between them. A single direct neighbour plus the next one
        /// further out in the SAME direction instead extrapolates that
        /// pair's own slope onward (2*near - far), so a continuous incline
        /// keeps rising/falling into the surrounding terrain instead of
        /// flattening out the moment the track ends. Solved outward from
        /// the known track cells in repeated passes - a cell resolved in
        /// pass N becomes a usable neighbour in pass N+1, which is what
        /// lets a slope keep propagating more than one cell deep.
        /// Every genuine two-point read (average or slope extrapolation)
        /// is exhausted across the WHOLE grid first, in its own priority
        /// tier, before a cell with only ever a single lone neighbour
        /// falls back to flatly copying it - so a real incline always
        /// wins over an arbitrary flat guess wherever one is available,
        /// regardless of which cell happens to be processed first. A cell
        /// no pass in either tier ever reaches (an isolated blocked/unused
        /// region with no path back to any track cell) settles at that
        /// cell's own resolved start height (see ResolveStartHeight) - the
        /// same baseline its own SubLevel's track chain hangs from.
        /// Only cells in an UNLOCKED SubLevel (see PathGrid.
        /// IsInUnlockedSubLevel/0046) are considered at all - a SubLevel
        /// not reached yet gets no filler (or track) blocks whatsoever,
        /// so the 3D terrain reveals itself progressively alongside the
        /// player's own progress instead of the whole grid's worth of
        /// surrounding terrain existing from the very first SubLevel on.
        private Dictionary<Vector2Int, float> SolveFillerHeights(Dictionary<Vector2Int, float> trackHeights)
        {
            Dictionary<Vector2Int, float> resolved = new Dictionary<Vector2Int, float>(trackHeights);
            List<Vector2Int> pending = new List<Vector2Int>();

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    if (resolved.ContainsKey(coord)) continue;
                    if (!grid.IsInUnlockedSubLevel(coord)) continue;
                    pending.Add(coord);
                }
            }

            // Width+Height passes are always enough for a slope to cross
            // the whole grid from any edge.
            int maxPasses = grid.Width + grid.Height;
            ResolveFillerPasses(resolved, pending, maxPasses, allowFlatFallback: false);
            ResolveFillerPasses(resolved, pending, maxPasses, allowFlatFallback: true);

            foreach (Vector2Int coord in pending) resolved[coord] = ResolveStartHeight(coord);

            resolved = SmoothFillerHeights(resolved, trackHeights);

            Dictionary<Vector2Int, float> fillerHeights = new Dictionary<Vector2Int, float>();
            foreach (KeyValuePair<Vector2Int, float> entry in resolved)
            {
                if (!trackHeights.ContainsKey(entry.Key)) fillerHeights[entry.Key] = entry.Value;
            }
            return fillerHeights;
        }

        /// Softens any locally sharp step SolveFillerHeights' per-axis
        /// rules can leave behind - most visibly right beside a track
        /// cell whose own height jumps a lot from its neighbour (e.g. a
        /// Trigger's own FallHeight drop): the slope-extrapolation rule
        /// would otherwise mirror that SAME jump at full strength into
        /// the very next filler cell, reading as an artificial cliff
        /// rather than the gentle surrounding terrain the ticket asked
        /// for. Standard Laplacian relaxation - each filler cell is
        /// repeatedly nudged (by fillerSmoothingStrength per iteration,
        /// so it converges gradually instead of overshooting) toward the
        /// average of whichever of its four grid neighbours are already
        /// resolved. Actual track cells are never part of `fillerCells`
        /// and so never move - they stay the fixed anchors every filler
        /// height still has to reach, just via a gentler curve now.
        private Dictionary<Vector2Int, float> SmoothFillerHeights(Dictionary<Vector2Int, float> resolved, Dictionary<Vector2Int, float> trackHeights)
        {
            if (fillerSmoothingIterations <= 0 || fillerSmoothingStrength <= 0f) return resolved;

            List<Vector2Int> fillerCells = new List<Vector2Int>();
            foreach (Vector2Int coord in resolved.Keys)
            {
                if (!trackHeights.ContainsKey(coord)) fillerCells.Add(coord);
            }

            for (int iteration = 0; iteration < fillerSmoothingIterations; iteration++)
            {
                Dictionary<Vector2Int, float> next = new Dictionary<Vector2Int, float>(resolved);

                foreach (Vector2Int coord in fillerCells)
                {
                    float sum = 0f;
                    int count = 0;

                    foreach (Vector2Int axis in FillerAxisOffsets)
                    {
                        if (resolved.TryGetValue(coord - axis, out float negative)) { sum += negative; count++; }
                        if (resolved.TryGetValue(coord + axis, out float positive)) { sum += positive; count++; }
                    }

                    if (count == 0) continue;
                    next[coord] = Mathf.Lerp(resolved[coord], sum / count, fillerSmoothingStrength);
                }

                resolved = next;
            }

            return resolved;
        }

        private static void ResolveFillerPasses(Dictionary<Vector2Int, float> resolved, List<Vector2Int> pending, int maxPasses, bool allowFlatFallback)
        {
            for (int pass = 0; pass < maxPasses && pending.Count > 0; pass++)
            {
                bool anyResolved = false;

                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    if (!TryDeriveFillerHeight(pending[i], resolved, allowFlatFallback, out float height)) continue;

                    resolved[pending[i]] = height;
                    pending.RemoveAt(i);
                    anyResolved = true;
                }

                if (!anyResolved) break;
            }
        }

        /// One cell's candidate height per axis (Left/Right, Down/Up),
        /// averaged across however many axes actually produced one - see
        /// SolveFillerHeights for the per-axis rules themselves.
        /// allowFlatFallback gates the lone-neighbour case specifically -
        /// see SolveFillerHeights' two-tier call.
        private static bool TryDeriveFillerHeight(Vector2Int coord, Dictionary<Vector2Int, float> resolved, bool allowFlatFallback, out float height)
        {
            float sum = 0f;
            int count = 0;

            foreach (Vector2Int axis in FillerAxisOffsets)
            {
                bool hasNegative = resolved.TryGetValue(coord - axis, out float negative);
                bool hasPositive = resolved.TryGetValue(coord + axis, out float positive);

                if (hasNegative && hasPositive)
                {
                    sum += (negative + positive) * 0.5f;
                }
                else if (hasNegative && resolved.TryGetValue(coord - axis * 2, out float negativeFar))
                {
                    sum += 2f * negative - negativeFar; // continue the negative-side slope onward
                }
                else if (hasPositive && resolved.TryGetValue(coord + axis * 2, out float positiveFar))
                {
                    sum += 2f * positive - positiveFar; // continue the positive-side slope onward
                }
                else if (allowFlatFallback && hasNegative)
                {
                    sum += negative; // no second point to derive a slope from - continue flat instead
                }
                else if (allowFlatFallback && hasPositive)
                {
                    sum += positive;
                }
                else
                {
                    continue;
                }

                count++;
            }

            if (count == 0)
            {
                height = 0f;
                return false;
            }

            height = sum / count;
            return true;
        }

        private void BuildFillerBlock(Vector2Int cell, float height, Vector2 blockSize)
        {
            float clampedHeight = Mathf.Max(height, minBlockHeight);

            TrackBlock block = Instantiate(trackBlockPrefab, fillerRoot.transform);
            block.name = $"Filler_{cell.x}_{cell.y}";

            Vector3 cellPos = grid.CellToLocalPosition(cell);
            block.transform.localPosition = new Vector3(cellPos.x, clampedHeight, cellPos.y);

            block.ClearCurve();
            block.Profile = FlatBoxProfile.Instance;
            block.Size = blockSize;
            block.Height = clampedHeight;
            block.Material = sharedMaterial;
            block.GrooveMaterial = sharedMaterial; // FlatBoxProfile has no groove segments, so this is never actually sampled
            block.YawDegrees = 0f;
            block.TiltDegrees = 0f;

            block.SetDefinition(new BlockDefinition(cell, Direction.None, Direction.None, clampedHeight,
                BlockType.Filler, 0f, 0f, TriggerBehavior.None, null, BlockDefinition.DefaultBiome, Color.white));

            // grooveRadius 0 here (unlike a real track block): a flat
            // filler has no groove to keep decorations clear of, so they
            // may as well scatter across its entire top surface.
            TerrainDecoration.Scatter(block, cell, BlockDefinition.DefaultBiome, 0f, blockSize.x * 0.5f, blockSize, terrainDecorationSettings);
        }

        /// The baseline height a track/filler chain should hang from at
        /// this cell: the SubLevel that owns it, via its own configurable
        /// StartHeight (see SubLevelDefinition/0047 follow-up - editable
        /// per SubLevel in the Level Grid Editor's SubLevels panel), or
        /// this spawner's own global startHeight field when the cell
        /// belongs to no SubLevel at all (a level that doesn't use
        /// SubLevels is a single implicit one - same convention as
        /// PathGrid.ActiveSubLevelArea).
        private float ResolveStartHeight(Vector2Int coord)
        {
            if (grid.Level != null && grid.TryGetSubLevelIndexAt(coord, out int index))
            {
                return grid.Level.SubLevels[index].StartHeight;
            }
            return startHeight;
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

            float runningExitY = ResolveStartHeight(start); // world/spawner-local Y of the groove floor the next block's entry must match, unless it's a Trigger block

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

                float requestedFallHeight = type == BlockType.Trigger ? triggerContent.FallHeight : 0f;
                float fallHeight = type == BlockType.Trigger ? Mathf.Max(requestedFallHeight, MinTriggerFallHeight) : 0f;

                if (fallHeight > requestedFallHeight + 1e-4f)
                {
                    Debug.LogWarning($"TrackBlockSpawner: Trigger-Inhalt an {cell} hat FallHeight {requestedFallHeight:0.###}, das Minimum ist {MinTriggerFallHeight:0.###} (sonst endet die Rille des Vorgaengers unterhalb der Pad-Leiste, auf die die Kugel fallen soll) - es wird mit dem Minimum gebaut.");
                }

                float entryY = runningExitY - fallHeight; // Start/Goal/Normal: fallHeight is always 0 -> perfectly height-matched to the previous block's exit
                float inclinationDegrees = ComputeSurfaceInclination(type, isTurn);
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

                // Where this block's entry REALLY ended up: identical to
                // entryY, unless minBlockHeight had to clamp the body
                // thickness (a long chain of falls, or one deeper than the
                // track's own startHeight, can ask for a negative one).
                // Everything downstream - the next block's entry, and this
                // block's own marble trace - chains off the height that
                // actually got built rather than the one that was asked
                // for, so a clamp can't tear the marble's path open.
                float actualEntryY = height + localEntryY;
                float effectiveFallHeight = Mathf.Max(runningExitY - actualEntryY, 0f);

                if (actualEntryY > entryY + 1e-4f)
                {
                    Debug.LogWarning($"TrackBlockSpawner: Block {i} bei {cell} muesste auf Hoehe {entryY:0.###} liegen, kann aber wegen minBlockHeight nur auf {actualEntryY:0.###} - die Summe der FallHeights hat die Bodenebene erreicht. startHeight erhoehen oder FallHeight der Trigger-Inhalte senken.");
                }

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

                // The marble travels IN InputDirection to arrive here, so
                // it arrives FROM the opposite side - undo this block's own
                // Yaw to get that side in local space, regardless of which
                // way the block itself faces. Both the fall (see
                // TriggerFallMarbleTrace) and the pad the marble falls onto
                // (XylophoneBlockDecoration) hang off this one vector, so
                // they always agree on where "from" is.
                Vector3 fallSideLocal = Quaternion.Euler(0f, -block.YawDegrees, 0f) * (-inputDir.ToLocalVector3());

                block.SetTrace(CreateTrace(block, type, isTurn, effectiveFallHeight, fallSideLocal, railExtension));

                // Start/Goal stay silent even with sound content: they sit
                // outside the loop's bar, overlapping the neighbouring laps
                // (see MarbleController.RunTrack/0043).
                SoundTriggerContent soundContent = type == BlockType.Trigger ? content as SoundTriggerContent : null;
                TriggerBehavior trigger = soundContent != null ? TriggerBehavior.OnEnter : TriggerBehavior.None; // XylophonePadContent is visual-only, no trigger
                Color flashColor = soundContent != null ? soundContent.FlashColor : Color.white;
                block.SetDefinition(new BlockDefinition(cell, inputDir, outputDir, actualEntryY, type, fallHeight,
                    inclinationDegrees, trigger, soundContent?.Clip, BlockDefinition.DefaultBiome, flashColor));

                if (isStart || isGoal)
                {
                    TunnelPortalDecoration.Build(block, closedAtEntry: isStart, grooveRadius, SideWidth, blockSize, railExtension, sharedMaterial, sharedTunnelMaterial);
                }
                else if (type == BlockType.Trigger)
                {
                    // The instrument element is built here, but how it
                    // LOOKS and how it reacts to being hit belongs to the
                    // block's own feedback component (see 0023's rule that
                    // a block decides its own reactions) - this just hands
                    // the two to each other. A block without that
                    // component simply keeps the element's plain material.
                    MeshRenderer padRenderer = XylophoneBlockDecoration.Build(block, fallSideLocal, grooveRadius, SideWidth, sharedMaterial);
                    block.GetComponent<InstrumentPadFeedback>()?.Attach(padRenderer);
                }
                else
                {
                    TerrainDecoration.Scatter(block, cell, block.Definition.Biome, grooveRadius, SideWidth, blockSize, terrainDecorationSettings);
                }

                blocks.Add(block);
                runningExitY = actualEntryY - drop; // baseline the next block's entry must match, unless it's a Trigger block
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
        /// of tracks belonging to the SubLevel being edited right now (see
        /// 0046) - other SubLevels may well be unlocked and looping
        /// alongside it (see PathGrid.IsInUnlockedSubLevel), but the
        /// guided isometric camera (see CameraModeTransition) should only
        /// ever frame the one the player is actually working on, not
        /// every track ever built. From each block's MeshRenderer.bounds
        /// (already reflecting its real position, yaw and staircase
        /// height). False (bounds left at default) if the active
        /// SubLevel's track isn't currently spawned.
        public bool TryGetTracksWorldBounds(out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (TrackInstance track in tracks)
            {
                if (grid != null && !grid.IsInActiveSubLevel(track.Path[0])) continue;

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
