using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Screenspace button shown while playing (see 0044): switches between
    /// the guided isometric follow camera and the free pan/zoom camera. Builds
    /// its own Canvas/Button at runtime, matching PlaybackHintUI's
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
    /// sit right above the play button and jump the guided camera to the
    /// previous/next SubLevel with a valid track (see
    /// MarbleController.ViewNextSubLevel).
    /// Lives on its own GameObject; cameraModeTransition and
    /// marbleController are wired in the Inspector or auto-found at Awake.
    /// </summary>
    public class FreeCameraReturnButtonUI : MonoBehaviour
    {
        [SerializeField] private CameraModeTransition cameraModeTransition;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private string guidedLabel = "Geführte Kamera";
        [SerializeField] private string freeLabel = "Freie Kamera";
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
        private RectTransform buttonRect;
        private RectTransform prevRect, nextRect, autoRect;
        private RectTransform trackPanelRect;
        private GameObject buttonGO;
        private Text cameraButtonText;
        private GameObject trackPanelGO;
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
            buttonRect.anchoredPosition = new Vector2(-M, -top);
            buttonRect.sizeDelta = new Vector2(W, 56f);
            trackPanelRect.anchoredPosition = new Vector2(0f, -top);
            SetRect(prevRect, new Vector2(-M - W / 2f - 10f, -66f), new Vector2(W / 2f - 10f, 56f));
            SetRect(nextRect, new Vector2(-M, -66f), new Vector2(W / 2f - 10f, 56f));
            SetRect(autoRect, new Vector2(-M, -132f), new Vector2(W, 56f));

            bool visible = cameraModeTransition != null && marbleController != null && marbleController.IsPlaying;

            if (buttonGO.activeSelf != visible) buttonGO.SetActive(visible);
            // Shows the mode a click switches TO: "Freie Kamera" while guided, "Geführte Kamera" while free.
            if (visible) cameraButtonText.text = cameraModeTransition.IsFreeCamera ? guidedLabel : freeLabel;

            bool tracksVisible = cameraModeTransition != null && marbleController != null
                && marbleController.IsPlaying && !cameraModeTransition.IsFreeCamera && cameraModeTransition.TrackCount > 1;
            if (trackPanelGO.activeSelf != tracksVisible) trackPanelGO.SetActive(tracksVisible);
            if (tracksVisible) autoCycleText.text = AutoLabel(cameraModeTransition.AutoCycleTracks);

            UpdateSubLevelPanel(top);
            UpdateLevelTitle();
        }

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

            // Each button only exists while it can actually be pressed. Planning:
            // "Zurück" whenever there is an earlier SubLevel, "Weiter" once the
            // active one is solved. Playing: only towards SubLevels with a valid track.
            SetActiveIfChanged(prevSubLevelGO, playing ? marbleController.CanViewPreviousSubLevel : marbleController.CanGoToPreviousSubLevel);
            SetActiveIfChanged(nextSubLevelGO, playing ? marbleController.CanViewNextSubLevel : marbleController.CanAdvanceSubLevel);

            // Labelled with the name (from the level editor) of the SubLevel each button leads to.
            if (prevSubLevelGO.activeSelf) SetLabel(prevSubLevelText, marbleController.PreviousSubLevelTarget);
            if (nextSubLevelGO.activeSelf) SetLabel(nextSubLevelText, marbleController.NextSubLevelTarget);
        }

        private void SetLabel(Text label, int subLevelIndex)
        {
            if (subLevelIndex < 0) return;
            string text = grid != null ? grid.GetSubLevelName(subLevelIndex) : string.Empty;
            if (label.text != text) label.text = text;
        }

        private void HandlePrevSubLevelClicked()
        {
            if (marbleController == null) return;
            if (marbleController.IsPlaying) marbleController.ViewPreviousSubLevel();
            else marbleController.RequestPreviousSubLevel();
        }

        private void HandleNextSubLevelClicked()
        {
            if (marbleController == null) return;
            if (marbleController.IsPlaying) marbleController.ViewNextSubLevel();
            else marbleController.RequestAdvanceSubLevel();
        }

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

            buttonGO = CreateButton(canvasGO.transform, "CameraModeButton", freeLabel, new Vector2(-M, 0f), new Vector2(W, 56f), HandleCameraButtonClicked, out cameraButtonText);
            buttonRect = (RectTransform)buttonGO.transform;
            buttonGO.SetActive(false);

            // Track switching for guided camera (see 0057), below the return button.
            trackPanelGO = new GameObject("TrackSwitchPanel", typeof(RectTransform));
            trackPanelGO.transform.SetParent(canvasGO.transform, false);
            trackPanelRect = (RectTransform)trackPanelGO.transform;
            RectTransform panelRect = trackPanelRect;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;

            prevRect = (RectTransform)CreateButton(trackPanelGO.transform, "PrevTrackButton", "< Bahn", new Vector2(-M - W / 2f - 10f, -66f), new Vector2(W / 2f - 10f, 56f), () => cameraModeTransition?.PreviousTrack(), out _).transform;
            nextRect = (RectTransform)CreateButton(trackPanelGO.transform, "NextTrackButton", "Bahn >", new Vector2(-M, -66f), new Vector2(W / 2f - 10f, 56f), () => cameraModeTransition?.NextTrack(), out _).transform;
            autoRect = (RectTransform)CreateButton(trackPanelGO.transform, "AutoCycleButton", AutoLabel(false), new Vector2(-M, -132f), new Vector2(W, 56f), () => cameraModeTransition?.ToggleAutoCycleTracks(), out autoCycleText).transform;
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
            nextSubLevelGO = CreateButton(subLevelPanelGO.transform, "NextSubLevelButton", "", Vector2.zero, Vector2.zero, HandleNextSubLevelClicked, out nextSubLevelText);
            nextSubLevelText.fontSize = GameViewLayout.SubLevelNavFontSize;
            nextSubLevelRect = (RectTransform)nextSubLevelGO.transform;
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

        private void HandleCameraButtonClicked()
        {
            if (cameraModeTransition == null) return;

            if (cameraModeTransition.IsFreeCamera) cameraModeTransition.ReturnToGuidedCamera();
            else cameraModeTransition.EnterFreeCamera();
        }
    }
}
