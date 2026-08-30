using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MarbleOrchestra.Grid.Editor
{
    /// <summary>
    /// Tilemap-style painter for LevelData: pick a layer (Pipe or Content),
    /// pick a brush from the palette of existing assets, click cells to paint,
    /// right-click to erase. Replaces authoring via the flat Inspector list.
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

        private LevelData level;
        private PaintLayer activeLayer;
        private PipeDefinition[] availablePipes = new PipeDefinition[0];
        private CellContentDefinition[] availableContents = new CellContentDefinition[0];
        private Object selectedBrush;
        private int pendingWidth;
        private int pendingHeight;
        private Vector2 paletteScroll;

        private bool useCustomBrush;
        private Direction customConnections = Direction.None;
        private Color customBackgroundColor = new Color(0.15f, 0.15f, 0.15f);
        private PipeRole customRole = PipeRole.Normal;
        private bool customLocked;

        private bool useCustomContent;
        private AudioClip customClip;
        private Color customFlashColor = Color.white;

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

            int required = level.Width * level.Height;
            if (level.Pipes.Count != required || level.Contents.Count != required)
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

        private static T[] LoadAllAssets<T>() where T : Object
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

            activeLayer = (PaintLayer)GUILayout.Toolbar((int)activeLayer, new[] { "Pipe", "Content" });

            if (activeLayer == PaintLayer.Pipe && GUILayout.Button("Randomize", GUILayout.Width(100)))
            {
                RandomizePipes();
            }

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

        private void DrawPalette()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(190));
            EditorGUILayout.LabelField(activeLayer == PaintLayer.Pipe ? "Pipes" : "Contents", EditorStyles.boldLabel);

            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(160));

            bool eraserSelected = !useCustomBrush && selectedBrush == null;
            if (DrawPaletteEntry("Eraser", Color.clear, eraserSelected))
            {
                selectedBrush = null;
                useCustomBrush = false;
            }

            if (activeLayer == PaintLayer.Pipe)
            {
                foreach (PipeDefinition pipe in availablePipes)
                {
                    bool selected = !useCustomBrush && selectedBrush == pipe;
                    if (DrawPaletteEntry(pipe.PipeId, pipe.Color, selected))
                    {
                        selectedBrush = pipe;
                        useCustomBrush = false;
                    }
                }
            }
            else
            {
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
            }

            EditorGUILayout.EndScrollView();

            if (activeLayer == PaintLayer.Pipe)
            {
                DrawCustomPipeBuilder();
            }
            else
            {
                DrawCustomContentBuilder();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCustomPipeBuilder()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(useCustomBrush ? EditorStyles.helpBox : GUIStyle.none);
            EditorGUILayout.LabelField("Custom Pipe", EditorStyles.boldLabel);

            DrawDirectionToggles();
            customBackgroundColor = EditorGUILayout.ColorField("Background", customBackgroundColor);
            customRole = (PipeRole)EditorGUILayout.EnumPopup("Role", customRole);
            customLocked = EditorGUILayout.Toggle("Locked", customLocked);

            if (GUILayout.Button(useCustomBrush ? "Custom (active)" : "Use Custom"))
            {
                useCustomBrush = true;
                selectedBrush = null;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDirectionToggles()
        {
            bool up = (customConnections & Direction.Up) != 0;
            bool right = (customConnections & Direction.Right) != 0;
            bool down = (customConnections & Direction.Down) != 0;
            bool left = (customConnections & Direction.Left) != 0;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            up = GUILayout.Toggle(up, "U", EditorStyles.miniButton, GUILayout.Width(28));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            left = GUILayout.Toggle(left, "L", EditorStyles.miniButton, GUILayout.Width(28));
            GUILayout.FlexibleSpace();
            right = GUILayout.Toggle(right, "R", EditorStyles.miniButton, GUILayout.Width(28));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            down = GUILayout.Toggle(down, "D", EditorStyles.miniButton, GUILayout.Width(28));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            customConnections = Direction.None;
            if (up) customConnections |= Direction.Up;
            if (right) customConnections |= Direction.Right;
            if (down) customConnections |= Direction.Down;
            if (left) customConnections |= Direction.Left;
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
        }

        private void DrawCell(int x, int y)
        {
            int index = y * level.Width + x;
            Rect rect = GUILayoutUtility.GetRect(CellSize, CellSize, GUILayout.Width(CellSize), GUILayout.Height(CellSize));

            PipeDefinition pipe = index < level.Pipes.Count ? level.Pipes[index] : null;
            CellContentDefinition content = index < level.Contents.Count ? level.Contents[index] : null;

            Color background = pipe != null ? pipe.BackgroundColor : new Color(0.18f, 0.18f, 0.18f);
            EditorGUI.DrawRect(rect, background);

            if (pipe != null)
            {
                Rect hub = new Rect(rect.x + rect.width * 0.35f, rect.y + rect.height * 0.35f, rect.width * 0.3f, rect.height * 0.3f);
                EditorGUI.DrawRect(hub, pipe.Color);
                DrawConnectionArms(rect, pipe);

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

            DrawGridLines(rect);

            HandleCellEvents(rect, index);
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

        private static void DrawConnectionArms(Rect rect, PipeDefinition pipe)
        {
            Direction connections = pipe.Connections;
            if (connections == Direction.None) return;

            float thickness = rect.width * 0.16f;
            float armLength = rect.width * 0.35f;

            if ((connections & Direction.Up) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x + rect.width * 0.5f - thickness * 0.5f, rect.y, thickness, armLength), pipe.Color);
            }

            if ((connections & Direction.Down) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x + rect.width * 0.5f - thickness * 0.5f, rect.yMax - armLength, thickness, armLength), pipe.Color);
            }

            if ((connections & Direction.Left) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height * 0.5f - thickness * 0.5f, armLength, thickness), pipe.Color);
            }

            if ((connections & Direction.Right) != 0)
            {
                EditorGUI.DrawRect(new Rect(rect.xMax - armLength, rect.y + rect.height * 0.5f - thickness * 0.5f, armLength, thickness), pipe.Color);
            }
        }

        private void HandleCellEvents(Rect rect, int index)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                ApplyBrush(index);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                ClearCell(index);
                e.Use();
            }
        }

        private void ApplyBrush(int index)
        {
            Undo.RecordObject(level, "Paint Cell");
            if (activeLayer == PaintLayer.Pipe)
            {
                PipeDefinition pipe = useCustomBrush
                    ? GetOrCreateCustomPipe(customConnections, customBackgroundColor, customRole, customLocked)
                    : selectedBrush as PipeDefinition;
                level.SetPipeAt(index, pipe);
            }
            else
            {
                CellContentDefinition content = useCustomContent
                    ? GetOrCreateCustomContent(customClip, customFlashColor)
                    : selectedBrush as CellContentDefinition;
                level.SetContentAt(index, content);
            }
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

        private void RandomizePipes()
        {
            int cellCount = level.Width * level.Height;
            List<int> freeSlots = new List<int>();
            List<PipeDefinition> pipesToShuffle = new List<PipeDefinition>();

            for (int i = 0; i < cellCount; i++)
            {
                PipeDefinition pipe = i < level.Pipes.Count ? level.Pipes[i] : null;
                if (pipe != null && pipe.Locked) continue;

                freeSlots.Add(i);
                if (pipe != null) pipesToShuffle.Add(pipe);
            }

            if (pipesToShuffle.Count == 0 || freeSlots.Count < 2) return;

            for (int i = freeSlots.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
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

        private void ClearCell(int index)
        {
            Undo.RecordObject(level, "Clear Cell");
            if (activeLayer == PaintLayer.Pipe)
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
    }
}
