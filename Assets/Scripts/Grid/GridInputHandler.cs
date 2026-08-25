using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Click pipe A, then click pipe B, to swap them. Purely translates
    /// clicks into PathGrid.SwapCards calls; no gameplay logic lives here.
    /// </summary>
    [RequireComponent(typeof(PathGrid))]
    public class GridInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private PathGrid grid;
        private PathPipe selectedPipe;

        private void Awake()
        {
            grid = GetComponent<PathGrid>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
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
