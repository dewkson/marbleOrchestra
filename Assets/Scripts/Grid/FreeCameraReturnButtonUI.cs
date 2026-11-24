using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Screenspace side-column UI. There is no guided/free camera toggle
    /// button (anymore, see 0044): a click/drag/zoom on the 3D view enters
    /// free camera (CameraModeTransition), and the track / SubLevel skip
    /// buttons below lead back to the guided camera. Builds its own
    /// Canvas/Buttons at runtime, matching PlaybackHintUI's
    /// procedural-UI pattern - no scene/prefab setup needed.
    /// A clickable uGUI Button needs an EventSystem with an input module to
    /// receive pointer events; since this project's Active Input Handling
    /// is New Input System only (no legacy StandaloneInputModule support),
    /// this also creates a minimal EventSystem + InputSystemUIInputModule
    /// the first time, if the scene doesn't already have one.
    /// Also shows track-switch buttons (previous/next track, auto-cycle) while
    /// playing with more than one track in the active SubLevel (see 0057).
    /// While planning in a level with several SubLevels, the same slot
    /// instead holds "Zurück"/"Weiter" SubLevel buttons, each shown only
    /// while usable - "Weiter" once the active SubLevel has a valid track
    /// (see MarbleController.CanAdvanceSubLevel); a click during the
    /// return animation from play mode is carried out once it finished.
    /// While playing, the same two buttons (same size and arrangement)
    /// sit right above the play button: towards a SubLevel with a valid
    /// track they jump the guided camera there, otherwise they leave play
    /// mode and switch to that SubLevel once back in planning (see
    /// MarbleController.PressNextSubLevel).
    /// Lives on its own GameObject; cameraModeTransition and
    /// marbleController are wired in the Inspector or auto-found at Awake.
    /// </summary>
    public class FreeCameraReturnButtonUI : MonoBehaviour
    {
        [SerializeField] private CameraModeTransition cameraModeTransition;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private int levelTitleFontSize = 48;
        [Tooltip("Abstand (Referenzpixel) zwischen Überschrift und dem Trenner zwischen Spielbereich und UI.")]
        [SerializeField] private float levelTitleGapAboveBorder = 20f;

        // Right column layout (see GameViewLayout): below the minimap (top-right),
        // with some room in between. On Mobile (see 0060) the minimap sits in
        // the left half of the bottom strip, so the buttons start at its top.
        private const float M = GameViewLayout.SideMargin;
        private static float W => GameViewLayout.ColumnWidthRef;
        private const float GapBelowMinimap = 24f;

        private MinimapCamera minimap;
        private CanvasScaler canvasScaler;
        private RectTransform prevRect, nextRect, autoRect;
        private RectTransform trackPanelRect;
        private GameObject trackPanelGO;
        private RectTransform prevSubLevelIcon, nextSubLevelIcon;

        private const float SkipIconMaxSize = 64f;
        private const float SkipIconMargin = 24f; // icon distance from the button edge (planning)
        private const float TrackIconSize = 40f;
        private Text autoCycleText;

        private PathGrid grid;
        private GameObject subLevelPanelGO;
        private RectTransform subLevelPanelRect, prevSubLevelRect, nextSubLevelRect;
        private GameObject prevSubLevelGO, nextSubLevelGO;
        private Text prevSubLevelText, nextSubLevelText;

        // Name of the level/SubLevel on screen (see UpdateLevelTitle).
        private RectTransform levelTitleRect;
        private Text levelTitleText;
        private const float LevelTitleHeight = 70f;

        private void Awake()
        {
            if (cameraModeTransition == null) cameraModeTransition = FindAnyObjectByType<CameraModeTransition>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            grid = FindAnyObjectByType<PathGrid>(FindObjectsInactive.Include); // inactive while playing (see MarbleController.PlayRoutine)

            EnsureEventSystem();
            BuildUI();
        }

        private void Update()
        {
            if (minimap == null) minimap = FindAnyObjectByType<MinimapCamera>();
            GameViewLayout.ApplyTo(canvasScaler);
            GameViewLayout.HasSubLevelNavRow = grid != null && grid.SubLevelCount > 1; // before SideContentTopRef is read below
            float top = GameViewLayout.IsMobile
                ? GameViewLayout.SideContentTopRef
                : (minimap != null ? minimap.BottomRef : M) + GapBelowMinimap;
            trackPanelRect.anchoredPosition = new Vector2(0f, -top);
            SetRect(prevRect, new Vector2(-M - W / 2f - 10f, 0f), new Vector2(W / 2f - 10f, 56f));
            SetRect(nextRect, new Vector2(-M, 0f), new Vector2(W / 2f - 10f, 56f));
            SetRect(autoRect, new Vector2(-M, -66f), new Vector2(W, 56f));

            bool tracksVisible = IsTrackPanelVisible(cameraModeTransition, marbleController);
            if (trackPanelGO.activeSelf != tracksVisible) trackPanelGO.SetActive(tracksVisible);
            if (tracksVisible) autoCycleText.text = AutoLabel(cameraModeTransition.AutoCycleTracks);

            UpdateSubLevelPanel(top);
            UpdateLevelTitle();
        }

        /// Track switching (see 0057) while playing with more than one
        /// track - also in free camera: a click on a track button is the way
        /// back to the guided camera (CameraModeTransition.SwitchTrack).
        /// Shared with LevelCompleteUI, which lays itself out below it.
        public static bool IsTrackPanelVisible(CameraModeTransition cameraModeTransition, MarbleController marbleController) =>
            cameraModeTransition != null && marbleController != null
            && marbleController.IsPlaying && cameraModeTransition.TrackCount > 1;

        /// Height of the track panel (two rows of 56, 10 apart).
        public const float TrackPanelHeightRef = 122f;

        /// Name of the SubLevel in focus (the edited one, while playing the
        /// one the camera follows) as named in the level editor, as plain
        /// text: Mobile centered a little above the horizontal border
        /// between main view and UI, Desktop at the top, right against the
        /// vertical border.
        private void UpdateLevelTitle()
        {
            string title = grid != null && marbleController != null ? grid.GetSubLevelName(marbleController.ViewedSubLevelIndex) : string.Empty;
            bool show = !string.IsNullOrEmpty(title);
            if (levelTitleRect.gameObject.activeSelf != show) levelTitleRect.gameObject.SetActive(show);
            if (!show) return;

            if (levelTitleText.text != title) levelTitleText.text = title;
            if (levelTitleText.fontSize != levelTitleFontSize) levelTitleText.fontSize = levelTitleFontSize;
            float width = levelTitleText.preferredWidth + 2f * M;

            if (GameViewLayout.IsMobile)
            {
                levelTitleRect.anchorMin = levelTitleRect.anchorMax = new Vector2(0.5f, 1f);
                levelTitleRect.pivot = new Vector2(0.5f, 0f); // bottom edge sits the gap above the border
                levelTitleRect.anchoredPosition = new Vector2(0f, -GameViewLayout.SideTopRef + levelTitleGapAboveBorder);
            }
            else
            {
                float sideWidth = GameViewLayout.ReferenceWidth * (1f - GameViewLayout.DesktopMainViewFraction);
                levelTitleRect.anchorMin = levelTitleRect.anchorMax = levelTitleRect.pivot = new Vector2(1f, 1f);
                levelTitleRect.anchoredPosition = new Vector2(-sideWidth, -M / 2f);
            }
            levelTitleRect.sizeDelta = new Vector2(width, LevelTitleHeight);
        }

        /// Desktop: planning below the (hidden) minimap at the top of the
        /// column, playing right above the play button - both side by side
        /// across the column. Mobile: its own row across the full screen
        /// width right below the play button (see
        /// GameViewLayout.SubLevelNavTopRef), in both modes.
        private void UpdateSubLevelPanel(float top)
        {
            bool playing = marbleController != null && marbleController.IsPlaying;
            // Mobile: each half exactly as wide as the column below it (minimap
            // left, buttons right, M apart). Desktop: two halves across the column.
            float gap = GameViewLayout.IsMobile ? M : 20f;
            float rowWidth = GameViewLayout.IsMobile ? 2f * W + gap : W;
            float half = (rowWidth - gap) / 2f;
            float rowTop = GameViewLayout.IsMobile ? GameViewLayout.SubLevelNavTopRef
                : playing ? GameViewLayout.DesktopPlaySubLevelNavTopRef
                : top;
            subLevelPanelRect.anchoredPosition = new Vector2(0f, -rowTop);
            SetRect(prevSubLevelRect, new Vector2(-M - half - gap, 0f), new Vector2(half, GameViewLayout.SubLevelNavRowHeight));
            SetRect(nextSubLevelRect, new Vector2(-M, 0f), new Vector2(half, GameViewLayout.SubLevelNavRowHeight));

            bool visible = marbleController != null && GameViewLayout.HasSubLevelNavRow;
            if (subLevelPanelGO.activeSelf != visible) subLevelPanelGO.SetActive(visible);
            if (!visible) return;

            // Each button only exists while it can actually be pressed (see
            // MarbleController.PreviousSubLevelTarget / NextSubLevelTarget).
            SetActiveIfChanged(prevSubLevelGO, marbleController.PreviousSubLevelTarget >= 0);
            SetActiveIfChanged(nextSubLevelGO, marbleController.NextSubLevelTarget >= 0);

            // Planning: skip icon at the outer edge (prev left, next right)
            // plus the name (from the level editor) of the SubLevel the
            // button leads to. Playing: just the icon, centered.
            if (prevSubLevelGO.activeSelf) LayoutSubLevelButton(prevSubLevelText, prevSubLevelIcon, marbleController.PreviousSubLevelTarget, playing, false);
            if (nextSubLevelGO.activeSelf) LayoutSubLevelButton(nextSubLevelText, nextSubLevelIcon, marbleController.NextSubLevelTarget, playing, true);
        }

        private void LayoutSubLevelButton(Text label, RectTransform icon, int subLevelIndex, bool playing, bool next)
        {
            float size = Mathf.Min(SkipIconMaxSize, GameViewLayout.SubLevelNavRowHeight * 0.45f);
            icon.sizeDelta = new Vector2(size, size);

            if (playing)
            {
                icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
                icon.anchoredPosition = Vector2.zero;
                SetActiveIfChanged(label.gameObject, false);
                return;
            }

            float side = next ? 1f : 0f;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(side, 0.5f);
            icon.anchoredPosition = new Vector2(next ? -SkipIconMargin : SkipIconMargin, 0f);

            // Keep the name clear of the icon.
            RectTransform labelRect = (RectTransform)label.transform;
            float inset = SkipIconMargin + size + SkipIconMargin / 2f;
            labelRect.offsetMin = new Vector2(next ? SkipIconMargin : inset, 0f);
            labelRect.offsetMax = new Vector2(next ? -inset : -SkipIconMargin, 0f);

            SetActiveIfChanged(label.gameObject, true);
            SetLabel(label, subLevelIndex);
        }

        private void SetLabel(Text label, int subLevelIndex)
        {
            if (subLevelIndex < 0) return;
            string text = grid != null ? grid.GetSubLevelName(subLevelIndex) : string.Empty;
            if (label.text != text) label.text = text;
        }

        private void HandlePrevSubLevelClicked() => marbleController?.PressPreviousSubLevel();

        private void HandleNextSubLevelClicked() => marbleController?.PressNextSubLevel();

        private static void SetActiveIfChanged(GameObject go, bool active)
        {
            if (go.activeSelf != active) go.SetActive(active);
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            GameObject canvasGO = new GameObject("FreeCameraReturnCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler = scaler;
            GameViewLayout.ApplyTo(scaler);

            canvasGO.AddComponent<GraphicRaycaster>();

            Sprite prevSprite = CreateSkipIconSprite(false);
            Sprite nextSprite = CreateSkipIconSprite(true);

            // Track switching for guided camera (see 0057), below the minimap.
            trackPanelGO = new GameObject("TrackSwitchPanel", typeof(RectTransform));
            trackPanelGO.transform.SetParent(canvasGO.transform, false);
            trackPanelRect = (RectTransform)trackPanelGO.transform;
            RectTransform panelRect = trackPanelRect;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;

            // Previous/next track: just the skip icon, centered, no text.
            GameObject prevTrackGO = CreateButton(trackPanelGO.transform, "PrevTrackButton", "", Vector2.zero, new Vector2(W / 2f - 10f, 56f), () => cameraModeTransition?.PreviousTrack(), out Text prevTrackText);
            prevTrackText.gameObject.SetActive(false);
            AddIcon(prevTrackGO.transform, prevSprite, TrackIconSize);
            prevRect = (RectTransform)prevTrackGO.transform;
            GameObject nextTrackGO = CreateButton(trackPanelGO.transform, "NextTrackButton", "", Vector2.zero, new Vector2(W / 2f - 10f, 56f), () => cameraModeTransition?.NextTrack(), out Text nextTrackText);
            nextTrackText.gameObject.SetActive(false);
            AddIcon(nextTrackGO.transform, nextSprite, TrackIconSize);
            nextRect = (RectTransform)nextTrackGO.transform;
            autoRect = (RectTransform)CreateButton(trackPanelGO.transform, "AutoCycleButton", AutoLabel(false), Vector2.zero, new Vector2(W, 56f), () => cameraModeTransition?.ToggleAutoCycleTracks(), out autoCycleText).transform;
            trackPanelGO.SetActive(false);

            // SubLevel navigation: while planning in the camera button's slot, while playing above the play button.
            subLevelPanelGO = new GameObject("SubLevelPanel", typeof(RectTransform));
            subLevelPanelGO.transform.SetParent(canvasGO.transform, false);
            subLevelPanelRect = (RectTransform)subLevelPanelGO.transform;
            subLevelPanelRect.anchorMin = subLevelPanelRect.anchorMax = subLevelPanelRect.pivot = new Vector2(1f, 1f);
            subLevelPanelRect.sizeDelta = Vector2.zero;

            prevSubLevelGO = CreateButton(subLevelPanelGO.transform, "PrevSubLevelButton", "", Vector2.zero, Vector2.zero, HandlePrevSubLevelClicked, out prevSubLevelText);
            prevSubLevelText.fontSize = GameViewLayout.SubLevelNavFontSize;
            prevSubLevelRect = (RectTransform)prevSubLevelGO.transform;
            prevSubLevelIcon = AddIcon(prevSubLevelGO.transform, prevSprite, SkipIconMaxSize);
            nextSubLevelGO = CreateButton(subLevelPanelGO.transform, "NextSubLevelButton", "", Vector2.zero, Vector2.zero, HandleNextSubLevelClicked, out nextSubLevelText);
            nextSubLevelText.fontSize = GameViewLayout.SubLevelNavFontSize;
            nextSubLevelRect = (RectTransform)nextSubLevelGO.transform;
            nextSubLevelIcon = AddIcon(nextSubLevelGO.transform, nextSprite, SkipIconMaxSize);
            subLevelPanelGO.SetActive(false);

            // Level title as plain text near the main view / UI border.
            GameObject titleGO = new GameObject("LevelTitle", typeof(RectTransform));
            titleGO.transform.SetParent(canvasGO.transform, false);
            levelTitleRect = (RectTransform)titleGO.transform;

            GameObject titleLabelGO = new GameObject("Label", typeof(RectTransform));
            titleLabelGO.transform.SetParent(titleGO.transform, false);
            levelTitleText = titleLabelGO.AddComponent<Text>();
            levelTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelTitleText.fontSize = levelTitleFontSize;
            levelTitleText.alignment = TextAnchor.MiddleCenter;
            levelTitleText.color = Color.white;
            levelTitleText.raycastTarget = false;
            levelTitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Shadow titleShadow = titleLabelGO.AddComponent<Shadow>(); // keeps it readable over the level without a background
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            titleShadow.effectDistance = new Vector2(2f, -2f);
            RectTransform titleLabelRect = (RectTransform)titleLabelGO.transform;
            titleLabelRect.anchorMin = Vector2.zero;
            titleLabelRect.anchorMax = Vector2.one;
            titleLabelRect.offsetMin = titleLabelRect.offsetMax = Vector2.zero;
            titleGO.SetActive(false);
        }

        private static string AutoLabel(bool on) => on ? "Automatischer Bahnwechsel: an" : "Automatischer Bahnwechsel: aus";

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick, out Text labelText)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Image image = go.AddComponent<Image>();
            GameViewLayout.StyleAsButton(image);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);

            labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 22;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = text;
            labelText.raycastTarget = false;

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return go;
        }

        /// Centered white icon inside a button (no raycast, the button takes the click).
        private static RectTransform AddIcon(Transform button, Sprite sprite, float size)
        {
            GameObject go = new GameObject("Icon", typeof(RectTransform));
            go.transform.SetParent(button, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }

        /// Music-player "skip" icon: a triangle with a bar at its tip -
        /// next (|>| pointing right) or previous (mirrored). Built
        /// procedurally like PlaybackHintUI's play/pause icons, with a
        /// soft 1px edge.
        private static Sprite CreateSkipIconSprite(bool next)
        {
            const int size = 128;
            const float slope = 0.35f / 0.48f; // triangle half-height over its length
            float slopeNorm = Mathf.Sqrt(1f + slope * slope);

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                if (!next) u = 1f - u; // mirror for "previous"

                // Triangle (0.2,0.15) (0.2,0.85) (0.68,0.5).
                float dEdge = u - 0.2f;
                float dSlant = (slope * (0.68f - u) - Mathf.Abs(v - 0.5f)) / slopeNorm;
                float triangle = Mathf.Min(dEdge, dSlant);

                // Bar at the tip.
                float bar = Mathf.Min(Mathf.Min(u - 0.7f, 0.82f - u), Mathf.Min(v - 0.15f, 0.85f - v));

                float a = Mathf.Clamp01(Mathf.Max(triangle, bar) * size + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
