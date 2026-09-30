using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Small orthographic top-down "2D minimap" of the level surface, shown
    /// as an overlay at the top of the right screen column (see
    /// GameViewLayout) while the 3D play mode runs (see 0058). Frames the
    /// active SubLevel's whole grid area (see 0046) looking straight
    /// down world -Y; the viewport itself takes the aspect of those bounds
    /// (always the full column width), so there are no empty stripes around the
    /// level. Builds its own secondary Camera (child of this object) and
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
        private const float MapMaxWidth = GameViewLayout.SideColumnRefWidth - 2f * GameViewLayout.SideMargin;
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

        /// World-space bounds of the active SubLevel's whole grid area (the
        /// whole grid if none are defined) - same corner-cell approach as
        /// CameraFitter, so the minimap shows the full level, not just the
        /// cells that currently carry a valid track.
        private bool TryComputeAreaBounds(out Bounds bounds)
        {
            bounds = default;
            if (grid == null || grid.Width <= 0 || grid.Height <= 0) return false;

            RectInt area = grid.ActiveSubLevelArea;
            Vector2Int min = new Vector2Int(area.xMin, area.yMin);
            Vector2Int max = new Vector2Int(area.xMax - 1, area.yMax - 1);

            bounds = new Bounds(grid.transform.TransformPoint(grid.CellToLocalPosition(min)), Vector3.zero);
            bounds.Encapsulate(grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(max.x, min.y))));
            bounds.Encapsulate(grid.transform.TransformPoint(grid.CellToLocalPosition(new Vector2Int(min.x, max.y))));
            bounds.Encapsulate(grid.transform.TransformPoint(grid.CellToLocalPosition(max)));
            return true;
        }

        /// Sizes the viewport to the bounds' aspect (full column width, right-aligned at the top) and frames the bounds
        /// exactly in it.
        private void Fit(Bounds bounds)
        {
            float contentW = 2f * bounds.extents.x + 2f * padding;
            float contentH = 2f * bounds.extents.z + 2f * padding;

            float fit = MapMaxWidth / contentW; // always exactly as wide as the buttons below it
            BottomRef = EdgeMargin + contentH * fit;
            float scale = GameViewLayout.CanvasScale; // canvas reference pixels -> real pixels
            float widthPx = contentW * fit * scale;
            float heightPx = contentH * fit * scale;
            float rightPx = EdgeMargin * scale;
            float topPx = EdgeMargin * scale;

            Rect rect = new Rect(
                1f - (rightPx + widthPx) / Screen.width,
                1f - (topPx + heightPx) / Screen.height,
                widthPx / Screen.width,
                heightPx / Screen.height);
            minimapCam.rect = rect;
            minimapCam.orthographicSize = contentH / 2f;

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
            scaler.referenceResolution = new Vector2(GameViewLayout.ReferenceWidth, 1080f);

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
