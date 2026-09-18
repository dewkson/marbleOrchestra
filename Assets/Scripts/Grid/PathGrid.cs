using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Parametric grid of PathPipes built from a LevelData asset.
    /// Owns the swap logic so it works independent of any input method.
    /// Also owns the content layer (e.g. sound triggers), which is bound to
    /// the cell coordinate and stays fixed regardless of pipe swaps.
    /// </summary>
    public class PathGrid : MonoBehaviour
    {
        [SerializeField] private LevelData level;
        [SerializeField] private float cellSize = 1.2f;
        [SerializeField] private float cardScale = 1f;

        private PathPipe[,] pipes;
        private CellContentDefinition[,] contents;
        private bool[,] blocked;
        private int activeSubLevelIndex;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize => cellSize;
        public LevelData Level => level;
        public IReadOnlyList<PathValidationResult> LastValidations { get; private set; } = new List<PathValidationResult>();

        public int ActiveSubLevelIndex => activeSubLevelIndex;
        public int SubLevelCount => level != null ? level.SubLevels.Count : 0;

        /// The area shown/editable in the 2D planning view right now (see
        /// 0046's follow-up) - the active SubLevel's own Area, or the
        /// whole grid if the level defines none (a single implicit
        /// SubLevel, keeping older single-puzzle levels working unchanged).
        /// NOT the same as "unlocked for gameplay" (see IsInUnlockedSubLevel) -
        /// every earlier SubLevel keeps validating and playing once
        /// reached, but only this one, the newest, stays editable.
        public RectInt ActiveSubLevelArea
        {
            get
            {
                if (level == null || level.SubLevels.Count == 0) return new RectInt(0, 0, Width, Height);
                int index = Mathf.Clamp(activeSubLevelIndex, 0, level.SubLevels.Count - 1);
                return level.SubLevels[index].Area;
            }
        }

        public bool IsInActiveSubLevel(Vector2Int coord)
        {
            return ActiveSubLevelArea.Contains(coord);
        }

        /// True for every cell belonging to a SubLevel reached so far
        /// (index <= ActiveSubLevelIndex) - PathValidator resolves ALL of
        /// these concurrently (see 0046 follow-up), not just the newest
        /// one, so every earlier SubLevel's track keeps spawning marbles
        /// and looping once built: several simultaneous tracks are the
        /// whole point (layered musical voices), not a single active
        /// puzzle. A level with no SubLevels at all is a single implicit
        /// one - always unlocked.
        public bool IsInUnlockedSubLevel(Vector2Int coord)
        {
            if (level == null || level.SubLevels.Count == 0) return true;
            return TryGetSubLevelIndexAt(coord, out int index) && index <= activeSubLevelIndex;
        }

        /// The Area of whichever SubLevel owns this cell - used to keep a
        /// single track's own traversal confined to ITS OWN SubLevel even
        /// though several SubLevels validate at once (see
        /// IsInUnlockedSubLevel), so a Start in one SubLevel can never
        /// bridge into a neighbouring one's pipes. A cell that belongs to
        /// no defined SubLevel (a gap in an otherwise SubLevel-divided
        /// grid) is isolated to just itself, so it can't bridge two
        /// SubLevels either.
        public RectInt GetOwnSubLevelArea(Vector2Int coord)
        {
            if (level == null || level.SubLevels.Count == 0) return new RectInt(0, 0, Width, Height);
            if (TryGetSubLevelIndexAt(coord, out int index)) return level.SubLevels[index].Area;
            return new RectInt(coord.x, coord.y, 1, 1);
        }

        /// Which SubLevel (by list index) owns this cell, if any - used by
        /// TrackBlockSpawner (see ResolveStartHeight/0047 follow-up) to
        /// look up a Start block's own SubLevel-specific height.
        public bool TryGetSubLevelIndexAt(Vector2Int coord, out int index)
        {
            for (int i = 0; i < level.SubLevels.Count; i++)
            {
                if (level.SubLevels[i].Area.Contains(coord))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        /// Moves the EDITABLE SubLevel forward in LevelData.SubLevels (its
        /// list order is the progression order) - false/no-op if there
        /// isn't one, e.g. no SubLevels defined at all or already on the
        /// last. The SubLevel left behind stays unlocked (see
        /// IsInUnlockedSubLevel) and keeps validating/playing - only its
        /// own 2D pipes stop being shown/editable (see
        /// UpdateActiveSubLevelVisibility).
        public bool AdvanceToNextSubLevel()
        {
            if (level == null) return false;
            int count = level.SubLevels.Count;
            if (count == 0 || activeSubLevelIndex >= count - 1) return false;

            activeSubLevelIndex++;
            UpdateActiveSubLevelVisibility();
            Revalidate();
            return true;
        }

        /// Only the active SubLevel's own pipes are shown/interactable in
        /// the 2D planning view (see 0046's follow-up) - a cell outside it
        /// gets its PathPipe GameObject disabled entirely, which as a
        /// side effect also drops its collider out of GridInputHandler's
        /// raycasts, so a SubLevel already left behind can't accidentally
        /// be re-edited either.
        private void UpdateActiveSubLevelVisibility()
        {
            if (pipes == null) return;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    PathPipe pipe = pipes[x, y];
                    if (pipe == null) continue;
                    pipe.gameObject.SetActive(IsInActiveSubLevel(new Vector2Int(x, y)));
                }
            }
        }

        private void Awake()
        {
            if (level != null)
            {
                Build(level);
            }
        }

        public void Build(LevelData levelData)
        {
            level = levelData;
            activeSubLevelIndex = 0;
            Width = levelData.Width;
            Height = levelData.Height;
            pipes = new PathPipe[Width, Height];
            contents = new CellContentDefinition[Width, Height];
            blocked = new bool[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    bool isBlocked = levelData.IsBlockedAt(index);
                    blocked[x, y] = isBlocked;
                    if (isBlocked) continue;

                    PipeDefinition definition = index < levelData.Pipes.Count ? levelData.Pipes[index] : null;
                    pipes[x, y] = CreatePipe(new Vector2Int(x, y), definition);
                    contents[x, y] = index < levelData.Contents.Count ? levelData.Contents[index] : null;
                }
            }

            UpdateActiveSubLevelVisibility();
            Revalidate();
        }

        public PathPipe GetPipe(Vector2Int coord)
        {
            return IsInBounds(coord) ? pipes[coord.x, coord.y] : null;
        }

        public CellContentDefinition GetContent(Vector2Int coord)
        {
            return IsInBounds(coord) ? contents[coord.x, coord.y] : null;
        }

        public bool IsInBounds(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < Width && coord.y >= 0 && coord.y < Height;
        }

        public bool IsBlocked(Vector2Int coord)
        {
            return !IsInBounds(coord) || blocked[coord.x, coord.y];
        }

        public IReadOnlyList<PathPipe> FindPipesByRole(PipeRole role)
        {
            List<PathPipe> result = new List<PathPipe>();
            if (pipes == null) return result;

            foreach (PathPipe pipe in pipes)
            {
                if (pipe != null && pipe.Role == role) result.Add(pipe);
            }

            return result;
        }

        public void SwapCards(Vector2Int a, Vector2Int b)
        {
            // Content layer is intentionally untouched here — it is bound to
            // the cell coordinate, not to the pipe occupying it.
            if (!IsInBounds(a) || !IsInBounds(b) || a == b) return;
            if (IsBlocked(a) || IsBlocked(b)) return;

            PathPipe pipeA = pipes[a.x, a.y];
            PathPipe pipeB = pipes[b.x, b.y];

            pipes[a.x, a.y] = pipeB;
            pipes[b.x, b.y] = pipeA;

            if (pipeA != null)
            {
                pipeA.SetCoord(b);
                pipeA.transform.localPosition = CellToLocalPosition(b);
            }

            if (pipeB != null)
            {
                pipeB.SetCoord(a);
                pipeB.transform.localPosition = CellToLocalPosition(a);
            }

            Revalidate();
        }

        public IReadOnlyList<PathValidationResult> Revalidate()
        {
            IReadOnlyList<PathValidationResult> results = PathValidator.EvaluateAll(this);
            LastValidations = results;

            foreach (PathPipe pipe in pipes)
            {
                if (pipe == null) continue;

                CellConnectivity connectivity = CellConnectivity.Disconnected;
                foreach (PathValidationResult result in results)
                {
                    if (!result.ConnectedCells.Contains(pipe.Coord)) continue;
                    connectivity = result.GoalReached ? CellConnectivity.PathComplete : CellConnectivity.Connected;
                    break;
                }

                pipe.SetConnectivity(connectivity);
            }

            return results;
        }

        public Vector3 CellToLocalPosition(Vector2Int coord)
        {
            return new Vector3(coord.x * cellSize, coord.y * cellSize, 0f);
        }

        private PathPipe CreatePipe(Vector2Int coord, PipeDefinition definition)
        {
            GameObject go = new GameObject($"Pipe_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellToLocalPosition(coord);
            go.transform.localScale = Vector3.one * cardScale;

            // A 3D BoxCollider rather than BoxCollider2D deliberately -
            // Physics2D only ever considers a transform's position.xy and
            // its rotation around Z, so it silently breaks once the grid
            // is rotated out of a pure front-on X/Y orientation (see 0029's
            // top-down planning view). A thin 3D box, raycast against with
            // Physics.Raycast (see GridInputHandler), works under any
            // grid/camera orientation.
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 0.1f);

            PathPipe pipe = go.AddComponent<PathPipe>();
            pipe.Initialize(definition, coord);
            return pipe;
        }
    }
}
