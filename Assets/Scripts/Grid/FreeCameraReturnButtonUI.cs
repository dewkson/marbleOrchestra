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
    /// instead holds previous/next SubLevel buttons - "next" only enabled
    /// once the active SubLevel has a valid track (see
    /// MarbleController.CanAdvanceSubLevel).
    /// Lives on its own GameObject; cameraModeTransition and
    /// marbleController are wired in the Inspector or auto-found at Awake.
    /// </summary>
    public class FreeCameraReturnButtonUI : MonoBehaviour
    {
        [SerializeField] private CameraModeTransition cameraModeTransition;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private string guidedLabel = "Geführte Kamera";
        [SerializeField] private string freeLabel = "Freie Kamera";

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
        private Button prevSubLevelButton, nextSubLevelButton;
        private Text prevSubLevelText, nextSubLevelText;

        private static readonly Color EnabledTextColor = Color.white;
        private static readonly Color DisabledTextColor = new Color(1f, 1f, 1f, 0.35f);

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
        }

        /// Desktop: below the minimap like the other buttons, both side by
        /// side across the column. Mobile: its own row across the full
        /// screen width right below the play button (see
        /// GameViewLayout.SubLevelNavTopRef).
        private void UpdateSubLevelPanel(float top)
        {
            float rowWidth = GameViewLayout.IsMobile ? GameViewLayout.ReferenceWidth - 2f * M : W;
            float half = rowWidth / 2f - 10f;
            subLevelPanelRect.anchoredPosition = new Vector2(0f, -(GameViewLayout.IsMobile ? GameViewLayout.SubLevelNavTopRef : top));
            SetRect(prevSubLevelRect, new Vector2(-M - rowWidth / 2f - 10f, 0f), new Vector2(half, GameViewLayout.SubLevelNavRowHeight));
            SetRect(nextSubLevelRect, new Vector2(-M, 0f), new Vector2(half, GameViewLayout.SubLevelNavRowHeight));

            bool visible = marbleController != null && GameViewLayout.HasSubLevelNavRow && !marbleController.IsPlaying;
            if (subLevelPanelGO.activeSelf != visible) subLevelPanelGO.SetActive(visible);
            if (!visible) return;

            bool canPrev = marbleController.CanGoToPreviousSubLevel;
            bool canNext = marbleController.CanAdvanceSubLevel;
            prevSubLevelButton.interactable = canPrev;
            nextSubLevelButton.interactable = canNext;
            prevSubLevelText.color = canPrev ? EnabledTextColor : DisabledTextColor;
            nextSubLevelText.color = canNext ? EnabledTextColor : DisabledTextColor;
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

            // SubLevel navigation while planning - same slot as the camera button, which only shows while playing.
            subLevelPanelGO = new GameObject("SubLevelPanel", typeof(RectTransform));
            subLevelPanelGO.transform.SetParent(canvasGO.transform, false);
            subLevelPanelRect = (RectTransform)subLevelPanelGO.transform;
            subLevelPanelRect.anchorMin = subLevelPanelRect.anchorMax = subLevelPanelRect.pivot = new Vector2(1f, 1f);
            subLevelPanelRect.sizeDelta = Vector2.zero;

            GameObject prevGO = CreateButton(subLevelPanelGO.transform, "PrevSubLevelButton", "< Sublevel", Vector2.zero, Vector2.zero, () => marbleController?.TryGoToPreviousSubLevel(), out prevSubLevelText);
            prevSubLevelRect = (RectTransform)prevGO.transform;
            prevSubLevelButton = prevGO.GetComponent<Button>();
            GameObject nextGO = CreateButton(subLevelPanelGO.transform, "NextSubLevelButton", "Sublevel >", Vector2.zero, Vector2.zero, () => marbleController?.TryAdvanceSubLevel(), out nextSubLevelText);
            nextSubLevelRect = (RectTransform)nextGO.transform;
            nextSubLevelButton = nextGO.GetComponent<Button>();
            subLevelPanelGO.SetActive(false);
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
