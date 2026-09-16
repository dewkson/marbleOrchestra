using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Screenspace button shown only while CameraModeTransition.IsFreeCamera
    /// is true (see 0044): lets the player hand control back to the guided
    /// isometric follow camera after panning/zooming away from it. Builds
    /// its own Canvas/Button at runtime, matching PlaybackHintUI's
    /// procedural-UI pattern - no scene/prefab setup needed.
    /// A clickable uGUI Button needs an EventSystem with an input module to
    /// receive pointer events; since this project's Active Input Handling
    /// is New Input System only (no legacy StandaloneInputModule support),
    /// this also creates a minimal EventSystem + InputSystemUIInputModule
    /// the first time, if the scene doesn't already have one.
    /// Lives on its own GameObject; cameraModeTransition and
    /// marbleController are wired in the Inspector or auto-found at Awake.
    /// </summary>
    public class FreeCameraReturnButtonUI : MonoBehaviour
    {
        [SerializeField] private CameraModeTransition cameraModeTransition;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private string label = "Geführte Kamera";

        private GameObject buttonGO;

        private void Awake()
        {
            if (cameraModeTransition == null) cameraModeTransition = FindAnyObjectByType<CameraModeTransition>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();

            EnsureEventSystem();
            BuildUI();
        }

        private void Update()
        {
            bool visible = cameraModeTransition != null && marbleController != null
                && marbleController.IsPlaying && cameraModeTransition.IsFreeCamera;

            if (buttonGO.activeSelf != visible) buttonGO.SetActive(visible);
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
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>();

            buttonGO = new GameObject("ReturnButton");
            buttonGO.transform.SetParent(canvasGO.transform, false);

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);

            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-30f, -30f);
            buttonRect.sizeDelta = new Vector2(260f, 56f);

            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(HandleReturnClicked);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);

            Text labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 22;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = label;
            labelText.raycastTarget = false;

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            buttonGO.SetActive(false);
        }

        private void HandleReturnClicked()
        {
            if (cameraModeTransition != null) cameraModeTransition.ReturnToGuidedCamera();
        }
    }
}
