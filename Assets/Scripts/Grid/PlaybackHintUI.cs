using UnityEngine;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Screenspace instruction shown during planning: tells the player they
    /// can test the current track with SPACE once a valid Start-to-Goal
    /// connection exists, and how to get back to planning while simulating.
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

        private void Awake()
        {
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            BuildUI();
        }

        private void Update()
        {
            if (marbleController.IsPlaying)
            {
                label.text = "SPACE – zurück zur Planung";
                label.color = playingColor;
                return;
            }

            if (marbleController.CanPlay)
            {
                label.text = "SPACE – Bahn testen";
                label.color = readyColor;
            }
            else
            {
                label.text = "Verbinde Start und Ziel, um die Bahn zu testen";
                label.color = notReadyColor;
            }
        }

        private void BuildUI()
        {
            GameObject canvasGO = new GameObject("PlaybackHintCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject panelGO = new GameObject("HintPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            Image panel = panelGO.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.55f);
            panel.raycastTarget = false;

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            panelRect.sizeDelta = new Vector2(620f, 50f);

            GameObject labelGO = new GameObject("HintLabel");
            labelGO.transform.SetParent(panelGO.transform, false);
            label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
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
