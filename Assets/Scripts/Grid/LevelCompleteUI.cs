using UnityEngine;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Winning indicator (see 0070): a "Level Complete" (planning) /
    /// "Nice Beat" (playing) panel
    /// at the bottom right that shows while the level is solved, i.e. every
    /// SubLevel has a valid track at the same time
    /// (MarbleController.IsLevelComplete) - for a level without SubLevels
    /// as soon as its single track is complete. It follows the live state:
    /// breaking a track hides it again. Pops in with a short scale bounce.
    /// Purely informative - no buttons, never blocks input.
    /// Layout: see UpdateLayout.
    /// Builds its own Canvas at runtime like PlaybackHintUI, which adds this
    /// component automatically - no scene setup needed.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private string planningText = "Level Complete";
        [SerializeField] private string playingText = "Nice Beat";
        [SerializeField] private Color textColor = new Color(0.35f, 0.9f, 0.45f);
        [Tooltip("Maximale Schriftgröße - die Schrift schrumpft automatisch, falls das Feld (z.B. auf kleinen Handys) zu niedrig ist.")]
        [SerializeField] private int fontSize = 64;
        [Tooltip("Dauer des Einblend-Effekts in Sekunden.")]
        [SerializeField, Min(0.01f)] private float popDuration = 0.35f;

        private const float M = GameViewLayout.SideMargin; // also the gap between the big UI buttons
        private const float MinHeight = 70f;
        private const float DesktopHeight = 220f;

        private CameraModeTransition cameraModeTransition;
        private CanvasScaler canvasScaler;
        private RectTransform badgeRect;
        private Text label;
        private bool visible;
        private float popElapsed;

        private void Awake()
        {
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            cameraModeTransition = FindAnyObjectByType<CameraModeTransition>();
            BuildUI();
            SetVisible(false);
        }

        private void Update()
        {
            if (marbleController == null) return;
            GameViewLayout.ApplyTo(canvasScaler);
            UpdateLayout();

            bool complete = marbleController.IsLevelComplete;
            if (complete != visible) SetVisible(complete);
            if (!visible) return;

            string text = marbleController.IsPlaying ? playingText : planningText;
            if (label.text != text) label.text = text;

            // Overshooting pop-in (ease-out-back): 0 -> ~1.1 -> 1.
            popElapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(popElapsed / popDuration) - 1f;
            const float c1 = 1.70158f;
            float scale = 1f + (c1 + 1f) * u * u * u + c1 * u * u;
            badgeRect.localScale = Vector3.one * Mathf.Max(0f, scale);
        }

        private void SetVisible(bool show)
        {
            visible = show;
            popElapsed = 0f;
            badgeRect.gameObject.SetActive(show);
            if (show) badgeRect.localScale = Vector3.zero;
        }

        /// Mobile, playing: mirrors the minimap (left half of the UI strip)
        /// on the right - same width and top/bottom edge - or starts the
        /// usual button gap (M) below the track-switch panel while that is
        /// shown (FreeCameraReturnButtonUI).
        /// Mobile, planning: no minimap and no camera button - spans the
        /// full width of the strip below the play button / SubLevel row.
        /// Desktop: the right column is full, so it sits minimap-wide in the
        /// bottom-right corner of the main view.
        private void UpdateLayout()
        {
            float width = GameViewLayout.ColumnWidthRef;
            if (GameViewLayout.IsMobile)
            {
                bool playing = marbleController.IsPlaying;
                float top = GameViewLayout.SideContentTopRef;
                if (playing && FreeCameraReturnButtonUI.IsTrackPanelVisible(cameraModeTransition, marbleController))
                    top += FreeCameraReturnButtonUI.TrackPanelHeightRef + M;
                if (!playing) width = GameViewLayout.ReferenceWidth - 2f * M;

                float height = Mathf.Max(MinHeight, GameViewLayout.ScreenHeightRef - M - top);
                badgeRect.anchoredPosition = new Vector2(-M, M);
                badgeRect.sizeDelta = new Vector2(width, height);
            }
            else
            {
                float right = GameViewLayout.ReferenceWidth * (1f - GameViewLayout.DesktopMainViewFraction) + M;
                badgeRect.anchoredPosition = new Vector2(-right, M);
                badgeRect.sizeDelta = new Vector2(width, DesktopHeight);
            }
        }

        private void BuildUI()
        {
            GameObject canvasGO = new GameObject("LevelCompleteCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // above the other runtime canvases

            canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            GameViewLayout.ApplyTo(canvasScaler);

            GameObject badge = new GameObject("NiceBeatBadge", typeof(RectTransform));
            badge.transform.SetParent(canvasGO.transform, false);
            badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(1f, 0f);
            badgeRect.sizeDelta = new Vector2(GameViewLayout.ColumnWidthRef, DesktopHeight);

            Image background = badge.AddComponent<Image>();
            GameViewLayout.StyleAsButton(background);
            background.raycastTarget = false;
            badge.GetComponent<Outline>().effectColor = textColor;

            GameObject labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(badge.transform, false);
            label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = textColor;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = fontSize;
            label.lineSpacing = 1.1f;
            label.text = planningText;
            label.raycastTarget = false;

            RectTransform labelRect = (RectTransform)labelGO.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 10f); // padding inside the frame
            labelRect.offsetMax = new Vector2(-16f, -10f);
        }
    }
}
