using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Shared screen layout: the Game view is split into two columns. The
    /// left three quarters show the MainCamera (2D planning / 3D play view); the
    /// right quarter only holds buttons and the minimap. All UI canvases use a
    /// 1920x1080 reference with match-width scaling, so the right column is
    /// exactly SideColumnRefWidth reference pixels wide.
    /// </summary>
    public static class GameViewLayout
    {
        public const float MainViewFraction = 3f / 4f;
        public const float ReferenceWidth = 1920f;
        public const float SideColumnRefWidth = ReferenceWidth * (1f - MainViewFraction); // 480
        public const float SideMargin = 40f; // reference pixels, inside the right column

        /// Screen pixels per canvas reference pixel (CanvasScaler match-width).
        public static float CanvasScale => Screen.width / ReferenceWidth;

        public static bool IsInMainView(Vector2 screenPos) => screenPos.x < Screen.width * MainViewFraction;

        /// Uniform button look: dark gray fill with a white border.
        public static void StyleAsButton(UnityEngine.UI.Image image)
        {
            image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            UnityEngine.UI.Outline outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(3f, 3f);
        }

        /// Restricts the camera to the left column and adds a plain
        /// background camera behind it, so the right column doesn't show
        /// uncleared frame-buffer garbage.
        public static void ApplyTo(Camera mainCamera)
        {
            mainCamera.rect = new Rect(0f, 0f, MainViewFraction, 1f);

            if (GameObject.Find("SideColumnBackdrop") != null) return;

            GameObject go = new GameObject("SideColumnBackdrop");
            Camera backdrop = go.AddComponent<Camera>();
            backdrop.clearFlags = CameraClearFlags.SolidColor;
            backdrop.backgroundColor = new Color(0.1f, 0.11f, 0.13f, 1f);
            backdrop.cullingMask = 0;
            backdrop.depth = mainCamera.depth - 1f;
            backdrop.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}
