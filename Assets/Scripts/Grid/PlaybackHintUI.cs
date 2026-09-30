using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Big play/stop button at the bottom of the right screen column (see
    /// GameViewLayout): tells the player they can test the current track
    /// with SPACE or a click once a valid Start-to-Goal connection exists,
    /// and switches back to planning while simulating.
    /// Builds its own Canvas/Text at runtime, matching this project's
    /// pattern of procedurally-built visuals - no scene/prefab setup needed.
    /// Lives on its own GameObject; marbleController is wired in the
    /// Inspector or auto-found at Awake.
    /// </summary>
    public class PlaybackHintUI : MonoBehaviour
    {
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private Color readyColor = new Color(0.35f, 0.9f, 0.45f);
        [SerializeField] private Color notReadyColor = new Color(0.85f, 0.85f, 0.85f, 0.6f);
        [SerializeField] private Color playingColor = new Color(0.95f, 0.8f, 0.3f);

        private Text label;
        private Image panel;
        private Button button;

        private void Awake()
        {
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            EnsureEventSystem();
            BuildUI();
        }

        private void Update()
        {
            if (marbleController.IsPlaying)
            {
                label.text = "Zurück zur Planung\n(SPACE)";
                label.color = playingColor;
                button.interactable = true;
                return;
            }

            if (marbleController.CanPlay)
            {
                label.text = "Bahn testen\n(SPACE)";
                label.color = readyColor;
                button.interactable = true;
            }
            else
            {
                label.text = "Verbinde Start und Ziel,\num die Bahn zu testen";
                label.color = notReadyColor;
                button.interactable = false;
            }
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
            GameObject canvasGO = new GameObject("PlaybackHintCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GameViewLayout.ReferenceWidth, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Big play/stop button at the bottom of the right column;
            // clicking it does the same as SPACE (MarbleController.TogglePlay).
            GameObject panelGO = new GameObject("HintPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            panel = panelGO.AddComponent<Image>();
            GameViewLayout.StyleAsButton(panel);

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-GameViewLayout.SideMargin, GameViewLayout.SideMargin);
            panelRect.sizeDelta = new Vector2(GameViewLayout.SideColumnRefWidth - 2f * GameViewLayout.SideMargin, 170f);

            button = panelGO.AddComponent<Button>();
            button.targetGraphic = panel;
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white; // keep the panel look; the label color signals "not ready"
            button.colors = colors;
            button.onClick.AddListener(() => marbleController.TogglePlay());

            GameObject labelGO = new GameObject("HintLabel");
            labelGO.transform.SetParent(panelGO.transform, false);
            label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }
}
