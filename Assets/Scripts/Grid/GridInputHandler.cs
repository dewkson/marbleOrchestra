using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Click pipe A, then click pipe B, to swap them. Purely translates
    /// clicks into PathGrid.SwapCards calls; no gameplay logic lives here.
    /// Disabled while MarbleController is simulating - the track can only
    /// be edited during planning. Lives on its own GameObject; grid and
    /// marbleController are wired in the Inspector or auto-found at Awake.
    /// </summary>
    public class GridInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private PathGrid grid;
        [SerializeField] private MarbleController marbleController;

        private PathPipe selectedPipe;

        private void Awake()
        {
            if (grid == null) grid = FindAnyObjectByType<PathGrid>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (marbleController != null && marbleController.IsPlaying) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            PathPipe clicked = RaycastPipe();
            if (clicked == null || clicked.IsLocked) return;

            if (selectedPipe == null)
            {
                selectedPipe = clicked;
                selectedPipe.SetSelected(true);
                return;
            }

            if (selectedPipe == clicked)
            {
                selectedPipe.SetSelected(false);
                selectedPipe = null;
                return;
            }

            grid.SwapCards(selectedPipe.Coord, clicked.Coord);
            selectedPipe.SetSelected(false);
            selectedPipe = null;
        }

        private PathPipe RaycastPipe()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector2 worldPos = targetCamera.ScreenToWorldPoint(screenPos);

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            return hit.collider != null ? hit.collider.GetComponent<PathPipe>() : null;
        }
    }
}
