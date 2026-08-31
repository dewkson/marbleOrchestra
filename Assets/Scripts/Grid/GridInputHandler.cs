using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Two ways to swap two pipes: click pipe A, then click pipe B - or
    /// press pipe A, drag onto pipe B, release (see 0004). Purely
    /// translates input into PathGrid.SwapCards calls; no gameplay logic
    /// lives here. Disabled while MarbleController is simulating - the
    /// track can only be edited during planning. Lives on its own
    /// GameObject; grid and marbleController are wired in the Inspector or
    /// auto-found at Awake.
    /// A press only turns into a drag once the pointer has moved past
    /// dragThresholdPixels - short of that, release is treated as a plain
    /// click, so both flows share one press without either stealing the
    /// other's gesture. While dragging, the source pipe visually follows
    /// the cursor (raycast against the grid's own plane, so this works
    /// under any grid/camera orientation - see 0029) and the pipe
    /// currently hovered as a drop target gets the same SetSelected
    /// highlight the click-click flow already uses.
    /// Raycasts in full 3D (Physics.Raycast via ScreenPointToRay) rather
    /// than Physics2D, since the planning camera/grid no longer have to be
    /// front-on (see 0029's top-down planning view) - Physics2D would
    /// silently ignore any rotation beyond the Z axis.
    /// </summary>
    public class GridInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private PathGrid grid;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private float dragThresholdPixels = 8f;

        private PathPipe selectedPipe;

        private PathPipe pressedPipe;
        private Collider pressedCollider;
        private Vector2 pressScreenPos;
        private bool isDragging;
        private Vector3 dragOriginalLocalPosition;
        private PathPipe dragHoverTarget;

        private void Awake()
        {
            if (grid == null) grid = FindAnyObjectByType<PathGrid>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (marbleController != null && marbleController.IsPlaying) return;
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                BeginPress();
            }
            else if (pressedPipe != null && Mouse.current.leftButton.isPressed)
            {
                ContinuePress();
            }
            else if (pressedPipe != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                EndPress();
            }
        }

        private void BeginPress()
        {
            PathPipe clicked = RaycastPipe();
            if (clicked == null || clicked.IsLocked) return;

            pressedPipe = clicked;
            pressScreenPos = Mouse.current.position.ReadValue();
            isDragging = false;
        }

        private void ContinuePress()
        {
            Vector2 currentScreenPos = Mouse.current.position.ReadValue();

            if (!isDragging)
            {
                if ((currentScreenPos - pressScreenPos).sqrMagnitude < dragThresholdPixels * dragThresholdPixels) return;
                BeginDrag();
            }

            if (RaycastGridPlane(currentScreenPos, out Vector3 worldPos)) pressedPipe.transform.position = worldPos;

            UpdateHoverHighlight(RaycastPipe());
        }

        private void EndPress()
        {
            if (isDragging) EndDrag();
            else HandleClick(pressedPipe);

            pressedPipe = null;
            isDragging = false;
        }

        private void BeginDrag()
        {
            isDragging = true;
            dragOriginalLocalPosition = pressedPipe.transform.localPosition;
            pressedPipe.SetSelected(true);
            pressedPipe.SetDragElevated(true);

            // Disabled for the whole drag so it can't shadow itself once it
            // sits exactly on top of the cursor - RaycastPipe would
            // otherwise hit the dragged pipe's own (now co-located)
            // collider instead of whatever's underneath.
            pressedCollider = pressedPipe.GetComponent<Collider>();
            if (pressedCollider != null) pressedCollider.enabled = false;

            // A drag takes over from any pending click-click selection so
            // the two flows never fight over the same pipe.
            if (selectedPipe != null)
            {
                selectedPipe.SetSelected(false);
                selectedPipe = null;
            }
        }

        private void EndDrag()
        {
            ClearHoverHighlight();
            pressedPipe.SetSelected(false);
            pressedPipe.SetDragElevated(false);
            pressedPipe.transform.localPosition = dragOriginalLocalPosition;

            PathPipe target = RaycastPipe();

            if (pressedCollider != null) pressedCollider.enabled = true;
            pressedCollider = null;

            if (target != null && target != pressedPipe && !target.IsLocked)
                grid.SwapCards(pressedPipe.Coord, target.Coord);
        }

        private void HandleClick(PathPipe clicked)
        {
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

        private void UpdateHoverHighlight(PathPipe hovered)
        {
            PathPipe validHover = hovered != null && hovered != pressedPipe && !hovered.IsLocked ? hovered : null;
            if (validHover == dragHoverTarget) return;

            if (dragHoverTarget != null) dragHoverTarget.SetSelected(false);
            dragHoverTarget = validHover;
            if (dragHoverTarget != null) dragHoverTarget.SetSelected(true);
        }

        private void ClearHoverHighlight()
        {
            if (dragHoverTarget != null) dragHoverTarget.SetSelected(false);
            dragHoverTarget = null;
        }

        private PathPipe RaycastPipe()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(screenPos);

            return Physics.Raycast(ray, out RaycastHit hit) ? hit.collider.GetComponent<PathPipe>() : null;
        }

        /// Intersects the given screen position's camera ray with the
        /// grid's own plane (normal = grid.transform.forward, since
        /// PathGrid.CellToLocalPosition always returns local Z = 0) - so
        /// the dragged pipe can follow the cursor in world space
        /// regardless of whether the grid is set up front-on or top-down
        /// (see 0029).
        private bool RaycastGridPlane(Vector2 screenPos, out Vector3 worldPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(grid.transform.forward, grid.transform.position);

            if (plane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                return true;
            }

            worldPos = default;
            return false;
        }
    }
}
