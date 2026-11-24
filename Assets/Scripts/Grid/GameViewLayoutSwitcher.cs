using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Switches between the Desktop and Mobile screen layout (see 0060) with
    /// the L key, and re-applies the main camera rect and framing whenever
    /// the layout changes. Added by CameraModeTransition at Awake.
    /// </summary>
    public class GameViewLayoutSwitcher : MonoBehaviour
    {
        [SerializeField] private Key toggleKey = Key.L;

        private Camera cam;
        private CameraFitter fitter;
        private MarbleController marbleController;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            fitter = GetComponent<CameraFitter>();
            marbleController = FindAnyObjectByType<MarbleController>();
        }

        private void OnEnable() => GameViewLayout.Changed += HandleChanged;
        private void OnDisable() => GameViewLayout.Changed -= HandleChanged;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                GameViewLayout.Toggle();

            // WebGL on phones: the canvas only reaches its final size after
            // the start tap (fullscreen, address bar gone) or on rotation -
            // long after CameraFitter's one-shot Fit at Start. Re-frame
            // whenever the screen size changes.
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                bool first = lastScreenWidth < 0;
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                if (!first) HandleChanged();
            }
        }

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private CameraModeTransition modeTransition;
        private SubLevelSwitchAnimator subLevelAnimator;

        private void HandleChanged()
        {
            GameViewLayout.ApplyTo(cam);
            // While playing, CameraModeTransition's follow camera keeps
            // re-framing every frame; only the planning view is static.
            // Running camera animations end on a fresh Fit anyway, so they
            // are left alone instead of being snapped mid-way.
            bool playing = marbleController != null && marbleController.IsPlaying;
            if (modeTransition == null) modeTransition = GetComponent<CameraModeTransition>();
            if (subLevelAnimator == null) subLevelAnimator = FindAnyObjectByType<SubLevelSwitchAnimator>();
            bool animating = (modeTransition != null && modeTransition.IsTransitioning)
                || (subLevelAnimator != null && subLevelAnimator.IsAnimating);
            if (!playing && !animating && fitter != null) fitter.Fit();
        }
    }
}
