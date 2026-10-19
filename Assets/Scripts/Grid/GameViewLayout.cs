using System;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    public enum ViewLayoutMode
    {
        /// Landscape: MainCamera left, side column (buttons + minimap) right.
        Desktop,
        /// Portrait: MainCamera top, side strip (buttons + minimap) bottom.
        Mobile,
    }

    /// <summary>
    /// Shared screen layout (see 0060). The Game view is split in two:
    /// the MainCamera (2D planning / 3D play view) and a "side region" that
    /// only holds buttons and the minimap. Desktop puts the side region in
    /// a right column (a quarter of the width), Mobile in a bottom strip
    /// (40% of the height). All UI canvases scale with match-width against
    /// <see cref="ReferenceWidth"/> x <see cref="ReferenceHeight"/>, so the
    /// numbers below are canvas reference pixels, not real pixels.
    /// The mode is persisted and can be switched at runtime (see
    /// GameViewLayoutSwitcher); listeners re-layout on <see cref="Changed"/>.
    /// </summary>
    public static class GameViewLayout
    {
        private const string PrefKey = "GameViewLayoutMode";

        public const float DesktopMainViewFraction = 3f / 4f;  // of the width
        public const float MobileMainViewFraction = 0.6f;      // of the height
        public const float SideMargin = 40f; // reference pixels, inside the side region

        private static ViewLayoutMode? mode;

        public static event Action Changed;

        public static ViewLayoutMode Mode
        {
            get
            {
                if (mode == null) mode = (ViewLayoutMode)PlayerPrefs.GetInt(PrefKey, (int)ViewLayoutMode.Desktop);
                return mode.Value;
            }
            set
            {
                if (mode == value) return;
                mode = value;
                PlayerPrefs.SetInt(PrefKey, (int)value);
                Changed?.Invoke();
            }
        }

        public static void Toggle() => Mode = Mode == ViewLayoutMode.Desktop ? ViewLayoutMode.Mobile : ViewLayoutMode.Desktop;

        public static bool IsMobile => Mode == ViewLayoutMode.Mobile;

        public static float ReferenceWidth => IsMobile ? 1080f : 1920f;
        public static float ReferenceHeight => IsMobile ? 1920f : 1080f;

        /// Width of one button / minimap column inside the side region.
        public static float ColumnWidthRef => IsMobile
            ? ReferenceWidth / 2f - 1.5f * SideMargin   // two columns side by side
            : ReferenceWidth * (1f - DesktopMainViewFraction) - 2f * SideMargin;

        /// Distance (reference pixels) from the top of the screen to the top
        /// of the side region: 0 on Desktop, below the main view on Mobile.
        public static float SideTopRef => IsMobile ? MobileMainViewFraction * Screen.height / CanvasScale : 0f;

        /// Height (reference pixels) of the side region, inside its margins.
        public static float SideInnerHeightRef => IsMobile
            ? Screen.height / CanvasScale - SideTopRef - 2f * SideMargin
            : float.MaxValue;

        /// Screen pixels per canvas reference pixel (CanvasScaler match-width).
        public static float CanvasScale => Screen.width / ReferenceWidth;

        public static bool IsInMainView(Vector2 screenPos) => IsMobile
            ? screenPos.y > Screen.height * (1f - MobileMainViewFraction)
            : screenPos.x < Screen.width * DesktopMainViewFraction;

        /// Gives a UI canvas the reference resolution of the current mode.
        public static void ApplyTo(UnityEngine.UI.CanvasScaler scaler)
        {
            Vector2 reference = new Vector2(ReferenceWidth, ReferenceHeight);
            if (scaler.referenceResolution != reference) scaler.referenceResolution = reference;
        }

        /// Uniform button look: dark gray fill with a white border.
        public static void StyleAsButton(UnityEngine.UI.Image image)
        {
            image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            UnityEngine.UI.Outline outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(3f, 3f);
        }

        /// Restricts the camera to the main view and adds a plain
        /// background camera behind it, so the side region doesn't show
        /// uncleared frame-buffer garbage.
        public static void ApplyTo(Camera mainCamera)
        {
            mainCamera.rect = IsMobile
                ? new Rect(0f, 1f - MobileMainViewFraction, 1f, MobileMainViewFraction)
                : new Rect(0f, 0f, DesktopMainViewFraction, 1f);

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
