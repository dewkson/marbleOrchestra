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
        private CanvasScaler canvasScaler;
        private RectTransform panelRect;
        private RectTransform labelRect;
        private Image icon;
        private Sprite playSprite, pauseSprite;

        private const float IconSize = 90f;
        private const float IconMargin = 36f;

        private void Awake()
        {
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            EnsureEventSystem();
            BuildUI();
        }

        /// Play triangle / pause bars at the left of the button; hidden (and
        /// the label re-centered) when there is no icon.
        private void ShowIcon(Sprite sprite, Color color)
        {
            icon.gameObject.SetActive(sprite != null);
            icon.sprite = sprite;
            icon.color = color;
        }

        private static Sprite CreateIconSprite(bool play)
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                float a;
                if (play)
                {
                    // Triangle (0.22,0.1) (0.22,0.9) (0.85,0.5): approximate signed distance to its edges, soft 1px edge.
                    float d1 = u - 0.22f;
                    float d2 = (0.4f * (0.85f - u) / 0.63f - Mathf.Abs(v - 0.5f)) * 0.844f;
                    float d = Mathf.Min(d1, d2) * size;
                    a = Mathf.Clamp01(d + 0.5f);
                }
                else
                {
                    bool bar = (u > 0.2f && u < 0.4f || u > 0.6f && u < 0.8f) && v > 0.12f && v < 0.88f;
                    a = bar ? 1f : 0f;
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void UpdateLayout()
        {
            GameViewLayout.ApplyTo(canvasScaler); // Desktop / Mobile reference resolution (see 0060)
            if (GameViewLayout.IsMobile)
            {
                // Full width, between the 3D view above and the strip below.
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 1f);
                panelRect.anchoredPosition = new Vector2(0f, -(GameViewLayout.SideTopRef + GameViewLayout.SideMargin));
                panelRect.sizeDelta = new Vector2(GameViewLayout.ReferenceWidth - 2f * GameViewLayout.SideMargin, GameViewLayout.PlayButtonHeight);
            }
            else
            {
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 0f);
                panelRect.anchoredPosition = new Vector2(-GameViewLayout.SideMargin, GameViewLayout.SideMargin);
                panelRect.sizeDelta = new Vector2(GameViewLayout.ColumnWidthRef, GameViewLayout.PlayButtonHeight);
            }
        }

        private void Update()
        {
            UpdateLayout();

            if (marbleController.IsPlaying)
            {
                label.text = "Zurück zur Planung";
                label.color = playingColor;
                button.interactable = true;
                ShowIcon(pauseSprite, playingColor);
                return;
            }

            if (marbleController.CanPlay)
            {
                label.text = "Bahn testen";
                label.color = readyColor;
                button.interactable = true;
                ShowIcon(playSprite, readyColor);
            }
            else
            {
                label.text = "Verbinde Start und Ziel,\num die Bahn zu testen";
                label.color = notReadyColor;
                button.interactable = false;
                ShowIcon(null, notReadyColor);
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
            canvasScaler = scaler;
            GameViewLayout.ApplyTo(scaler);

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
            this.panelRect = panelRect;
            UpdateLayout();

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

            labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            playSprite = CreateIconSprite(true);
            pauseSprite = CreateIconSprite(false);
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(panelGO.transform, false);
            icon = iconGO.AddComponent<Image>();
            icon.raycastTarget = false;
            RectTransform iconRect = (RectTransform)iconGO.transform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(IconMargin, 0f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
        }
    }
}
