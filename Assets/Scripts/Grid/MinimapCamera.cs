using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Small orthographic top-down "2D minimap" of the level surface, shown
    /// as an overlay at the top of the right screen column (see
    /// GameViewLayout) while the 3D play mode runs (see 0058). Frames the
    /// combined area of every SubLevel with a valid track (see 0046) looking straight
    /// down world -Y; the viewport itself always fills the space reserved
    /// for it (fixed frame), the content is fitted centered inside. Builds its own secondary Camera (child of this object) and
    /// a white border at runtime - no scene/prefab setup needed.
    /// CameraModeTransition adds this component itself at Awake if the
    /// scene doesn't have one. The M key shows/hides it.
    /// </summary>
    public class MinimapCamera : MonoBehaviour
    {
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private PathGrid grid;

        [SerializeField] private float padding = 0.6f; // world units around the grid, same as CameraFitter
        [SerializeField] private float heightAboveTracks = 100f;
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
        [SerializeField] private Key toggleKey = Key.M;

        // In canvas reference pixels (see GameViewLayout).
        private const float EdgeMargin = GameViewLayout.SideMargin;
        private static float MapMaxWidth => GameViewLayout.ColumnWidthRef;
        private const float BorderThickness = 3f;

        /// Distance (canvas reference pixels) from the top of the screen to
        /// the bottom of the minimap as it is drawn right now, or just the
        /// top margin while it is hidden - the right column's other buttons
        /// are placed below this.
        public float BottomRef { get; private set; } = EdgeMargin;

        private Camera minimapCam;
        private bool userEnabled = true;
        private GameObject borderGO;
        private RectTransform borderRect;
        private CanvasScaler canvasScaler;

        private void Awake()
        {
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            if (grid == null) grid = FindAnyObjectByType<PathGrid>();

            BuildCamera();
            BuildBorder();
            minimapCam.enabled = false;
        }

        private void Update()
        {
            GameViewLayout.ApplyTo(canvasScaler);
            BottomRef = EdgeMargin;
            bool playing = marbleController != null && marbleController.IsPlaying;

            if (playing && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                userEnabled = !userEnabled;

            Bounds bounds = default;
            bool hasBounds = playing && TryComputeAreaBounds(out bounds);
            bool show = hasBounds && userEnabled;

            if (borderGO.activeSelf != show) borderGO.SetActive(show);
            if (minimapCam.enabled != show) minimapCam.enabled = show;
            if (show) Fit(bounds);
        }

        private void BuildCamera()
        {
            GameObject go = new GameObject("MinimapCamera");
            go.transform.SetParent(transform, false);

            minimapCam = go.AddComponent<Camera>();
            minimapCam.orthographic = true;
            minimapCam.clearFlags = CameraClearFlags.SolidColor;
            minimapCam.backgroundColor = backgroundColor;
            minimapCam.depth = Camera.main != null ? Camera.main.depth + 1f : 1f; // drawn on top of the main camera
            minimapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // straight down, screen-up = world +Z
        }

        /// World-space bounds of every SubLevel that currently has a valid
        /// track (MarbleController.GetValidSubLevelIndices - the ones
        /// looping right now), or of the active SubLevel if none qualifies
        /// (the whole grid without SubLevels). Same corner-cell approach as
        /// CameraFitter, so whole SubLevel areas show, not just the track cells.
        private bool TryComputeAreaBounds(out Bounds bounds)
        {
            bounds = default;
            if (grid == null || grid.Width <= 0 || grid.Height <= 0) return false;

            System.Collections.Generic.List<int> valid = marbleController != null
                ? marbleController.GetValidSubLevelIndices()
                : new System.Collections.Generic.List<int>();

            RectInt area = valid.Count > 0 ? grid.GetSubLevelArea(valid[0]) : grid.ActiveSubLevelArea;
            int xMin = area.xMin, yMin = area.yMin, xMax = area.xMax, yMax = area.yMax;
            for (int i = 1; i < valid.Count; i++)
            {
                RectInt other = grid.GetSubLevelArea(valid[i]);
                xMin = Mathf.Min(xMin, other.xMin);
                yMin = Mathf.Min(yMin, other.yMin);
                xMax = Mathf.Max(xMax, other.xMax);
                yMax = Mathf.Max(yMax, other.yMax);
            }

            Vector2Int min = new Vector2Int(xMin, yMin);
            Vector2Int max = new Vector2Int(xMax - 1, yMax - 1);

            bounds = new Bounds(grid.transform.TransformPoint(grid.CellToLocalPosition(min)), Vector3.zero);
            bounds.Encapsulate(grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(max.x, min.y))));
            bounds.Encapsulate(grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(min.x, max.y))));
            bounds.Encapsulate(grid.transform.TransformPoint(grid.CellToLocalPosition(max)));
            return true;
        }

        /// The viewport (and its white border) always has the same size:
        /// the full space reserved for the minimap - Desktop the column
        /// width below the top margin, Mobile the left half of the bottom
        /// strip - so it never jumps when the shown SubLevels change. The
        /// bounds are framed centered inside it; the leftover space shows
        /// the background color.
        private void Fit(Bounds bounds)
        {
            float contentW = 2f * bounds.extents.x + 2f * padding;
            float contentH = 2f * bounds.extents.z + 2f * padding;

            float boxW = MapMaxWidth;
            float boxH = GameViewLayout.SideInnerHeightRef;
            float topRef = GameViewLayout.IsMobile ? GameViewLayout.SideContentTopRef : EdgeMargin;
            BottomRef = topRef + boxH;

            float scale = GameViewLayout.CanvasScale; // canvas reference pixels -> real pixels
            float widthPx = boxW * scale;
            float heightPx = boxH * scale;
            float edgePx = EdgeMargin * scale;
            float topPx = topRef * scale;
            float leftPx = GameViewLayout.IsMobile ? edgePx : Screen.width - edgePx - widthPx;

            Rect rect = new Rect(
                leftPx / Screen.width,
                1f - (topPx + heightPx) / Screen.height,
                widthPx / Screen.width,
                heightPx / Screen.height);
            minimapCam.rect = rect;

            // Fit the content in both directions; the tighter one decides.
            float aspect = boxW / Mathf.Max(1f, boxH);
            minimapCam.orthographicSize = Mathf.Max(contentH / 2f, contentW / 2f / aspect);

            float distance = bounds.extents.y + heightAboveTracks;
            minimapCam.transform.position = bounds.center + Vector3.up * distance;
            minimapCam.nearClipPlane = 0.1f;
            minimapCam.farClipPlane = distance + bounds.extents.y + heightAboveTracks;

            borderRect.anchorMin = new Vector2(rect.xMin, rect.yMin);
            borderRect.anchorMax = new Vector2(rect.xMax, rect.yMax);
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
        }

        private void BuildBorder()
        {
            GameObject canvasGO = new GameObject("MinimapCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler = scaler;
            GameViewLayout.ApplyTo(scaler);

            // White border: four strips around the viewport rect. Overlay
            // canvases draw over cameras, so it has to be hollow.
            borderGO = new GameObject("MinimapBorder", typeof(RectTransform));
            borderGO.transform.SetParent(canvasGO.transform, false);
            borderRect = (RectTransform)borderGO.transform;
            CreateStrip("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-BorderThickness, 0f), new Vector2(BorderThickness, BorderThickness));
            CreateStrip("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-BorderThickness, -BorderThickness), new Vector2(BorderThickness, 0f));
            CreateStrip("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-BorderThickness, 0f), new Vector2(0f, 0f));
            CreateStrip("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(BorderThickness, 0f));

            borderGO.SetActive(false);
        }

        private void CreateStrip(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(borderGO.transform, false);

            Image image = go.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
