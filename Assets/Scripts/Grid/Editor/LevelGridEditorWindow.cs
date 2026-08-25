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

        private LevelData level;
        private PaintLayer activeLayer;
        private PipeDefinition[] availablePipes = new PipeDefinition[0];
        private CellContentDefinition[] availableContents = new CellContentDefinition[0];
        private Object selectedBrush;
        private int pendingWidth;
        private int pendingHeight;
        private Vector2 paletteScroll;

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
            EditorGUILayout.BeginVertical(GUILayout.Width(160));
            EditorGUILayout.LabelField(activeLayer == PaintLayer.Pipe ? "Pipes" : "Contents", EditorStyles.boldLabel);

            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);

            bool eraserSelected = selectedBrush == null;
            if (DrawPaletteEntry("Eraser", Color.clear, eraserSelected))
            {
                selectedBrush = null;
            }

            if (activeLayer == PaintLayer.Pipe)
            {
                foreach (PipeDefinition pipe in availablePipes)
                {
                    bool selected = selectedBrush == pipe;
                    if (DrawPaletteEntry(pipe.PipeId, pipe.Color, selected))
                    {
                        selectedBrush = pipe;
                    }
                }
            }
            else
            {
                foreach (CellContentDefinition content in availableContents)
                {
                    bool selected = selectedBrush == content;
                    string label = $"{content.ContentId} ({content.GetType().Name})";
                    if (DrawPaletteEntry(label, new Color(0.4f, 0.6f, 0.9f), selected))
                    {
                        selectedBrush = content;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
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
            }

            if (content != null)
            {
                Rect marker = new Rect(rect.x + 2, rect.y + 2, 8, 8);
                EditorGUI.DrawRect(marker, new Color(1f, 0.85f, 0.2f));
            }

            GUI.Box(rect, GUIContent.none);

            HandleCellEvents(rect, index);
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
                level.SetPipeAt(index, selectedBrush as PipeDefinition);
            }
            else
            {
                level.SetContentAt(index, selectedBrush as CellContentDefinition);
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
