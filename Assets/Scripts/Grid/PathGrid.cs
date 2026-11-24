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
        [Tooltip("Seconds a card takes to glide into its new cell after a 15-Puzzle slide (see 0067). 0 = jump instantly.")]
        [SerializeField] private float slideDuration = 0.15f;
        [Header("Content Icons (see 0069)")]
        [Tooltip("Größe des Instrument-Icons relativ zur Karte.")]
        [SerializeField, Range(0.1f, 1f)] private float contentIconScale = 0.55f;
        [Tooltip("Versatz des Icons von der Kartenmitte (in Kartengrößen), z.B. (0.25, -0.25) für unten rechts.")]
        [SerializeField] private Vector2 contentIconOffset = Vector2.zero;
        [SerializeField] private Color contentIconTint = Color.white;
        [Tooltip("Deckkraft des Icons, solange eine Pipekarte auf der Zelle liegt (1 = voll sichtbar, 0 = unsichtbar).")]
        [SerializeField, Range(0f, 1f)] private float coveredContentIconAlpha = 0.85f;
        [Tooltip("Über allen Karten-Renderern (max. 5), aber unter einer gezogenen Karte (+100).")]
        [SerializeField] private int contentIconSortingOrder = 10;

        private PathPipe[,] pipes;
        private CellContentDefinition[,] contents;
        private bool[,] blocked;
        private PipeVisual[,] blockedVisuals; // one per blocked cell, with or without a picture (see 0030/0054) - purely decorative, no collider
        private PipeVisual[,] slotVisuals; // 15-Puzzle only (see 0067): an empty slot under every buildable cell, drawn below the cards so it only shows where a cell is free - no collider
        private SpriteRenderer[,] contentIcons; // instrument icon per content cell (see 0069) - bound to the cell like the content itself, so it stays put while cards swap/slide underneath; no collider
        private int activeSubLevelIndex;
        private int highestReachedSubLevelIndex; // stays put when stepping back, so SubLevels already reached keep validating/playing (see IsInUnlockedSubLevel)

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize => cellSize;
        public LevelData Level => level;
        public bool IsFifteenPuzzle => level != null && level.GameMode == GameMode.FifteenPuzzle;
        public IReadOnlyList<PathValidationResult> LastValidations { get; private set; } = new List<PathValidationResult>();

        public int ActiveSubLevelIndex => activeSubLevelIndex;
        public int SubLevelCount => level != null ? level.SubLevels.Count : 0;
        public int HighestReachedSubLevelIndex => highestReachedSubLevelIndex;
        public bool HasNextSubLevel => activeSubLevelIndex < SubLevelCount - 1;
        public bool HasPreviousSubLevel => SubLevelCount > 0 && activeSubLevelIndex > 0;

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

        /// Area of the SubLevel at this list index (clamped), or the whole
        /// grid without SubLevels.
        public RectInt GetSubLevelArea(int index)
        {
            if (level == null || level.SubLevels.Count == 0) return new RectInt(0, 0, Width, Height);
            return level.SubLevels[Mathf.Clamp(index, 0, level.SubLevels.Count - 1)].Area;
        }

        /// Display name of the SubLevel at this list index as set in the
        /// level editor; without SubLevels the level asset's own name.
        public string GetSubLevelName(int index)
        {
            if (level == null) return string.Empty;
            if (level.SubLevels.Count == 0) return level.name;
            return level.SubLevels[Mathf.Clamp(index, 0, level.SubLevels.Count - 1)].Name;
        }

        public bool IsInActiveSubLevel(Vector2Int coord)
        {
            return ActiveSubLevelArea.Contains(coord);
        }

        /// True for every cell belonging to a SubLevel reached so far
        /// (index <= the highest SubLevel ever advanced to, even after
        /// stepping back to an earlier one) - PathValidator resolves ALL of
        /// these concurrently (see 0046 follow-up), not just the newest
        /// one, so every earlier SubLevel's track keeps spawning marbles
        /// and looping once built: several simultaneous tracks are the
        /// whole point (layered musical voices), not a single active
        /// puzzle. A level with no SubLevels at all is a single implicit
        /// one - always unlocked.
        public bool IsInUnlockedSubLevel(Vector2Int coord)
        {
            if (level == null || level.SubLevels.Count == 0) return true;
            return TryGetSubLevelIndexAt(coord, out int index) && index <= highestReachedSubLevelIndex;
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
            highestReachedSubLevelIndex = Mathf.Max(highestReachedSubLevelIndex, activeSubLevelIndex);
            UpdateActiveSubLevelVisibility();
            Revalidate();
            return true;
        }

        /// Moves the EDITABLE SubLevel one back so an earlier puzzle can be
        /// reworked. Everything reached so far stays unlocked (see
        /// IsInUnlockedSubLevel) - only GoalReached tracks ever play, so a
        /// later SubLevel keeps looping exactly as long as its own track
        /// stays valid.
        public bool GoToPreviousSubLevel()
        {
            if (!HasPreviousSubLevel) return false;

            activeSubLevelIndex--;
            UpdateActiveSubLevelVisibility();
            Revalidate();
            return true;
        }

        /// Makes any SubLevel the EDITABLE one (MarbleController decides
        /// whether that is allowed) - also counts as reached, like
        /// AdvanceToNextSubLevel.
        public bool GoToSubLevel(int index)
        {
            if (level == null || index < 0 || index >= level.SubLevels.Count || index == activeSubLevelIndex) return false;

            activeSubLevelIndex = index;
            highestReachedSubLevelIndex = Mathf.Max(highestReachedSubLevelIndex, index);
            UpdateActiveSubLevelVisibility();
            Revalidate();
            return true;
        }

        /// Every visual bound to a cell of this SubLevel - card, blocked
        /// cell, empty 15-Puzzle slot, content icon - with its cell, for
        /// SubLevelSwitchAnimator's fly-in/fly-out.
        public void CollectSubLevelVisuals(int index, List<KeyValuePair<Vector2Int, Transform>> into)
        {
            if (pipes == null) return;
            RectInt area = GetSubLevelArea(index);

            for (int y = Mathf.Max(0, area.yMin); y < Mathf.Min(Height, area.yMax); y++)
            {
                for (int x = Mathf.Max(0, area.xMin); x < Mathf.Min(Width, area.xMax); x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    if (blockedVisuals != null && blockedVisuals[x, y] != null) into.Add(new KeyValuePair<Vector2Int, Transform>(coord, blockedVisuals[x, y].transform));
                    if (slotVisuals != null && slotVisuals[x, y] != null) into.Add(new KeyValuePair<Vector2Int, Transform>(coord, slotVisuals[x, y].transform));
                    if (contentIcons != null && contentIcons[x, y] != null) into.Add(new KeyValuePair<Vector2Int, Transform>(coord, contentIcons[x, y].transform));
                    if (pipes[x, y] != null) into.Add(new KeyValuePair<Vector2Int, Transform>(coord, pipes[x, y].transform));
                }
            }
        }

        /// Temporarily shows a non-active SubLevel's visuals (during a
        /// SubLevel switch animation); RefreshSubLevelVisibility undoes it.
        public void ShowSubLevelVisuals(int index)
        {
            List<KeyValuePair<Vector2Int, Transform>> visuals = new List<KeyValuePair<Vector2Int, Transform>>();
            CollectSubLevelVisuals(index, visuals);
            foreach (KeyValuePair<Vector2Int, Transform> visual in visuals) visual.Value.gameObject.SetActive(true);
        }

        public void RefreshSubLevelVisibility() => UpdateActiveSubLevelVisibility();

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
                    bool active = IsInActiveSubLevel(new Vector2Int(x, y));

                    PipeVisual blockedVisual = blockedVisuals != null ? blockedVisuals[x, y] : null;
                    if (blockedVisual != null) blockedVisual.gameObject.SetActive(active);

                    PipeVisual slotVisual = slotVisuals != null ? slotVisuals[x, y] : null;
                    if (slotVisual != null) slotVisual.gameObject.SetActive(active);

                    SpriteRenderer contentIcon = contentIcons != null ? contentIcons[x, y] : null;
                    if (contentIcon != null) contentIcon.gameObject.SetActive(active);

                    PathPipe pipe = pipes[x, y];
                    if (pipe == null) continue;
                    pipe.gameObject.SetActive(active);
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
            highestReachedSubLevelIndex = 0;
            Width = levelData.Width;
            Height = levelData.Height;
            pipes = new PathPipe[Width, Height];
            contents = new CellContentDefinition[Width, Height];
            blocked = new bool[Width, Height];
            blockedVisuals = new PipeVisual[Width, Height];
            slotVisuals = new PipeVisual[Width, Height];
            contentIcons = new SpriteRenderer[Width, Height];
            bool fifteenPuzzle = IsFifteenPuzzle;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    bool isBlocked = levelData.IsBlockedAt(index);
                    blocked[x, y] = isBlocked;
                    if (isBlocked)
                    {
                        blockedVisuals[x, y] = CreateBlockedVisual(new Vector2Int(x, y), levelData.GetBlockedLookAt(index));
                        continue;
                    }

                    contents[x, y] = index < levelData.Contents.Count ? levelData.Contents[index] : null;
                    if (contents[x, y] != null && contents[x, y].Icon != null)
                    {
                        contentIcons[x, y] = CreateContentIcon(new Vector2Int(x, y), contents[x, y].Icon);
                    }
                    PipeDefinition definition = index < levelData.Pipes.Count ? levelData.Pipes[index] : null;

                    // 15-Puzzle (see 0065): a cell without a card stays
                    // genuinely empty - no PathPipe at all - so it's the
                    // free slot cards slide into. Card Swap keeps building
                    // an empty, swappable card there as before.
                    if (fifteenPuzzle)
                    {
                        slotVisuals[x, y] = CreateSlotVisual(new Vector2Int(x, y));
                        if (definition == null) continue;
                    }

                    pipes[x, y] = CreatePipe(new Vector2Int(x, y), definition);
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

        /// This cell's manually-set height override (see 0050,
        /// LevelData.GetHeightOverrideAt) - null if none was set. Used by
        /// TrackBlockSpawner.ResolveStartHeight, which falls back to the
        /// owning SubLevel's own Start Height when this is null.
        public float? GetHeightOverride(Vector2Int coord)
        {
            if (level == null || !IsInBounds(coord)) return null;
            return level.GetHeightOverrideAt(coord.y * Width + coord.x);
        }

        /// The 3D block color for this cell (see 0050) - null means "no
        /// color of its own", i.e. TrackBlockSpawner falls back to its
        /// global terrain color.
        ///
        /// A cell holding a CARD takes that card's own face color
        /// (PipeDefinition.BackgroundColor), so the 3D block always shows
        /// the color of whichever card is lying there at the switch to 3D.
        /// That's deliberately derived rather than stored per cell: the
        /// card carries its color with it, so moving one in the 2D
        /// planning view - or shuffling them with Randomize in the level
        /// editor - can't leave a block colored after a card that has long
        /// since moved away, and there is no second list to keep in sync.
        ///
        /// Only a cell WITHOUT a card - a blocked one, or an empty cell
        /// that just gets a filler block (0047) - uses the per-cell
        /// override authored in the Level Grid Editor
        /// (LevelData.GetBlockColorOverrideAt): there's no card there to
        /// take a color from, and that terrain stays put anyway.
        ///
        /// Since 0068 the owning SubLevel's own base color (Visuals tab,
        /// LevelData.ResolveBlockBaseColor) sits in between: when set, it
        /// replaces the card colors of that SubLevel's blocks, but a
        /// card-less cell's own override still wins over it. Priority:
        /// card-less cell override > SubLevel base color > card color >
        /// spawner terrain color (null).
        public Color? GetBlockColorOverride(Vector2Int coord)
        {
            if (level == null || !IsInBounds(coord)) return null;

            PathPipe pipe = pipes != null ? pipes[coord.x, coord.y] : null;
            bool hasCard = pipe != null && pipe.Definition != null;

            if (!hasCard)
            {
                Color? cellOverride = level.GetBlockColorOverrideAt(coord.y * Width + coord.x);
                if (cellOverride.HasValue) return cellOverride;
            }

            Color? baseColor = level.ResolveBlockBaseColor(SubLevelIndexOrNone(coord));
            if (baseColor.HasValue) return baseColor;

            return hasCard ? pipe.Definition.BackgroundColor : (Color?)null;
        }

        /// This cell's visual effect (see 0068), or null if it has none.
        public CellVisualEffect GetVisualEffect(Vector2Int coord)
        {
            if (level == null || !IsInBounds(coord)) return null;
            return level.GetVisualEffectAt(coord.y * Width + coord.x);
        }

        /// The area a SubLevel-wide visual effect fired from this cell
        /// covers (see 0068): its own SubLevel's Area, or the whole grid
        /// for a level without SubLevels / a cell outside all of them.
        public RectInt GetVisualEffectArea(Vector2Int coord)
        {
            int index = SubLevelIndexOrNone(coord);
            return index >= 0 ? level.SubLevels[index].Area : new RectInt(0, 0, Width, Height);
        }

        private int SubLevelIndexOrNone(Vector2Int coord)
        {
            if (level == null || level.SubLevels.Count == 0) return -1;
            return TryGetSubLevelIndexAt(coord, out int index) ? index : -1;
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

        /// Returns true if a swap actually happened.
        public bool SwapCards(Vector2Int a, Vector2Int b)
        {
            // Content layer is intentionally untouched here — it is bound to
            // the cell coordinate, not to the pipe occupying it.
            if (!IsInBounds(a) || !IsInBounds(b) || a == b) return false;
            if (IsBlocked(a) || IsBlocked(b)) return false;

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
            return true;
        }

        /// A buildable cell without a card - the slot 15-Puzzle cards slide
        /// into (see 0065). Always false in Card Swap, where every
        /// buildable cell holds a (possibly empty) card.
        public bool IsFree(Vector2Int coord)
        {
            return IsInBounds(coord) && !blocked[coord.x, coord.y] && pipes[coord.x, coord.y] == null;
        }

        private PuzzleCell GetPuzzleCell(Vector2Int coord)
        {
            if (IsBlocked(coord)) return PuzzleCell.Blocked;
            PathPipe pipe = pipes[coord.x, coord.y];
            if (pipe == null) return PuzzleCell.Free;
            return pipe.IsLocked ? PuzzleCell.LockedCard : PuzzleCell.Card;
        }

        /// Which cards a 15-Puzzle slide starting at `from` would move (see
        /// FifteenPuzzleRules) - confined to `from`'s own SubLevel, so a
        /// slide never pushes cards across into a neighbouring puzzle.
        /// Direction.None only succeeds if exactly one direction is open
        /// (a plain click, see FifteenPuzzleRules.TryGetOnlySlide); a given
        /// direction only tries that one (a drag).
        public bool TryGetSlide(Vector2Int from, Direction direction, List<Vector2Int> line, out Direction resolvedDirection)
        {
            resolvedDirection = Direction.None;
            if (!IsFifteenPuzzle || !IsInBounds(from)) return false;

            RectInt bounds = GetOwnSubLevelArea(from);
            if (direction == Direction.None)
                return FifteenPuzzleRules.TryGetOnlySlide(from, bounds, GetPuzzleCell, line, out resolvedDirection);

            if (!FifteenPuzzleRules.TryGetSlideLine(from, direction, bounds, GetPuzzleCell, line, out _)) return false;
            resolvedDirection = direction;
            return true;
        }

        /// Whether this card can slide anywhere at all (by click or drag).
        public bool CanSlideAny(Vector2Int from)
        {
            if (!IsFifteenPuzzle || !IsInBounds(from)) return false;
            return FifteenPuzzleRules.CanSlideAny(from, GetOwnSubLevelArea(from), GetPuzzleCell);
        }

        /// Performs a 15-Puzzle slide (see 0066): every card from `from`
        /// up to the free cell moves one step in the resolved direction,
        /// gliding there over slideDuration (see 0067). Like SwapCards,
        /// the content layer stays bound to its cell. Returns true if
        /// anything moved.
        public bool SlideCards(Vector2Int from, Direction direction = Direction.None)
        {
            List<Vector2Int> line = new List<Vector2Int>();
            if (!TryGetSlide(from, direction, line, out Direction resolved)) return false;

            Vector2Int offset = resolved.ToGridOffset();

            // Front of the line (next to the free cell) first, so every
            // card always moves into a cell that has just been vacated.
            for (int i = line.Count - 1; i >= 0; i--)
            {
                Vector2Int source = line[i];
                Vector2Int target = source + offset;
                PathPipe pipe = pipes[source.x, source.y];

                pipes[target.x, target.y] = pipe;
                pipes[source.x, source.y] = null;
                pipe.SetCoord(target);
                pipe.SlideTo(CellToLocalPosition(target), slideDuration);
            }

            Revalidate();
            return true;
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

            UpdateContentIconCoverage();
            return results;
        }

        /// A content icon shows fully on a cell without a pipe card and
        /// slightly transparent (coveredContentIconAlpha) while a card lies
        /// on it. Runs from Revalidate, which every swap/slide/build ends
        /// with. An empty card (no PipeDefinition) doesn't count as covering it.
        private void UpdateContentIconCoverage()
        {
            if (contentIcons == null) return;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    SpriteRenderer icon = contentIcons[x, y];
                    if (icon == null) continue;

                    PathPipe pipe = pipes[x, y];
                    bool covered = pipe != null && pipe.Definition != null;
                    Color color = contentIconTint;
                    if (covered) color.a *= coveredContentIconAlpha;
                    icon.color = color;
                }
            }
        }

        public Vector3 CellToLocalPosition(Vector2Int coord)
        {
            return new Vector3(coord.x * cellSize, coord.y * cellSize, 0f);
        }

        /// The 2D stand-in for a blocked cell - built for EVERY blocked
        /// cell, whether or not a picture was assigned to it (0054). It
        /// used to bail out on a null look, which left an unpictured
        /// blocked cell as a literal hole in the planning view while the
        /// 3D view built solid filler terrain over the very same spot -
        /// the transition between the two read as incomplete.
        /// Deliberately no BoxCollider, unlike CreatePipe: that is the
        /// whole of its non-interactivity, since GridInputHandler only
        /// ever finds a cell by raycasting against one (and SwapCards
        /// refuses a blocked coord on top of that).
        private PipeVisual CreateBlockedVisual(Vector2Int coord, CardLook look)
        {
            GameObject go = new GameObject($"Blocked_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellToLocalPosition(coord);
            go.transform.localScale = Vector3.one * cardScale;

            PipeVisual visual = go.AddComponent<PipeVisual>();
            visual.RefreshBlocked(look, level);
            return visual;
        }

        /// The empty slot of a 15-Puzzle cell (see 0067) - built under
        /// every buildable cell, not just the initially free ones, since
        /// the free cells move with every slide; a card lying on top
        /// covers it completely. No collider, like CreateBlockedVisual -
        /// a free cell is never something to click on directly.
        private PipeVisual CreateSlotVisual(Vector2Int coord)
        {
            GameObject go = new GameObject($"Slot_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellToLocalPosition(coord);
            go.transform.localScale = Vector3.one * cardScale;

            PipeVisual visual = go.AddComponent<PipeVisual>();
            visual.RefreshFreeSlot(level);
            return visual;
        }

        /// The instrument icon of a content cell in the 2D planning view
        /// (see 0069) - its own child of the grid rather than part of the
        /// card, since content belongs to the cell and cards move across
        /// it. Sized to contentIconScale of a card regardless of the
        /// sprite's pixels-per-unit. Hidden with the rest of the grid when
        /// Play switches to 3D (MarbleController deactivates the grid).
        private SpriteRenderer CreateContentIcon(Vector2Int coord, Sprite icon)
        {
            GameObject go = new GameObject($"ContentIcon_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);
            Vector2 offset = contentIconOffset * cardScale;
            go.transform.localPosition = CellToLocalPosition(coord) + new Vector3(offset.x, offset.y, 0f);

            Vector2 size = icon.bounds.size;
            float target = cardScale * contentIconScale;
            float longest = Mathf.Max(size.x, size.y);
            go.transform.localScale = Vector3.one * (longest > 0f ? target / longest : 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = icon;
            renderer.color = contentIconTint;
            renderer.sortingOrder = contentIconSortingOrder;
            return renderer;
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
            pipe.Initialize(definition, coord, level);
            return pipe;
        }
    }
}
