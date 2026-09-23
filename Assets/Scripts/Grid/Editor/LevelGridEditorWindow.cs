using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MarbleOrchestra.Grid.Editor
{
    /// <summary>
    /// Tilemap-style painter for LevelData: pick a layer (Pipe or Content).
    /// Pipe layer: drag a pattern (one of the 15 usable direction
    /// combinations, or the Blocked pattern, see 0049) from the palette
    /// onto a grid cell to place it; drag an already-placed cell onto
    /// another cell to swap their pipes; click a cell to select it and
    /// toggle its Locked/Start/Goal attributes; right-click to erase (a
    /// blocked cell unblocks). Content layer: pick a brush from the
    /// palette of existing assets, click cells to paint, right-click to
    /// erase. Replaces authoring via the flat Inspector list.
    /// </summary>
    public class LevelGridEditorWindow : EditorWindow
    {
        private enum PaintLayer
        {
            Pipe,
            Content
        }

        private const float CellSize = 48f;

        [MenuItem("MarbleOrchestra/Level Grid Editor")]
        public static void Open()
        {
            GetWindow<LevelGridEditorWindow>("Level Grid Editor");
        }

        public static void OpenFor(LevelData target)
        {
            LevelGridEditorWindow window = GetWindow<LevelGridEditorWindow>("Level Grid Editor");
            window.Bind(target);
        }

        private const string GeneratedPipeFolder = "Assets/Levels/Pipes";
        private const string GeneratedContentFolder = "Assets/Levels/Contents";
        private const string PipePatternDragKey = "MarbleOrchestra.PipePattern";
        private const string PipeCellDragKey = "MarbleOrchestra.PipeCellSwap";
        private const string PipeBlockDragKey = "MarbleOrchestra.PipeBlock";

        /// The 15 connection combinations with at least one open side (the
        /// all-closed "None" pattern isn't a usable pipe), in a fixed
        /// 5-row x 3-column layout specified by the user rather than a
        /// derived/sorted order (see 0049 follow-up).
        private static readonly Direction[] AllDirectionPatterns =
        {
            Direction.Up, Direction.Left, Direction.Down,
            Direction.Right, Direction.Left | Direction.Right, Direction.Up | Direction.Down,
            Direction.Down | Direction.Right, Direction.Left | Direction.Down | Direction.Right, Direction.Left | Direction.Down,
            Direction.Up | Direction.Right | Direction.Down, Direction.Up | Direction.Right | Direction.Down | Direction.Left, Direction.Up | Direction.Left | Direction.Down,
            Direction.Up | Direction.Right, Direction.Left | Direction.Up | Direction.Right, Direction.Left | Direction.Up,
        };

        private LevelData level;
        private PaintLayer activeLayer;
        private PipeDefinition[] availablePipes = new PipeDefinition[0];
        private CellContentDefinition[] availableContents = new CellContentDefinition[0];
        private UnityEngine.Object selectedBrush;
        private int pendingWidth;
        private int pendingHeight;
        private Vector2 paletteScroll;

        private Color customBackgroundColor = new Color(0.15f, 0.15f, 0.15f);

        private bool useCustomContent;
        private AudioClip customClip;
        private Color customFlashColor = Color.white;

        private bool subLevelsFoldout = true;
        private int selectedSubLevelIndex = -1;
        private Rect[,] cellRects;
        private int? selectedCellIndex; // cell picked in the Pipe layer, awaiting attribute toggles - see DrawCellAttributesPanel
        private int? pipeDragCandidateIndex; // cell pressed in the Pipe layer, may turn into a swap-drag - see HandlePipeCellDragStart

        private static readonly Color[] SubLevelPalette =
        {
            new Color(0.95f, 0.35f, 0.35f),
            new Color(0.35f, 0.75f, 0.95f),
            new Color(0.95f, 0.75f, 0.25f),
            new Color(0.55f, 0.85f, 0.45f),
            new Color(0.75f, 0.45f, 0.95f),
            new Color(0.95f, 0.55f, 0.75f),
        };

        private void OnEnable()
        {
            RefreshPalette();
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnFocus()
        {
            RefreshPalette();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is LevelData selected && selected != level)
            {
                Bind(selected);
                Repaint();
            }
        }

        private void Bind(LevelData target)
        {
            level = target;
            if (level == null) return;

            pendingWidth = level.Width;
            pendingHeight = level.Height;
            selectedSubLevelIndex = -1;
            cellRects = null;
            selectedCellIndex = null;
            pipeDragCandidateIndex = null;

            int required = level.Width * level.Height;
            if (level.Pipes.Count != required || level.Contents.Count != required || level.Blocked.Count != required || level.HeightOverrides.Count != required)
            {
                Undo.RecordObject(level, "Fix Level Grid List Sizes");
                level.EnsureListSizes();
                EditorUtility.SetDirty(level);
            }
        }

        private void RefreshPalette()
        {
            availablePipes = LoadAllAssets<PipeDefinition>();
            availableContents = LoadAllAssets<CellContentDefinition>();
        }

        private static T[] LoadAllAssets<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> results = new List<T>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) results.Add(asset);
            }
            return results.ToArray();
        }

        private void OnGUI()
        {
            LevelData newLevel = (LevelData)EditorGUILayout.ObjectField("Level", level, typeof(LevelData), false);
            if (newLevel != level)
            {
                Bind(newLevel);
            }

            if (level == null)
            {
                EditorGUILayout.HelpBox("Select or assign a LevelData asset to edit its grid.", MessageType.Info);
                return;
            }

            DrawResizeControls();
            EditorGUILayout.Space();

            DrawSubLevelPanel();
            EditorGUILayout.Space();

            activeLayer = (PaintLayer)GUILayout.Toolbar((int)activeLayer, new[] { "Pipe", "Content" });
            if (activeLayer != PaintLayer.Pipe)
            {
                selectedCellIndex = null;
                pipeDragCandidateIndex = null;
            }

            HandlePipeCellDragStart();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            DrawPalette();
            DrawGrid();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResizeControls()
        {
            EditorGUILayout.BeginHorizontal();
            pendingWidth = EditorGUILayout.IntField("Width", pendingWidth);
            pendingHeight = EditorGUILayout.IntField("Height", pendingHeight);
            if (GUILayout.Button("Apply Resize", GUILayout.Width(100)))
            {
                Undo.RecordObject(level, "Resize Level Grid");
                level.ResizeGrid(pendingWidth, pendingHeight);
                EditorUtility.SetDirty(level);
                pendingWidth = level.Width;
                pendingHeight = level.Height;
            }
            EditorGUILayout.EndHorizontal();
        }

        /// Lists every SubLevel (see 0046) as one row: a color swatch
        /// matching its overlay on the grid (see DrawSubLevelOverlays),
        /// name, area (x/y/w/h), reorder buttons (list order IS
        /// progression order) and remove. Mutations that would change the
        /// list while it's being iterated (reorder/remove) are collected
        /// and applied after the loop instead, since IMGUI re-lays-out the
        /// same frame it's drawn in.
        private void DrawSubLevelPanel()
        {
            subLevelsFoldout = EditorGUILayout.Foldout(subLevelsFoldout, "SubLevels", true);
            if (!subLevelsFoldout) return;

            int removeIndex = -1;
            int moveFrom = -1;
            int moveTo = -1;

            for (int i = 0; i < level.SubLevels.Count; i++)
            {
                SubLevelDefinition subLevel = level.SubLevels[i];
                EditorGUILayout.BeginHorizontal(selectedSubLevelIndex == i ? EditorStyles.helpBox : GUIStyle.none);

                Rect swatch = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
                EditorGUI.DrawRect(swatch, SubLevelColor(i));

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedSubLevelIndex = i;
                }

                string newName = EditorGUILayout.TextField(subLevel.Name, GUILayout.Width(100));
                if (newName != subLevel.Name)
                {
                    Undo.RecordObject(level, "Rename SubLevel");
                    level.SetSubLevelName(i, newName);
                    EditorUtility.SetDirty(level);
                }

                RectInt area = subLevel.Area;
                EditorGUILayout.LabelField("x", GUILayout.Width(10));
                int x = EditorGUILayout.IntField(area.x, GUILayout.Width(30));
                EditorGUILayout.LabelField("y", GUILayout.Width(10));
                int y = EditorGUILayout.IntField(area.y, GUILayout.Width(30));
                EditorGUILayout.LabelField("w", GUILayout.Width(12));
                int w = EditorGUILayout.IntField(area.width, GUILayout.Width(30));
                EditorGUILayout.LabelField("h", GUILayout.Width(12));
                int h = EditorGUILayout.IntField(area.height, GUILayout.Width(30));

                RectInt newArea = new RectInt(x, y, Mathf.Max(1, w), Mathf.Max(1, h));
                if (!newArea.Equals(area))
                {
                    Undo.RecordObject(level, "Resize SubLevel Area");
                    level.SetSubLevelArea(i, newArea);
                    EditorUtility.SetDirty(level);
                }

                EditorGUILayout.LabelField(new GUIContent("Y0", "Fallback world/spawner-local height for this SubLevel's cells (TrackBlockSpawner.ResolveStartHeight) - a per-cell Custom Height set on a Start pipe or blocked cell (Selected Cell panel) takes priority over this."), GUILayout.Width(20));
                float startHeight = EditorGUILayout.FloatField(subLevel.StartHeight, GUILayout.Width(40));
                if (!Mathf.Approximately(startHeight, subLevel.StartHeight))
                {
                    Undo.RecordObject(level, "Set SubLevel Start Height");
                    level.SetSubLevelStartHeight(i, startHeight);
                    EditorUtility.SetDirty(level);
                }

                GUILayout.FlexibleSpace();

                // Scoped to this SubLevel's own Area only (see
                // RandomizePipesInArea) - shuffling across the whole grid
                // would mix pipes between SubLevels that are meant to be
                // independent puzzles.
                if (GUILayout.Button("Randomize", GUILayout.Width(80))) RandomizePipesInArea(area);

                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(20))) { moveFrom = i; moveTo = i - 1; }
                GUI.enabled = i < level.SubLevels.Count - 1;
                if (GUILayout.Button("↓", GUILayout.Width(20))) { moveFrom = i; moveTo = i + 1; }
                GUI.enabled = true;

                if (GUILayout.Button("Remove", GUILayout.Width(60))) removeIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            if (moveFrom >= 0)
            {
                Undo.RecordObject(level, "Reorder SubLevels");
                level.MoveSubLevel(moveFrom, moveTo);
                selectedSubLevelIndex = moveTo;
                EditorUtility.SetDirty(level);
            }

            if (removeIndex >= 0)
            {
                Undo.RecordObject(level, "Remove SubLevel");
                level.RemoveSubLevelAt(removeIndex);
                if (selectedSubLevelIndex >= level.SubLevels.Count) selectedSubLevelIndex = level.SubLevels.Count - 1;
                EditorUtility.SetDirty(level);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add SubLevel", GUILayout.Width(120)))
            {
                Undo.RecordObject(level, "Add SubLevel");
                level.AddSubLevel($"SubLevel {level.SubLevels.Count + 1}", new RectInt(0, 0, level.Width, level.Height));
                selectedSubLevelIndex = level.SubLevels.Count - 1;
                EditorUtility.SetDirty(level);
            }

            // No SubLevels defined at all: the whole grid acts as a single
            // implicit one (same convention as PathGrid.ActiveSubLevelArea
            // at runtime), so Randomize still needs a home here rather
            // than just disappearing for these simpler, un-sublevel'd
            // level assets.
            if (level.SubLevels.Count == 0 && GUILayout.Button("Randomize (whole grid)", GUILayout.Width(150)))
            {
                RandomizePipesInArea(new RectInt(0, 0, level.Width, level.Height));
            }
            EditorGUILayout.EndHorizontal();
        }

        private static Color SubLevelColor(int index)
        {
            return SubLevelPalette[index % SubLevelPalette.Length];
        }

        private void DrawPalette()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(190));

            if (activeLayer == PaintLayer.Pipe)
            {
                DrawPipePalette();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("Contents", EditorStyles.boldLabel);

            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(160));

            bool eraserSelected = !useCustomContent && selectedBrush == null;
            if (DrawPaletteEntry("Eraser", Color.clear, eraserSelected))
            {
                selectedBrush = null;
                useCustomContent = false;
            }

            foreach (CellContentDefinition content in availableContents)
            {
                bool selected = !useCustomContent && selectedBrush == content;
                string label = $"{content.ContentId} ({content.GetType().Name})";
                if (DrawPaletteEntry(label, new Color(0.4f, 0.6f, 0.9f), selected))
                {
                    selectedBrush = content;
                    useCustomContent = false;
                }
            }

            EditorGUILayout.EndScrollView();

            DrawCustomContentBuilder();

            EditorGUILayout.EndVertical();
        }

        /// Palette + workflow for the Pipe layer (see 0049): all direction
        /// patterns (plus the Blocked pattern, see follow-up) are listed as
        /// drag sources instead of a flat list of existing PipeDefinition
        /// assets, shown in full (no scrolling - there's room for all of
        /// them at once); Locked/Start/Goal are edited per-selected-cell
        /// afterwards instead of being baked into the brush before
        /// painting; and swapping two cells is a drag directly on the grid
        /// instead of a separate layer/tab.
        private void DrawPipePalette()
        {
            EditorGUILayout.LabelField("Pipe Patterns", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag a pattern onto a cell to place it, or the Blocked tile to mark it unbuildable. Drag an already-placed cell onto another to swap them. Click a cell to select it and toggle its attributes below. Right-click a cell to clear it (a blocked cell unblocks).", MessageType.None);

            customBackgroundColor = EditorGUILayout.ColorField("New Pipe Color", customBackgroundColor);

            DrawPatternGrid();

            EditorGUILayout.Space();
            DrawBlockedPatternRow();

            EditorGUILayout.Space();
            if (GUILayout.Button("Block Empty Cells")) FillEmptyCellsWithBlocked();

            DrawCellAttributesPanel();
        }

        private void DrawPatternGrid()
        {
            const float boxSize = 44f;
            const int columns = 3;

            for (int i = 0; i < AllDirectionPatterns.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int patternIndex = i + c;
                    if (patternIndex >= AllDirectionPatterns.Length)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }
                    DrawPatternEntry(AllDirectionPatterns[patternIndex], boxSize);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPatternEntry(Direction pattern, float boxSize)
        {
            Rect rect = GUILayoutUtility.GetRect(boxSize, boxSize, GUILayout.Width(boxSize), GUILayout.Height(boxSize));
            EditorGUI.DrawRect(rect, customBackgroundColor);

            Rect hub = new Rect(rect.x + rect.width * 0.35f, rect.y + rect.height * 0.35f, rect.width * 0.3f, rect.height * 0.3f);
            EditorGUI.DrawRect(hub, Color.white);
            DrawConnectionArms(rect, pattern, Color.white);
            DrawGridLines(rect);

            HandlePatternDragSource(rect, pattern);
        }

        /// Starts an in-window drag carrying the pattern's Direction combo
        /// as generic data - picked up by HandlePipeDrop on whichever grid
        /// cell the drag is released over.
        private static void HandlePatternDragSource(Rect rect, Direction pattern)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition)) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(PipePatternDragKey, pattern);
            DragAndDrop.StartDrag("Pipe Pattern");
            e.Use();
        }

        /// The Blocked "pattern" (see 0049 follow-up): drawn apart from
        /// the direction-pattern grid since it isn't a connections
        /// combination, but dragged onto a cell the same way to mark it
        /// unbuildable - replaces the old dedicated Blocked layer/tab.
        private void DrawBlockedPatternRow()
        {
            const float boxSize = 44f;

            EditorGUILayout.BeginHorizontal();
            DrawBlockedPatternEntry(boxSize);
            EditorGUILayout.LabelField("Blocked", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBlockedPatternEntry(float boxSize)
        {
            Rect rect = GUILayoutUtility.GetRect(boxSize, boxSize, GUILayout.Width(boxSize), GUILayout.Height(boxSize));
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
            DrawBlockedOverlay(rect);
            DrawGridLines(rect);

            HandleBlockedPatternDragSource(rect);
        }

        /// Starts an in-window drag carrying no payload beyond the
        /// PipeBlockDragKey's presence - picked up by HandlePipeDrop on
        /// whichever grid cell the drag is released over.
        private static void HandleBlockedPatternDragSource(Rect rect)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition)) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(PipeBlockDragKey, true);
            DragAndDrop.StartDrag("Block Cell");
            e.Use();
        }

        /// Bulk action requested alongside dropping the dedicated Blocked
        /// layer/tab (see 0049 follow-up): blocks every cell that has
        /// neither a pipe nor content yet, across the whole grid (not
        /// scoped to a SubLevel - unlike Randomize, blocking is a one-off
        /// setup step rather than something done per-puzzle-area).
        private void FillEmptyCellsWithBlocked()
        {
            bool changed = false;
            int cellCount = level.Width * level.Height;

            for (int index = 0; index < cellCount; index++)
            {
                if (level.IsBlockedAt(index)) continue;

                bool hasPipe = index < level.Pipes.Count && level.Pipes[index] != null;
                bool hasContent = index < level.Contents.Count && level.Contents[index] != null;
                if (hasPipe || hasContent) continue;

                if (!changed)
                {
                    Undo.RecordObject(level, "Block Empty Cells");
                    changed = true;
                }
                level.SetBlockedAt(index, true);
            }

            if (!changed) return;
            EditorUtility.SetDirty(level);
            Repaint();
        }

        /// Shown below the pattern palette while the Pipe layer is active:
        /// lets the user flip Locked/Start/Goal for whichever cell was
        /// last clicked (see HandleCellClick), by resolving/creating a
        /// PipeDefinition variant with the same connections+color but the
        /// new attributes (same mechanism as GetOrCreateCustomPipe already
        /// used for brush painting). A Start pipe or a blocked cell also
        /// gets a Custom Height field here (see 0050) - the per-cell
        /// override TrackBlockSpawner.ResolveStartHeight now prefers over
        /// the owning SubLevel's own Start Height.
        private void DrawCellAttributesPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Cell", EditorStyles.boldLabel);

            if (!selectedCellIndex.HasValue)
            {
                EditorGUILayout.HelpBox("Click a cell to select it.", MessageType.None);
                return;
            }

            int index = selectedCellIndex.Value;

            if (level.IsBlockedAt(index))
            {
                EditorGUILayout.HelpBox("Blocked cell. Isolated blocked/unused regions the surrounding terrain can't interpolate a height for settle at the height below, falling back to the SubLevel's own Start Height if none is set.", MessageType.None);
                DrawHeightOverrideField(index);
                return;
            }

            if (index >= level.Pipes.Count || level.Pipes[index] == null)
            {
                EditorGUILayout.HelpBox("Selected cell has no pipe - drag a pattern onto it first.", MessageType.None);
                return;
            }

            PipeDefinition pipe = level.Pipes[index];
            bool wasLocked = pipe.Locked;
            bool wasStart = pipe.Role == PipeRole.Start;
            bool wasGoal = pipe.Role == PipeRole.Goal;

            bool nowLocked = GUILayout.Toggle(wasLocked, "Locked", EditorStyles.miniButton);

            EditorGUILayout.BeginHorizontal();
            bool nowStart = GUILayout.Toggle(wasStart, "Start", EditorStyles.miniButton);
            bool nowGoal = GUILayout.Toggle(wasGoal, "Goal", EditorStyles.miniButton);
            EditorGUILayout.EndHorizontal();

            // Only one of Start/Goal toggles can have actually changed
            // this frame (a single click), so whichever one just turned on
            // wins the (mutually exclusive) Role; turning the active one
            // off falls back to Normal.
            PipeRole newRole = pipe.Role;
            if (nowStart && !wasStart) newRole = PipeRole.Start;
            else if (nowGoal && !wasGoal) newRole = PipeRole.Goal;
            else if (wasStart && !nowStart) newRole = PipeRole.Normal;
            else if (wasGoal && !nowGoal) newRole = PipeRole.Normal;

            if (nowLocked != wasLocked || newRole != pipe.Role)
            {
                PipeDefinition updated = GetOrCreateCustomPipe(pipe.Connections, pipe.BackgroundColor, newRole, nowLocked);
                Undo.RecordObject(level, "Set Pipe Attributes");
                level.SetPipeAt(index, updated);
                EditorUtility.SetDirty(level);
                Repaint();
            }

            if (newRole == PipeRole.Start)
            {
                DrawHeightOverrideField(index);
            }
        }

        /// A toggle-gated float field for one cell's height override (see
        /// LevelData.SetHeightOverrideAt/0050): off leaves the cell at
        /// "not set" (null), so ResolveStartHeight falls back to the
        /// owning SubLevel's own Start Height.
        private void DrawHeightOverrideField(int index)
        {
            float? current = level.GetHeightOverrideAt(index);
            bool hadOverride = current.HasValue;

            bool wantsOverride = EditorGUILayout.Toggle("Custom Height", hadOverride);

            if (!wantsOverride)
            {
                if (hadOverride)
                {
                    Undo.RecordObject(level, "Clear Cell Height");
                    level.SetHeightOverrideAt(index, null);
                    EditorUtility.SetDirty(level);
                }
                return;
            }

            float newValue = EditorGUILayout.FloatField("Height", current ?? 0f);
            if (!hadOverride || !Mathf.Approximately(newValue, current.Value))
            {
                Undo.RecordObject(level, "Set Cell Height");
                level.SetHeightOverrideAt(index, newValue);
                EditorUtility.SetDirty(level);
            }
        }

        private void DrawCustomContentBuilder()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(useCustomContent ? EditorStyles.helpBox : GUIStyle.none);
            EditorGUILayout.LabelField("Custom Sound Trigger", EditorStyles.boldLabel);

            customClip = (AudioClip)EditorGUILayout.ObjectField("Clip", customClip, typeof(AudioClip), false);
            customFlashColor = EditorGUILayout.ColorField("Flash Color", customFlashColor);

            if (GUILayout.Button(useCustomContent ? "Custom (active)" : "Use Custom"))
            {
                useCustomContent = true;
                selectedBrush = null;
            }

            EditorGUILayout.EndVertical();
        }

        private bool DrawPaletteEntry(string label, Color swatchColor, bool selected)
        {
            EditorGUILayout.BeginHorizontal(selected ? EditorStyles.helpBox : GUIStyle.none);

            Rect swatchRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
            EditorGUI.DrawRect(swatchRect, swatchColor.a > 0f ? swatchColor : new Color(0.2f, 0.2f, 0.2f));

            bool clicked = GUILayout.Button(label, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
            return clicked;
        }

        private void DrawGrid()
        {
            if (cellRects == null || cellRects.GetLength(0) != level.Width || cellRects.GetLength(1) != level.Height)
            {
                cellRects = new Rect[level.Width, level.Height];
            }

            EditorGUILayout.BeginVertical();

            for (int y = level.Height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < level.Width; x++)
                {
                    DrawCell(x, y);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            DrawSubLevelOverlays();
        }

        private void DrawCell(int x, int y)
        {
            int index = y * level.Width + x;
            Rect rect = GUILayoutUtility.GetRect(CellSize, CellSize, GUILayout.Width(CellSize), GUILayout.Height(CellSize));
            cellRects[x, y] = rect;

            bool isBlocked = level.IsBlockedAt(index);
            PipeDefinition pipe = isBlocked || index >= level.Pipes.Count ? null : level.Pipes[index];
            CellContentDefinition content = isBlocked || index >= level.Contents.Count ? null : level.Contents[index];

            Color background = pipe != null ? pipe.BackgroundColor : new Color(0.18f, 0.18f, 0.18f);
            EditorGUI.DrawRect(rect, background);

            if (pipe != null)
            {
                Rect hub = new Rect(rect.x + rect.width * 0.35f, rect.y + rect.height * 0.35f, rect.width * 0.3f, rect.height * 0.3f);
                EditorGUI.DrawRect(hub, pipe.Color);
                DrawConnectionArms(rect, pipe.Connections, pipe.Color);

                if (pipe.Role != PipeRole.Normal)
                {
                    DrawRoleBadge(rect, pipe.Role);
                }

                if (pipe.Locked)
                {
                    DrawLockedBorder(rect);
                }
            }

            if (content != null)
            {
                Rect marker = new Rect(rect.x + 2, rect.y + 2, 14, 14);
                EditorGUI.DrawRect(marker, new Color(0f, 0f, 0f, 0.55f));
                GUI.Label(marker, content.Label, GetContentLabelStyle());
            }

            if (isBlocked)
            {
                DrawBlockedOverlay(rect);
            }

            if (activeLayer == PaintLayer.Pipe && selectedCellIndex == index)
            {
                DrawSelectedCellBorder(rect);
            }

            DrawGridLines(rect);

            HandleCellEvents(rect, index);
        }

        private static void DrawBlockedOverlay(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.6f));

            Handles.BeginGUI();
            Handles.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMax));
            Handles.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMin, rect.yMax));
            Handles.EndGUI();
        }

        /// Highlights the cell currently selected for attribute toggling
        /// (see DrawCellAttributesPanel) - a distinct magenta so it reads
        /// apart from Locked (orange) and Blocked (red X).
        private static void DrawSelectedCellBorder(Rect rect)
        {
            const float thickness = 3f;
            Color color = new Color(1f, 0.3f, 0.85f);

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        /// Draws a colored border + name label over each SubLevel's own
        /// area (see 0046), using the cell rects DrawCell just stored -
        /// drawn as a post-pass after the whole grid, so overlays never
        /// get painted over by a later cell's own background.
        private void DrawSubLevelOverlays()
        {
            for (int i = 0; i < level.SubLevels.Count; i++)
            {
                SubLevelDefinition subLevel = level.SubLevels[i];
                RectInt area = ClampAreaToGrid(subLevel.Area);
                if (area.width <= 0 || area.height <= 0) continue;

                Rect bottomLeft = cellRects[area.xMin, area.yMin];
                Rect topRight = cellRects[area.xMax - 1, area.yMax - 1];
                Rect screenRect = Rect.MinMaxRect(bottomLeft.xMin, topRight.yMin, topRight.xMax, bottomLeft.yMax);

                Color color = SubLevelColor(i);
                DrawSubLevelBorder(screenRect, color, i == selectedSubLevelIndex ? 3f : 2f);
                GUI.Label(new Rect(screenRect.x + 3, screenRect.y + 2, screenRect.width - 6, 16), subLevel.Name, BuildSubLevelLabelStyle(color));
            }
        }

        private RectInt ClampAreaToGrid(RectInt area)
        {
            int xMin = Mathf.Clamp(area.xMin, 0, level.Width - 1);
            int yMin = Mathf.Clamp(area.yMin, 0, level.Height - 1);
            int xMax = Mathf.Clamp(area.xMax, xMin + 1, level.Width);
            int yMax = Mathf.Clamp(area.yMax, yMin + 1, level.Height);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static void DrawSubLevelBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static GUIStyle BuildSubLevelLabelStyle(Color color)
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = color }
            };
        }

        private static void DrawGridLines(Rect rect)
        {
            Color color = new Color(1f, 1f, 1f, 0.5f);

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private GUIStyle contentLabelStyle;

        private GUIStyle GetContentLabelStyle()
        {
            if (contentLabelStyle == null)
            {
                contentLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }
            return contentLabelStyle;
        }

        private GUIStyle roleLabelStyle;

        private GUIStyle GetRoleLabelStyle()
        {
            if (roleLabelStyle == null)
            {
                roleLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.black }
                };
            }
            return roleLabelStyle;
        }

        private void DrawRoleBadge(Rect rect, PipeRole role)
        {
            Rect badge = new Rect(rect.xMax - 16, rect.y + 2, 14, 14);
            Color badgeColor = role == PipeRole.Start ? new Color(0.2f, 0.8f, 0.3f) : new Color(1f, 0.84f, 0.2f);
            EditorGUI.DrawRect(badge, badgeColor);
            GUI.Label(badge, role == PipeRole.Start ? "S" : "G", GetRoleLabelStyle());
        }

        private static void DrawLockedBorder(Rect rect)
        {
            const float thickness = 3f;
            Color color = new Color(1f, 0.45f, 0f);

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawConnectionArms(Rect rect, Direction connections, Color armColor)
        {
            if (connections == Direction.None) return;

            float thickness = rect.width * 0.16f;
            float armLength = rect.width * 0.35f;

            if ((connections & Direction.Up) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x + rect.width * 0.5f - thickness * 0.5f, rect.y, thickness, armLength), armColor);
            }

            if ((connections & Direction.Down) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x + rect.width * 0.5f - thickness * 0.5f, rect.yMax - armLength, thickness, armLength), armColor);
            }

            if ((connections & Direction.Left) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height * 0.5f - thickness * 0.5f, armLength, thickness), armColor);
            }

            if ((connections & Direction.Right) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.xMax - armLength, rect.y + rect.height * 0.5f - thickness * 0.5f, armLength, thickness), armColor);
            }
        }

        private void HandleCellEvents(Rect rect, int index)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            if (activeLayer == PaintLayer.Pipe && HandlePipeDrop(e, index)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                HandleCellClick(index);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                ClearCell(index);
                e.Use();
            }
        }

        /// Drop target side of both Pipe-layer drags: a pattern dropped
        /// from the palette (see HandlePatternDragSource) places a fresh
        /// pipe, a cell dropped from elsewhere on the grid (see
        /// HandlePipeCellDragStart) swaps the two cells' pipes. Returns
        /// true once it has consumed the event (drag hover or drop), so
        /// HandleCellEvents skips the normal click handling for that event.
        private bool HandlePipeDrop(Event e, int index)
        {
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return false;

            if (DragAndDrop.GetGenericData(PipePatternDragKey) is Direction pattern)
            {
                if (level.IsBlockedAt(index))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    e.Use();
                    return true;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    PlacePipePattern(index, pattern);
                }
                e.Use();
                return true;
            }

            if (DragAndDrop.GetGenericData(PipeCellDragKey) is int sourceIndex)
            {
                if (sourceIndex == index || level.IsBlockedAt(index))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    e.Use();
                    return true;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    SwapPipes(sourceIndex, index);
                    selectedCellIndex = index;
                    Repaint();
                }
                e.Use();
                return true;
            }

            if (DragAndDrop.GetGenericData(PipeBlockDragKey) != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    Undo.RecordObject(level, "Block Cell");
                    level.SetBlockedAt(index, true);
                    EditorUtility.SetDirty(level);
                    if (selectedCellIndex == index) selectedCellIndex = null;
                    Repaint();
                }
                e.Use();
                return true;
            }

            return false;
        }

        private void HandleCellClick(int index)
        {
            if (activeLayer == PaintLayer.Pipe)
            {
                selectedCellIndex = index;
                pipeDragCandidateIndex = !level.IsBlockedAt(index) && index < level.Pipes.Count && level.Pipes[index] != null
                    ? index
                    : (int?)null;
                Repaint();
                return;
            }

            ApplyBrush(index);
        }

        /// Turns a pressed Pipe cell (see HandleCellClick) into a swap-drag
        /// once the mouse actually moves - called once per OnGUI (not per
        /// cell) since the resulting MouseDrag event is only visible to
        /// whichever cell rect currently contains the pointer, which by
        /// then is no longer the source cell. The drop side lives in
        /// HandlePipeDrop, which does run per-cell since DragUpdated/
        /// DragPerform need the target cell's index.
        private void HandlePipeCellDragStart()
        {
            if (!pipeDragCandidateIndex.HasValue) return;

            Event e = Event.current;
            if (e.type == EventType.MouseDrag)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(PipeCellDragKey, pipeDragCandidateIndex.Value);
                DragAndDrop.StartDrag("Swap Pipe");
                pipeDragCandidateIndex = null;
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                pipeDragCandidateIndex = null;
            }
        }

        /// Places a fresh Normal/unlocked pipe of the dropped pattern
        /// (re-dropping onto an already-placed cell resets its attributes -
        /// use DrawCellAttributesPanel afterwards to set Locked/Start/Goal).
        private void PlacePipePattern(int index, Direction pattern)
        {
            if (level.IsBlockedAt(index)) return;

            PipeDefinition pipe = GetOrCreateCustomPipe(pattern, customBackgroundColor, PipeRole.Normal, false);

            Undo.RecordObject(level, "Place Pipe");
            level.SetPipeAt(index, pipe);
            EditorUtility.SetDirty(level);
            selectedCellIndex = index;
            Repaint();
        }

        private void ApplyBrush(int index)
        {
            if (level.IsBlockedAt(index)) return;

            Undo.RecordObject(level, "Paint Cell");
            CellContentDefinition content = useCustomContent
                ? GetOrCreateCustomContent(customClip, customFlashColor)
                : selectedBrush as CellContentDefinition;
            level.SetContentAt(index, content);
            EditorUtility.SetDirty(level);
            Repaint();
        }

        private PipeDefinition GetOrCreateCustomPipe(Direction connections, Color backgroundColor, PipeRole role, bool locked)
        {
            foreach (PipeDefinition existing in availablePipes)
            {
                if (existing.Connections == connections &&
                    existing.BackgroundColor == backgroundColor &&
                    existing.Role == role &&
                    existing.Locked == locked)
                {
                    return existing;
                }
            }

            return CreateCustomPipeAsset(connections, backgroundColor, role, locked);
        }

        private PipeDefinition CreateCustomPipeAsset(Direction connections, Color backgroundColor, PipeRole role, bool locked)
        {
            PipeDefinition asset = CreateInstance<PipeDefinition>();

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("connections").intValue = (int)connections;
            serialized.FindProperty("backgroundColor").colorValue = backgroundColor;
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.FindProperty("locked").boolValue = locked;
            string pipeId = BuildPipeId(connections, role, locked);
            serialized.FindProperty("pipeId").stringValue = pipeId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder(GeneratedPipeFolder))
            {
                AssetDatabase.CreateFolder("Assets/Levels", "Pipes");
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedPipeFolder}/Pipe_{pipeId}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            RefreshPalette();
            return asset;
        }

        private static string BuildPipeId(Direction connections, PipeRole role, bool locked)
        {
            string id = connections == Direction.None ? "None" : connections.ToString().Replace(", ", "");
            if (role != PipeRole.Normal) id += $"_{role}";
            if (locked) id += "_Locked";
            return id;
        }

        private SoundTriggerContent GetOrCreateCustomContent(AudioClip clip, Color flashColor)
        {
            foreach (CellContentDefinition existing in availableContents)
            {
                if (existing is SoundTriggerContent sound &&
                    sound.Clip == clip && sound.FlashColor == flashColor)
                {
                    return sound;
                }
            }

            return CreateCustomContentAsset(clip, flashColor);
        }

        private SoundTriggerContent CreateCustomContentAsset(AudioClip clip, Color flashColor)
        {
            SoundTriggerContent asset = CreateInstance<SoundTriggerContent>();

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("clip").objectReferenceValue = clip;
            serialized.FindProperty("flashColor").colorValue = flashColor;
            string contentId = BuildContentId(clip);
            serialized.FindProperty("contentId").stringValue = contentId;
            serialized.FindProperty("label").stringValue = BuildContentLabel(contentId);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder(GeneratedContentFolder))
            {
                AssetDatabase.CreateFolder("Assets/Levels", "Contents");
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedContentFolder}/Sound_{contentId}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            RefreshPalette();
            return asset;
        }

        private static string BuildContentId(AudioClip clip)
        {
            return clip != null ? clip.name : "Empty";
        }

        private static string BuildContentLabel(string contentId)
        {
            return contentId.Length > 0 ? contentId.Substring(0, 1).ToUpperInvariant() : "?";
        }

        /// Shuffles pipes among the free (unblocked, unlocked) cells of a
        /// single SubLevel's own Area (or the whole grid, for a level that
        /// doesn't use SubLevels at all - see DrawSubLevelPanel) - never
        /// across the whole grid regardless of SubLevel boundaries, since
        /// that would mix pipes between SubLevels meant to be independent
        /// puzzles.
        private void RandomizePipesInArea(RectInt area)
        {
            List<int> freeSlots = new List<int>();
            List<PipeDefinition> pipesToShuffle = new List<PipeDefinition>();

            for (int y = area.yMin; y < area.yMax; y++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    if (x < 0 || x >= level.Width || y < 0 || y >= level.Height) continue;

                    int index = y * level.Width + x;
                    if (level.IsBlockedAt(index)) continue;

                    PipeDefinition pipe = index < level.Pipes.Count ? level.Pipes[index] : null;
                    if (pipe != null && pipe.Locked) continue;

                    freeSlots.Add(index);
                    if (pipe != null) pipesToShuffle.Add(pipe);
                }
            }

            if (pipesToShuffle.Count == 0 || freeSlots.Count < 2) return;

            for (int i = freeSlots.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (freeSlots[i], freeSlots[j]) = (freeSlots[j], freeSlots[i]);
            }

            Undo.RecordObject(level, "Randomize Pipes");
            for (int i = 0; i < freeSlots.Count; i++)
            {
                PipeDefinition pipe = i < pipesToShuffle.Count ? pipesToShuffle[i] : null;
                level.SetPipeAt(freeSlots[i], pipe);
            }
            EditorUtility.SetDirty(level);
            Repaint();
        }

        /// Right-click erase: a blocked cell always unblocks first (there's
        /// no separate Blocked layer any more to target it directly, see
        /// 0049 follow-up), otherwise clears whatever the active layer owns.
        private void ClearCell(int index)
        {
            Undo.RecordObject(level, "Clear Cell");
            if (level.IsBlockedAt(index))
            {
                level.SetBlockedAt(index, false);
            }
            else if (activeLayer == PaintLayer.Pipe)
            {
                level.SetPipeAt(index, null);
            }
            else
            {
                level.SetContentAt(index, null);
            }
            EditorUtility.SetDirty(level);
            Repaint();
        }

        /// Swaps two cells' pipes - triggered by dragging one Pipe cell
        /// onto another on the grid (see HandlePipeDrop/
        /// HandlePipeCellDragStart, 0049 follow-up), the level-editor
        /// equivalent of GridInputHandler's runtime click-click swap,
        /// without needing to enter Play mode to test a reorder.
        private void SwapPipes(int indexA, int indexB)
        {
            if (indexA == indexB) return;

            PipeDefinition pipeA = indexA < level.Pipes.Count ? level.Pipes[indexA] : null;
            PipeDefinition pipeB = indexB < level.Pipes.Count ? level.Pipes[indexB] : null;

            Undo.RecordObject(level, "Swap Pipes");
            level.SetPipeAt(indexA, pipeB);
            level.SetPipeAt(indexB, pipeA);
            EditorUtility.SetDirty(level);
        }
    }
}
