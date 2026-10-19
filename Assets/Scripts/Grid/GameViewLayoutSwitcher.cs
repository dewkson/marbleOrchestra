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
        }

        private void HandleChanged()
        {
            GameViewLayout.ApplyTo(cam);
            // While playing, CameraModeTransition's follow camera keeps
            // re-framing every frame; only the planning view is static.
            bool playing = marbleController != null && marbleController.IsPlaying;
            if (!playing && fitter != null) fitter.Fit();
        }
    }
}
